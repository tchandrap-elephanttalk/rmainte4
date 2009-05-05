using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Text;

namespace rmainte4.TimelineControl
{
    public delegate float[] TimeToXCoordinateDelegate(DateTime time);

    public interface ITimelineItem
    {
        IAxis Axis { get; }

        DateTime ItemStartTime { get; }
        
        DateTime ItemEndTime { get; }
        
        void Draw(Graphics g, 
            TimeToXCoordinateDelegate timeToXCoordinateConverter, 
            float yStart, 
            DateTime currentWindowStart,
            DateTime currentWindowEnd);

    
    }

    // 名前、高さ、上下の位置、等を持つ。
    public interface IAxis
    {
        string Name {
            get;
            set;
        }
        
        float Y 
        { 
            get; 
            set;
        }

        float Height
        {
            get;
            set;
        }
        
        float GetNameWidth(Graphics g);

        void Draw(Graphics g, float width);
    }


    // デフォルトで用意した枠
    public class DefaultAxis : IAxis, IDisposable
    {
        // コンストラクタ
        public DefaultAxis(string name)
            : this(name, false)
        {

        }

        public DefaultAxis(string name, bool drawAxisLine)
        {
            this._name = name;
            this._drawAxisLine = drawAxisLine;
        }

        private readonly Font _font = new Font("Calibri", 8);

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
        private bool _drawAxisLine;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        private string _name;

        public float GetNameWidth(Graphics g)
        {
            return g.MeasureString(this._name, this._font).Width;
        }

        public virtual void Draw(Graphics g, float width)
        {
            DrawBackground(g, width);
            DrawText(g, this.Name);
        }

        protected virtual void DrawBackground(Graphics g, float width)
        {
            using (Brush brush = new SolidBrush(Color.Black))
            {
                g.FillRectangle(brush, new RectangleF(0, this.Y, width, this.Height));
            }
        }

        protected virtual void DrawText(Graphics g, string text)
        {
            float textHeight = g.MeasureString(text, _font).Height;
            using (SolidBrush textBrush = new SolidBrush(Color.Gray))
            {
                g.DrawString(text, _font, textBrush, 0, this.Y - textHeight/2 + this.Height / 2);
            }
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
