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
        {            //this.SetStyle(
            //  ControlStyles.DoubleBuffer |
            //  ControlStyles.UserPaint |
            //  ControlStyles.AllPaintingInWmPaint,
            //  true);

        }

        Rectangle getScreenBounds()
        {

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

            return new Rectangle(left, top, right - left, bottom - top);
        }

        Bitmap currentScreenshot_ = null;

        public Bitmap takeScreenshot(Form owner, bool hideOwnerForm = true)
        {
            if (hideOwnerForm)
            {
                owner.Hide();
                System.Threading.Thread.Sleep(500);
            }

            var bound = getScreenBounds();
            using (var screenshot = UI.TakeScreenshot(bound))
            {
                using (var form = new Form())
                {
                    // 背景（スクリーンショット画像）を表示するフォームを作成
                    form.SetBounds(bound.X, bound.Y, bound.Width, bound.Height);
                    form.Paint += (sender, e) =>
                    {
                        if (screenshot != null)
                        {
                            prevScreenshot_ = currentScreenshot_;
                            e.Graphics.DrawImage(currentScreenshot_, Point.Empty);
                            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(128, 0, 0, 0)), new Rectangle(0, 0, Width, Height));
                        }
                    };
                    form.StartPosition = FormStartPosition.Manual;
                    form.TopMost = true;
                    form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    form.WindowState = FormWindowState.Normal;
                    form.Show();
                    form.Activate();


                    // キャプチャ範囲を表示するフォームを初期化
                    this.SetBounds(bound.X, bound.Y, bound.Width, bound.Height);


                    // キャプチャ
                    currentScreenshot_ = screenshot;
                    this.ShowDialog();
                    currentScreenshot_ = null;


                    if (hideOwnerForm)
                    {
                        owner.Show();
                    }


                    // ドラッグした範囲の画像を切り出す
                    if (start != null && end != null)
                    {
                        Rectangle rect = GetRectangle(start.Value, end.Value);
                        Bitmap bmp = new Bitmap(rect.Width, rect.Height, screenshot.PixelFormat);

                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.DrawImage(screenshot, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
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
            if (start == null)
            {
                return;
            }
            end = MousePosition;
            this.Invalidate();
        }

        private void TakeScreenshotForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        Bitmap prevScreenshot_ = null;

        private void TakeScreenshotForm_Paint(object sender, PaintEventArgs e)
        {
            if (start == null || end.Value == null)
            {
                return;
            }

            var rect = GetRectangle(start.Value, end.Value);
            e.Graphics.DrawRectangle(pen, rect);

            var pt1 = new Point(rect.Left, (rect.Top + rect.Bottom) / 2);
            var pt2 = new Point(rect.Right, pt1.Y);
            e.Graphics.DrawLine(pen, pt1, pt2);

            pt1 = new Point((rect.Left + rect.Right) / 2, rect.Top);
            pt2 = new Point(pt1.X, rect.Bottom);
            e.Graphics.DrawLine(pen, pt1, pt2);

        //    if (currentScreenshot_ != null)// && prevScreenshot_ != currentScreenshot_)
        //    {
        //        prevScreenshot_ = currentScreenshot_;
        //        e.Graphics.DrawImage(currentScreenshot_, Point.Empty);
        //        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(128, 0, 0, 0)), new Rectangle(0, 0, Width, Height));
        //    }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (start == null || end.Value == null)
            {
                return;
            }

            var rect = GetRectangle(start.Value, end.Value);
            e.Graphics.DrawRectangle(pen, rect);

            var pt1 = new Point(rect.Left, (rect.Top + rect.Bottom) / 2);
            var pt2 = new Point(rect.Right, pt1.Y);
            e.Graphics.DrawLine(pen, pt1, pt2);

            pt1 = new Point((rect.Left + rect.Right) / 2, rect.Top);
            pt2 = new Point(pt1.X, rect.Bottom);
            e.Graphics.DrawLine(pen, pt1, pt2);
        }
    }
}
