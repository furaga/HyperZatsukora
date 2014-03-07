using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace FLib
{
    public partial class SockswitchViewControl : UserControl
    {
        public List<Point> SensorPositions = new List<Point>();
        public List<float> SensorForces = new List<float>();
        Bitmap planterImage;
        Bitmap heatMap;
        public List<int> sensorTopin = new List<int>();

        public Bitmap PlanterImage
        {
            get
            {
                return planterImage;
            }
            set
            {
                if (value != null) planterImage = value;

            }
        }

        public SockswitchViewControl()
        {
            InitializeComponent();
            try
            {
                MouseWheel += new MouseEventHandler(SockswitchViewControl_MouseWheel);

                //                PlanterImage = new Bitmap("Resources/planter4uno.png");
                PlanterImage = new Bitmap("Resources/planter4fio.png");
                heatMap = new Bitmap("Resources/heatmap.png");
                pictureBox2.Invalidate();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + ":" + ex.StackTrace);
            }
        }

        PointF graphRatio = new Point(1, 1);

        void SockswitchViewControl_MouseWheel(object sender, MouseEventArgs e)
        {
            int sign = e.Delta > 0 ? 1 : -1;
            graphRatio.X += 0.1f * sign;
            graphRatio.Y += 0.1f * sign;
            pictureBox2.Invalidate();
        }

        private void SockswitchViewControl_Load(object sender, EventArgs e)
        {

        }


        unsafe public void LoadSensorPositions()
        {
            SensorPositions.Clear();
            SensorForces.Clear();

            if (planterImage == null) return;

            using (BitmapIterator iter = new BitmapIterator(planterImage, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            {
                byte* data = (byte*)iter.PixelData;
                for (int y = 0; y < planterImage.Height; y++)
                {
                    for (int x = 0; x < planterImage.Width; x++)
                    {
                        int idx = 3 * x + y * iter.Stride;
                        if (data[idx + 2] >= 230 && data[idx + 1] <= 40 && data[idx] <= 40)
                        {
                            SensorPositions.Add(new Point(x, y));
                            SensorForces.Add(0);
                        }
                    }
                }
            }

            int n = SensorPositions.Count;
            for (int i = 0; i < n; i++)
            {
                SensorPositions.Add(new Point(2 * planterImage.Width - SensorPositions[i].X, SensorPositions[i].Y));
                SensorForces.Add(0);
            }
            for (int i = 0; i < SensorPositions.Count; i++)
            {
                sensorTopin.Add(i);
            }

            Debug.Assert(SensorPositions.Count == 10 || SensorPositions.Count == 16);
            Debug.Assert(SensorForces.Count == SensorPositions.Count);
        }

        public void LoadPinSensorCorrespondence(string filepath)
        {
            sensorTopin = DeserializeSensorToPin(System.IO.File.ReadAllLines(filepath));
        }

        int frameIdx = 0;
        SockswitchSensor sensor;
        public enum RenderMode
        {
            Planter, RawSensor, Mean, Div, MeanDiv, Window,
        }
        RenderMode renderMode = RenderMode.Planter;
        List<double[]> baselines = null;
        List<double>[] normalized_waves= null;

        public void UpdateSensorForces(SockswitchSensor sensor, int frameIdx, RenderMode renderMode = RenderMode.Planter)
        {
            if (sensor == null) return;

            if (sensor != this.sensor)
            {
                var raw_waves = sensor.GetPressureDataRange(0, sensor.FrameCount - 1);
                baselines = SockswitchSVM.Baselines(raw_waves, 5, 0.03);
                normalized_waves = new List<double>[raw_waves.Length];
                for (int j = 0; j < baselines[0].Length; j++)
                {
                    normalized_waves[j] = new List<double>();
                }
                for (int i = 0; i < baselines.Count; i++)
                {
                    for (int j = 0; j < baselines[i].Length; j++)
                    {
                        normalized_waves[j].Add(raw_waves[j][i] - baselines[i][j]);
                    }
                }
            }

            long timeStamp;
            float[] data = sensor.GetPressureData(frameIdx, out timeStamp);
            if (data != null)
            {
                for (int i = 0; i < Math.Min(data.Length, SensorForces.Count); i++)
                {
                    SensorForces[i] = data[i];
                }
            }

            // ?
            this.sensor = sensor;
            this.frameIdx = frameIdx;
            this.renderMode = renderMode;

            pictureBox2.Invalidate();
        }

        System.Drawing.Drawing2D.Matrix transform = new System.Drawing.Drawing2D.Matrix();

        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                e.Graphics.Transform = transform;
                if (renderMode == RenderMode.Planter) DrawPlanter(e.Graphics);
                else DrawSensorData(e.Graphics);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex + ":" + ex.StackTrace);
            }
        }


        void DrawPlanter(Graphics g)
        {
            if (planterImage == null)
            {
                return;
            }
            float ratio = (float)pictureBox2.Height / planterImage.Height;
            int w = (int)(planterImage.Width * ratio);
            int h = pictureBox2.Height;
            int r = (int)(10 * ratio);
            g.Clear(Color.White);
            g.DrawImage(planterImage, new Rectangle(0, 0, w, h));
            g.DrawImage(planterImage, new Rectangle(2 * w, 0, -w, h));
            for (int i = 0; i < SensorPositions.Count; i++)
            {
                int x = (int)(ratio * SensorPositions[i].X);
                int y = (int)(ratio * SensorPositions[i].Y);
                int val = sensorTopin[i] < 0 || SensorForces.Count <= sensorTopin[i] ? 0 :
                    (int)Math.Max(0, Math.Min(254,  100 * 255 * SensorForces[sensorTopin[i]]));
                g.FillEllipse(
                    new SolidBrush(heatMap.GetPixel(val, heatMap.Height / 2)),
                    new Rectangle(x - r, y - r, 2 * r, 2 * r));
            }
        }

        Brush[] sensorDataBrushes = new Brush[] {
            Brushes.Red,
            Brushes.Yellow,
            Brushes.Orange,
            Brushes.Green,
            Brushes.Cyan,
            Brushes.Blue,
            Brushes.SkyBlue,
            Brushes.White,
        };
        List<Point>[] graph = null;

        void DrawSensorData(Graphics g)
        {
            var sw = Stopwatch.StartNew();

            if (sensor == null) return;

            //            float ratio = (float)pictureBox2.Height / planterImage.Height;
            float w = pictureBox2.Width * graphRatio.X;
            float h = pictureBox2.Height * graphRatio.Y;

            g.Clear(Color.Black);
            graph = new List<Point>[SensorForces.Count];

            for (int i = 0; i < graph.Length; i++) graph[i] = new List<Point>();

            const int span = 100;
            long ot = -1;
            var raw_waves = sensor.GetPressureDataRange(Math.Max(0, frameIdx - span), Math.Min(sensor.FrameCount - 1, frameIdx));
            switch (renderMode)
            {
                case RenderMode.RawSensor:
                    for (int i = 0; i < raw_waves.Length; i++)
                    {
                        for (int j = 0; j < raw_waves[i].Count; j++)
                        {
                            graph[i].Add(new Point((int)(w * j / (double)raw_waves[i].Count), (int)(h - h * raw_waves[i][j])));
                        }
                    }
                    break;
                case RenderMode.Window:
                    // SVMの窓関数
                    // TODO
                    var window = SockswitchSVM.Window(normalized_waves, 10, 4);
                    for (int j = 0; j < raw_waves[0].Count; j++)
                    {
                        int idx = Math.Max(0, frameIdx - span + j);
                        if (idx < window.Count)
                        {
                            var pt = new Point((int)(w * (10 + j) / (double)window.Count), (int)(h - h / 8 * window[idx]));
                            graph[0].Add(pt);
                            graph[8].Add(pt);
                        }
                    }
                    break;
                default:
                    for (int i = 0; i < raw_waves.Length; i++)
                    {
                        for (int j = 0; j < raw_waves[i].Count; j++)
                        {
                            double val = Math.Abs(normalized_waves[i][Math.Max(0, frameIdx - span) + j]);
                            graph[i].Add(new Point((int)(w * j / (double)raw_waves[i].Count), (int)(h - h * val)));
                        }
                    }
                    for (int i = 0; i < raw_waves.Length; i++)
                    {
                        for (int j = 0; j < raw_waves[i].Count; j++)
                        {
//                            graph[i].Add(new Point((int)(w * j / (double)raw_waves[i].Count), (int)(h - h * baselines[Math.Max(0, frameIdx - span) + j][i])));
                        }
                    }
                    break;
            }

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (selectionStart >= 0)
            {
                int idx0 = selectionStart - (frameIdx - graph[8].Count);
                int idx1 = (selectionEnd <= -1 ? selectionStart + 1 : selectionEnd) - (frameIdx - graph[8].Count);
                int x0 = graph[8][Math.Max(0, Math.Min(graph[8].Count - 1, idx0))].X;
                int x1 = graph[8][Math.Max(0, Math.Min(graph[8].Count - 1, idx1))].X;
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 255, 255, 255)), new Rectangle(x0, 0, x1 - x0, (int)h));
            }
            for (int i = 8; i < graph.Length; i++)
            {
                if (graph[i].Count >= 3)
                {
                    g.DrawLines(new Pen(sensorDataBrushes[i % sensor.sensorNumPerFoot], 2), graph[i].ToArray());
                }
            }
            g.DrawLine(new Pen(Brushes.Yellow),
                new Point(0, (int)(h * 8 * Baseline)),
                new Point(pictureBox2.Width, (int)(h * 8 * Baseline)));

            //            Console.WriteLine("draw: " + sw.ElapsedMilliseconds + " ms");
            //          sw.Restart();
        }

        public int PointToSensor(Point ptInCanvas)
        {
            if (planterImage != null)
            {
                float ratio = (float)pictureBox2.Height / planterImage.Height;
                int w = (int)(planterImage.Width * ratio);
                int h = pictureBox2.Height;
                int r = (int)(10 * ratio);
                for (int i = 0; i < SensorPositions.Count; i++)
                {
                    int x = (int)(ratio * SensorPositions[i].X);
                    int y = (int)(ratio * SensorPositions[i].Y);
                    float dx = ptInCanvas.X - x;
                    float dy = ptInCanvas.Y - y;
                    if (dx * dx + dy * dy < r * r)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public static string SerializeSensorToPin(List<int> sensorTopin)
        {
            string text = "sensor position id <=> analog pin id\n";
            for (int i = 0; i < sensorTopin.Count; i++)
            {
                text += i + " " + sensorTopin[i] + "\n";
            }
            return text;
        }
        public static List<int> DeserializeSensorToPin(string[] lines)
        {
            List<int> sensorTopin = new List<int>();
            int skipCount = 0;
            foreach (var line in lines)
            {
                skipCount++;
                if (line.Contains("sensor position id <=> analog pin id"))
                {
                    break;
                }
            }
            Debug.Assert(skipCount < lines.Length);
            foreach (var line in lines.Skip(skipCount)) sensorTopin.Add(-1);
            foreach (var line in lines.Skip(skipCount))
            {
                var tokens = line.Split(' ').Select(s => int.Parse(s)).ToArray();
                System.Diagnostics.Debug.Assert(tokens.Length == 2);
                sensorTopin[tokens[0]] = tokens[1];
            }
            return sensorTopin;
        }

        public void Save(string filepath)
        {
            System.IO.File.AppendAllText(filepath, SerializeSensorToPin(sensorTopin));
        }

        Point tmpMousePoint = Point.Empty;
        PointF prevTranslation = PointF.Empty;
        PointF translation = PointF.Empty;

        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            if (graph == null) return;
            pictureBox2.Focus();
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                tmpMousePoint = e.Location;
                prevTranslation = translation;
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                int x = e.Location.X;
                for (int i = 0; i < graph[0].Count - 1; i++)
                {
                    float px = graph[0][i].X;
                    float cx = graph[0][i + 1].X;
                    if (px <= x && x < cx)
                    {
                        if (selectionStart <= -1)
                        {
                            selectionStart = frameIdx - graph[0].Count + i;
                        }
                        else if (selectionEnd <= -1)
                        {
                            selectionEnd = frameIdx - graph[0].Count + i;
                        }
                        else
                        {
                            selectionStart = selectionEnd = -1;
                        }
                    }
                }
                pictureBox2.Invalidate();
            }
        }
       public int selectionStart = -1;
       public int selectionEnd = -1;

        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                int dx = e.Location.X - tmpMousePoint.X;
                int dy = e.Location.Y - tmpMousePoint.Y;
                transform.Translate(dx, dy, System.Drawing.Drawing2D.MatrixOrder.Append);
                tmpMousePoint = e.Location;
                pictureBox2.Invalidate();
            }
        }

        private void pictureBox2_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                int dx = e.Location.X - tmpMousePoint.X;
                int dy = e.Location.Y - tmpMousePoint.Y;
                transform.Translate(dx, dy, System.Drawing.Drawing2D.MatrixOrder.Append);
                tmpMousePoint = e.Location;
                pictureBox2.Invalidate();
            }
        }

        public double Baseline { get; set; }
    }
}
