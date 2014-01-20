using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace FLib
{
    /// <summary>
    /// BitmapIteratorとは別。注意
    /// </summary>
    static public class BitmapHandler
    {
        static public BitmapIterator GetBitmapIterator(Bitmap bmp, ImageLockMode lockMode, PixelFormat pixelFormat)
        {
            return new BitmapIterator(bmp, lockMode, pixelFormat);
        }

        static public Bitmap CreateThumbnail(Bitmap bmp, int w, int h)
        {
            return CreateThumbnail(bmp, w, h, Color.FromArgb(240, 240, 240));
        }

        static public Bitmap CreateThumbnail(Bitmap bmp, int w, int h, Color bgColor)
        {
            Bitmap thumbnail = new Bitmap(w, h, bmp.PixelFormat);
            using (Graphics g = Graphics.FromImage(thumbnail))
            {
                g.Clear(bgColor);
                float ratio = Math.Min((float)w / bmp.Width, (float)h / bmp.Height);
                g.DrawImage(bmp, new Rectangle(0, 0, (int)(bmp.Width * ratio), (int)(bmp.Height * ratio)));
            }
            return thumbnail;
        }
    }
}
