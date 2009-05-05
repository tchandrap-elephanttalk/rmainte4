using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

// Last modified at 2009.01.07 for rmainte4

namespace rmainte4.Threading
{
    public partial class TestWorkThreadPool : Form
    {
        public TestWorkThreadPool()
        {
            InitializeComponent();

            _wtpool = new WorkThreadPool(25);
            _wtpool.AllWorkItemCompletedEventHandler += new EventHandler(_wtpool_AllWorkItemCompletedEventHandler);
            _wtpool.WorkItemStateChangedEventHandler += new WorkItemStateChangedEvent(_wtpool_WorkItemStateChangedEventHandler);
        }

        private WorkThreadPool _wtpool = null; 
        private TimeSpan _refreshInterval = TimeSpan.FromSeconds(0.20);
        private DateTime _nextRefreshTime = DateTime.Now;

        void _wtpool_WorkItemStateChangedEventHandler(object sender, WorkItemStateChangedEventArgs e)
        {
            if (DateTime.Now > _nextRefreshTime)
            {
                RefreshCounts();
                _nextRefreshTime = DateTime.Now + _refreshInterval;
            }
        }

        void _wtpool_AllWorkItemCompletedEventHandler(object sender, EventArgs e)
        {
            RefreshCounts();
            Completed();
        }

        private void RefreshCounts()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(RefreshCounts));
                return;
            }

            lock (this)
            {
                _label_Completed.Text = _wtpool.Stats[(int)WorkItemState.Completed].ToString("N0");
                _label_Queued.Text = _wtpool.Stats[(int)WorkItemState.Queued].ToString("N0");
                _label_Running.Text = _wtpool.Stats[(int)WorkItemState.Running].ToString("N0");
                _label_Scheduled.Text = _wtpool.Stats[(int)WorkItemState.Scheduled].ToString("N0");

                _progressBar_Completed.Value = (int)_wtpool.Stats[(int)WorkItemState.Completed] * 100 / GetNumOfWorkItem();
            }
        }

        private void Completed()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(Completed));
                return;
            }

            lock (this)
            {
                _progressBar_Completed.Visible = false;
                _progressBar_Completed.Value = 0;
            }
        }

        private void _button_Execute_Click(object sender, EventArgs e)
        {
            _wtpool.ClearStats();
            _wtpool.MaxThreads = GetNumOfThreads();
            _progressBar_Completed.Visible = true;
            _nextRefreshTime = DateTime.Now + _refreshInterval;

            for (int i = 0; i < GetNumOfWorkItem(); i++)
            {
                _wtpool.Add(new TestWorkItem());
            }
        }

        private int GetNumOfThreads()
        {
            return Int32.Parse(_textBox_MaxThread.Text);
        }

        private int GetNumOfWorkItem()
        {
            return Int32.Parse(_textBox_WorkItem.Text);
        }


    }

    internal class TestWorkItem : WorkItemImpl
    {
        private static Random random = new Random();

        public override void Work()
        {
            System.Threading.Thread.Sleep(random.Next(100, 500));
        }

        public override void Terminate()
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

}