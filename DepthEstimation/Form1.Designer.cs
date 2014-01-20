namespace DepthEstimation
{
    partial class Form1
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

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileFToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openOToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.calculateCToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.calculateCToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.viewVToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.labLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.edgeEToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.patchesPToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aggregatedPatchesAToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.regionMapRToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.inputCanvas = new DepthEstimation.DragablePictureBox();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.outputCanvas = new DepthEstimation.DragablePictureBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileFToolStripMenuItem,
            this.calculateCToolStripMenuItem,
            this.viewVToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(747, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileFToolStripMenuItem
            // 
            this.fileFToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openOToolStripMenuItem,
            this.saveSToolStripMenuItem});
            this.fileFToolStripMenuItem.Name = "fileFToolStripMenuItem";
            this.fileFToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.fileFToolStripMenuItem.Text = "File(&F)";
            // 
            // openOToolStripMenuItem
            // 
            this.openOToolStripMenuItem.Name = "openOToolStripMenuItem";
            this.openOToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openOToolStripMenuItem.Size = new System.Drawing.Size(213, 22);
            this.openOToolStripMenuItem.Text = "Open Image(&O)";
            this.openOToolStripMenuItem.Click += new System.EventHandler(this.openOToolStripMenuItem_Click);
            // 
            // saveSToolStripMenuItem
            // 
            this.saveSToolStripMenuItem.Name = "saveSToolStripMenuItem";
            this.saveSToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveSToolStripMenuItem.Size = new System.Drawing.Size(213, 22);
            this.saveSToolStripMenuItem.Text = "Save Results(&S)";
            this.saveSToolStripMenuItem.Click += new System.EventHandler(this.saveSToolStripMenuItem_Click);
            // 
            // calculateCToolStripMenuItem
            // 
            this.calculateCToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.calculateCToolStripMenuItem1});
            this.calculateCToolStripMenuItem.Name = "calculateCToolStripMenuItem";
            this.calculateCToolStripMenuItem.Size = new System.Drawing.Size(90, 20);
            this.calculateCToolStripMenuItem.Text = "Calculate(&C)";
            // 
            // calculateCToolStripMenuItem1
            // 
            this.calculateCToolStripMenuItem1.Name = "calculateCToolStripMenuItem1";
            this.calculateCToolStripMenuItem1.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.calculateCToolStripMenuItem1.Size = new System.Drawing.Size(191, 22);
            this.calculateCToolStripMenuItem1.Text = "Calculate(&C)";
            this.calculateCToolStripMenuItem1.Click += new System.EventHandler(this.calculateCToolStripMenuItem1_Click);
            // 
            // viewVToolStripMenuItem
            // 
            this.viewVToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.labLToolStripMenuItem,
            this.edgeEToolStripMenuItem,
            this.patchesPToolStripMenuItem,
            this.aggregatedPatchesAToolStripMenuItem,
            this.regionMapRToolStripMenuItem});
            this.viewVToolStripMenuItem.Name = "viewVToolStripMenuItem";
            this.viewVToolStripMenuItem.Size = new System.Drawing.Size(65, 20);
            this.viewVToolStripMenuItem.Text = "View(&V)";
            // 
            // labLToolStripMenuItem
            // 
            this.labLToolStripMenuItem.Name = "labLToolStripMenuItem";
            this.labLToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.labLToolStripMenuItem.Text = "Lab(&L)";
            this.labLToolStripMenuItem.Click += new System.EventHandler(this.labLToolStripMenuItem_Click);
            // 
            // edgeEToolStripMenuItem
            // 
            this.edgeEToolStripMenuItem.Name = "edgeEToolStripMenuItem";
            this.edgeEToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.edgeEToolStripMenuItem.Text = "Edge(&E)";
            this.edgeEToolStripMenuItem.Click += new System.EventHandler(this.edgeEToolStripMenuItem_Click);
            // 
            // patchesPToolStripMenuItem
            // 
            this.patchesPToolStripMenuItem.Name = "patchesPToolStripMenuItem";
            this.patchesPToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.patchesPToolStripMenuItem.Text = "Patches(&P)";
            this.patchesPToolStripMenuItem.Click += new System.EventHandler(this.patchesPToolStripMenuItem_Click);
            // 
            // aggregatedPatchesAToolStripMenuItem
            // 
            this.aggregatedPatchesAToolStripMenuItem.Name = "aggregatedPatchesAToolStripMenuItem";
            this.aggregatedPatchesAToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.aggregatedPatchesAToolStripMenuItem.Text = "AggregatedPatches(&A)";
            this.aggregatedPatchesAToolStripMenuItem.Click += new System.EventHandler(this.aggregatedPatchesAToolStripMenuItem_Click);
            // 
            // regionMapRToolStripMenuItem
            // 
            this.regionMapRToolStripMenuItem.Name = "regionMapRToolStripMenuItem";
            this.regionMapRToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.regionMapRToolStripMenuItem.Text = "Region Map(&R)";
            this.regionMapRToolStripMenuItem.Click += new System.EventHandler(this.regionMapRToolStripMenuItem_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.inputCanvas);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(747, 461);
            this.splitContainer1.SplitterDistance = 374;
            this.splitContainer1.TabIndex = 1;
            // 
            // inputCanvas
            // 
            this.inputCanvas.BackColor = System.Drawing.Color.White;
            this.inputCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.inputCanvas.Location = new System.Drawing.Point(0, 0);
            this.inputCanvas.Name = "inputCanvas";
            this.inputCanvas.Size = new System.Drawing.Size(374, 461);
            this.inputCanvas.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.outputCanvas);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.flowLayoutPanel1);
            this.splitContainer2.Size = new System.Drawing.Size(369, 461);
            this.splitContainer2.SplitterDistance = 365;
            this.splitContainer2.TabIndex = 0;
            // 
            // outputCanvas
            // 
            this.outputCanvas.BackColor = System.Drawing.Color.White;
            this.outputCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputCanvas.Location = new System.Drawing.Point(0, 0);
            this.outputCanvas.Name = "outputCanvas";
            this.outputCanvas.Size = new System.Drawing.Size(369, 365);
            this.outputCanvas.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(369, 92);
            this.flowLayoutPanel1.TabIndex = 0;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(747, 485);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Form1";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileFToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openOToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveSToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewVToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private DragablePictureBox inputCanvas;
        private DragablePictureBox outputCanvas;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ToolStripMenuItem labLToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem edgeEToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem regionMapRToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem calculateCToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem calculateCToolStripMenuItem1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.ToolStripMenuItem patchesPToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aggregatedPatchesAToolStripMenuItem;
    }
}

