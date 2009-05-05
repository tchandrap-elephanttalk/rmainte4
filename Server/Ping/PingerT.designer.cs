namespace rmainte4.PingMonitor
{
    partial class PingerT
    {
        /// <summary>
        /// 必要なデザイナ変数です。
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

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            rmainte4.TimelineControl.DefaultTimeScalePainter defaultTimeScalePainter1 = new rmainte4.TimelineControl.DefaultTimeScalePainter();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this._button_Add = new System.Windows.Forms.Button();
            this._textBox_Target = new System.Windows.Forms.TextBox();
            this._trackBar_Zoom = new System.Windows.Forms.TrackBar();
            this._timeline = new rmainte4.TimelineControl.Timeline();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._trackBar_Zoom)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this._trackBar_Zoom);
            this.splitContainer1.Panel1.Controls.Add(this._button_Add);
            this.splitContainer1.Panel1.Controls.Add(this._textBox_Target);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this._timeline);
            this.splitContainer1.Size = new System.Drawing.Size(551, 266);
            this.splitContainer1.SplitterDistance = 158;
            this.splitContainer1.TabIndex = 1;
            // 
            // _button_Add
            // 
            this._button_Add.Location = new System.Drawing.Point(30, 71);
            this._button_Add.Name = "_button_Add";
            this._button_Add.Size = new System.Drawing.Size(75, 23);
            this._button_Add.TabIndex = 1;
            this._button_Add.Text = "Add";
            this._button_Add.UseVisualStyleBackColor = true;
            this._button_Add.Click += new System.EventHandler(this._button_Add_Click);
            // 
            // _textBox_Target
            // 
            this._textBox_Target.Location = new System.Drawing.Point(30, 46);
            this._textBox_Target.Name = "_textBox_Target";
            this._textBox_Target.Size = new System.Drawing.Size(100, 19);
            this._textBox_Target.TabIndex = 0;
            this._textBox_Target.Text = "localhost";
            // 
            // _trackBar_Zoom
            // 
            this._trackBar_Zoom.Location = new System.Drawing.Point(60, 118);
            this._trackBar_Zoom.Name = "_trackBar_Zoom";
            this._trackBar_Zoom.Orientation = System.Windows.Forms.Orientation.Vertical;
            this._trackBar_Zoom.Size = new System.Drawing.Size(45, 104);
            this._trackBar_Zoom.TabIndex = 2;
            // 
            // _timeline
            // 
            this._timeline.AxisHeight = 20F;
            this._timeline.BackColor = System.Drawing.Color.Black;
            this._timeline.CurrentZoomFactorIndex = 0;
            this._timeline.Location = new System.Drawing.Point(16, 12);
            this._timeline.Name = "_timeline";
            this._timeline.OffsetTimelineFromAxis = true;
            this._timeline.Size = new System.Drawing.Size(352, 242);
            this._timeline.TabIndex = 0;
            defaultTimeScalePainter1.AlternatingColor1 = System.Drawing.Color.Gray;
            defaultTimeScalePainter1.AlternatingColor2 = System.Drawing.Color.DarkGray;
            defaultTimeScalePainter1.Left = 100F;
            defaultTimeScalePainter1.TextColor = System.Drawing.Color.White;
            defaultTimeScalePainter1.TimeFormat = "HH:mm:ss";
            defaultTimeScalePainter1.TimeZoneFont = new System.Drawing.Font("Tahoma", 8F);
            this._timeline.TimeScalePainter = defaultTimeScalePainter1;
            this._timeline.TimeZoneWidth = 50F;
            this._timeline.WorldEndTime = new System.DateTime(2009, 1, 31, 12, 0, 57, 515);
            this._timeline.WorldStartTime = new System.DateTime(2009, 1, 31, 11, 58, 57, 515);
            this._timeline.ZoomFactors = new double[] {
        0.016666666666666666,
        0.16666666666666666,
        0.5,
        1,
        2,
        4,
        8,
        10,
        12,
        15,
        20,
        25,
        30,
        60,
        120};
            // 
            // PingerT
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(551, 266);
            this.Controls.Add(this.splitContainer1);
            this.Name = "PingerT";
            this.Text = "Form1";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._trackBar_Zoom)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button _button_Add;
        private System.Windows.Forms.TextBox _textBox_Target;
        private System.Windows.Forms.TrackBar _trackBar_Zoom;
        private rmainte4.TimelineControl.Timeline _timeline;
    }
}

