using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FLib
{
    public partial class TakeScreenshotForm : Form
    {

        Pen pen = new Pen(Color.Red, 2);
        Point? start = null;
        Point? end = null;
        private Rectangle GetRectangle(Point start, Point end)
        {
            var x = Math.Min(start.X, end.X) - Location.X;
            var y = Math.Min(start.Y, end.Y) - Location.Y;
            var w = Math.Abs(start.X - end.X);
            var h = Math.Abs(start.Y - end.Y);
            return new Rectangle(x, y, w, h);
        }


        public TakeScreenshotForm()
        {
            InitializeComponent();
        }

        private void TakeScreenshotForm_Load(object sender, EventArgs e)
        {
            this.SetStyle(
              ControlStyles.DoubleBuffer |
              ControlStyles.UserPaint |
              ControlStyles.AllPaintingInWmPaint,
              true);
        }

        Bitmap currentScreenshot_ = null;

        public Bitmap takeScreenshot(Form owner)
        {
            using (var form = new Form())
            {
                owner.Hide();
                System.Threading.Thread.Sleep(500);

                int left = int.MaxValue;
                int right = int.MinValue;
                int top = int.MaxValue;
                int bottom = int.MinValue;

                foreach (var scr in Screen.AllScreens)
                {
                    left = Math.Min(left, scr.WorkingArea.Left);
                    right = Math.Max(right, scr.WorkingArea.Right);
                    top = Math.Min(top, scr.WorkingArea.Top);
                    bottom = Math.Max(bottom, scr.WorkingArea.Bottom);
                }

                var bound = new Rectangle(left, top, right - left, bottom - top);
                using (var screenshot = UI.TakeScreenshot(bound))
                {
                    if (form.WindowState == FormWindowState.Maximized)
                    {
                    }
                    //form.SetBounds(bound.X, bound.Y, bound.Width, bound.Height);
                    //form.Show();
                    //form.Activate();

                    //form.StartPosition = FormStartPosition.Manual;
                    //form.SetBounds(bound.X, bound.Y, bound.Width, bound.Height);
                    //form.WindowState = FormWindowState.Normal;
                    //form.FormBorderStyle = FormBorderStyle.None;
                    //form.Paint += (sender, e) =>
                    //        e.Graphics.DrawImage(screenshot, bound.Location);
                    //form.Show();
                    //form.Activate();

                    this.SetBounds(bound.X, bound.Y, bound.Width, bound.Height);
                    this.StartPosition = FormStartPosition.Manual;
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.WindowState = FormWindowState.Normal;

                    currentScreenshot_ = screenshot;
                    this.ShowDialog();
                    currentScreenshot_ = null;

                    owner.Show();

                    if (start != null && end != null)
                    {
                        Rectangle rect = GetRectangle(start.Value, end.Value);
                        Bitmap bmp = new Bitmap(rect.Width, rect.Height, screenshot.PixelFormat);
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.DrawImage(screenshot, new Rectangle(0 ,0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
                        }
                        return bmp;
                    }
                }
            }
            return null;
        }

        private void TakeScreenshotForm_MouseDown(object sender, MouseEventArgs e)
        {
            start = MousePosition;
        }

        private void TakeScreenshotForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (start != null && end != null)
            {
                var rect = GetRectangle(start.Value, end.Value);
                if (rect.Width != 0 && rect.Height != 0)
                {
                    this.Opacity = 0;
                }
            }
            Hide();
        }

        private void BlackForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (start == null) return;

            end = MousePosition;

            this.Invalidate();

            //using (var g = CreateGraphics())
            //{
            //    g.Clear(Color.Black);

            //    if (currentScreenshot_ != null)
            //    {
            //        g.DrawImage(currentScreenshot_, Point.Empty);
            //    }

            //    var rect = GetRectangle(start.Value, end.Value);
            //    g.DrawRectangle(pen, rect);

            //    var pt1 = new Point(rect.Left, (rect.Top + rect.Bottom) / 2);
            //    var pt2 = new Point(rect.Right, pt1.Y);
            //    g.DrawLine(pen, pt1, pt2);

            //    pt1 = new Point((rect.Left + rect.Right) / 2, rect.Top);
            //    pt2 = new Point(pt1.X, rect.Bottom);
            //    g.DrawLine(pen, pt1, pt2);
            //}
        }

        private void TakeScreenshotForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        private void TakeScreenshotForm_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
           // using (var g = CreateGraphics())
            {
                g.Clear(Color.Black);

                if (currentScreenshot_ != null)
                {
                    g.DrawImage(currentScreenshot_, Point.Empty);
                }

                if (start == null || end.Value == null)
                {
                    return;
                }

                var rect = GetRectangle(start.Value, end.Value);
                g.DrawRectangle(pen, rect);

                var pt1 = new Point(rect.Left, (rect.Top + rect.Bottom) / 2);
                var pt2 = new Point(rect.Right, pt1.Y);
                g.DrawLine(pen, pt1, pt2);

                pt1 = new Point((rect.Left + rect.Right) / 2, rect.Top);
                pt2 = new Point(pt1.X, rect.Bottom);
                g.DrawLine(pen, pt1, pt2);
            }
        }
    }
}
