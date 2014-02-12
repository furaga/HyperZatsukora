using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace FLib
{
    public class SketchTyping
    {
        const int KeyPointerHalfSize = 5;

        Bitmap canvasImage;
        Bitmap keyboardImage;
        Font font = new Font("Arial", 10);

        List<Point> keyPoints = new List<Point>();
        public Dictionary<char, Point> keyPointsDict = new Dictionary<char, Point>();

        string keychars =
            @"????????????????" +
            @"?1234567890-^??" +
            @"?qwertyuiop@[?" +
            @"?asdfghjkl;:]" +
            @"?zxcvbnm,./??" +
            @"????? ???????";

        unsafe public SketchTyping(Bitmap keyboardImage)
        {
            this.keyboardImage = keyboardImage;
            List<Point> tmpKeyPoints = new List<Point>();
            using (BitmapIterator iter = new BitmapIterator(keyboardImage, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                for (int y = 0; y < keyboardImage.Height; y++)
                {
                    for (int x = 0; x < keyboardImage.Width; x++)
                    {
                        byte bb = iter.Data[4 * x + y * iter.Stride + 0];
                        byte gg = iter.Data[4 * x + y * iter.Stride + 1];
                        byte rr = iter.Data[4 * x + y * iter.Stride + 2];
                        if (rr == 255 && gg == 0 && bb == 0)
                        {
                            tmpKeyPoints.Add(new Point(x, y));
                        }
                    }
                }
            }

            const int sameRowThresDist = 30;

            while (tmpKeyPoints.Count >= 1)
            {
                Point offset = tmpKeyPoints.First();
                int removeCnt = 0;
                for (int i = 0; i < tmpKeyPoints.Count; i++)
                {
                    int dist = Math.Abs(offset.Y - tmpKeyPoints[i].Y);
                    if (dist < sameRowThresDist) removeCnt++;
                    else break;
                }
                var ls = tmpKeyPoints.Take(removeCnt).ToList();
                ls.Sort((pt1, pt2) => pt1.X - pt2.X);
                keyPoints.AddRange(ls);
                tmpKeyPoints.RemoveRange(0, removeCnt);
            }

            for (int i = 0; i < keyPoints.Count; i++)
            {
                keyPointsDict[keychars[i]] = keyPoints[i];
            }
        }

        public List<Point> GetStroke(string text)
        {
            text = text.Trim().ToLower();
            if (text.Length <= 0) return null;
            List<Point> stroke = new List<Point>();

            for (int i = 0; i < text.Length; i++)
            {
                if (keyPointsDict.ContainsKey(text[i]))
                {
                    stroke.Add(keyPointsDict[text[i]]);
                }
            }

            return stroke;
        }


    }
}
