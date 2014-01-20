using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using FLib;

namespace HumanComposer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void partsListView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // ドラッグ中のファイルやディレクトリの取得
                string[] drags = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string d in drags)
                {
                    if (!System.IO.File.Exists(d))
                    {
                        // ファイル以外であればイベント・ハンドラを抜ける
                        return;
                    }
                }
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void partsListView_DragDrop(object sender, DragEventArgs e)
        {
            // ドラッグ＆ドロップされたファイル
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    Bitmap bmp = new Bitmap(files[i]);
                    Global.partsList[files[i]] = bmp;
                    partsList.Images.Add(files[i], BitmapHandler.CreateThumbnail(bmp, partsList.ImageSize.Width, partsList.ImageSize.Height));
                    partsListView.Items.Add(Path.GetFileName(files[i]), files[i]);
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.ToString());
                }
            }
        }
    }
}
