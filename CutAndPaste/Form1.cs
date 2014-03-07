using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Imaging;
using FLib;

namespace CutAndPaste
{
    public partial class Form1 : Form
    {
        enum InSelectMode
        {
            FloodFill,
            FloodFillRemove,
            TrapBall,
            TrapBallRemove,
            RegionGrowing,
        }

        Bitmap inBmp;
        Bitmap inOverBmp;
        string currentInputPath = "";
        List<BmpPart> outParts = new List<BmpPart>();

        // input
        System.Drawing.Drawing2D.Matrix inTransform = new System.Drawing.Drawing2D.Matrix();
        InSelectMode inSelectMode = InSelectMode.FloodFill;

        // output
        System.Drawing.Drawing2D.Matrix outTransform = new System.Drawing.Drawing2D.Matrix();

        Dictionary<string, TextBox> inTextBox = new Dictionary<string, TextBox>();

        int brushSize = 10;

        public Form1()
        {
            InitializeComponent();

            var label = new Label() { Text = "brush size", Location = new Point(10, 10) };
            var textbox = new TextBox() { Text = "10", Location = new Point(80, 10) };
            textbox.TextChanged += new EventHandler(textbox_TextChanged);
            inPanel.Controls.Add(textbox);
            inPanel.Controls.Add(label);

            label = new Label() { Text = "flood fill threshold", Location = new Point(10, 40) };
            textbox = new TextBox() { Text = "10", Location = new Point(80, 40) };
            textbox.TextChanged += new EventHandler(textbox_TextChanged2);
            inPanel.Controls.Add(textbox);
            inPanel.Controls.Add(label);

        }

        void textbox_TextChanged(object sender, EventArgs e)
        {
            int size;
            if (int.TryParse((sender as TextBox).Text, out size))
            {
                brushSize = size;
            }
        }
        void textbox_TextChanged2(object sender, EventArgs e)
        {
            int val;
            if (int.TryParse((sender as TextBox).Text, out val))
            {
                sqColorDistThreshold = val;
            }
        }

        private void openImageOToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Filter = "画像ファイル|*.png;*.bmp;*.jpg";
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                currentInputPath = openFileDialog1.FileName;
                using (Bitmap bmp = new Bitmap(openFileDialog1.FileName))
                {
                    inBmp = new Bitmap(bmp);
                    inOverBmp = new Bitmap(bmp.Width, bmp.Height);
                    brushedPixels.Clear();
                }
                inputCanvas.Invalidate();
            }
        }

        private void inputCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (inBmp != null)
            {
                e.Graphics.Transform = outTransform;
                e.Graphics.DrawImage(inBmp, Point.Empty);
                e.Graphics.DrawImage(inOverBmp, Point.Empty);
            }
            e.Graphics.DrawEllipse(new Pen(Brushes.Blue), new Rectangle(mousePt.X - brushSize / 2, mousePt.Y - brushSize / 2, brushSize, brushSize));
        }

        private void outputCanvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Transform = inTransform;
            foreach (TreeNode n in treeView1.Nodes)
            {
                e.Graphics.DrawImage((n.Tag as BmpPart).bmp, (n.Tag as BmpPart).position);
            }
        }

        private void outputCanvas_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }


        /// AnnotationSketch:mouse.png
        #region 
        Point mousePt = Point.Empty;
        List<Point> mouseTrajectory = new List<Point>();
        HashSet<int> brushedPixels = new HashSet<int>();
        bool brushing = false;

        private void inputCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            mousePt = e.Location;
            brushing = true;
        }

        unsafe private void inputCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (inOverBmp == null) return;

            if (brushing)
            {
                using (var iter = new BitmapIterator(inOverBmp, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb))
                {
                    int minx = Math.Max(0, e.X - brushSize / 2);
                    int miny = Math.Max(0, e.Y - brushSize / 2);
                    int maxx = Math.Min(inOverBmp.Width - 1, e.X + brushSize / 2);
                    int maxy = Math.Min(inOverBmp.Height - 1, e.Y + brushSize / 2);
                    for (int y = miny; y <= maxy; y++)
                    {
                        float dy = y - e.Y;
                        for (int x = minx; x <= maxx; x++)
                        {
                            float dx = x - e.X;
                            float dist = dx * dx + dy * dy;
                            if (dist <= brushSize * brushSize * 0.25f)
                            {
                                iter.Data[4 * x + y * iter.Stride + 0] = 255;
                                iter.Data[4 * x + y * iter.Stride + 1] = 0;
                                iter.Data[4 * x + y * iter.Stride + 2] = 0;
                                iter.Data[4 * x + y * iter.Stride + 3] = 100;
                                brushedPixels.Add(x + y * inBmp.Width);
                            }
                        }
                    }
                }
            }

            mousePt = e.Location;
            inputCanvas.Invalidate();
        }

        HashSet<int> extractPixels = new HashSet<int>();

        unsafe void inputCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (inOverBmp == null) return;
            brushing = false;
            extractPixels = InFloodFill(brushedPixels);
            using (var iter = new BitmapIterator(inOverBmp, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb))
            {
                foreach (var idx in extractPixels)
                {
                    int x;
                    int y = Math.DivRem(idx, inBmp.Width, out x);
                    iter.Data[4 * x + y * iter.Stride + 0] = 255;
                    iter.Data[4 * x + y * iter.Stride + 1] = 0;
                    iter.Data[4 * x + y * iter.Stride + 2] = 255;
                    iter.Data[4 * x + y * iter.Stride + 3] = 255;
                }
            }
            inputCanvas.Invalidate();
        }



        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (!System.IO.File.Exists(currentInputPath)) return;
            var part = new BmpPart(currentInputPath, "part", extractPixels);
            outParts.Add(part);
            treeView1.Nodes.Add(new TreeNode() { Text = part.Name, Tag = part });
            using (var g = Graphics.FromImage(inOverBmp))
            {
                g.Clear(Color.Transparent);
            }
            brushedPixels.Clear();
            extractPixels.Clear();
            outputCanvas.Invalidate();
        }

        #endregion

        private void outputCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                foreach (TreeNode n in treeView1.Nodes)
                {
                    var part = n.Tag as BmpPart;
                    if (part.Contains(e.Location))
                    {
                        // todo
                    }
                }
            }
        }

        private void outputCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
            }
        }

        private void outputCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {

            }
        }
        
        /// AnnotationSketch:FloodFill.png
        #region
        HashSet<int> inFloodFill_checked = new HashSet<int>();
        float sqColorDistThreshold = 10;
        private HashSet<int> InFloodFill(HashSet<int> brushedPixels)
        {
            List<int> curs = brushedPixels.ToList();
            List<int> nexts = new List<int>();
            HashSet<int> filled = new HashSet<int>(brushedPixels);
            using (var iter = new BitmapIterator(inBmp, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb))
            {
                while (curs.Count > 0)
                {
                    for (int i = 0; i < curs.Count; i++)
                    {
                        int src = curs[i];
                        int x, y;
                        y = Math.DivRem(src, inBmp.Width, out x);
                        int offset = 4 * x + y * iter.Stride;
                        // 左
                        if (CanFill(src - 1, x - 1, y, x, y, filled, iter))
                        {
                            nexts.Add(src - 1);
                            filled.Add(src - 1);
                        }
                        // 右
                        if (CanFill(src + 1, x + 1, y, x, y, filled, iter))
                        {
                            nexts.Add(src + 1);
                            filled.Add(src + 1);
                        }
                        // 上
                        if (CanFill(src - inBmp.Width, x, y - 1, x, y, filled, iter))
                        {
                            nexts.Add(src - inBmp.Width);
                            filled.Add(src - inBmp.Width);
                        }
                        // 下
                        if (CanFill(src + inBmp.Width, x, y + 1, x, y, filled, iter))
                        {
                            nexts.Add(src + inBmp.Width);
                            filled.Add(src + inBmp.Width);
                        }
                    }
                    curs.Clear();
                    FMath.Swap(ref curs, ref nexts);
                }
            }
            return filled;
        }

        unsafe bool CanFill(int idx, int ptX, int ptY, int srcX, int srcY, HashSet<int> filled, BitmapIterator orgIter)
        {
            if (ptX < 0) return false;
            if (ptY < 0) return false;
            if (ptX >= orgIter.Bmp.Width) return false;
            if (ptY >= orgIter.Bmp.Height) return false;
            if (filled.Contains(idx))
            {
                return false;
            }
            if (SqColorDist(orgIter, srcX, srcY, ptX, ptY) >= sqColorDistThreshold)
            {
                return false;
            }
            return true;
        }

        unsafe float SqColorDist(BitmapIterator iter, int x0, int y0, int x1, int y1)
        {
            byte* data = (byte*)iter.PixelData;
            int offset0 = 4 * x0 + y0 * iter.Stride;
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
        #endregion

        private void treeView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                if (treeView1.SelectedNode != null)
                {
                    treeView1.Nodes.Remove(treeView1.SelectedNode);
                    outputCanvas.Invalidate();
                }
            }
        }

    }

    class BmpPart
    {
        public PointF position;
        public PointF scale = new PointF(1, 1);
        public string orgImagePath;
        public HashSet<int> extractPixels;
        // operation
        public Bitmap bmp;
        public string Name;

        unsafe public BmpPart(string orgImagePath, string name, HashSet<int> extractPixels)
        {
            Debug.Assert(System.IO.File.Exists(orgImagePath));

            this.orgImagePath = orgImagePath;
            this.extractPixels = extractPixels;
            this.Name = name;

            using (var input = new Bitmap(orgImagePath))
            {
                int minx = extractPixels.Min(idx => idx % input.Width);
                int miny = extractPixels.Min(idx => idx / input.Width);
                int maxx = extractPixels.Max(idx => idx % input.Width);
                int maxy = extractPixels.Max(idx => idx / input.Width);

                int ox = minx;
                int oy = miny;
                this.position = new PointF(ox, oy);
                int w = maxx - minx + 1;
                int h = maxy - miny + 1;
                bmp = new Bitmap(w, h);

                using (var outIter = new BitmapIterator(bmp, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                using (var inIter = new BitmapIterator(input, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    foreach (var idx in extractPixels)
                    {
                        int x;
                        int y = Math.DivRem(idx, input.Width, out x);
                        outIter.Data[4 * (x - ox) + (y - oy) * outIter.Stride + 0] = inIter.Data[4 * x + y * inIter.Stride + 0];
                        outIter.Data[4 * (x - ox) + (y - oy) * outIter.Stride + 1] = inIter.Data[4 * x + y * inIter.Stride + 1];
                        outIter.Data[4 * (x - ox) + (y - oy) * outIter.Stride + 2] = inIter.Data[4 * x + y * inIter.Stride + 2];
                        outIter.Data[4 * (x - ox) + (y - oy) * outIter.Stride + 3] = inIter.Data[4 * x + y * inIter.Stride + 3];
                    }
                }
            }
        }

        internal bool Contains(Point point)
        {
            throw new NotImplementedException();
        }
    }
}