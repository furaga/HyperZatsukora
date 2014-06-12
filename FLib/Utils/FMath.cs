using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace FLib
{
    public class FMath
    {
        public static float MahalanobisDistance(Color[] pixels1, Color[] pixels2)
        {
            float sqdist = 0;

            foreach (Func<Color, float> getter in new Func<Color, float>[] {
                col => col.R,
                col => col.G,
                col => col.B
            })
            {
                float avg1, vrc1;
                float avg2, vrc2;
                GetAverageVariance(pixels1, getter, out avg1, out vrc1);
                GetAverageVariance(pixels2, getter, out avg2, out vrc2);
                sqdist += (avg1 - avg2) * (avg1 - avg2);
            }

            return (float)Math.Sqrt(sqdist);
        }
        public static void Swap<T>(ref T x, ref T y)
        {
            T t = x;
            x = y;
            y = t;
        }
        public static float Clamp(float x, float min, float max)
        {
            if (min > max) Swap(ref min, ref max);
            return Math.Min(max, Math.Max(min, x));
        }

        public static void GetAverageVariance(Color[] pixels, Func<Color, float> getter, out float avg, out float vrc)
        {
            float sum = 0;
            float sqsum = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                float val = getter(pixels[i]);
                sum += val;
                sqsum += val * val;
            }
            avg = sum / pixels.Length;
            vrc = sqsum / pixels.Length - avg * avg;
        }

        public static bool IsCrossed(PointF p1, PointF p2, PointF p3, PointF p4)
        {
            float ksi = (p4.Y - p3.Y) * (p4.X - p1.X) - (p4.X - p3.X) * (p4.Y - p1.Y);
            float eta = -(p2.Y - p1.Y) * (p4.X - p1.X) + (p2.X - p1.X) * (p4.Y - p1.Y);
            float delta = (p4.Y - p3.Y) * (p2.X - p1.X) - (p4.X - p3.X) * (p2.Y - p1.Y);
            if (Math.Abs(delta) <= 1e-4)
                return false;
            float lambda = ksi / delta;
            float mu = eta / delta;
            return 0 <= lambda && lambda <= 1 && 0 <= mu && mu <= 1;
        }

        public static float SqDistance(PointF start, PointF end)
        {
            float dx = start.X - end.X;
            float dy = start.Y - end.Y;
            return dx * dx + dy * dy;
        }

        public static float GetDistanceToLine(PointF pt, PointF lineStart, PointF lineEnd)
        {
            float v0x = pt.X - lineStart.X;
            float v0y = pt.Y - lineStart.Y;
            float v1x = lineEnd.X - lineStart.X;
            float v1y = lineEnd.Y - lineStart.Y;

            float len0 = (float)Math.Sqrt(v0x * v0x + v0y * v0y);
            float len1 = (float)Math.Sqrt(v1x * v1x + v1y * v1y);

            if (len0 <= 1e-4f || len1 <= 1e-4f)
                return float.MaxValue;

            v0x /= len0;
            v0y /= len0;
            v1x /= len1;
            v1y /= len1;

            float cos = v0x * v1x + v0y * v1y;
            // 点が線分からはみ出していたら無効
            if (cos < 0 || len1 < len0 * cos)
                return float.MaxValue;

            float sin = (float)Math.Sqrt(1 - cos * cos);
            float dist = len0 * sin;

            return dist;
        }


    }
}
