using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;

namespace rmainte4.TimelineControl
{
    public interface ITimeScalePainter
    {
        float Left { get; set;}
        float Height { get; }
        void DrawTimeScale(Graphics g, DateTime timeStart, double timeIntervalInMinutes, int timeZonesCount, float timeZoneWidth, float controlHeight);
    }

    public class DefaultTimeScalePainter : ITimeScalePainter, IDisposable
    {
        public float Height
        {
            get { return _timeScaleHeight; }
        }
        private float _timeScaleHeight = 20;

        public float Left
        {
            get { return _left; }
            set { _left = value; }
        }
        private float _left = 100;

        public string TimeFormat
        {
            get { return _timeFormat; }
            set { _timeFormat = value; }
        }
        private string _timeFormat = "HH:mm:ss";

        public Color AlternatingColor1
        {
            get { return _color1; }
            set
            {
                if (_color1.Equals(value))
                {
                    return;
                }

                _color1 = value;
                _brush1.Dispose();
                _brush1 = new SolidBrush(_color1);
            }
        }
        private Color _color1 = Color.Gray;
        private SolidBrush _brush1 = new SolidBrush(Color.Gray);

        public Color AlternatingColor2
        {
            get { return _color2; }
            set
            {
                if (_color2.Equals(value))
                {
                    return;
                }

                _color2 = value;
                _brush2.Dispose();
                _brush2 = new SolidBrush(_color2);
            }
        }
        private Color _color2 = Color.DarkGray;
        private SolidBrush _brush2 = new SolidBrush(Color.DarkGray);

        public Color TextColor
        {
            get { return _textColor; }
            set
            {
                if (_textColor.Equals(value))
                {
                    return;
                }

                _textColor = value;
                _textBrush.Dispose();
                _textBrush = new SolidBrush(_textColor);
            }
        }
        private Color _textColor = Color.White;
        private SolidBrush _textBrush = new SolidBrush(Color.White);
        
        
        private readonly Pen _gridPen = new Pen(Color.Green);
        private readonly Pen _nowPen = new Pen(Color.White);


        public Font TimeZoneFont
        {
            get { return _timeZoneFont; }
            set { _timeZoneFont = value; }
        }
        private Font _timeZoneFont = new Font("Tahoma", 8);

        public void DrawTimeScale(Graphics g, DateTime timeStart, double timeIntervalInMinutes, int timeZonesCount, float timeZoneWidth, float controlHeight)
        {
            DateTime now = DateTime.Now;

            SolidBrush rectBrush = _brush2;

            // タイムゾーンの時刻
            DateTime timeZoneTime;

            // 次の時刻
            DateTime timeZoneTimeNext = timeStart;
            
            // タイムゾーンの先頭のX座標
            float xStart = _left;

            // タイムゾーンの終わりのX座標
            float xEnd = _left;

            // 現在時刻のラインは既に描いた？
            bool nowLineDrawn = false;

            for (int i = 0; i < timeZonesCount; i++)
            {
                if (rectBrush.Equals(_brush2))
                {
                    rectBrush = _brush1;
                }
                else
                {
                    rectBrush = _brush2;
                }

                // xEnd側で描画するので、最初の列の縦線は描かれない
                xStart = xEnd;
                xEnd += timeZoneWidth;
                g.DrawLine(_gridPen, xEnd, _timeScaleHeight, xEnd, controlHeight);

                timeZoneTime = timeZoneTimeNext;
                timeZoneTimeNext = timeStart.AddMinutes(timeIntervalInMinutes * (i + 1));

                // 現在時刻がこの間に入ってるなら、xEnd側に描画
                if (nowLineDrawn == false && now >= timeZoneTime && now <= timeZoneTimeNext)
                {
                    g.DrawLine(_nowPen, xEnd, _timeScaleHeight, xEnd, controlHeight);
                    nowLineDrawn = true;
                }

                string timeText = timeZoneTime.ToString(_timeFormat, System.Globalization.CultureInfo.InvariantCulture);
                float textHeight = g.MeasureString(timeText, _timeZoneFont).Height;

                g.FillRectangle(rectBrush, xStart, 0, timeZoneWidth, _timeScaleHeight);
                g.DrawString(timeText, _timeZoneFont, _textBrush, timeZoneWidth * i + _left, (_timeScaleHeight - textHeight) / 2);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timeZoneFont.Dispose();
                _brush1.Dispose();
                _brush2.Dispose();
                _textBrush.Dispose();
                _gridPen.Dispose();
                _nowPen.Dispose();
            }
        }
    }
}
