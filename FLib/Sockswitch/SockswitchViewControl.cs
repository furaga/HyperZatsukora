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
        List<Point> SensorPositions = new List<Point>();
        List<float> SensorForces = new List<float>();
        Bitmap planterImage;
        Bitmap heatMap;

        public Bitmap PlanterImage
        {
            get
            {
                return planterImage;
            }
            set
            {
                if (value != null)
                {
                    planterImage = value;
                    UpdateSensorPositions(planterImage);
                }
            }
        }

        unsafe private void UpdateSensorPositions(Bitmap planterImage)
        {
            SensorPositions.Clear();
            SensorForces.Clear();

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

            Debug.Assert(SensorPositions.Count == 10 || SensorPositions.Count == 16);
            Debug.Assert(SensorForces.Count == SensorPositions.Count);
        }
        
        public SockswitchViewControl()
        {
            InitializeComponent();
            try
            {
                PlanterImage = new Bitmap("Resources/planter4uno.png");
                heatMap = new Bitmap("Resources/heatmap.png");
                pictureBox2.Invalidate();
            }
            catch (Exception ex)
            {

            }
        }

        private void SockswitchViewControl_Load(object sender, EventArgs e)
        {

        }

        public void UpdateSensorForces(SockswitchSensor sensor, int frameIdx)
        {
            long timeStamp;
            float[] data = sensor.GetPressureData(frameIdx, out timeStamp);
            if (data != null)
            {
                for (int i = 0; i < Math.Min(data.Length, SensorForces.Count); i++)
                {
                    SensorForces[i] = data[i];
                }
            }
            pictureBox2.Invalidate();
        }

        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                float ratio = (float)pictureBox2.Height / planterImage.Height;
                int w = (int)(planterImage.Width * ratio);
                int h = pictureBox2.Height;
                int r = (int)(10 * ratio);
                e.Graphics.Clear(Color.White);
                e.Graphics.DrawImage(planterImage, new Rectangle(0, 0, w, h));
                e.Graphics.DrawImage(planterImage, new Rectangle(2 * w, 0, -w, h));
                for (int i = 0; i < SensorPositions.Count; i++)
                {
                    int x = (int)(ratio * SensorPositions[i].X);
                    int y = (int)(ratio * SensorPositions[i].Y);
                    int val = (int)Math.Max(0, Math.Min(254, 255 * SensorForces[i]));
                    e.Graphics.FillEllipse(
                        new SolidBrush(heatMap.GetPixel(val, heatMap.Height / 2)),
                        new Rectangle(x - r, y - r, 2 * r, 2 * r));
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void dummySerialTimer_Tick(object sender, EventArgs e)
        {

        }
    }
}
