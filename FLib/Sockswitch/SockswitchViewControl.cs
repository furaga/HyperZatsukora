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
    /// <summary>
    /// センサーのデータを可視化する。足裏の画像に圧力分布を重ねるかセンサデータの時系列データをグラフで表示
    /// </summary>
    public partial class SockswitchViewControl : UserControl
    {
        public enum RenderMode
        {
            Planter, RawSensor, Baseline, Window,
            Diff,
        }

        // センサ
        SockswitchSensor sensor;

        // 圧力
        public List<float> sensorForces = new List<float>();
        public List<Point> sensorPositions = new List<Point>();
        public List<int> sensorTopin = new List<int>();
        
        // 可視化モード
        RenderMode renderMode = RenderMode.Planter;
        
        // 足裏モード
        Bitmap planterImage;
        Bitmap heatMap;
        
        // 時系列モード
        List<Point>[] timeline = null;
        bool[] filter; // 各センサのデータを表示するか
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
        
        // 表示位置
        int currentFrameIdx = 0;
        
        // 選択位置
        public int selectionStart = -1;
        public int selectionEnd = -1;
        
        // SVM
        SockswitchSVM svm;

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

        /// <summary>
        /// 
        /// </summary>
        public SockswitchViewControl()
        {
            InitializeComponent();
            try
            {
                // PlanterImage = new Bitmap("Resources/planter4uno.png");
                PlanterImage = new Bitmap("Resources/planter4fio.png");
                heatMap = new Bitmap("Resources/heatmap.png");
                canvas.Invalidate();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + ":" + ex.StackTrace);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        unsafe public void LoadSensorPositions()
        {
            sensorPositions.Clear();
            sensorForces.Clear();

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
                            sensorPositions.Add(new Point(x, y));
                            sensorForces.Add(0);
                        }
                    }
                }
            }

            int n = sensorPositions.Count;
            for (int i = 0; i < n; i++)
            {
                sensorPositions.Add(new Point(2 * planterImage.Width - sensorPositions[i].X, sensorPositions[i].Y));
                sensorForces.Add(0);
            }
            for (int i = 0; i < sensorPositions.Count; i++)
            {
                sensorTopin.Add(i);
            }

            Debug.Assert(sensorPositions.Count == 10 || sensorPositions.Count == 16);
            Debug.Assert(sensorForces.Count == sensorPositions.Count);
        }

        public void LoadPinSensorCorrespondence(string filepath)
        {
            sensorTopin = DeserializeSensorToPin(System.IO.File.ReadAllLines(filepath));
        }

        List<List<double>[]> raw_waves_buffer = new List<List<double>[]>();
        List<List<double[]>> baselines_buffer = new List<List<double[]>>();
        public void UpdateWindowFunction(SockswitchSensor sensor, SockswitchSVM svm = null)
        {
            var raw_waves = sensor.GetPressureDataRange(0, sensor.FrameCount - 1);
            raw_waves_buffer.Add(raw_waves);
            var _b = new List<double[]>();
            for (int i = 0; i < raw_waves[0].Count; i++)
            {
                _b.Add(svm.Baselines(raw_waves, i, 5, 0.1));
                svm.Window(raw_waves, svm.baselines, i, 10, 4);
            }
            baselines_buffer.Add(_b);
        }

        public void UpdateSensorView(SockswitchSensor sensor, int frameIdx, RenderMode renderMode = RenderMode.Planter, SockswitchSVM svm = null, List<CheckBox> filterCB = null)
        {
            if (sensor == null) return;

            long timeStamp;
            float[] data = sensor.GetPressureData(frameIdx, out timeStamp);
            if (data != null)
            {
                for (int i = 0; i < Math.Min(data.Length, sensorForces.Count); i++)
                {
                    sensorForces[i] = data[i];
                }
            }

            this.sensor = sensor;
            this.currentFrameIdx = frameIdx;
            this.renderMode = renderMode;
            this.svm = svm;

            // フィルターの更新
            if (this.filter == null)
            {
                this.filter = new bool[sensor.sensorNum];
                for (int i = 0; i < this.filter.Length; i++)
                {
                    this.filter[i] = true;
                }
            }
            if (filterCB != null)
            {
                for (int i = 0; i < this.filter.Length; i++)
                {
                    this.filter[i] = filterCB[i].Checked;
                }
            }

            canvas.Invalidate();
        }


        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                if (renderMode == RenderMode.Planter) DrawPlanter(e.Graphics, filter);
                else DrawSensorData(e.Graphics, filter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex + ":" + ex.StackTrace);
            }
        }

        void DrawPlanter(Graphics g, bool[] filter)
        {
            if (planterImage == null)
            {
                return;
            }
            float ratio = (float)canvas.Height / planterImage.Height;
            int w = (int)(planterImage.Width * ratio);
            int h = canvas.Height;
            int r = (int)(10 * ratio);
            g.Clear(Color.White);
            g.DrawImage(planterImage, new Rectangle(0, 0, w, h));
            g.DrawImage(planterImage, new Rectangle(2 * w, 0, -w, h));
            for (int i = 0; i < sensorPositions.Count; i++)
            {
                int pinId = sensorTopin[i];
                int x = (int)(ratio * sensorPositions[i].X);
                int y = (int)(ratio * sensorPositions[i].Y);
                if (filter == null || filter[pinId])
                {
                    int val = pinId < 0 || sensorForces.Count <= pinId ?
                        0 :
                        (int)Math.Max(0, Math.Min(254, 100 * 255 * sensorForces[pinId]));
                    g.FillEllipse(
                        new SolidBrush(heatMap.GetPixel(val, heatMap.Height / 2)),
                        new Rectangle(x - r, y - r, 2 * r, 2 * r));
                }
                else
                {
                    g.FillEllipse(
                        Brushes.LightGray,
                        new Rectangle(x - r, y - r, 2 * r, 2 * r));
                }
            }
        }

        enum TimelineDrawPolicy
        {
            Top,
            Bottom,
            Full
        }

        Point GetTimelinePoint(TimelineDrawPolicy policy,  double x, double y)
        {
            float ox, oy, w, h;
            switch (policy)
            {
                case TimelineDrawPolicy.Top:
                    ox = 0;
                    oy = 10;
                    w = canvas.Width;
                    h = canvas.Height / 2 - 20;
                    break;
                case TimelineDrawPolicy.Bottom:
                    ox = 0;
                    oy = canvas.Height / 2 + 10;
                    w = canvas.Width;
                    h = canvas.Height / 2 - 20;
                    break;
                default:
                    ox = 0;
                    oy = 10;
                    w = canvas.Width;
                    h = canvas.Height - 20;
                    break;
            }

            x = Math.Min(1, Math.Max(0, x));
            y = Math.Min(1, Math.Max(0, y));

            int ptX = (int)(ox + w * x);
            int ptY = (int)(oy + h - h * y);

            return new Point(ptX, ptY);
        }

        void DrawSensorData(Graphics g, bool[] filter)
        {
            var sw = Stopwatch.StartNew();

            if (sensor == null) return;
            if (svm == null) return;

            //            float ratio = (float)pictureBox2.Height / planterImage.Height;
            float w = canvas.Width;
            float h = canvas.Height;

            g.Clear(Color.Black);
            timeline = new List<Point>[sensorForces.Count];

            for (int i = 0; i < timeline.Length; i++) timeline[i] = new List<Point>();

            const int viewSpan = 100;
            var raw_waves = sensor.GetPressureDataRange(Math.Max(0, currentFrameIdx - viewSpan), Math.Min(sensor.FrameCount - 1, currentFrameIdx));
            if (raw_waves == null) return;

            switch (renderMode)
            {
                case RenderMode.RawSensor:
                    for (int i = 0; i < raw_waves.Length; i++)
                    {
                        int pinId = sensorTopin[i];
                        var raw_wave = raw_waves[pinId];
                        for (int j = 0; j < raw_waves[i].Count; j++)
                        {
                            var policy = pinId < sensor.sensorNumPerFoot ? TimelineDrawPolicy.Top : TimelineDrawPolicy.Bottom;
                            double x = (double)(viewSpan - raw_waves[i].Count + j) / viewSpan;
                            var pt = GetTimelinePoint(policy, x, raw_wave[j]);
                            timeline[i].Add(pt);
                        }
                    }
                    break;
                case RenderMode.Baseline:
                    for (int i = 0; i < raw_waves.Length; i++)
                    {
                        int pinId = sensorTopin[i];
                        for (int j = 0; j < raw_waves[pinId].Count; j++)
                        {
                            int idx = currentFrameIdx - viewSpan + j;
                            double x = (double)(viewSpan - raw_waves[i].Count + j) / viewSpan;
                            double y = 0 <= idx && idx < svm.window.Count ? svm.baselines[idx][pinId] : 0;
                            var policy = pinId < sensor.sensorNumPerFoot ? TimelineDrawPolicy.Top : TimelineDrawPolicy.Bottom;
                            var pt = GetTimelinePoint(policy, x, y);
                            timeline[i].Add(pt);
                        }
                    }
                    break;
                case RenderMode.Diff:
                    for (int i = 0; i < raw_waves.Length; i++)
                    {
                        int pinId = sensorTopin[i];
                        for (int j = 0; j < raw_waves[i].Count; j++)
                        {
                            int idx = currentFrameIdx - viewSpan + j;
                            double x = (double)(viewSpan - raw_waves[i].Count + j) / viewSpan;
                            double y = 0 <= idx && idx < svm.window.Count ? (raw_waves[pinId][j] - svm.baselines[idx][pinId]) : 0;
                            var policy = pinId < sensor.sensorNumPerFoot ? TimelineDrawPolicy.Top : TimelineDrawPolicy.Bottom;
                            var pt = GetTimelinePoint(policy, x, y);
                            timeline[i].Add(pt);
                        }
                    }
                    break;
                case RenderMode.Window:
                    // SVMの窓関数
                    for (int j = 0; j < raw_waves[0].Count - 1; j++)
                    {
                        int pinId = sensorTopin[0];
                        int idx = currentFrameIdx - raw_waves[pinId].Count + j;
                        double x = (double)(viewSpan - raw_waves[pinId].Count + j) / viewSpan;
                        double y = 0 <= idx && idx < svm.window.Count ? svm.window[idx] : 0;
                        var pt = GetTimelinePoint(TimelineDrawPolicy.Full, x, y);
                        timeline[0].Add(pt);
                        timeline[8].Add(pt);
                    }
                    break;
            }

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 目盛り（横）
            Pen scalePen = new Pen(Brushes.Gray, 1);
            for (int i = 0; i <= 10; i++)
            {
                if (renderMode != RenderMode.Window)
                {
                    var pt0 = GetTimelinePoint(TimelineDrawPolicy.Top, 0, 0.1 * i);
                    var pt1 = GetTimelinePoint(TimelineDrawPolicy.Top, 1, 0.1 * i);
                    g.DrawLine(scalePen, pt0, pt1);
                    var pt2 = GetTimelinePoint(TimelineDrawPolicy.Bottom, 0, 0.1 * i);
                    var pt3 = GetTimelinePoint(TimelineDrawPolicy.Bottom, 1, 0.1 * i);
                    g.DrawLine(scalePen, pt2, pt3);
                }
                else
                {
                    var pt0 = GetTimelinePoint(TimelineDrawPolicy.Full, 0, 0.1 * i);
                    var pt1 = GetTimelinePoint(TimelineDrawPolicy.Full, 1, 0.1 * i);
                    g.DrawLine(scalePen, pt0, pt1);
                }
            }
            // 目盛り（縦）
            int _idx = currentFrameIdx / 10 * 10;
            while (true)
            {
                // TODO
                int i = currentFrameIdx - _idx;
                if (i > viewSpan) break;
                double x = (double)(viewSpan - i) / viewSpan;
                if (renderMode != RenderMode.Window)
                {
                    var pt0 = GetTimelinePoint(TimelineDrawPolicy.Top, x, 0);
                    var pt1 = GetTimelinePoint(TimelineDrawPolicy.Top, x, 1);
                    g.DrawLine(scalePen, pt0, pt1);
                    var pt2 = GetTimelinePoint(TimelineDrawPolicy.Bottom, x, 0);
                    var pt3 = GetTimelinePoint(TimelineDrawPolicy.Bottom, x, 1);
                    g.DrawLine(scalePen, pt2, pt3);
                }
                else
                {
                    var pt0 = GetTimelinePoint(TimelineDrawPolicy.Full, x, 0);
                    var pt1 = GetTimelinePoint(TimelineDrawPolicy.Full, x, 1);
                    g.DrawLine(scalePen, pt0, pt1);
                }
                _idx -= 10;
            }

            // 窓
            var windowBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255));
            var selectedBrush = new SolidBrush(Color.FromArgb(100, 255, 0, 0));
            var frames = svm.GetAllFrames();
            foreach (var frame in frames) 
            {
                HighlightSpan(g, windowBrush, frame, viewSpan, 0);
            }
            // 選択範囲
            if (selectionStart >= 0)
            {
                CharacterRange range = selectionEnd < 0 ?
                    new CharacterRange(selectionStart, 1):
                    new CharacterRange(selectionStart, selectionEnd - selectionStart);
                HighlightSpan(g, selectedBrush, range, viewSpan, 0);
            }
            // 時系列グラフ
            for (int i = 0; i < timeline.Length; i++)
            {
                int pinId = sensorTopin[i];
                if (filter != null && pinId < filter.Length && filter[pinId])
                {
                    if (timeline[pinId] != null && timeline[pinId].Count >= 3)
                    {
                        g.DrawLines(new Pen(sensorDataBrushes[pinId % sensor.sensorNumPerFoot], 2), timeline[pinId].ToArray());
                    }
                }
            }
        }

        void HighlightSpan(Graphics g, Brush brush, CharacterRange range, int viewSpan, int margin)
        {
            int start = range.First - margin;
            int end = range.First + range.Length + margin;
            double _x0 = (start - currentFrameIdx + viewSpan) / (double)viewSpan;
            double _x1 = (end - currentFrameIdx + viewSpan) / (double)viewSpan;
            int x0 = GetTimelinePoint(TimelineDrawPolicy.Full, _x0, 0).X;
            int x1 = GetTimelinePoint(TimelineDrawPolicy.Full, _x1, 0).X;
            if (renderMode != RenderMode.Window)
            {
                int y0 = GetTimelinePoint(TimelineDrawPolicy.Top, 0, 0).Y;
                int y1 = GetTimelinePoint(TimelineDrawPolicy.Top, 0, 1).Y;
                int y2 = GetTimelinePoint(TimelineDrawPolicy.Bottom, 0, 0).Y;
                int y3 = GetTimelinePoint(TimelineDrawPolicy.Bottom, 0, 1).Y;
                Rectangle rect0 = new Rectangle(x0, y1, x1 - x0, y0 - y1);
                Rectangle rect1 = new Rectangle(x0, y3, x1 - x0, y2 - y3);
                g.FillRectangle(brush, rect0);
                g.FillRectangle(brush, rect1);
            }
            else
            {
                int y0 = GetTimelinePoint(TimelineDrawPolicy.Full, 0, 0).Y;
                int y1 = GetTimelinePoint(TimelineDrawPolicy.Full, 0, 1).Y;
                Rectangle rect = new Rectangle(x0, y1, x1 - x0, y0 - y1);
                g.FillRectangle(brush, rect);
            }
        }
        
        public int PointToPin(Point ptInCanvas)
        {
            int sensorId = PointToSensor(ptInCanvas);
            if (0 <= sensorId && sensorId < sensorTopin.Count)
            {
                return sensorTopin[sensorId];
            }
            return -1;
        }

        public int PointToSensor(Point ptInCanvas)
        {
            if (planterImage != null)
            {
                float ratio = (float)canvas.Height / planterImage.Height;
                int w = (int)(planterImage.Width * ratio);
                int h = canvas.Height;
                int r = (int)(10 * ratio);
                for (int i = 0; i < sensorPositions.Count; i++)
                {
                    int x = (int)(ratio * sensorPositions[i].X);
                    int y = (int)(ratio * sensorPositions[i].Y);
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

        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            if (timeline == null) return;
            canvas.Focus();

            if (e.Button == System.Windows.Forms.MouseButtons.Left && renderMode != RenderMode.Planter)
            {
                int x = e.Location.X;
                for (int i = 0; i < timeline[0].Count - 1; i++)
                {
                    float px = timeline[0][i].X;
                    float cx = timeline[0][i + 1].X;
                    if (px <= x && x < cx)
                    {
                        if (selectionStart <= -1)
                        {
                            selectionStart = currentFrameIdx - timeline[0].Count + i;
                        }
                        else if (selectionEnd <= -1)
                        {
                            int t0 = selectionStart;
                            int t1 = currentFrameIdx - timeline[0].Count + i;
                            selectionStart = Math.Min(t0, t1);
                            selectionEnd = Math.Max(t0, t1);
                        }
                        else
                        {
                            selectionStart = selectionEnd = -1;
                        }
                        Parent.Text = "[" + selectionStart + "," + selectionEnd + "]";
                    }
                }
                canvas.Invalidate();
            }
            if (e.Button == System.Windows.Forms.MouseButtons.Right && svm != null)
            {
                int x = e.Location.X;
                var frames = svm.GetAllFrames();
                for (int i = 0; i < frames.Count; i++)
                {
                    int idx0 = frames[i].First - (currentFrameIdx - timeline[8].Count);
                    int idx1 = frames[i].First + frames[i].Length - (currentFrameIdx - timeline[8].Count);
                    if (0 <= idx0 && idx1 < timeline[8].Count)
                    {
                        int left = timeline[8][idx0].X;
                        int right = timeline[8][idx1].X;
                        if (left <= x && x <= right)
                        {
                            selectionStart = frames[i].First;
                            selectionEnd = frames[i].First + frames[i].Length;
                            var p = Parent;
                            while (!(p is Form)) p = p.Parent; 
                            p.Text = "[" + selectionStart + "," + selectionEnd + "]";
                            canvas.Invalidate();
                            break;
                        }
                    }
                }
            }
        }
    }
}
