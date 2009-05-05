namespace rmainte4
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this._splitContainer_Main = new System.Windows.Forms.SplitContainer();
            this._splitContainer_Node_H = new System.Windows.Forms.SplitContainer();
            this._splitContainer_Node_V = new System.Windows.Forms.SplitContainer();
            this._treeView_Node = new System.Windows.Forms.TreeView();
            this._listView_Node = new System.Windows.Forms.ListView();
            this.columnHeader7 = new System.Windows.Forms.ColumnHeader();
            this._panel_Property = new System.Windows.Forms.Panel();
            this._textBox_Hostname = new System.Windows.Forms.TextBox();
            this._textBox_Address = new System.Windows.Forms.TextBox();
            this._button_Unlock = new System.Windows.Forms.Button();
            this._button_Lock = new System.Windows.Forms.Button();
            this._flowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this._button_Register = new System.Windows.Forms.Button();
            this._tabControl_Log = new System.Windows.Forms.TabControl();
            this._tabPage_Log = new System.Windows.Forms.TabPage();
            this._tabPage_Scheduler = new System.Windows.Forms.TabPage();
            this._toolStrip = new System.Windows.Forms.ToolStrip();
            this._imageList = new System.Windows.Forms.ImageList(this.components);
            this._contextMenuStrip_TreeView = new System.Windows.Forms.ContextMenuStrip(this.components);
            this._contextMenuStrip_ListView = new System.Windows.Forms.ContextMenuStrip(this.components);
            this._statusStrip = new System.Windows.Forms.StatusStrip();
            this._toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this._listView_Log = new rmainte4.Controls.ListViewEx();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader4 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader5 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader6 = new System.Windows.Forms.ColumnHeader();
            this._splitContainer_Main.Panel1.SuspendLayout();
            this._splitContainer_Main.Panel2.SuspendLayout();
            this._splitContainer_Main.SuspendLayout();
            this._splitContainer_Node_H.Panel1.SuspendLayout();
            this._splitContainer_Node_H.Panel2.SuspendLayout();
            this._splitContainer_Node_H.SuspendLayout();
            this._splitContainer_Node_V.Panel1.SuspendLayout();
            this._splitContainer_Node_V.Panel2.SuspendLayout();
            this._splitContainer_Node_V.SuspendLayout();
            this._panel_Property.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this._tabControl_Log.SuspendLayout();
            this._tabPage_Log.SuspendLayout();
            this._statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // _splitContainer_Main
            // 
            this._splitContainer_Main.Dock = System.Windows.Forms.DockStyle.Fill;
            this._splitContainer_Main.Location = new System.Drawing.Point(0, 25);
            this._splitContainer_Main.Name = "_splitContainer_Main";
            this._splitContainer_Main.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // _splitContainer_Main.Panel1
            // 
            this._splitContainer_Main.Panel1.Controls.Add(this._splitContainer_Node_H);
            this._splitContainer_Main.Panel1.Controls.Add(this._button_Unlock);
            this._splitContainer_Main.Panel1.Controls.Add(this._button_Lock);
            this._splitContainer_Main.Panel1.Controls.Add(this._flowLayoutPanel);
            this._splitContainer_Main.Panel1.Controls.Add(this.groupBox1);
            // 
            // _splitContainer_Main.Panel2
            // 
            this._splitContainer_Main.Panel2.Controls.Add(this._tabControl_Log);
            this._splitContainer_Main.Size = new System.Drawing.Size(816, 561);
            this._splitContainer_Main.SplitterDistance = 382;
            this._splitContainer_Main.TabIndex = 5;
            // 
            // _splitContainer_Node_H
            // 
            this._splitContainer_Node_H.Location = new System.Drawing.Point(23, 14);
            this._splitContainer_Node_H.Name = "_splitContainer_Node_H";
            this._splitContainer_Node_H.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // _splitContainer_Node_H.Panel1
            // 
            this._splitContainer_Node_H.Panel1.Controls.Add(this._splitContainer_Node_V);
            // 
            // _splitContainer_Node_H.Panel2
            // 
            this._splitContainer_Node_H.Panel2.Controls.Add(this._panel_Property);
            this._splitContainer_Node_H.Size = new System.Drawing.Size(285, 338);
            this._splitContainer_Node_H.SplitterDistance = 241;
            this._splitContainer_Node_H.TabIndex = 0;
            // 
            // _splitContainer_Node_V
            // 
            this._splitContainer_Node_V.Dock = System.Windows.Forms.DockStyle.Fill;
            this._splitContainer_Node_V.Location = new System.Drawing.Point(0, 0);
            this._splitContainer_Node_V.Name = "_splitContainer_Node_V";
            // 
            // _splitContainer_Node_V.Panel1
            // 
            this._splitContainer_Node_V.Panel1.Controls.Add(this._treeView_Node);
            // 
            // _splitContainer_Node_V.Panel2
            // 
            this._splitContainer_Node_V.Panel2.Controls.Add(this._listView_Node);
            this._splitContainer_Node_V.Size = new System.Drawing.Size(285, 241);
            this._splitContainer_Node_V.SplitterDistance = 119;
            this._splitContainer_Node_V.TabIndex = 0;
            // 
            // _treeView_Node
            // 
            this._treeView_Node.Dock = System.Windows.Forms.DockStyle.Fill;
            this._treeView_Node.Location = new System.Drawing.Point(0, 0);
            this._treeView_Node.Name = "_treeView_Node";
            this._treeView_Node.Size = new System.Drawing.Size(119, 241);
            this._treeView_Node.TabIndex = 0;
            // 
            // _listView_Node
            // 
            this._listView_Node.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader7});
            this._listView_Node.Dock = System.Windows.Forms.DockStyle.Fill;
            this._listView_Node.Location = new System.Drawing.Point(0, 0);
            this._listView_Node.Name = "_listView_Node";
            this._listView_Node.Size = new System.Drawing.Size(162, 241);
            this._listView_Node.TabIndex = 1;
            this._listView_Node.UseCompatibleStateImageBehavior = false;
            this._listView_Node.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "Hostname";
            this.columnHeader7.Width = 105;
            // 
            // _panel_Property
            // 
            this._panel_Property.BackColor = System.Drawing.Color.Gainsboro;
            this._panel_Property.Controls.Add(this._textBox_Hostname);
            this._panel_Property.Controls.Add(this._textBox_Address);
            this._panel_Property.Dock = System.Windows.Forms.DockStyle.Fill;
            this._panel_Property.Location = new System.Drawing.Point(0, 0);
            this._panel_Property.Name = "_panel_Property";
            this._panel_Property.Size = new System.Drawing.Size(285, 93);
            this._panel_Property.TabIndex = 12;
            // 
            // _textBox_Hostname
            // 
            this._textBox_Hostname.Location = new System.Drawing.Point(63, 12);
            this._textBox_Hostname.Name = "_textBox_Hostname";
            this._textBox_Hostname.Size = new System.Drawing.Size(169, 19);
            this._textBox_Hostname.TabIndex = 0;
            // 
            // _textBox_Address
            // 
            this._textBox_Address.Location = new System.Drawing.Point(63, 37);
            this._textBox_Address.Name = "_textBox_Address";
            this._textBox_Address.Size = new System.Drawing.Size(169, 19);
            this._textBox_Address.TabIndex = 0;
            // 
            // _button_Unlock
            // 
            this._button_Unlock.Enabled = false;
            this._button_Unlock.Location = new System.Drawing.Point(731, 238);
            this._button_Unlock.Name = "_button_Unlock";
            this._button_Unlock.Size = new System.Drawing.Size(75, 23);
            this._button_Unlock.TabIndex = 11;
            this._button_Unlock.Text = "Unlock";
            this._button_Unlock.UseVisualStyleBackColor = true;
            this._button_Unlock.Click += new System.EventHandler(this._button_Unlock_Click);
            // 
            // _button_Lock
            // 
            this._button_Lock.Location = new System.Drawing.Point(650, 238);
            this._button_Lock.Name = "_button_Lock";
            this._button_Lock.Size = new System.Drawing.Size(75, 23);
            this._button_Lock.TabIndex = 10;
            this._button_Lock.Text = "Lock";
            this._button_Lock.UseVisualStyleBackColor = true;
            this._button_Lock.Click += new System.EventHandler(this._button_Lock_Click);
            // 
            // _flowLayoutPanel
            // 
            this._flowLayoutPanel.BackColor = System.Drawing.Color.Silver;
            this._flowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this._flowLayoutPanel.Location = new System.Drawing.Point(628, 133);
            this._flowLayoutPanel.Name = "_flowLayoutPanel";
            this._flowLayoutPanel.Size = new System.Drawing.Size(163, 86);
            this._flowLayoutPanel.TabIndex = 6;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this._button_Register);
            this.groupBox1.Location = new System.Drawing.Point(669, 28);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(126, 99);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "groupBox1";
            // 
            // _button_Register
            // 
            this._button_Register.Location = new System.Drawing.Point(20, 35);
            this._button_Register.Name = "_button_Register";
            this._button_Register.Size = new System.Drawing.Size(75, 23);
            this._button_Register.TabIndex = 2;
            this._button_Register.Text = "register";
            this._button_Register.UseVisualStyleBackColor = true;
            this._button_Register.Click += new System.EventHandler(this._button_Register_Click);
            // 
            // _tabControl_Log
            // 
            this._tabControl_Log.Controls.Add(this._tabPage_Log);
            this._tabControl_Log.Controls.Add(this._tabPage_Scheduler);
            this._tabControl_Log.Location = new System.Drawing.Point(34, 16);
            this._tabControl_Log.Multiline = true;
            this._tabControl_Log.Name = "_tabControl_Log";
            this._tabControl_Log.SelectedIndex = 0;
            this._tabControl_Log.Size = new System.Drawing.Size(761, 134);
            this._tabControl_Log.TabIndex = 5;
            // 
            // _tabPage_Log
            // 
            this._tabPage_Log.Controls.Add(this._listView_Log);
            this._tabPage_Log.Location = new System.Drawing.Point(4, 21);
            this._tabPage_Log.Name = "_tabPage_Log";
            this._tabPage_Log.Padding = new System.Windows.Forms.Padding(3);
            this._tabPage_Log.Size = new System.Drawing.Size(753, 109);
            this._tabPage_Log.TabIndex = 0;
            this._tabPage_Log.Text = "ログ一覧";
            this._tabPage_Log.UseVisualStyleBackColor = true;
            // 
            // _tabPage_Scheduler
            // 
            this._tabPage_Scheduler.Location = new System.Drawing.Point(4, 21);
            this._tabPage_Scheduler.Name = "_tabPage_Scheduler";
            this._tabPage_Scheduler.Padding = new System.Windows.Forms.Padding(3);
            this._tabPage_Scheduler.Size = new System.Drawing.Size(753, 109);
            this._tabPage_Scheduler.TabIndex = 1;
            this._tabPage_Scheduler.Text = "スケジュール";
            this._tabPage_Scheduler.UseVisualStyleBackColor = true;
            // 
            // _toolStrip
            // 
            this._toolStrip.Location = new System.Drawing.Point(0, 0);
            this._toolStrip.Name = "_toolStrip";
            this._toolStrip.Size = new System.Drawing.Size(816, 25);
            this._toolStrip.TabIndex = 9;
            this._toolStrip.Text = "toolStrip1";
            // 
            // _imageList
            // 
            this._imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("_imageList.ImageStream")));
            this._imageList.TransparentColor = System.Drawing.Color.Transparent;
            this._imageList.Images.SetKeyName(0, "house.png");
            this._imageList.Images.SetKeyName(1, "fol_cl_y.ico");
            this._imageList.Images.SetKeyName(2, "Router16.png");
            // 
            // _contextMenuStrip_TreeView
            // 
            this._contextMenuStrip_TreeView.Name = "_contextMenuStrip_TreeView";
            this._contextMenuStrip_TreeView.Size = new System.Drawing.Size(61, 4);
            // 
            // _contextMenuStrip_ListView
            // 
            this._contextMenuStrip_ListView.Name = "_contextMenuStrip_ListView";
            this._contextMenuStrip_ListView.Size = new System.Drawing.Size(61, 4);
            // 
            // _statusStrip
            // 
            this._statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripStatusLabel});
            this._statusStrip.Location = new System.Drawing.Point(0, 586);
            this._statusStrip.Name = "_statusStrip";
            this._statusStrip.Size = new System.Drawing.Size(816, 22);
            this._statusStrip.TabIndex = 6;
            this._statusStrip.Text = "statusStrip1";
            // 
            // _toolStripStatusLabel
            // 
            this._toolStripStatusLabel.Name = "_toolStripStatusLabel";
            this._toolStripStatusLabel.Size = new System.Drawing.Size(801, 17);
            this._toolStripStatusLabel.Spring = true;
            // 
            // _listView_Log
            // 
            this._listView_Log.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6});
            this._listView_Log.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._listView_Log.FullRowSelect = true;
            this._listView_Log.GridLines = true;
            this._listView_Log.Location = new System.Drawing.Point(27, 17);
            this._listView_Log.Name = "_listView_Log";
            this._listView_Log.Size = new System.Drawing.Size(669, 86);
            this._listView_Log.TabIndex = 4;
            this._listView_Log.UseCompatibleStateImageBehavior = false;
            this._listView_Log.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "名前";
            this.columnHeader1.Width = 111;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "ターゲット";
            this.columnHeader2.Width = 115;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "ステータス";
            this.columnHeader3.Width = 82;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "時刻";
            this.columnHeader4.Width = 161;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "開始時刻";
            this.columnHeader5.Width = 176;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "完了時刻";
            this.columnHeader6.Width = 553;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(816, 608);
            this.Controls.Add(this._splitContainer_Main);
            this.Controls.Add(this._statusStrip);
            this.Controls.Add(this._toolStrip);
            this.Name = "MainForm";
            this.Text = "Form1";
            this._splitContainer_Main.Panel1.ResumeLayout(false);
            this._splitContainer_Main.Panel2.ResumeLayout(false);
            this._splitContainer_Main.ResumeLayout(false);
            this._splitContainer_Node_H.Panel1.ResumeLayout(false);
            this._splitContainer_Node_H.Panel2.ResumeLayout(false);
            this._splitContainer_Node_H.ResumeLayout(false);
            this._splitContainer_Node_V.Panel1.ResumeLayout(false);
            this._splitContainer_Node_V.Panel2.ResumeLayout(false);
            this._splitContainer_Node_V.ResumeLayout(false);
            this._panel_Property.ResumeLayout(false);
            this._panel_Property.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this._tabControl_Log.ResumeLayout(false);
            this._tabPage_Log.ResumeLayout(false);
            this._statusStrip.ResumeLayout(false);
            this._statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button _button_Register;
        private System.Windows.Forms.GroupBox groupBox1;
        private rmainte4.Controls.ListViewEx _listView_Log;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.SplitContainer _splitContainer_Main;
        private System.Windows.Forms.TreeView _treeView_Node;
        private System.Windows.Forms.TabControl _tabControl_Log;
        private System.Windows.Forms.TabPage _tabPage_Log;
        private System.Windows.Forms.TabPage _tabPage_Scheduler;
        private System.Windows.Forms.ContextMenuStrip _contextMenuStrip_TreeView;
        private System.Windows.Forms.ImageList _imageList;
        private System.Windows.Forms.ListView _listView_Node;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ContextMenuStrip _contextMenuStrip_ListView;
        private System.Windows.Forms.FlowLayoutPanel _flowLayoutPanel;
        private System.Windows.Forms.ToolStrip _toolStrip;
        private System.Windows.Forms.TextBox _textBox_Address;
        private System.Windows.Forms.TextBox _textBox_Hostname;
        private System.Windows.Forms.Button _button_Unlock;
        private System.Windows.Forms.Button _button_Lock;
        private System.Windows.Forms.Panel _panel_Property;
        private System.Windows.Forms.StatusStrip _statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel _toolStripStatusLabel;
        private System.Windows.Forms.SplitContainer _splitContainer_Node_H;
        private System.Windows.Forms.SplitContainer _splitContainer_Node_V;
    }
}

