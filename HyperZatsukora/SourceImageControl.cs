using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HyperZatsukora
{
    public partial class SourceImageControl : UserControl
    {

        Dictionary<string, Bitmap> srcImages = new Dictionary<string,Bitmap>();



        public SourceImageControl()
        {
            InitializeComponent();
        }

        private void SourceImageList_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        public void Update()
        {
            SourceImageView.Items.Clear();

            for (int i = 0; i < srcImages.Count; i++)
            {
           //     SourceImageView.Items.Add(
            }
        }

        public void AddSourceImage(string key, Bitmap bmp)
        {
            if (srcImages.ContainsKey(key))
            {
                srcImages[key].Dispose();

            }
            srcImages[key] = new Bitmap(bmp);
            SourceImageList.Images.Add(key, bmp);
        }
    }
}
