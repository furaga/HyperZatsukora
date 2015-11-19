namespace FLib
{
    partial class TakeScreenshotForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // TakeScreenshotForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "TakeScreenshotForm";
            this.Opacity = 0.5D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "TakeScreenshotForm";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.TakeScreenshotForm_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.TakeScreenshotForm_Paint);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TakeScreenshotForm_KeyDown);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TakeScreenshotForm_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.BlackForm_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TakeScreenshotForm_MouseUp);
            this.ResumeLayout(false);

        }

        #endregion

    }
}