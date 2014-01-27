using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using OpenCvSharp;

namespace FLib
{
    public class TrappedBallSegmentation : IDisposable
    {
        // 入出力
        const int edgeThreshold = 10;
        public Bitmap edgeImage;
        public Bitmap brushedImage;
        public Bitmap orgImage;
        float sqColorDistThreshold = 10;


        public TrappedBallSegmentation(Bitmap orgImage, Bitmap edgeImage)
        {
            this.orgImage = new Bitmap(orgImage);
            this.edgeImage = new Bitmap(edgeImage);
            this.brushedImage = new Bitmap(orgImage.Width, orgImage.Height, PixelFormat.Format32bppArgb);
        }

        public void Dispose()
        {
            if (edgeImage != null) orgImage.Dispose();
            if (edgeImage != null) edgeImage.Dispose();
            if (brushedImage != null) brushedImage.Dispose();
        }

        // 各ピクセルを左上端としてエッジ画像に充填できる正方形の大きさ
        // DPで求める
        unsafe int[] CalcBallSize(Bitmap mono, out int maxSize, out int minSize)
        {
            // 各ピクセルを左上端としてエッジ画像に充填できる正方形の大きさ
            // DPで求める
            maxSize = 0;
            minSize = int.MaxValue;
            int[] ballSize = new int[mono.Width * mono.Height];
            using (BitmapIterator iter = new BitmapIterator(mono, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed))
            {
                byte* data = (byte*)iter.PixelData;
                // 右辺
                for (int y = mono.Height - 1; y >= 0; y--)
                {
                    int pixelIdx = mono.Width - 1 + y * iter.Stride;
                    byte val = data[pixelIdx];
                    if (val <= edgeThreshold)
                    {
                        int ballIdx = mono.Width - 1 + y * mono.Width;
                        ballSize[ballIdx] = 1;
                    }
                }
                // 下辺
                for (int x = mono.Width - 1; x >= 0; x--)
                {
                    int pixelIdx = x + (mono.Height - 1) * iter.Stride;
                    byte val = data[pixelIdx];
                    if (val <= edgeThreshold)
                    {
                        int ballIdx = x + (mono.Height - 1) * mono.Width;
                        ballSize[ballIdx] = 1;
                    }
                }
                // それ以外
                for (int y = mono.Height - 2; y >= 0; y--)
                {
                    for (int x = mono.Width - 2; x >= 0; x--)
                    {
                        int idx = x + y * iter.Stride;
                        byte val = data[idx];
                        if (val <= 10)
                        {
                            int ballIdx = x + y * mono.Width;
                            int ballIdxR = (x + 1) + y * mono.Width;
                            int ballIdxB = x + (y + 1) * mono.Width;
                            int ballIdxD = (x + 1) + (y + 1) * mono.Width;
                            ballSize[ballIdx] = 1 + Math.Min(ballSize[ballIdxR], Math.Min(ballSize[ballIdxB], ballSize[ballIdxD]));
                            maxSize = Math.Max(maxSize, ballSize[ballIdx]);
                            minSize = Math.Min(minSize, ballSize[ballIdx]);
                        }
                    }
                }
            }

            return ballSize;
        }

        unsafe void FlashArrayToBitmap(int[] array, int w, int h, Bitmap bmp)
        {
            using (var iter = new BitmapIterator(bmp, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb))
            {
                byte* data = (byte*)iter.PixelData;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int idx = 4 * x + y * iter.Stride;
                        int arIdx = x + y * w;
                        data[idx + 0] = (byte)array[arIdx];
                        data[idx + 1] = (byte)array[arIdx];
                        data[idx + 2] = (byte)array[arIdx];
                        data[idx + 3] = (byte)255;
                    }
                }
            }
        }

        unsafe List<Bitmap> MoveBall(HashSet<Point> uncoloredPixels, int[] labelMap, int radius, BitmapIterator edgeIter, BitmapIterator orgIter, int[] ballSizeList)
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();


            List<Bitmap> segments = new List<Bitmap>();
            byte* edgeData = (byte*)edgeIter.PixelData;
            int ballSize = 1 + 2 * radius;

            Bitmap segmentImage = null;

            bool segmentFound = true;

            while (segmentFound)
            {
                segmentFound = false;
                foreach (Point pt in uncoloredPixels)
                {
                    int x = pt.X;
                    int y = pt.Y;
                    int idx = x + y * edgeImage.Width;
                    // ボールが入るか
                    if (ballSizeList[idx] < ballSize) continue;

                    // 彩色済みか
                    bool colored = false;
                    for (int yy = y; yy < y + ballSize; yy++)
                        for (int xx = x; xx < x + ballSize; xx++)
                            if (!uncoloredPixels.Contains(new Point(xx, yy)))
                            {
                                colored = true;
                                goto COLORED_DECIDED;
                            }

                COLORED_DECIDED:

                    if (colored) continue;



                Console.WriteLine("color decision: " + sw.ElapsedMilliseconds + " ms");
                sw.Restart();



                    segmentImage = new Bitmap(edgeImage.Width, edgeImage.Height, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(segmentImage))
                    {
                        g.Clear(Color.Transparent);
                    }

                    int roiMinX = int.MaxValue;
                    int roiMinY = int.MaxValue;
                    int roiMaxX = int.MinValue;
                    int roiMaxY = int.MinValue;

                    using (BitmapIterator segmentIter = new BitmapIterator(segmentImage, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb))
                    {
                        byte* segmentData = (byte*)segmentIter.PixelData;
                        Random rand = new Random();
                        Color c = Color.FromArgb(rand.Next(255) + 1, rand.Next(255) + 1, rand.Next(255) + 1);

                        for (int yy = y; yy < y + ballSize - 1; yy++)
                            for (int xx = x; xx < x + ballSize - 1; xx++)
                            {
                                segmentData[4 * xx + yy * segmentIter.Stride] = c.B;
                                segmentData[4 * xx + yy * segmentIter.Stride + 1] = c.G;
                                segmentData[4 * xx + yy * segmentIter.Stride + 2] = c.R;
                                segmentData[4 * xx + yy * segmentIter.Stride + 3] = 255;
                            }

                        Console.WriteLine("init fill: " + sw.ElapsedMilliseconds + " ms");
                        sw.Restart();

                        
                        // 新しい色でFlood fill
                        HashSet<Point> initPoints = new HashSet<Point>();
                        for (int yy = y; yy < y + radius * 2; yy++) initPoints.Add(new Point(x, yy));
                        for (int yy = y; yy < y + radius * 2; yy++) initPoints.Add(new Point(x + ballSize - 1, yy));
                        for (int xx = x; xx < x + radius * 2; xx++) initPoints.Add(new Point(xx, y));
                        for (int xx = x; xx < x + radius * 2; xx++) initPoints.Add(new Point(xx, y + ballSize - 1));

                        FloodFill(initPoints, segmentIter, c, uncoloredPixels, edgeIter, orgIter);

                        Console.WriteLine("flood fill: " + sw.ElapsedMilliseconds + " ms");
                        sw.Restart();

                        for (int yy = 0; yy < edgeImage.Height; yy++)
                            for (int xx = 0; xx < edgeImage.Width; xx++)
                            {
                                if (segmentData[4 * xx + yy * segmentIter.Stride + 3] != 0)
                                {
                                    roiMinX = Math.Min(roiMinX, xx);
                                    roiMaxX = Math.Max(roiMaxX, xx);
                                    roiMinY = Math.Min(roiMinY, yy);
                                    roiMaxY = Math.Max(roiMaxY, yy);
                                }
                            }

                        Console.WriteLine("get ROI: " + sw.ElapsedMilliseconds + " ms");
                        sw.Restart();
                    }

                    using (IplImage iplPart = BitmapConverter.ToIplImage(segmentImage))
                    {
                        Cv.SetImageROI(iplPart, new CvRect(roiMinX - 1, roiMinY - 1, roiMaxX - roiMinX + 2, roiMaxY - roiMinY + 2));
                        IplConvKernel kernel = new IplConvKernel(2 * radius + 1, 2 * radius + 1, radius, radius, ElementShape.Ellipse);
                        Cv.Erode(iplPart, iplPart, kernel, 1);
                        Cv.Dilate(iplPart, iplPart, kernel, 1);
                        Cv.ResetImageROI(iplPart);
                        segmentImage.Dispose();
                        segmentImage = BitmapConverter.ToBitmap(iplPart);
                        segments.Add(segmentImage);

                        segmentImage.Save(radius + "_" + segments.Count + ".png");
                    }

                    Console.WriteLine("erode: " + sw.ElapsedMilliseconds + " ms");
                    sw.Restart();

                    segmentFound = true;
                    break;
                }

                if (segmentFound)
                {
                    using (BitmapIterator segmentIter = new BitmapIterator(segmentImage, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb))
                    {
                        Point[] coloredPixels = uncoloredPixels.Where(p => ((byte*)segmentIter.PixelData)[4 * p.X + p.Y * segmentIter.Stride + 3] != 0).ToArray();
                        foreach (Point pt in coloredPixels)
                        {
                            labelMap[pt.X + pt.Y * edgeImage.Width] = segmentCnt;
                            uncoloredPixels.Remove(pt);
                        }
                        segmentCnt++;
                    }
                }
            }

            return segments;
        }

        int segmentCnt = 1;

        unsafe public void Execute()
        {
            const int deltaRadius = 3;
            segmentCnt = 1;

            // 充填できるボールサイズを事前計算
            int maxBallSize, minBallSize;
            int[] ballSizeList = CalcBallSize(edgeImage, out maxBallSize, out minBallSize);
            int maxRadius = ((maxBallSize / 2) / deltaRadius) * deltaRadius;
            int minRadius = ((minBallSize / 2) / deltaRadius + 1) * deltaRadius;
            FlashArrayToBitmap(ballSizeList, edgeImage.Width, edgeImage.Height, brushedImage);

            Dictionary<Bitmap, int> segmentList = new Dictionary<Bitmap, int>();

            HashSet<Point> uncheckPixels = new HashSet<Point>();
            int[] labelMap = new int[edgeImage.Width * edgeImage.Height];

            for (int y = 0; y < edgeImage.Height; y++)
                for (int x = 0; x < edgeImage.Width; x++)
                    uncheckPixels.Add(new Point(x, y));

            // エッジの間に大雑把に色を塗っていく
            using (BitmapIterator edgeIter = new BitmapIterator(edgeImage, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed))
            using (BitmapIterator orgIter = new BitmapIterator(orgImage, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
            {
                byte* edgeData = (byte*)edgeIter.PixelData;
                // ボールを徐々に小さくしながらTrappedBallSegmentation
                for (int radius = maxRadius; radius >= minRadius; radius -= deltaRadius)
                {
                    List<Bitmap> segments = MoveBall(uncheckPixels, labelMap, radius, edgeIter, orgIter, ballSizeList);
                    for (int i = 0; i < segments.Count; i++)
                    {
                        segmentList[segments[i]] = radius;
                    }
                }

                Console.WriteLine("move ball: done");

                using (Graphics g = Graphics.FromImage(brushedImage))
                {
                    foreach (var kv in segmentList)
                    {
                        Bitmap bmp = kv.Key;
                        int radius = kv.Value;
                        g.DrawImage(bmp, Point.Empty);
                    }
                }

                Point[] deltas = new Point[] { new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1) };

                byte* orgData = (byte*)orgIter.PixelData;

                float threshold = sqColorDistThreshold;

                while (uncheckPixels.Count >= 1 && threshold <= 1000000)
                {
                    // ラベルが塗られていない領域と接するピクセル集合
                    PriorityQueue<ContourPixel> contours = new PriorityQueue<ContourPixel>();
                    HashSet<Point> tmp_contours = new HashSet<Point>();
                    foreach (Point pt in uncheckPixels)
                    {
                        if (!uncheckPixels.Contains(new Point(pt.X - 1, pt.Y))) tmp_contours.Add(new Point(pt.X - 1, pt.Y));
                        if (!uncheckPixels.Contains(new Point(pt.X, pt.Y - 1))) tmp_contours.Add(new Point(pt.X, pt.Y - 1));
                        if (!uncheckPixels.Contains(new Point(pt.X + 1, pt.Y))) tmp_contours.Add(new Point(pt.X + 1, pt.Y));
                        if (!uncheckPixels.Contains(new Point(pt.X, pt.Y + 1))) tmp_contours.Add(new Point(pt.X, pt.Y + 1));
                    }
                    foreach (Point pt in tmp_contours)
                    {
                        if (pt.X < 0 || edgeImage.Width <= pt.X || pt.Y < 0 || edgeImage.Height <= pt.Y) continue;
                        contours.Push(new ContourPixel(pt, 0));
                    }

                    Console.WriteLine("init contour: done");

                    // エッジは削除
                    uncheckPixels.RemoveWhere(pt => edgeData[pt.X + pt.Y * edgeIter.Stride] > edgeThreshold);

                    while (contours.Count >= 1)
                    {
                        ContourPixel cpt = contours.Top;
                        contours.Pop();
                        if (cpt.error >= sqColorDistThreshold)
                        {
                            break;
                        }
                        Point pt = cpt.Point;
                        foreach (Point delta in deltas)
                        {
                            Point neighbor = new Point(pt.X + delta.X, pt.Y + delta.Y);
                            if (uncheckPixels.Contains(neighbor))
                            {
                                if (SqColorDist(orgIter, pt.X, pt.Y, neighbor.X, neighbor.Y) <= threshold)
                                {
                                    contours.Push(new ContourPixel(neighbor, 0));
                                    labelMap[neighbor.X + neighbor.Y * edgeImage.Width] = labelMap[pt.X + pt.Y * edgeImage.Width];
                                    // brushedImage.SetPixel(neighbor.X, neighbor.Y, brushedImage.GetPixel(pt.X, pt.Y));
                                    uncheckPixels.Remove(neighbor);
                                }
                            }
                        }
                    }

                    Console.WriteLine("region growing: done (" + uncheckPixels.Count + ", " + threshold + ")");

                    threshold *= 2;
                }

                Random rand = new Random();
                Dictionary<int, Color> segmentColor = new Dictionary<int, Color>();
                for (int i = 0; i < segmentCnt; i++)
                {
                    segmentColor[i] = Color.FromArgb(rand.Next(255), rand.Next(255), rand.Next(255));
                }

                Dictionary<int, int> segmentCorrespondence = new Dictionary<int, int>();
                for (int y = 1; y < edgeImage.Height - 1; y++)
                    for (int x = 1; x < edgeImage.Width - 1; x++)
                    {
                        foreach (Point delta in deltas)
                        {
                            Point neighbor = new Point(x + delta.X, y + delta.Y);

                            int label0 = labelMap[x + y * edgeImage.Width];
                            int label1 = labelMap[neighbor.X + neighbor.Y * edgeImage.Width];
                            if (label0 == 0 || label1 == 0 || label0 == label1) continue;

                            while (segmentCorrespondence.ContainsKey(label0)) label0 = segmentCorrespondence[label0];
                            while (segmentCorrespondence.ContainsKey(label1)) label1 = segmentCorrespondence[label1];
                            if (label0 == label1) continue;

                            // 色が同じ領域が隣り合っていたら統合
                            if (SqColorDist(orgIter, x, y, neighbor.X, neighbor.Y) <= sqColorDistThreshold)
                            {
                                segmentCorrespondence[Math.Max(label0, label1)] = Math.Min(label0, label1);
                            }
                        }
                    }

                using (BitmapIterator brushIter = new BitmapIterator(brushedImage, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb))
                {
                    byte* brushData = (byte*)brushIter.PixelData;
                    for (int y = 0; y < edgeImage.Height; y++)
                        for (int x = 0; x < edgeImage.Width; x++)
                        {
                            int idx = x + y * edgeImage.Width;
                            int label = labelMap[idx];
                            while (segmentCorrespondence.ContainsKey(label)) label = segmentCorrespondence[label];
                            brushData[4 * x + y * brushIter.Stride + 0] = segmentColor[label].R;
                            brushData[4 * x + y * brushIter.Stride + 1] = segmentColor[label].G;
                            brushData[4 * x + y * brushIter.Stride + 2] = segmentColor[label].B;
                            brushData[4 * x + y * brushIter.Stride + 3] = 255;
                        }
                }
            }
        }

        public class ContourPixel : IComparable<ContourPixel>
        {
            public float error = 0;
            public Point Point = Point.Empty;
            public ContourPixel(Point pt, float error)
            {
                this.error = error;
                this.Point = pt;
            }
            public int CompareTo(ContourPixel obj)
            {
                if (error == obj.error) return 0;
                return error < obj.error ? -1 : 1;
            }
        }
   
        unsafe float SqColorDist(BitmapIterator iter, int x0, int y0, int x1, int y1)
        {
            byte* data = (byte*)iter.PixelData;
            int offset0 = 4* x0 + y0 * iter.Stride;
            int offset1 = 4 * x1 + y1 * iter.Stride;
            byte r0 = data[offset0 + 2];
            byte g0 = data[offset0 + 1];
            byte b0 = data[offset0 + 0];
            byte r1 = data[offset1 + 2];
            byte g1 = data[offset1 + 1];
            byte b1 = data[offset1 + 0];
            float dr = r1 - r0;
            float dg = g1 - g0;
            float db = b1 - b0;
            float sqDist = dr * dr + dg * dg + db * db;
            return sqDist;
        }

        unsafe bool CanFill(Point pt, Point src, HashSet<Point> curs, HashSet<Point> nexts, HashSet<Point> uncoloredPixels, BitmapIterator segmentIter, BitmapIterator edgeIter, BitmapIterator orgIter)
        {
            if (!uncoloredPixels.Contains(pt)) return false;
            if (curs.Contains(pt)) return false;
            if (nexts.Contains(pt)) return false;
            if (((byte*)segmentIter.PixelData)[4 * pt.X + pt.Y * segmentIter.Stride + 3] != 0) return false;
            if (((byte*)edgeIter.PixelData)[pt.X + pt.Y * edgeIter.Stride] >= edgeThreshold) return false;
            if (SqColorDist(orgIter, src.X, src.Y, pt.X, pt.Y) >= sqColorDistThreshold) return false;
            return true;
        }

        unsafe public void FloodFill(HashSet<Point> initPoints, BitmapIterator segmentIter, Color c, HashSet<Point> uncoloredPixels, BitmapIterator edgeIter, BitmapIterator orgIter)
        {
            if (initPoints.Count <= 0) return;
            HashSet<Point> curs = new HashSet<Point>(initPoints);
            HashSet<Point> nexts = new HashSet<Point>();
            byte* edgeData = (byte*)edgeIter.PixelData;
            byte* partData = (byte*)segmentIter.PixelData;
            int w = edgeIter.Bmp.Width;



            while (curs.Count > 0)
            {
                foreach (Point src in curs)
                {
                    int checkIdx = src.X + src.Y * segmentIter.Bmp.Width;

                    partData[4 * src.X + src.Y * segmentIter.Stride + 0] = c.B;
                    partData[4 * src.X + src.Y * segmentIter.Stride + 1] = c.G;
                    partData[4 * src.X + src.Y * segmentIter.Stride + 2] = c.R;
                    partData[4 * src.X + src.Y * segmentIter.Stride + 3] = 255;

                    Stopwatch sw = Stopwatch.StartNew();

                    List<int> nums = new List<int>();

                    for (int i = 0; i < 78778; i++)
                    {
                        Point[] pts = new[] {
                        new Point(src.X + 1, src.Y),
                        new Point(src.X, src.Y + 1),
                        new Point(src.X - 1, src.Y),
                        new Point(src.X, src.Y - 1)
                    };
                        foreach (var pt in pts)
                        {
//                            if (CanFill(pt, src, curs, nexts, uncoloredPixels, segmentIter, edgeIter, orgIter))
                            {
                                nexts.Add(pt);
                            }
                        }
                    }

                    Console.Write("sw = " + sw.ElapsedMilliseconds);

                    break;
/*
                    foreach (var pt in pts)
                    {
                        if (CanFill(pt, src, curs, nexts, uncoloredPixels, segmentIter, edgeIter, orgIter))
                        {
                            nexts.Add(pt);
                        }
                    }
  */              }
                curs.Clear();
                FMath.Swap(ref curs, ref nexts);
            }
        }

    }
}
