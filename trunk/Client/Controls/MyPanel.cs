using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace rmainte4.Controls
{
    public class MyPanel : System.Windows.Forms.Panel
    {
        public enum BorderStyleType : int
        {
            None = 0,
            Dash = 2,
            DashDot = 4,
            DashDotDot = 5,
            Dot = 3,
            Solid = 1,
        }

        public MyPanel()
        {
            base.BackColor = Color.Transparent;
            base.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._BorderColor = Color.Black;
            this._BorderStyle = BorderStyleType.None;
            this._BorderWidth = 1;
            this._Curvature = 0;
        }


        protected bool _isDragging = false;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if ((e.Button & MouseButtons.Left) == 0)
            {
                _isDragging = false;
            }
            else
            {
                _isDragging = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if ((e.Button & MouseButtons.Left) == 0)
            {
                if (_isDragging == false)
                {
                    return;
                }
            }

            if (_isDragging)
            {
                DragDropEffects dropEffect = this.DoDragDrop(this, DragDropEffects.All);
                _isDragging = false;
                return;
            }

        }





        protected override void OnMouseEnter(EventArgs e)
        {
            this.BorderColor = Color.Blue;
            this.BorderStyle = BorderStyleType.Solid;
            this.BorderWidth = 5;
            this.Curvature = 10;

            base.OnMouseEnter(e);
        }


        protected override void OnMouseLeave(EventArgs e)
        {
            this.BorderColor = Color.Black;
            this.BorderStyle = BorderStyleType.Solid;
            this.BorderWidth = 2;
            this.Curvature = 10;

            base.OnMouseLeave(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if ((this.BorderStyle != BorderStyleType.None) && (this.Curvature > 0))
            {
                base.OnPaintBackground(e);
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            if (this.BorderStyle == BorderStyleType.None)
            {
                // 背景のみ描画
                using (SolidBrush sb = new SolidBrush(this.BackColor))
                {
                    pe.Graphics.FillRectangle(sb, this.ClientRectangle);
                }
            }
            else
            {
                using (Pen p = new Pen(this.BorderColor, this.BorderWidth))
                {
                    p.DashStyle = this.ConvertToDashStyle(this.BorderStyle);
                    Rectangle r = this.ClientRectangle;
                    
                    // 境界を太くしているので
                    r.X += r.X + Convert.ToInt32(decimal.Floor(Convert.ToDecimal(this.BorderWidth / 2)));
                    r.Y += r.Y + Convert.ToInt32(decimal.Floor(Convert.ToDecimal(this.BorderWidth / 2)));
                    r.Width -= this.BorderWidth;
                    r.Height -= this.BorderWidth;

                    if (this.Curvature == 0)
                    {
                        // 通常の境界線を描画
                        using (SolidBrush sb = new SolidBrush(this.BackColor))
                        {
                            pe.Graphics.FillRectangle(sb, this.ClientRectangle);
                        }
                        pe.Graphics.DrawRectangle(p, r);
                    }
                    else
                    {
                        // 角が丸い境界線を描画
                        int w = this.Curvature;
                        if (this.Curvature > r.Width)
                        {
                            w = r.Width;
                        }
                        int h = this.Curvature;
                        if (this.Curvature > r.Height)
                        {
                            h = r.Height;
                        }

                        GraphicsPath gp = new GraphicsPath();
                        gp.StartFigure();

                        // 右上
                        gp.AddArc(r.Right - w, r.Top, w, h, 270, 90);
                        
                        // 右下
                        gp.AddArc(r.Right - w, r.Bottom - h -10, w, h, 0, 90);

                        // 右下から左中央側に直線
                        gp.AddLine(new Point(r.Right -w, r.Bottom -10), new Point(r.Left + r.Width/2 + 10, r.Bottom -10));

                        // 三角部分の右
                        gp.AddLine(new Point(r.Left + r.Width / 2 + 10, r.Bottom -10), new Point(r.Left + r.Width / 2, r.Bottom));
                        
                        // 三角部分の左
                        gp.AddLine(new Point(r.Left + r.Width / 2, r.Bottom), new Point(r.Left + r.Width / 2 -10, r.Bottom -10));
                        
                        gp.AddArc(r.Left, r.Bottom - h -10, w, h, 90, 90);
                        gp.AddArc(r.Left, r.Top, w, h, 180, 90);


                        gp.CloseFigure();

                        using (SolidBrush sb = new SolidBrush(this.BackColor))
                        {
                            pe.Graphics.FillPath(sb, gp);
                        }

                        SmoothingMode sm = pe.Graphics.SmoothingMode;
                        pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        pe.Graphics.DrawPath(p, gp);
                        pe.Graphics.SmoothingMode = sm;
                    }
                }
            }
        }

        private DashStyle ConvertToDashStyle(BorderStyleType style)
        {
            return (DashStyle)style - 1;
        }

        private Color _BackColor;
        public new Color BackColor
        {
            get
            {
                if (this._BackColor != Color.Empty)
                {
                    return this._BackColor;
                }

                if (this.Parent != null)
                {
                    return this.Parent.BackColor;
                }

                return Control.DefaultBackColor;
            }
            set
            {
                this._BackColor = value;
                this.Invalidate();
            }
        }

        public override void ResetBackColor()
        {
            this.BackColor = Color.Empty;
        }

        private Boolean ShouldSerializeBackColor()
        {
            return this._BackColor != Color.Empty;
        }



        private Color _BorderColor;
        [Category("表示")]
        [DefaultValue(typeof(Color), "Black")]
        [Description("コントロールの境界線色を取得または設定します。")]
        public Color BorderColor
        {
            get { return this._BorderColor; }
            set
            {
                this._BorderColor = value;
                this.Invalidate();
            }
        }

        private BorderStyleType _BorderStyle;
        [Category("表示")]
        [DefaultValue(typeof(BorderStyleType), "None")]
        [Description("コントロールの境界線スタイルを取得または設定します。")]
        public new BorderStyleType BorderStyle
        {
            get { return this._BorderStyle; }
            set
            {
                this._BorderStyle = value;
                this.Invalidate();
            }
        }

        private int _BorderWidth;
        [Category("表示")]
        [DefaultValue(1)]
        [Description("コントロールの境界線の幅を取得または設定します。")]
        public int BorderWidth
        {
            get { return this._BorderWidth; }
            set
            {
                this._BorderWidth = value;
                this.Invalidate();
            }
        }

        private int _Curvature;
        [Category("表示")]
        [DefaultValue(0)]
        [Description("コントロールの境界線の角の半径を取得または設定します。")]
        public int Curvature
        {
            get { return this._Curvature; }
            set
            {
                this._Curvature = value;
                this.Invalidate();
            }
        }

    }
}
