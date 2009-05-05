namespace rmainte4.Threading
{
    partial class TestWorkThreadPool
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
            this._label_Queued = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this._label_Scheduled = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this._label_Running = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this._label_Failed = new System.Windows.Forms.Label();
            this._label_Completed = new System.Windows.Forms.Label();
            this._textBox_MaxThread = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this._textBox_WorkItem = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this._progressBar_Completed = new System.Windows.Forms.ProgressBar();
            this._button_Execute = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // _label_Queued
            // 
            this._label_Queued.AutoSize = true;
            this._label_Queued.Location = new System.Drawing.Point(100, 50);
            this._label_Queued.Name = "_label_Queued";
            this._label_Queued.Size = new System.Drawing.Size(11, 12);
            this._label_Queued.TabIndex = 0;
            this._label_Queued.Text = "0";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 50);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "Queued";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 12);
            this.label2.TabIndex = 1;
            this.label2.Text = "Scheduled";
            // 
            // _label_Scheduled
            // 
            this._label_Scheduled.AutoSize = true;
            this._label_Scheduled.Location = new System.Drawing.Point(100, 25);
            this._label_Scheduled.Name = "_label_Scheduled";
            this._label_Scheduled.Size = new System.Drawing.Size(11, 12);
            this._label_Scheduled.TabIndex = 0;
            this._label_Scheduled.Text = "0";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(18, 77);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(46, 12);
            this.label3.TabIndex = 1;
            this.label3.Text = "Running";
            // 
            // _label_Running
            // 
            this._label_Running.AutoSize = true;
            this._label_Running.Location = new System.Drawing.Point(100, 77);
            this._label_Running.Name = "_label_Running";
            this._label_Running.Size = new System.Drawing.Size(11, 12);
            this._label_Running.TabIndex = 0;
            this._label_Running.Text = "0";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 104);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(36, 12);
            this.label4.TabIndex = 1;
            this.label4.Text = "Failed";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(18, 133);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(59, 12);
            this.label5.TabIndex = 1;
            this.label5.Text = "Completed";
            // 
            // _label_Failed
            // 
            this._label_Failed.AutoSize = true;
            this._label_Failed.Location = new System.Drawing.Point(100, 104);
            this._label_Failed.Name = "_label_Failed";
            this._label_Failed.Size = new System.Drawing.Size(11, 12);
            this._label_Failed.TabIndex = 0;
            this._label_Failed.Text = "0";
            // 
            // _label_Completed
            // 
            this._label_Completed.AutoSize = true;
            this._label_Completed.Location = new System.Drawing.Point(100, 133);
            this._label_Completed.Name = "_label_Completed";
            this._label_Completed.Size = new System.Drawing.Size(11, 12);
            this._label_Completed.TabIndex = 0;
            this._label_Completed.Text = "0";
            // 
            // _textBox_MaxThread
            // 
            this._textBox_MaxThread.Location = new System.Drawing.Point(14, 204);
            this._textBox_MaxThread.Name = "_textBox_MaxThread";
            this._textBox_MaxThread.Size = new System.Drawing.Size(100, 19);
            this._textBox_MaxThread.TabIndex = 2;
            this._textBox_MaxThread.Text = "25";
            this._textBox_MaxThread.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(16, 186);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(75, 12);
            this.label6.TabIndex = 3;
            this.label6.Text = "最大スレッド数";
            // 
            // _textBox_WorkItem
            // 
            this._textBox_WorkItem.Location = new System.Drawing.Point(120, 204);
            this._textBox_WorkItem.Name = "_textBox_WorkItem";
            this._textBox_WorkItem.Size = new System.Drawing.Size(100, 19);
            this._textBox_WorkItem.TabIndex = 2;
            this._textBox_WorkItem.Text = "500";
            this._textBox_WorkItem.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(118, 186);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(124, 12);
            this.label7.TabIndex = 3;
            this.label7.Text = "生成するワークアイテム数";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this._progressBar_Completed);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this._label_Queued);
            this.groupBox1.Controls.Add(this._label_Running);
            this.groupBox1.Controls.Add(this._label_Failed);
            this.groupBox1.Controls.Add(this._label_Completed);
            this.groupBox1.Controls.Add(this._label_Scheduled);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Location = new System.Drawing.Point(18, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(429, 153);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Stats";
            // 
            // _progressBar_Completed
            // 
            this._progressBar_Completed.Location = new System.Drawing.Point(153, 124);
            this._progressBar_Completed.Name = "_progressBar_Completed";
            this._progressBar_Completed.Size = new System.Drawing.Size(100, 23);
            this._progressBar_Completed.TabIndex = 2;
            this._progressBar_Completed.Visible = false;
            // 
            // _button_Execute
            // 
            this._button_Execute.Location = new System.Drawing.Point(271, 200);
            this._button_Execute.Name = "_button_Execute";
            this._button_Execute.Size = new System.Drawing.Size(75, 23);
            this._button_Execute.TabIndex = 5;
            this._button_Execute.Text = "実行";
            this._button_Execute.UseVisualStyleBackColor = true;
            this._button_Execute.Click += new System.EventHandler(this._button_Execute_Click);
            // 
            // TestWorkThreadPool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(459, 266);
            this.Controls.Add(this._button_Execute);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this._textBox_WorkItem);
            this.Controls.Add(this._textBox_MaxThread);
            this.Name = "TestWorkThreadPool";
            this.Text = "TestWorkThreadPool";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _label_Queued;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label _label_Scheduled;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label _label_Running;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label _label_Failed;
        private System.Windows.Forms.Label _label_Completed;
        private System.Windows.Forms.TextBox _textBox_MaxThread;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox _textBox_WorkItem;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button _button_Execute;
        private System.Windows.Forms.ProgressBar _progressBar_Completed;
    }
}