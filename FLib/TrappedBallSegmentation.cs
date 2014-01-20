using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace FLib
{
    public class TrappedBallSegmentation : IDisposable
    {
        // 入出力
        public Bitmap edgeImage;
        public Bitmap brushedImage;

        public TrappedBallSegmentation(Bitmap edgeImage)
        {
            this.edgeImage = new Bitmap(edgeImage);
            this.brushedImage = new Bitmap(edgeImage.Width, edgeImage.Height, PixelFormat.Format32bppArgb);
        }

        public void Dispose()
        {
            if (edgeImage != null) edgeImage.Dispose();
            if (brushedImage != null) brushedImage.Dispose();
        }

        public void Execute()
        {
            const int maxRadius = 100;
            const int minRadius = 1;
            const int deltaRadius = 10;

            using (IplImage iplEdge = BitmapConverter.ToIplImage(edgeImage))
            using (IplImage iplErode = new IplImage(iplEdge.Width, iplEdge.Height, iplEdge.Depth, iplEdge.NChannels))
            using (IplImage iplDilate = new IplImage(iplEdge.Width, iplEdge.Height, iplEdge.Depth, iplEdge.NChannels))
            {
                // とりあえずちょっとずつボールのサイズを小さくしてみる
                int radius = maxRadius;
                while (radius >= minRadius)
                {
                    IplConvKernel kernel = new IplConvKernel(2 * radius + 1, 2 * radius + 1, radius, radius, ElementShape.Ellipse);
                    Cv.Erode(iplEdge, iplErode, kernel, 1);
                    Cv.Dilate(iplErode, iplDilate, kernel, 1);
                    radius -= deltaRadius;
                }
            }
        }

        // TODO
        public static void FloodFill(HashSet<Point> unchecks, BitmapIterator srcIter, BitmapIterator dstIter, int row, int col)
        {
            if (unchecks.Count <= 0) return;
            Point pt = unchecks.First();

            HashSet<Point> curs = new HashSet<Point>() { pt };
            HashSet<Point> nexts = new HashSet<Point>();
            HashSet<Point> filled = new HashSet<Point>();
            while (curs.Count > 0)
            {/*
                foreach (Point src in curs)
                {
                    src.id = id;
                    filled.Add(src);
                    unchecks.Remove(src);
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
                            Point dst = patches[px + py * row];
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
                }*/
                curs.Clear();
                FMath.Swap(ref curs, ref nexts);
            }

            if (filled.Count <= 6)
                foreach (var p in filled)
                { }//                    p.id = -1;
        }

    }
}
