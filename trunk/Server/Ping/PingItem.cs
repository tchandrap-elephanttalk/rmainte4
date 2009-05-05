using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;

using rmainte4.TimelineControl;

namespace rmainte4.PingMonitor
{
    class PingItem : ITimelineItem
    {

        public PingItem(PingAxis axis, DateTime occurence)
        {
            _axis = axis;
            _occurence = occurence;
        }

        public void SetSuccessResult(IPAddress replyAddress, int replyBufferLen, long replyRoundtripTime, int replyTtl)
        {
            _success = true;
            _replyAddress = replyAddress;
            _replyBufferLength = replyBufferLen;
            _replyRoundtripTime = replyRoundtripTime;
            _replyTtl = replyTtl;
        }

        public void SetErrorResult(string message)
        {
            _success = false;
            _replyErrorMessage = message;
        }

        //--- Ping result
        private bool _success;
        private IPAddress _replyAddress;
        private int _replyBufferLength;
        private long _replyRoundtripTime;
        private int _replyTtl;
        private string _replyErrorMessage;


        public DateTime Occurence
        {
            get { return _occurence; }
        }
        private DateTime _occurence;

        public DateTime ItemStartTime
        {
            get { return _occurence; }
        }

        public DateTime ItemEndTime
        {
            get { return _occurence; }
        }

        public float Y
        {
            get { return _axis.Y + _axis.Height / 2; }
        }

        public IAxis Axis
        {
            get { return (IAxis)_axis; }
        }
        PingAxis _axis;

        private readonly Color _color = Color.Green;
        private readonly Pen _pen = new Pen(Color.Green);
        private readonly Brush _brush = Brushes.Green;

        // TimelineコントロールのOnPaint内で、これが呼ばれる。
        public void Draw(Graphics g, TimeToXCoordinateDelegate timeToXCoordinateConverter, float yStart, DateTime currentWindowStart, DateTime currentWindowEnd)
        {
            float[] x = timeToXCoordinateConverter(_occurence);
            float y = Y;

            if (_success)
            {
                // ○を描いてみる
                g.FillPie(_brush, x[0], y - 5, 10, 10, 0, 360);
                //g.DrawArc(_pen, x[0], y -5, 10, 10, 0, 360);
            }
        }
    }


    public class PingAxis : IAxis, IDisposable
    {
        // コンストラクタ
        public PingAxis(string name)
        {
            _name = name;
        }

        public PingAxis(string name, bool drawAxisLine)
        {
            _name = name;
            _drawAxisLine = drawAxisLine;
        }

        private readonly Font _font = new Font("Tahoma", 8);

        public float Y
        {
            get { return _y; }
            set { _y = value; }
        }
        private float _y;

        public float Height
        {
            get { return _height; }
            set { _height = value; }
        }
        private float _height = 20;

        public bool DrawAxisLine
        {
            get { return _drawAxisLine; }
        }
        bool _drawAxisLine = true;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        string _name;

        public float GetNameWidth(Graphics g)
        {
            return g.MeasureString(_name, _font).Width;
        }

        public float GetNameHeight(Graphics g)
        {
            return g.MeasureString(_name, _font).Height;
        }

        public virtual void Draw(Graphics g, float width)
        {
            DrawBackground(g, width);
            DrawText(g, _name);
        }

        private readonly Brush _brushBlack = new SolidBrush(Color.Black);
        private readonly Brush _brushWhite = new SolidBrush(Color.White);
        private readonly Brush _brushGray = new SolidBrush(Color.Gray);
        private readonly Pen _penGold = new Pen(Color.Gold);

        protected virtual void DrawBackground(Graphics g, float width)
        {
            //g.FillRectangle(_brushBlack, new RectangleF(0, this.Y, width, this.Height));
            //g.FillRectangle(_brushWhite, new RectangleF(0, this.Y, width, this.Height));
            g.DrawRectangle(_penGold, 0, this.Y, width, this.Height);
            g.DrawLine(_penGold, 0, Y, width, Y);
        }

        protected virtual void DrawText(Graphics g, string text)
        {
            float textHeight = g.MeasureString(text, _font).Height;
            g.DrawString(text, _font, _brushGray, 0, this.Y - textHeight / 2 + this.Height / 2);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _font.Dispose();
        }
    }


}
