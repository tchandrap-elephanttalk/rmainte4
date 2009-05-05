using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

using rmainte4.TimelineControl;

namespace rmainte4.PingMonitor
{
    public class Pinger
    {
        public Pinger(string target, Timeline timeline)
        {
            _target = target;
            _timeline = timeline;

            // グラフを一つ持つ。
            _axis = new PingAxis(target);
            _timeline.AddAxis(_axis);

            // Pingオブジェクトを作成
            _ping = new System.Net.NetworkInformation.Ping();

            _pingOptions = new PingOptions(_ttl, _df);
            _payloadBytes = System.Text.Encoding.ASCII.GetBytes(new string('A', _payloadSize));

            // 実行完了時に呼ばれるイベントハンドラを追加
            _ping.PingCompleted += new System.Net.NetworkInformation.PingCompletedEventHandler(PingCompleted);
        }

        // スレッド
        private Thread _thread = null;
        public Thread Thread
        {
            get { return _thread; }
        }

        private PingAxis _axis = null;

        private Timeline _timeline = null;

        private string _target = null;

        private System.Net.NetworkInformation.Ping _ping = null;

        public int Ttl
        {
            get { return _ttl; }
            set
            {
                _ttl = value;
                _pingOptions = new PingOptions(_ttl, _df);
            }
        }
        private int _ttl = 64;

        public bool Df
        {
            get { return _df; }
            set
            {
                _df = value;
                _pingOptions = new PingOptions(_ttl, _df);
            }
        }
        private bool _df = false;

        private PingOptions _pingOptions;

        public int PayloadSize
        {
            get { return _payloadSize; }
            set
            {
                _payloadSize = value;
                _payloadBytes = System.Text.Encoding.ASCII.GetBytes(new string('A', _payloadSize));
            }
        }
        private int _payloadSize = 32;

        private byte[] _payloadBytes;

        private int _timeout_ms = 1000;

        public void Start()
        {
            _thread = new Thread(new ThreadStart(Send));
            _thread.Start();
        }

        private void Send()
        {
            // 非同期実行
            _ping.SendAsync(_target, _timeout_ms, _payloadBytes, _pingOptions, null);
        }

        private void PingCompleted(object sender, System.Net.NetworkInformation.PingCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                return;
            }

            DateTime now = DateTime.Now;
            PingItem item = new PingItem(_axis, now);

            if (e.Error != null)
            {
                item.SetErrorResult(e.Error.Message);
            }
            else
            {
                item.SetSuccessResult(e.Reply.Address, e.Reply.Buffer.Length, e.Reply.RoundtripTime, e.Reply.Options.Ttl);
            }

            _timeline.AddItem(item);

            Thread.Sleep(500);
            System.Windows.Forms.Application.DoEvents();

            Send();
        }


    }
}
