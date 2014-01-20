using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DepthEstimation
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

       }

        private void openOToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Filter = "画像ファイル|*.png;*.bmp;*.jpg";
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Bitmap bmp = new Bitmap(openFileDialog1.FileName);
                inputCanvas.Bmp = bmp;
                inputCanvas.Draw();

                // 計算
                est = new DepthEstimator(inputCanvas.Bmp);
                est.Edging();
                est.Regions();
                outputCanvas.Bmp = est.Region;
                outputCanvas.Draw();
            }
        }

        DepthEstimator est;
        private void calculateCToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            est = new DepthEstimator(inputCanvas.Bmp);
            est.Edging();
            est.Regions();
            outputCanvas.Bmp = est.AggregatedPatches;
            outputCanvas.Draw();

            // EdgeDetection
            // RegionMap
            // RegionCorrespondence
        }

        private void labLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (est != null)
            {
                outputCanvas.Bmp = est.Lab;
                outputCanvas.Draw();
            }
        }

        private void edgeEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (est != null)
            {
                outputCanvas.Bmp = est.Edge;
                outputCanvas.Draw();
            }
        }

        private void patchesPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (est != null)
            {
                outputCanvas.Bmp = est.Patches;
                outputCanvas.Draw();
            }
        }

        private void aggregatedPatchesAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (est != null)
            {
                outputCanvas.Bmp = est.AggregatedPatches;
                outputCanvas.Draw();
            }
        }

        private void saveSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (est == null) return;
            string dir = string.Format("./{0:0000}{1:00}{2:00}{3:00}{4:00}{5:00}/",
                DateTime.Now.Year,
                DateTime.Now.Month,
                DateTime.Now.Day,
                DateTime.Now.Hour,
                DateTime.Now.Minute,
                DateTime.Now.Second);
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            System.Diagnostics.Process.Start(System.IO.Path.GetFullPath(dir));
            est.Save(dir);
        }

        private void regionMapRToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (est != null)
            {
                outputCanvas.Bmp = est.Region;
                outputCanvas.Draw();
            }
        }
    }
}
