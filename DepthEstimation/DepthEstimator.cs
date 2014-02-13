using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using FLib;
using OpenCvSharp;

namespace DepthEstimation
{
    unsafe class DepthEstimator
    {
        public Bitmap Input;
        public Bitmap Lab;
        public Bitmap Patches;
        public Bitmap AggregatedPatches;
        public Bitmap Edge;
        public Bitmap Region;

        public DepthEstimator(Bitmap input)
        {
            this.Input = input;
        }

        #region エッジ抽出

        unsafe public void Edging()
        {
            if (Input == null) return;
            if (Edge != null)
            {
                Edge.Dispose();
                Edge = null;
            }

            using (IplImage rgb = BitmapConverter.ToIplImage(Input))
            {
                using (IplImage lab = new IplImage(rgb.Width, rgb.Height, rgb.Depth, 3))
                using (IplImage luminous = new IplImage(lab.Width, lab.Height, lab.Depth, 1))
                using (IplImage smooth = new IplImage(lab.Width, lab.Height, lab.Depth, 1))
                using (IplImage edge = new IplImage(lab.Width, lab.Height, lab.Depth, 1))
                {
                    Cv.CvtColor(rgb, lab, ColorConversion.RgbToLab);
                    Cv.CvtColor(rgb, luminous, ColorConversion.RgbToGray);
                    Cv.Smooth(luminous, smooth, SmoothType.Median, 9);
                    Cv.AbsDiff(luminous, smooth, edge);
                    Lab = BitmapConverter.ToBitmap(lab);
                    Edge = BitmapConverter.ToBitmap(edge);
                }

            }
        }


        #endregion

        #region 領域抽出

        public void Regions()
        {
            if (Input == null) return;

            Dictionary<int, Color> randColors = new Dictionary<int, Color>();

            // パッチ分割
            Patch[] patches = GetPatches();
            DrawPatches(patches, randColors);

            // 明らかに同じ領域のパッチを統合
            AggregatePatches(patches);
            DrawAggregatedPatches(patches, randColors);


            // LazyBrushで残りの領域を塗りつぶす
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            using (TrappedBallSegmentation tb = new TrappedBallSegmentation(Lab, Edge))
            {
                tb.Execute();
                Region = new Bitmap(tb.brushedImage);
            }
            Console.WriteLine("TrappedBallSegmentation: " + sw.ElapsedMilliseconds + " ms");
        }

        unsafe Patch[] GetPatches()
        {
            // パッチを求める
            using (BitmapIterator labIter = new BitmapIterator(Lab, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb))
            {
                using (BitmapIterator edgeLck = new BitmapIterator(Edge, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed))
                {
                    int row = Lab.Width / Patch.Size;
                    int col = Lab.Height / Patch.Size;
                    Patch[] patches = new Patch[row * col];
                    for (int py = 0; py < col; py++)
                    {
                        for (int px = 0; px < row; px++)
                        {
                            int offsetX = px * Patch.Size;
                            int offsetY = py * Patch.Size;
                            byte maxLightness = MaxLightness(edgeLck, offsetX, offsetY, Patch.Size, Patch.Size);
                            // エッジが含まれてないパッチはnullでない
                            patches[px + py * row] = maxLightness > 10 ? null : new Patch(labIter, px, py, px + py * row);
                        }
                    }
                    return patches;
                }
            }
        }

        void AggregatePatches(Patch[] patches)
        {
            int row = Lab.Width / Patch.Size;
            int col = Lab.Height / Patch.Size;
            HashSet<Patch> uncheck = new HashSet<Patch>(patches.Where(p => p != null).ToArray());
            while (uncheck.Count > 0) FloodFill(uncheck, patches, row, col);
        }
        void FloodFill(HashSet<Patch> uncolored, Patch[] patches, int row, int col)
        {
            int id = uncolored.First().id;
            HashSet<Patch> curs = new HashSet<Patch>() { uncolored.First() };
            HashSet<Patch> nexts = new HashSet<Patch>();
            HashSet<Patch> filled = new HashSet<Patch>();
            while (curs.Count > 0)
            {
                foreach (Patch src in curs)
                {
                    src.id = id;
                    filled.Add(src);
                    uncolored.Remove(src);
                    for (int pdy = -1; pdy <= 1; pdy++)
                    {
                        for (int pdx = -1; pdx <= 1; pdx++)
                        {
                            if (pdx == 0 && pdy == 0) continue;
                            // TODO?
                            if (pdx * pdy != 0) continue; 
                            
                            int px = src.px + pdx;
                            int py = src.py + pdy;
                            if (px < 0 || row <= px) continue;
                            if (py < 0 || col <= py) continue;
                            Patch dst = patches[px + py * row];
                            if (dst == null) continue;
                            if (uncolored.Contains(dst))
                            {
                                float dist = FMath.MahalanobisDistance(src.pixels, dst.pixels);
                              //  Console.WriteLine(dist);
                                if (dist < 20)
                                {
                                    nexts.Add(dst);
                                }
                            }
                        }
                    }
                }
                curs.Clear();
                FMath.Swap(ref curs, ref nexts);
            }

            if (filled.Count <= 6)
                foreach (var p in filled)
                    p.id = -1;
        }

        byte MaxLightness(BitmapIterator iter, int offsetX, int offsetY, int dx, int dy)
        {
            byte* data = (byte*)iter.PixelData;
            byte max = 0;
            for (int y = offsetY; y < offsetY + dy; y++)
                for (int x = offsetX; x < offsetX + dx; x++)
                    if (max < data[y * iter.Stride + x])
                        max = data[y * iter.Stride + x];
            return max;
        }

        #endregion

        public void TJunctions()
        {

        }

        public void OrderGraph()
        {

        }

        public void SolveOrderGraph()
        {

        }

        #region 途中結果の出力
   
        void DrawPatches(Patch[] patches, Dictionary<int, Color> randColors)
        {
            int row = Lab.Width / Patch.Size;
            int col = Lab.Height / Patch.Size;
            // パッチを描画
            if (Patches != null) Patches.Dispose();
            Patches = new Bitmap(Input);
            Random rand = new Random();
            using (var g = Graphics.FromImage(Patches))
            {
                g.Clear(Color.Transparent);
                for (int py = 0; py < col; py++)
                {
                    for (int px = 0; px < row; px++)
                    {
                        if (patches[px + py * row] != null)
                        {
                            Color c = Color.FromArgb(255, rand.Next(255), rand.Next(255), rand.Next(255));
                            randColors[px + py * row] = c;
                            g.FillRectangle(new SolidBrush(c), new Rectangle(px * Patch.Size, py * Patch.Size, Patch.Size, Patch.Size));
                        }
                    }
                }
            }
        }

        void DrawAggregatedPatches(Patch[] patches, Dictionary<int, Color> randColors)
        {
            Dictionary<int, Color> patchColors = new Dictionary<int, Color>();
            int row = Lab.Width / Patch.Size;
            int col = Lab.Height / Patch.Size;

            // パッチを描画
            if (AggregatedPatches != null) AggregatedPatches.Dispose();
            AggregatedPatches = new Bitmap(Input);
            using (var g = Graphics.FromImage(AggregatedPatches))
            {
                g.Clear(Color.Transparent);
                for (int py = 0; py < col; py++)
                {
                    for (int px = 0; px < row; px++)
                    {
                        if (patches[px + py * row] != null)
                        {
                            int id = patches[px + py * row].id;
                            Color color = Color.Transparent;
                            if (id >= 0)
                            {
                                if (patchColors.ContainsKey(id))
                                    color = patchColors[id];
                                else
                                    color = patchColors[id] = randColors[px + py * row];// patches[px + py * row].AverageColor();
                            }
                            g.FillRectangle(
                                new SolidBrush(color),
                                new Rectangle(px * Patch.Size, py * Patch.Size, Patch.Size, Patch.Size));
                        }
                    }
                }
            }
        }

        #endregion

        #region 保存
 
        public void Save(string dir)
        {
            foreach (var path in System.IO.Directory.GetFiles(dir)) System.IO.File.Delete(path);
            Input.Save(dir + "Input.png");
            Lab.Save(dir + "Lab.png");
            Patches.Save(dir + "Patches.png");
            AggregatedPatches.Save(dir + "AggregatedPatches.png");
            Edge.Save(dir + "Edge.png");
            Region.Save(dir + "Region.png");
        }

        #endregion
    }

    unsafe class Patch
    {
        public int id;
        public int px, py;
        public const int Size = 3;
        public Color[] pixels = new Color[Size * Size];
        public Patch(BitmapIterator labIter, int px, int py, int id)
        {
            byte* labData = (byte*)labIter.PixelData;
            int offsetX = px * Size;
            int offsetY = py * Size;
            for (int dy = 0; dy < Size; dy++)
            {
                for (int dx = 0; dx < Size; dx++)
                {
                    int index = 3 * (offsetX + dx) + (offsetY + dy) * labIter.Stride;
                    byte l = labData[index + 2];
                    byte a = labData[index + 1];
                    byte b = labData[index + 0];
                    pixels[dx + dy * Size] = Color.FromArgb(l, a, b);
                }
            }
            this.id = id;
            this.px = px;
            this.py = py;
        }

        public Color AverageColor()
        {
            int l = 0;
            int a = 0;
            int b = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                l += pixels[i].R;
                a += pixels[i].G;
                b += pixels[i].B;
            }
            l /= pixels.Length;
            a /= pixels.Length;
            b /= pixels.Length;
            return Color.FromArgb(l, a, b);
        }
    }
}
