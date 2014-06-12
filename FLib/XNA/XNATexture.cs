using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing.Imaging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace FLib
{
    public static class XNATexture
    {
        unsafe public static void Load(GraphicsDevice GraphicsDevice, System.Drawing.Bitmap bmp, out Texture2D texture, out Color[] texData, bool reverse = false)
        {
            if (GraphicsDevice == null || bmp == null)
            {
                texture = null;
                texData = null;
                return;
            }

            using (BitmapIterator iter = new BitmapIterator(bmp, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
            {
                Stopwatch sw = Stopwatch.StartNew();

                texData = new Color[bmp.Width * bmp.Height];
                byte* data = (byte*)iter.PixelData;
                int texIdx = 0;

                Console.WriteLine("0: " + sw.ElapsedMilliseconds + "ms");
                sw.Restart();

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        int idx = !reverse ?
                            4 * x + y * iter.Stride :
                            4 * (bmp.Width - 1 - x) + y * iter.Stride;
                        if (data[idx + 3] == 0)
                        {
                            texData[texIdx] = Color.Transparent;
                        }
                        else
                        {
                            texData[texIdx].R = data[idx + 2];
                            texData[texIdx].G = data[idx + 1];
                            texData[texIdx].B = data[idx + 0];
                            texData[texIdx].A = data[idx + 3];
                        }
                        texIdx++;
                    }
                }


                Console.WriteLine("1: " + sw.ElapsedMilliseconds + "ms");
                sw.Restart();
                
                texture = new Texture2D(GraphicsDevice, bmp.Width, bmp.Height);
                texture.SetData(texData);

                Console.WriteLine("2: " + sw.ElapsedMilliseconds + "ms");
                sw.Restart();
            }
        }

        unsafe public static void Load(GraphicsDevice GraphicsDevice, string path, out Texture2D texture, out Color[] texData, bool reverse = false)
        {
            path = System.IO.Path.GetFullPath(path);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("File not found", path);
            }
            else
            {
                using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(path))
                {
                    Load(GraphicsDevice, bmp, out texture, out texData, reverse);
                }
            }
        }

        public static Texture2D Load(GraphicsDevice GraphicsDevice, string path, bool reverse = false)
        {
            Texture2D texture;
            Color[] colors;
            Load(GraphicsDevice, path, out texture, out colors, reverse);
            return texture;
        }

        public static Texture2D Load(GraphicsDevice GraphicsDevice, System.Drawing.Bitmap bmp, bool reverse = false)
        {
            Texture2D texture;
            Color[] colors;
            Load(GraphicsDevice, bmp, out texture, out colors, reverse);
            return texture;
        }
    }
}
