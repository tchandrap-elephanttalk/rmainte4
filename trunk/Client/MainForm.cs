#define TEST_TREEVIEW

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

using rmainte4.Controls;

namespace rmainte4
{
    public partial class MainForm : Form
    {

        private static string MESSAGE_LOCK_FAILED = IsJapanese ? "ロックの取得に失敗しました。時間をあけてから再度実行してください。" : "Lock failed";

        private MainForm()
        {
            InitializeComponent();
        }

        public static MainForm GetInstance()
        {
            return _instance;
        }

        public static MainForm Instance
        {
            get { return _instance; }
        }
        private static readonly MainForm _instance = new MainForm();

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // リストビューのダブルバッファを有効にしてちらつきを抑える。
            // 継承してDoubleBufferedプロパティをtrueにしてもよいが、面倒なので、プロパティをセットする
            System.Reflection.PropertyInfo listViewProp = typeof(ListView).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
            listViewProp.SetValue(_listView_Node, true, null);
            listViewProp.SetValue(_listView_Log, true, null);

            // ツリービューのダブルバッファを有効にしてちらつきを抑える。
            // 継承してDoubleBufferedプロパティをtrueにしてもよいが、面倒なので、プロパティをセットする
            System.Reflection.PropertyInfo treeViewProp = typeof(TreeView).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
            treeViewProp.SetValue(_treeView_Node, true, null);

            OnLoad_Tip();

            // ListViewの調整。
            OnLoad_ListView_Node();

            // 本当は接続先サーバを選択できるようにした方がいい。
            // とりあえず設定ファイルから読むことにする。

            // リモーティングサーバと接続
            // MainForm.Logic.cs
            ServerEnv env = InitializeRemoting();

            // サーバから取得したデータセットを元にツリービューを復元
            // MainForm.TreeView.cs
            OnLoad_TreeView();

#if TEST_TREEVIEW
            if (DialogResult.OK == MessageBox.Show("ツリービューのテストを開始しますか？", "TEST_TREEVIEW", MessageBoxButtons.OKCancel))
            {
                START_TEST_TREEVIEW();
            }
#endif

            // Notification受信時のイベントを仕込んでから、
            env.NotificationEventHandler += new NotificationEvent(OnReceiveNotification);

            // 非同期受信を開始する
            env.StartAsyncReceiveNotification();

            // 切断されたときのイベントを仕込む
            env.OfflineEventHandler += new OfflineEvent(OnOffline);
        }

        /// <summary>
        /// バージョン情報
        /// </summary>
        public const double CLIENT_VERSION = MyVersion.CURRENT_VERSION;


        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            foreach (ServerEnv env in _serverEnvList)
            {
                if (env.Address.Equals("localhost"))
                {

                    // ToDo: 終了するよ、っていうメッセージをサーバから飛ばす？
                    RemotingServer.Instance.Unregister();
                }
            }
        }
        

        private static void ShowWaitCursor(bool Show)
        {
            if (Show == true)
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            }
            else
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
            return;
        }


        private static void ShowError(string msg)
        {
            MessageBox.Show(msg);
        }

        private static bool IsJapanese
        {
            get { return System.Threading.Thread.CurrentThread.CurrentUICulture.Name.StartsWith("ja"); }
        }

        private delegate void RefreshLogDelegate(ListViewItem item);
        private void RefreshLog(ListViewItem item)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new RefreshLogDelegate(RefreshLog), new object[] { item });
                return;
            }

            _listView_Log.BeginUpdate();
            _listView_Log.Items.Insert(0, item);
            _listView_Log.EndUpdate();
        }


        #region ShowTip
        private System.Timers.Timer _tipTimer;

        private void OnLoad_Tip()
        {
            _toolStripStatusLabel.Visible = false;

            _tipTimer = new System.Timers.Timer(2200);
            _tipTimer.Enabled = false;
            _tipTimer.AutoReset = false;
            _tipTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnHideTip);
        }

        // タイマーで呼ばれる
        private void OnHideTip(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new HideTipDelegate(OnHideTip), null, null);
                return;
            }

            lock (_toolStripStatusLabel)
            {
                _toolStripStatusLabel.Visible = false;
            }
        }
        private delegate void HideTipDelegate(object sender, System.Timers.ElapsedEventArgs e);

        private void ShowTip(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new ShowTipDelegate(ShowTip), message);
                return;
            }

            lock (_toolStripStatusLabel)
            {
                _toolStripStatusLabel.Text = message;
                _toolStripStatusLabel.Visible = true;
                
                _tipTimer.Stop();
                _tipTimer.Start();
            }
        }
        private delegate void ShowTipDelegate(string message);
        #endregion



        // --------------------------------
        // テスト用のコード
        // --------------------------------
        
        private void _button_Register_Click(object sender, EventArgs e)
        {
            if (_treeView_Node.TopNode == null)
            {
                return;
            }

            string serverAddress = _treeView_Node.TopNode.Text;
            ServerEnv env = GetServerEnv(serverAddress);
            if (env == null || env.IsConnected == false)
            {
                return;
            }

            env.RegisterJob(_i++);
        }
        int _i = 0;
 

        private void _button_Lock_Click(object sender, EventArgs e)
        {
            ServerEnv env = GetServerEnv("localhost");
            if (env.IRmainte.GetLock(_guid))
            {
                _button_Lock.Enabled = false;
                _button_Unlock.Enabled = true;
                ShowTip("ロックしました");
            }
            else
            {
                _button_Lock.Enabled = true;
                _button_Unlock.Enabled = false;
                ShowTip("ロック失敗");
            }
        }


        private void _button_Unlock_Click(object sender, EventArgs e)
        {
            ServerEnv env = GetServerEnv("localhost");
            env.IRmainte.ReleaseLock(_guid);
            _button_Lock.Enabled = true;
            _button_Unlock.Enabled = false;
        }


        #region START_TEST_TREEVIEW

#if TEST_TREEVIEW

        Thread _testTreeViewThread = null;

        private void START_TEST_TREEVIEW()
        {
            _testTreeViewThread = new Thread(new ThreadStart(TEST_TREEVIEW));
            _testTreeViewThread.IsBackground = true;
            _testTreeViewThread.Start();
        }

        private void TEST_TREEVIEW()
        {
            while (true)
            {
                TEST_ADD_AND_DELETE_NODE();
            }
        }

        private void TEST_ADD_AND_DELETE_NODE()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(TEST_ADD_AND_DELETE_NODE));
                return;
            }

            // 500msec
            int sleep = 500;

            // 実施回数
            int exec = 10;

            try
            {
                // exec個作成し、
                for (int i = 0; i < exec; i++)
                {
                    // ルートを選択し、
                    _treeView_Node.SelectedNode = _treeView_Node.TopNode;

                    // ノードを追加する
                    treeViewRightClickInsertGroup(null, null);

                    // 待つ。
                    Application.DoEvents();
                    Thread.Sleep(sleep);
                    Application.DoEvents();
                }

                // exec個削除する
                for (int i = 0; i < exec; i++)
                {
                    TreeNode root = _treeView_Node.TopNode;
                    if (root.Nodes.Count > 0)
                    {
                        _treeView_Node.SelectedNode = root.Nodes[0];
                        treeViewRightClickDelete(null, null);

                        Application.DoEvents();
                        Thread.Sleep(sleep);
                        Application.DoEvents();
                    }
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine(err.Message + Environment.NewLine + err.StackTrace);
                Debug.Flush();
                ShowError(err.StackTrace);
                // throw;
            }
        }
   
#endif
        #endregion


        #region FlowLayoutPanel
        private void OnLoad_FlowLayoutPanel()
        {
            _flowLayoutPanel.AllowDrop = true;

            _flowLayoutPanel.DragDrop += new DragEventHandler(_flowLayoutPanel_DragDrop);
            _flowLayoutPanel.DragOver += new DragEventHandler(_flowLayoutPanel_DragOver);
            _flowLayoutPanel.DragEnter += new DragEventHandler(_flowLayoutPanel_DragEnter);
        }

        void _flowLayoutPanel_DragEnter(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(typeof(MyPanel)) == false)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            e.Effect = DragDropEffects.Move;
        }

        void _flowLayoutPanel_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(MyPanel)) == false)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            // ターゲットになっているパネルをマウスの座標から取得する
            Point clientPoint = _flowLayoutPanel.PointToClient(new Point(e.X, e.Y));
            Control targetControl = (Control)_flowLayoutPanel.GetChildAtPoint(clientPoint);
            if (targetControl == null)
            {
                e.Effect = DragDropEffects.Move;

                // マウスポインタの上にあるコントロールで最も近いコントロールは？
                MyPanel upper = (MyPanel)GetNearestUpperControl(_flowLayoutPanel.Controls, clientPoint);
                MyPanel lower = (MyPanel)GetNearestLowerControl(_flowLayoutPanel.Controls, clientPoint);

                upper.BorderWidth = 3;
                upper.BorderColor = Color.Gold;
                lower.BorderWidth = 3;
                lower.BorderColor = Color.Gold;

                Graphics g = _flowLayoutPanel.CreateGraphics();



                g.DrawLine(new Pen(Color.Green, 2), new Point(lower.Location.X, lower.Location.Y - 3), new Point(lower.Location.X + lower.Width, lower.Location.Y - 3));
                // g.FillPolygon(new SolidBrush(m_lineColor), new Point[] { new Point(target.Bounds.X + target.Bounds.Width - 5, target.Bounds.Y), new Point(target.Bounds.X + target.Bounds.Width + 5, target.Bounds.Y), new Point(target.Bounds.X + target.Bounds.Width, target.Bounds.Y + 5) });
                // g.FillPolygon(new SolidBrush(m_lineColor), new Point[] { new Point(target.Bounds.X + target.Bounds.Width - 5, target.Bounds.Y + target.Bounds.Height), new Point(target.Bounds.X + target.Bounds.Width + 5, target.Bounds.Y + target.Bounds.Height), new Point(target.Bounds.X + target.Bounds.Width, target.Bounds.Y + target.Bounds.Height - 5) });

                _flowLayoutPanel.Invalidate();

                return;
            }

            e.Effect = DragDropEffects.None;

            MyPanel target = (MyPanel)_flowLayoutPanel.GetChildAtPoint(clientPoint);

            /*
            Graphics g = _flowLayoutPanel.CreateGraphics();

            Color m_lineColor = Color.Red;

            if (target == null)
            {
                target = (MyPanel) _flowLayoutPanel.Controls[_flowLayoutPanel.Controls.Count - 1];

                g.DrawLine(new Pen(m_lineColor, 2), new Point(target.Bounds.X + target.Bounds.Width, target.Bounds.Y), new Point(target.Bounds.X + target.Bounds.Width, target.Bounds.Y + target.Bounds.Height));
                g.FillPolygon(new SolidBrush(m_lineColor), new Point[] { new Point(target.Bounds.X + target.Bounds.Width - 5, target.Bounds.Y), new Point(target.Bounds.X + target.Bounds.Width + 5, target.Bounds.Y), new Point(target.Bounds.X + target.Bounds.Width, target.Bounds.Y + 5) });
                g.FillPolygon(new SolidBrush(m_lineColor), new Point[] { new Point(target.Bounds.X + target.Bounds.Width - 5, target.Bounds.Y + target.Bounds.Height), new Point(target.Bounds.X + target.Bounds.Width + 5, target.Bounds.Y + target.Bounds.Height), new Point(target.Bounds.X + target.Bounds.Width, target.Bounds.Y + target.Bounds.Height - 5) });

                return;
            }

            g.DrawLine(new Pen(m_lineColor, 2), new Point(target.Bounds.X, target.Bounds.Y), new Point(target.Bounds.X, target.Bounds.Y + target.Bounds.Height));
            g.FillPolygon(new SolidBrush(m_lineColor), new Point[] { new Point(target.Bounds.X - 5, target.Bounds.Y), new Point(target.Bounds.X + 5, target.Bounds.Y), new Point(target.Bounds.X, target.Bounds.Y + 5) });
            g.FillPolygon(new SolidBrush(m_lineColor), new Point[] { new Point(target.Bounds.X - 5, target.Bounds.Y + target.Bounds.Height), new Point(target.Bounds.X + 5, target.Bounds.Y + target.Bounds.Height), new Point(target.Bounds.X, target.Bounds.Y + target.Bounds.Height - 5) });
            */

        }

        private static Control GetNearestUpperControl(Control.ControlCollection controls, Point clientPoint)
        {
            Control minControl = null;
            int min = Int32.MaxValue;
            foreach (Control c in controls)
            {
                int diff = clientPoint.Y - c.Location.Y;
                if (diff > 0 && diff < min)
                {
                    min = diff;
                    minControl = c;
                }
            }
            return minControl;
        }

        private static Control GetNearestLowerControl(Control.ControlCollection controls, Point clientPoint)
        {
            Control minControl = null;
            int min = Int32.MaxValue;
            foreach (Control c in controls)
            {
                int diff = c.Location.Y - clientPoint.Y;
                if (diff > 0 && diff < min)
                {
                    min = diff;
                    minControl = c;
                }
            }
            return minControl;
        }

        void _flowLayoutPanel_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(MyPanel)) == false)
            {
                return;
            }

            // ドラッグで掴んでいるパネルを取得する
            MyPanel source = (MyPanel)e.Data.GetData(typeof(MyPanel));

            // ターゲットになっているパネルをマウスの座標から取得する
            Point clientPoint = _flowLayoutPanel.PointToClient(new Point(e.X, e.Y));
            MyPanel target = (MyPanel)_flowLayoutPanel.GetChildAtPoint(clientPoint);
            if (target == null)
            {
                return;
            }

            // ターゲットの位置を取得する
            int targetIndex = _flowLayoutPanel.Controls.GetChildIndex(target);

            // ソースの位置を変える
            _flowLayoutPanel.Controls.SetChildIndex(source, targetIndex);


            // 必要？
            // _flowLayoutPanel.Invalidate();

        }
        #endregion



    }
}