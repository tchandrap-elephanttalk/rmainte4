using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using rmainte4.TimelineControl;

namespace rmainte4.PingMonitor
{
    public partial class PingerT : Form
    {
        public PingerT()
        {
            InitializeComponent();

            DateTime now = DateTime.Now.Subtract(new TimeSpan(0, 0, 3));

            _timeline.SetTotalTimeWindow(now, DateTime.MaxValue);
            _timeline.SetInitialWindow(now, 0);

            _trackBar_Zoom.Minimum = 0;
            _trackBar_Zoom.Maximum = _timeline.ZoomFactors.Length - 1;
            _trackBar_Zoom.TickFrequency = 1;
            _trackBar_Zoom.LargeChange = 1;
            _trackBar_Zoom.ValueChanged += new EventHandler(_trackBar_Zoom_ValueChanged);
        }



        void _trackBar_Zoom_ValueChanged(object sender, EventArgs e)
        {
            int i = _trackBar_Zoom.Value;
            _timeline.SetZoom(i);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        private void _button_Add_Click(object sender, EventArgs e)
        {
            string target = _textBox_Target.Text;
            if (string.IsNullOrEmpty(target))
            {
                return;
            }

            Pinger pinger = new Pinger(target, _timeline);
            pinger.Start();
        }

    }
}