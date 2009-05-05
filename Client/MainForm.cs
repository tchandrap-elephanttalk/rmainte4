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

        private static string MESSAGE_LOCK_FAILED = IsJapanese ? "���b�N�̎擾�Ɏ��s���܂����B���Ԃ������Ă���ēx���s���Ă��������B" : "Lock failed";

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

            // ���X�g�r���[�̃_�u���o�b�t�@��L���ɂ��Ă������}����B
            // �p������DoubleBuffered�v���p�e�B��true�ɂ��Ă��悢���A�ʓ|�Ȃ̂ŁA�v���p�e�B���Z�b�g����
            System.Reflection.PropertyInfo listViewProp = typeof(ListView).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
            listViewProp.SetValue(_listView_Node, true, null);
            listViewProp.SetValue(_listView_Log, true, null);

            // �c���[�r���[�̃_�u���o�b�t�@��L���ɂ��Ă������}����B
            // �p������DoubleBuffered�v���p�e�B��true�ɂ��Ă��悢���A�ʓ|�Ȃ̂ŁA�v���p�e�B���Z�b�g����
            System.Reflection.PropertyInfo treeViewProp = typeof(TreeView).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
            treeViewProp.SetValue(_treeView_Node, true, null);

            OnLoad_Tip();

            // ListView�̒����B
            OnLoad_ListView_Node();

            // �{���͐ڑ���T�[�o��I���ł���悤�ɂ������������B
            // �Ƃ肠�����ݒ�t�@�C������ǂނ��Ƃɂ���B

            // �����[�e�B���O�T�[�o�Ɛڑ�
            // MainForm.Logic.cs
            ServerEnv env = InitializeRemoting();

            // �T�[�o����擾�����f�[�^�Z�b�g�����Ƀc���[�r���[�𕜌�
            // MainForm.TreeView.cs
            OnLoad_TreeView();

#if TEST_TREEVIEW
            if (DialogResult.OK == MessageBox.Show("�c���[�r���[�̃e�X�g���J�n���܂����H", "TEST_TREEVIEW", MessageBoxButtons.OKCancel))
            {
                START_TEST_TREEVIEW();
            }
#endif

            // Notification��M���̃C�x���g���d����ł���A
            env.NotificationEventHandler += new NotificationEvent(OnReceiveNotification);

            // �񓯊���M���J�n����
            env.StartAsyncReceiveNotification();

            // �ؒf���ꂽ�Ƃ��̃C�x���g���d����
            env.OfflineEventHandler += new OfflineEvent(OnOffline);
        }

        /// <summary>
        /// �o�[�W�������
        /// </summary>
        public const double CLIENT_VERSION = MyVersion.CURRENT_VERSION;


        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            foreach (ServerEnv env in _serverEnvList)
            {
                if (env.Address.Equals("localhost"))
                {

                    // ToDo: �I�������A���Ă������b�Z�[�W���T�[�o�����΂��H
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

        // �^�C�}�[�ŌĂ΂��
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
        // �e�X�g�p�̃R�[�h
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
                ShowTip("���b�N���܂���");
            }
            else
            {
                _button_Lock.Enabled = true;
                _button_Unlock.Enabled = false;
                ShowTip("���b�N���s");
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

            // ���{��
            int exec = 10;

            try
            {
                // exec�쐬���A
                for (int i = 0; i < exec; i++)
                {
                    // ���[�g��I�����A
                    _treeView_Node.SelectedNode = _treeView_Node.TopNode;

                    // �m�[�h��ǉ�����
                    treeViewRightClickInsertGroup(null, null);

                    // �҂B
                    Application.DoEvents();
                    Thread.Sleep(sleep);
                    Application.DoEvents();
                }

                // exec�폜����
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

            // �^�[�Q�b�g�ɂȂ��Ă���p�l�����}�E�X�̍��W����擾����
            Point clientPoint = _flowLayoutPanel.PointToClient(new Point(e.X, e.Y));
            Control targetControl = (Control)_flowLayoutPanel.GetChildAtPoint(clientPoint);
            if (targetControl == null)
            {
                e.Effect = DragDropEffects.Move;

                // �}�E�X�|�C���^�̏�ɂ���R���g���[���ōł��߂��R���g���[���́H
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

            // �h���b�O�Œ͂�ł���p�l�����擾����
            MyPanel source = (MyPanel)e.Data.GetData(typeof(MyPanel));

            // �^�[�Q�b�g�ɂȂ��Ă���p�l�����}�E�X�̍��W����擾����
            Point clientPoint = _flowLayoutPanel.PointToClient(new Point(e.X, e.Y));
            MyPanel target = (MyPanel)_flowLayoutPanel.GetChildAtPoint(clientPoint);
            if (target == null)
            {
                return;
            }

            // �^�[�Q�b�g�̈ʒu���擾����
            int targetIndex = _flowLayoutPanel.Controls.GetChildIndex(target);

            // �\�[�X�̈ʒu��ς���
            _flowLayoutPanel.Controls.SetChildIndex(source, targetIndex);


            // �K�v�H
            // _flowLayoutPanel.Invalidate();

        }
        #endregion



    }
}