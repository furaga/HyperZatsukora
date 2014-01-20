namespace HyperZatsukora
{
    partial class SourceImageControl
    {
        /// <summary> 
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region コンポーネント デザイナーで生成されたコード

        /// <summary> 
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を 
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.SourceImageView = new System.Windows.Forms.ListView();
            this.SourceImageEditor = new System.Windows.Forms.PictureBox();
            this.SourceImageList = new System.Windows.Forms.ImageList(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SourceImageEditor)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.SourceImageView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.SourceImageEditor);
            this.splitContainer1.Size = new System.Drawing.Size(588, 491);
            this.splitContainer1.SplitterDistance = 102;
            this.splitContainer1.TabIndex = 0;
            // 
            // SourceImageView
            // 
            this.SourceImageView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SourceImageView.Location = new System.Drawing.Point(0, 0);
            this.SourceImageView.Name = "SourceImageView";
            this.SourceImageView.Size = new System.Drawing.Size(588, 102);
            this.SourceImageView.TabIndex = 0;
            this.SourceImageView.UseCompatibleStateImageBehavior = false;
            this.SourceImageView.SelectedIndexChanged += new System.EventHandler(this.SourceImageList_SelectedIndexChanged);
            // 
            // SourceImageEditor
            // 
            this.SourceImageEditor.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.SourceImageEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SourceImageEditor.Location = new System.Drawing.Point(0, 0);
            this.SourceImageEditor.Name = "SourceImageEditor";
            this.SourceImageEditor.Size = new System.Drawing.Size(588, 385);
            this.SourceImageEditor.TabIndex = 0;
            this.SourceImageEditor.TabStop = false;
            // 
            // SourceImageList
            // 
            this.SourceImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth24Bit;
            this.SourceImageList.ImageSize = new System.Drawing.Size(16, 16);
            this.SourceImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // SourceImageControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "SourceImageControl";
            this.Size = new System.Drawing.Size(588, 491);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SourceImageEditor)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListView SourceImageView;
        private System.Windows.Forms.PictureBox SourceImageEditor;
        private System.Windows.Forms.ImageList SourceImageList;
    }
}
