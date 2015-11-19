using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Drawing2D;

namespace FLib
{
    public class FCircle
    {
        public PointF center;
        public float radius;
        Pen pen = new Pen(Brushes.Gray, 1);

        public FCircle(PointF c, float r)
        {
            center = c;
            radius = r;
        }

        public void Draw(Graphics g)
        {
            g.DrawEllipse(pen, new RectangleF(center.X - radius / 2, center.Y - radius / 2, radius, radius));
        }
    }

    public class FTriangle
    {
        public PointF p1, p2, p3;
        Pen pen = new Pen(Brushes.Black, 1);

        public FTriangle(PointF p1, PointF p2, PointF p3)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as FTriangle);
        }

        public bool Equals(FTriangle t)
        {
            if (Object.ReferenceEquals(t, null))
                return false;
            if (Object.ReferenceEquals(this, t))
                return true;
            if (this.GetType() != t.GetType())
                return false;

            return (p1 == t.p1 && p2 == t.p2 && p3 == t.p3 ||
                    p1 == t.p2 && p2 == t.p3 && p3 == t.p1 ||
                    p1 == t.p3 && p2 == t.p1 && p3 == t.p2 ||
                    p1 == t.p3 && p2 == t.p2 && p3 == t.p1 ||
                    p1 == t.p2 && p2 == t.p1 && p3 == t.p3 ||
                    p1 == t.p1 && p2 == t.p3 && p3 == t.p2);
        }

        public static bool operator ==(FTriangle lhs, FTriangle rhs)
        {
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null))
                    return true;
                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator !=(FTriangle lhs, FTriangle rhs)
        {
            return !(lhs == rhs);
        }

        public void Draw(Graphics g)
        {
            g.DrawLines(pen, new[] { p1, p2, p3, p1 });
        }

        public bool hasCommonPoints(FTriangle t)
        {
            return p1 == t.p1 || p1 == t.p2 || p1 == t.p3 ||
                    p2 == t.p1 || p2 == t.p2 || p2 == t.p3 ||
                    p3 == t.p1 || p3 == t.p2 || p3 == t.p3;
        }

        public FCircle GetCircumscribedCircles()
        {
            float x1 = p1.X;
            float y1 = p1.Y;
            float x2 = p2.X;
            float y2 = p2.Y;
            float x3 = p3.X;
            float y3 = p3.Y;

            float c = 2f * ((x2 - x1) * (y3 - y1) - (y2 - y1) * (x3 - x1));
            float x = ((y3 - y1) * (x2 * x2 - x1 * x1 + y2 * y2 - y1 * y1)
                     + (y1 - y2) * (x3 * x3 - x1 * x1 + y3 * y3 - y1 * y1)) / c;
            float y = ((x1 - x3) * (x2 * x2 - x1 * x1 + y2 * y2 - y1 * y1)
                     + (x2 - x1) * (x3 * x3 - x1 * x1 + y3 * y3 - y1 * y1)) / c;
            PointF center = new PointF(x, y);

            // 外接円の半径 r は、半径から三角形の任意の頂点までの距離に等しい
            float r = (float)Math.Sqrt(FMath.SqDistance(center, p1));

            return new FCircle(center, r);
        }
    }

    public class DelaunayTriangle
    {
        public List<FTriangle> triangleList { get; private set; }

        public DelaunayTriangle(List<PointF> points, RectangleF boundingBox)
        {
            triangleList = DelaunayTriangulation(points, boundingBox);
        }

        public static List<FTriangle> DelaunayTriangulation(List<PointF> points, RectangleF boundingBox)
        {
            if (points == null)
                return new List<FTriangle>();

            List<FTriangle> triangleSet = new List<FTriangle>();

            // 巨大な外部三角形をリストに追加
            FTriangle hugeTriangle = getHugeTriangle(boundingBox);
            triangleSet.Add(hugeTriangle);

            try
            {
                foreach (PointF p in points)
                {
                    Dictionary<FTriangle, bool> tmpTriangleSet = new Dictionary<FTriangle, bool>();

                    for (int i = 0; i < triangleSet.Count; i++)
                    {
                        FTriangle t = triangleSet[i];

                        // その外接円を求める。
                        FCircle c = t.GetCircumscribedCircles();

                        if (FMath.SqDistance(c.center, p) <= c.radius * c.radius)
                        {
                            addElementToRedundanciesMap(tmpTriangleSet, new FTriangle(p, t.p1, t.p2));
                            addElementToRedundanciesMap(tmpTriangleSet, new FTriangle(p, t.p2, t.p3));
                            addElementToRedundanciesMap(tmpTriangleSet, new FTriangle(p, t.p3, t.p1));

                            triangleSet.RemoveAt(i);
                            i--;
                        }
                    }

                    foreach (var kv in tmpTriangleSet)
                    {
                        if (kv.Value)
                            triangleSet.Add(kv.Key);
                    }
                }

                // 最後に、外部三角形の頂点を削除

                for (int i = 0; i < triangleSet.Count; i++)
                {
                    FTriangle t = triangleSet[i];
                    // もし外部三角形の頂点を含む三角形があったら、それを削除
                    if (hugeTriangle.hasCommonPoints(t))
                    {
                        triangleSet.RemoveAt(i);
                        i--;
                    }
                }

                return triangleSet;
            }
            catch (Exception)
            {
                return null;
            }
        }


        private static void addElementToRedundanciesMap(Dictionary<FTriangle, bool> hashMap, FTriangle t)
        {
            hashMap[t] = !hashMap.ContainsKey(t);
        }


        private static FTriangle getHugeTriangle(RectangleF rect)
        {
            PointF center = new PointF(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            float radius = (float)Math.Sqrt(FMath.SqDistance(center, rect.Location)) + 1f;

            float sq3 = (float)Math.Sqrt(3);
            float x1 = center.X - sq3 * radius;
            float y1 = center.Y - radius;
            PointF p1 = new PointF(x1, y1);

            float x2 = center.X + sq3 * radius;
            float y2 = center.Y - radius;
            PointF p2 = new PointF(x2, y2);

            float x3 = center.X;
            float y3 = center.Y + 2 * radius;
            PointF p3 = new PointF(x3, y3);

            return new FTriangle(p1, p2, p3);
        }
    }

}