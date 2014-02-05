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

        unsafe List<Bitmap> MoveBall(HashSet<int> orgUncoloredPixels, int[] labelMap, int radius, BitmapIterator edgeIter, BitmapIterator orgIter, int[] ballSizeList)
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            System.Diagnostics.Stopwatch sw2 = new Stopwatch();

            List<Bitmap> segments = new List<Bitmap>();
            byte* edgeData = (byte*)edgeIter.PixelData;
            int ballSize = 1 + 2 * radius;
            HashSet<int> uncoloredPixels = new HashSet<int>(orgUncoloredPixels);
            Queue<int> uncoloredPixelList = new Queue<int>(orgUncoloredPixels);

            while (uncoloredPixels.Count >= 1)
            {
                int idx = uncoloredPixelList.Dequeue();
                if (!uncoloredPixels.Contains(idx)) continue;
                uncoloredPixels.Remove(idx);

                int x, y;
                y = Math.DivRem(idx, edgeImage.Width, out x);

                // ボールが入るか
                if (ballSizeList[idx] < ballSize) continue;

                // 彩色済みなら塗らない
                bool colored = false;
                for (int yy = y; yy < y + ballSize; yy++)
                    for (int xx = x; xx < x + ballSize; xx++)
                        if (!(y == yy && x == xx) && !uncoloredPixels.Contains(xx + yy * edgeImage.Width))
                        {
                            colored = true;
                            goto COLORED_DECIDED;
                        }

            COLORED_DECIDED:
            
                if (colored) continue;

                // 新しいセグメント
                using (IplImage segmentImage = new IplImage(edgeImage.Width, edgeImage.Height, BitDepth.U8, 4))
                {
                    Cv.Set(segmentImage, new CvScalar(0, 0, 0, 0));

                    Random rand = new Random();
                    Color c = Color.FromArgb(rand.Next(255) + 1, rand.Next(255) + 1, rand.Next(255) + 1);

                    Cv.Rectangle(segmentImage, x + 1, y + 1, x + ballSize - 1, y + ballSize - 1, Cv.RGB(c.R, c.G, c.B));

                    // 新しい色でFlood fill
                    HashSet<int> initPoints = new HashSet<int>();
                    for (int yy = y; yy < y + ballSize - 1; yy++)
                    {
                        initPoints.Add(x + yy * edgeImage.Width);
                        initPoints.Add((x + ballSize - 1) + yy * edgeImage.Width);
                    }
                    for (int xx = x; xx < x + ballSize - 1; xx++)
                    {
                        initPoints.Add(xx + y * edgeImage.Width);
                        initPoints.Add(xx + (y + ballSize - 1) * edgeImage.Width);
                    }

                    CvRect roi = FloodFill(initPoints, segmentImage, c, uncoloredPixels, orgIter, sw2);



                    sw2.Start();

                    Cv.SetImageROI(segmentImage, roi);
                    {
                        IplConvKernel kernel = new IplConvKernel(2 * radius + 1, 2 * radius + 1, radius, radius, ElementShape.Ellipse);
                        Cv.Erode(segmentImage, segmentImage, kernel, 1);
                        Cv.Dilate(segmentImage, segmentImage, kernel, 1);
                    }
                    Cv.ResetImageROI(segmentImage);


                    for (int yy = roi.Y; yy < roi.Bottom; yy++)
                        for (int xx = roi.X; xx < roi.Right; xx++)
                            if (segmentImage.ImageDataPtr[4 * xx + yy * segmentImage.WidthStep + 3] != 0)
                            {
                                int idx2 = xx + yy * edgeImage.Width;
                                labelMap[idx2] = segmentCnt;
                                uncoloredPixels.Remove(idx2);
                                orgUncoloredPixels.Remove(idx2);
                            }
                    segmentCnt++;

                    Bitmap segmentBmp = BitmapConverter.ToBitmap(segmentImage);
                    segments.Add(segmentBmp);

                    sw2.Stop();
                }
            }

            if (segments.Count >= 1) segments.First().Save(radius + ".png");

            if (sw.ElapsedMilliseconds >= 1000)
            {
                Console.WriteLine("[radius=" + radius + "]");
                Console.WriteLine("total= " + sw.ElapsedMilliseconds + " ms");
                Console.WriteLine("total= " + sw2.ElapsedMilliseconds + " ms");
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
            int maxRadius = ((maxBallSize / 2) / deltaRadius - 1) * deltaRadius;
            int minRadius = ((minBallSize / 2) / deltaRadius + 1) * deltaRadius;
//            FlashArrayToBitmap(ballSizeList, edgeImage.Width, edgeImage.Height, brushedImage);


            // 出力
            Dictionary<Bitmap, int> segmentList = new Dictionary<Bitmap, int>();
            int[] labelMap = new int[edgeImage.Width * edgeImage.Height];

            // 塗るべきピクセルを追加
            HashSet<int> uncoloredPixels = new HashSet<int>();
            using (BitmapIterator edgeIter = new BitmapIterator(edgeImage, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed))
            {
                byte* edgeData = (byte*)edgeIter.PixelData;
                int i = 0;
                for (int y = 0; y < edgeImage.Height; y++)
                {
                    for (int x = 0; x < edgeImage.Width; x++)
                    {
                        if (edgeData[x + y * edgeIter.Stride] <= edgeThreshold)
                            uncoloredPixels.Add(i);
                        i++;
                    }
                }
            }

            // エッジの間に大雑把に色を塗っていく
            using (BitmapIterator edgeIter = new BitmapIterator(edgeImage, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed))
            using (BitmapIterator orgIter = new BitmapIterator(orgImage, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
            {
                byte* edgeData = (byte*)edgeIter.PixelData;

                // ボールを徐々に小さくしながらTrappedBallSegmentation
                for (int radius = maxRadius; radius >= minRadius; radius -= deltaRadius)
                {
                    List<Bitmap> segments = MoveBall(uncoloredPixels, labelMap, radius, edgeIter, orgIter, ballSizeList);
                    for (int i = 0; i < segments.Count; i++)
                    {
                        segmentList[segments[i]] = radius;
                    }
                }

                Console.WriteLine("move ball: done");

// TODO                RegionGrowing(...);
                using (Graphics g = Graphics.FromImage(brushedImage))
                {
                    foreach (var kv in segmentList)
                    {
                        Bitmap bmp = kv.Key;
                        int radius = kv.Value;
                        g.DrawImage(bmp, Point.Empty);
                    }
                }

                /*
                                Point[] deltas = new Point[] { new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1) };

                                byte* orgData = (byte*)orgIter.PixelData;

                                float threshold = sqColorDistThreshold;

                                while (uncoloredPixels.Count >= 1 && threshold <= 1000000)
                                {
                                    // ラベルが塗られていない領域と接するピクセル集合
                                    PriorityQueue<ContourPixel> contours = new PriorityQueue<ContourPixel>();
                                    HashSet<Point> tmp_contours = new HashSet<Point>();
                                    foreach (Point pt in uncoloredPixels)
                                    {
                                        if (!uncoloredPixels.Contains(new Point(pt.X - 1, pt.Y))) tmp_contours.Add(new Point(pt.X - 1, pt.Y));
                                        if (!uncoloredPixels.Contains(new Point(pt.X, pt.Y - 1))) tmp_contours.Add(new Point(pt.X, pt.Y - 1));
                                        if (!uncoloredPixels.Contains(new Point(pt.X + 1, pt.Y))) tmp_contours.Add(new Point(pt.X + 1, pt.Y));
                                        if (!uncoloredPixels.Contains(new Point(pt.X, pt.Y + 1))) tmp_contours.Add(new Point(pt.X, pt.Y + 1));
                                    }
                                    foreach (Point pt in tmp_contours)
                                    {
                                        if (pt.X < 0 || edgeImage.Width <= pt.X || pt.Y < 0 || edgeImage.Height <= pt.Y) continue;
                                        contours.Push(new ContourPixel(pt, 0));
                                    }

                                    Console.WriteLine("init contour: done");

                                    // エッジは削除
                                    uncoloredPixels.RemoveWhere(pt => edgeData[pt.X + pt.Y * edgeIter.Stride] > edgeThreshold);

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
                                            if (uncoloredPixels.Contains(neighbor))
                                            {
                                                if (SqColorDist(orgIter, pt.X, pt.Y, neighbor.X, neighbor.Y) <= threshold)
                                                {
                                                    contours.Push(new ContourPixel(neighbor, 0));
                                                    labelMap[neighbor.X + neighbor.Y * edgeImage.Width] = labelMap[pt.X + pt.Y * edgeImage.Width];
                                                    // brushedImage.SetPixel(neighbor.X, neighbor.Y, brushedImage.GetPixel(pt.X, pt.Y));
                                                    uncoloredPixels.Remove(neighbor);
                                                }
                                            }
                                        }
                                    }

                                    Console.WriteLine("region growing: done (" + uncoloredPixels.Count + ", " + threshold + ")");

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
                 */
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

        unsafe bool CanFill(int idx, int idxSeg, int ptX, int ptY, int srcX, int srcY, HashSet<int> filled, HashSet<int> uncoloredPixels, IplImage segmentImage, BitmapIterator orgIter, Stopwatch sw2 = null)
        {
//            sw2.Start();
            if (!uncoloredPixels.Contains(idx))
            {
  //              sw2.Stop();
                return false;
            }
            if (filled.Contains(idx))
            {
    //            sw2.Stop();
                return false;
            }
            if (ptX < 0 || edgeImage.Width <= ptX)
            {
      //          sw2.Stop();
                return false;
            }
            if (ptY < 0 || edgeImage.Height <= ptY)
            {
        //        sw2.Stop();
                return false;
            }
            if (segmentImage.ImageDataPtr[idxSeg] != 0) 
            {
          //      sw2.Stop();
                return false;
            }
            if (SqColorDist(orgIter, srcX, srcY, ptX, ptY) >= sqColorDistThreshold)
            {
            //    sw2.Stop();
                return false;
            }
//            sw2.Stop();
            return true;
        }

        unsafe public CvRect FloodFill(HashSet<int> initPoints, IplImage segmentImage, Color c, HashSet<int> uncoloredPixels, BitmapIterator orgIter, Stopwatch sw2 = null)
        {
            int roiMinX = int.MaxValue;
            int roiMinY = int.MaxValue;
            int roiMaxX = int.MinValue;
            int roiMaxY = int.MinValue; 
            List<int> curs = initPoints.ToList();
            List<int> nexts = new List<int>();
            HashSet<int> filled = new HashSet<int>(initPoints);
            byte* partData = segmentImage.ImageDataPtr;
            while (curs.Count > 0)
            {
                for (int i = 0; i < curs.Count; i++)
                {
                    int src = curs[i];
                    int x, y;
                    y = Math.DivRem(src, edgeImage.Width, out x);
                    if (x < roiMinX) roiMinX = x;
                    if (x > roiMaxX) roiMaxX = x;
                    if (y < roiMinY) roiMinY = y;
                    if (y > roiMaxY) roiMaxY = y;
                    int offset = 4 * x + y * segmentImage.WidthStep;
                    if (CanFill(src - 1, offset - 4 + 3, x - 1, y, x, y, filled, uncoloredPixels, segmentImage, orgIter, sw2))
                    {
                        nexts.Add(src - 1);
                        filled.Add(src - 1);
                    }
                    if (CanFill(src + 1, offset + 4 + 3, x + 1, y, x, y, filled, uncoloredPixels, segmentImage, orgIter, sw2))
                    {
                        nexts.Add(src + 1);
                        filled.Add(src + 1);
                    }
                    if (CanFill(src - edgeImage.Width, offset - segmentImage.WidthStep + 3, x, y - 1, x, y, filled, uncoloredPixels, segmentImage, orgIter, sw2))
                    {
                        nexts.Add(src - segmentImage.Width);
                        filled.Add(src - segmentImage.Width);
                    }
                    if (CanFill(src + edgeImage.Width, offset + segmentImage.WidthStep + 3, x, y + 1, x, y, filled, uncoloredPixels, segmentImage, orgIter, sw2))
                    {
                        nexts.Add(src + segmentImage.Width);
                        filled.Add(src + segmentImage.Width);
                    }
                    partData[offset + 0] = c.B;
                    partData[offset + 1] = c.G;
                    partData[offset + 2] = c.R;
                    partData[offset + 3] = 255;
                }
                curs.Clear();
                FMath.Swap(ref curs, ref nexts);
            }

            return new CvRect(
                Math.Max(0, roiMinX - 1),
                Math.Max(0, roiMinY - 1),
                Math.Min(edgeImage.Width, roiMaxX - roiMinX + 2),
                Math.Min(edgeImage.Height, roiMaxY - roiMinY + 2));
        }

    }
}
