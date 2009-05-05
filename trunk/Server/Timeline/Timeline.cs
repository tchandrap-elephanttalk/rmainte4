using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace rmainte4.TimelineControl
{
    public partial class Timeline : UserControl
    {
        // どこにデータを持たせるべきか、という議論はある。
        // Axisの中に持つべきではないかと思っているのだが、後回し。
        // まずはスクロールバーに対応させる。


        // ITimelineItemを格納するリスト。
        // どのAxisに属しているかは関係なく、全てここに格納。
        private List<ITimelineItem> _timelineItems = new List<ITimelineItem>();

        // グラフAxis
        private List<IAxis> _axes = new List<IAxis>();

        // 時刻の部分を描画するクラスオブジェクト
        // 後から変えてもよいが、デフォルトのオブジェクトを設定しておく。
        public ITimeScalePainter TimeScalePainter
        {
            get { return _timeScalePainter; }
            set { _timeScalePainter = value; }
        }
        private ITimeScalePainter _timeScalePainter = new DefaultTimeScalePainter();

        // タイムゾーンの個数
        private int _timeZonesCount;

        // タイムゾーンの幅
        public float TimeZoneWidth
        {
            get { return _timeZoneWidth; }
            set { _timeZoneWidth = value; }
        }
        private float _timeZoneWidth = 50;

        // 現在の拡大倍率
        public int CurrentZoomFactorIndex
        {
            get { return _currentZoomFactorIndex; }
            set
            {
                // ズームしたら、1分当たりのドットドット数を計算し直す
                _currentZoomFactorIndex = value;
                _currentWidthPerMinutes = _timeZoneWidth / _zoomFactors[_currentZoomFactorIndex];
            }
        }
        private int _currentZoomFactorIndex = 0;
        private double _currentWidthPerMinutes = 3000;

        // ズームファクタ。単位は分
        public double[] ZoomFactors
        {
            get { return _zoomFactors; }
            set { _zoomFactors = value; }
        }
        private double[] _zoomFactors = { 1.0 / 60, 1.0 / 6, 1.0 / 2, 1, 2, 4, 8, 10, 12, 15, 20, 25, 30, 60, 120 };
        
        // 時刻の左側を空けるかどうか。
        // Axisで名前(DrawText)を描画するなら、trueの方がよい。
        public bool OffsetTimelineFromAxis
        {
            get { return _offsetTimeLineFromAxis; }
            set { _offsetTimeLineFromAxis = value; }
        }
        private bool _offsetTimeLineFromAxis = true;

        // 現在のウィンドウ幅で始まりの時刻と終わりの時刻
        private DateTime _windowStartTime = DateTime.Now.Subtract(TimeSpan.FromMinutes(1));
        private DateTime _windowEndTime = DateTime.Now.AddMinutes(1);

        public DateTime WorldStartTime
        {
            get { return _worldStartTime; }
            set { _windowStartTime = value; }
        }
        private DateTime _worldStartTime = DateTime.Now.Subtract(TimeSpan.FromMinutes(1));

        public DateTime WorldEndTime
        {
            get { return _worldEndTime; }
            set { _windowEndTime = value; }
        }
        private DateTime _worldEndTime = DateTime.Now.AddMinutes(1);

        public float AxisHeight
        {
            get { return _axisHeight; }
            set
            {
                if (value <= 0)
                {
                    _axisHeight = value;
                }
            }
        }
        private float _axisHeight = 20;

        private int _refreshInterval = 1000;
        private System.Threading.Timer _refreshTimer;
        private void StartRefreshTimer()
        {
            _refreshTimer.Change(0, 1000);
        }
        private void StopRefreshTimer()
        {
            _refreshTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }


        // コンストラクタ
        public Timeline()
        {
            InitializeComponent();

            // ダブルバッファを使うおまじない
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            this.BackColor = Color.Black;

            _refreshTimer = new System.Threading.Timer(new System.Threading.TimerCallback(OnRefresh), null, 0, _refreshInterval);
        }

        private void OnRefresh(Object o)
        {
            MoveTimeWindowRight(1/60F);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetWindow(_windowStartTime, _windowEndTime, false);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            SetAxes();
            SetWindow(_windowStartTime, _windowEndTime, false);
            this.Invalidate();

            base.OnSizeChanged(e);
        }

        private void SetWindow(DateTime expectedWindowStart, DateTime expectedWindowEnd, bool autoCalculateZoom)
        {
            if (expectedWindowStart < _worldStartTime)
            {
                return;
            }

            if (expectedWindowEnd > _worldEndTime)
            {
                return;
            }

            _windowStartTime = expectedWindowStart;
            _windowEndTime = expectedWindowEnd;

            if (this.Width == 0)
            {
                return;
            }

            if (autoCalculateZoom)
            {
                int zoomFactorIndex = CalculateDefaultZoomIndex(_windowStartTime, _windowEndTime);
                SetZoom(zoomFactorIndex);
            }
            else
            {
                SetZoom(_currentZoomFactorIndex);
            }
        }

        public void AddAxis(IAxis axis)
        {
            // スレッドセーフにするためロックをかける
            lock (_axes)
            {
                // リストに加え、
                _axes.Add(axis);
            }

            // 高さとか、各枠の高さを計算する
            SetAxes();
        }


        // ITimelineItemの中で最大の時刻の物を記録する。
        private DateTime _itemDateMax = DateTime.MinValue;

        public void AddItem(ITimelineItem item)
        {
            AddItem(new ITimelineItem[] { item });
        }

        public void AddItem(ITimelineItem[] items)
        {
            if (items == null || items.Length == 0)
            {
                return;
            }

            lock (_timelineItems)
            {
                foreach (ITimelineItem item in items)
                {
                    if (_itemDateMax < item.ItemEndTime)
                    {
                        _itemDateMax = item.ItemEndTime;
                    }
                    _timelineItems.Add(item);
                }
               
                _timelineItems.AddRange(items);

                // int width = (int)Math.Ceiling((_itemDateMax - _worldStartTime).TotalMinutes * _currentWidthPerMinutes);
                // int height = (int)Math.Ceiling(_axes.Count * _axisHeight);
                // SetAutoScrollMinSize(new Size(width, height));
            }
        }

        private delegate void SetAutoScrollMinSizeDelegate(Size size);
        private void SetAutoScrollMinSize(Size size)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new SetAutoScrollMinSizeDelegate(SetAutoScrollMinSize), new object[] { size });
                return;
            }

            this.AutoScrollMinSize = size;
        }

        private void SetAxes()
        {
            lock (_axes)
            {
                if (_axes.Count == 0)
                {
                    return;
                }

                Graphics g = CreateGraphics();
                float timelineLeft = _timeScalePainter.Left;
                float y = _timeScalePainter.Height;
                foreach (IAxis axis in _axes)
                {
                    axis.Y = y;
                    axis.Height = _axisHeight;
                    y += _axisHeight;

                    // 一番長い名前の長さ
                    timelineLeft = Math.Max(axis.GetNameWidth(g), timelineLeft);
                }

                if (_offsetTimeLineFromAxis)
                {
                    _timeScalePainter.Left = timelineLeft;
                }
            }
        }

        public void SetTotalTimeWindow(DateTime start, DateTime end)
        {
            _worldStartTime = start;
            _worldEndTime = end;

            _windowStartTime = (_windowStartTime == DateTime.MinValue ? _worldStartTime : _windowStartTime);
            _windowEndTime = (_windowEndTime == DateTime.MaxValue ? _worldEndTime : _windowEndTime);
        }

        public void SetInitialWindow(DateTime startTime, DateTime endTime)
        {
            SetWindow(startTime, endTime, false);
        }

        public void SetInitialWindow(DateTime startTime, int zoomFactorIndex)
        {
            SetZoom(zoomFactorIndex);
            SetWindow(startTime, _worldEndTime, false);
        }

        private int CalculateDefaultZoomIndex(DateTime expectedWindowStart, DateTime expectedWindowEnd)
        {
            double minutesInWindow = (expectedWindowEnd - expectedWindowStart).TotalMinutes;

            int tempZoomFactorIndex = 0;

            double tempTimeZones = minutesInWindow / _zoomFactors[tempZoomFactorIndex];

            while ((tempTimeZones * _timeZoneWidth + _timeScalePainter.Left) > this.ClientRectangle.Width)
            {
                tempZoomFactorIndex++;

                if (tempZoomFactorIndex >= _zoomFactors.Length)
                    break;

                tempTimeZones = minutesInWindow / _zoomFactors[tempZoomFactorIndex];
            }

            return tempZoomFactorIndex;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _refreshTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _refreshTimer.Change(0, 1000);
        }


        private Point _oldPosition;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.Left)
            {
                Point newPosition = e.Location;

                if (newPosition.X > _oldPosition.X)
                {
                    MoveTimeWindowLeft();
                }
                else if (newPosition.X < _oldPosition.X)
                {
                    MoveTimeWindowRight();
                }

                _oldPosition = newPosition;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (e.Delta < 0)
            {
                MoveTimeWindowLeft();
            }
            else if (e.Delta > 0)
            {
                MoveTimeWindowRight();
            }

        }

        public void MoveTimeWindowLeft()
        {
            double minutesForTimeSpan = _zoomFactors[_currentZoomFactorIndex];
            MoveTimeWindowLeft(minutesForTimeSpan);
        }

        public void MoveTimeWindowLeft(double minutes)
        {
            DateTime targetWindowStart = _windowStartTime.Subtract(TimeSpan.FromMinutes(minutes));

            DateTime targetWindowEnd = _windowEndTime.Subtract(TimeSpan.FromMinutes(minutes));
            this.SetWindow(targetWindowStart, targetWindowEnd, false);
            this.Invalidate();
        }

        public void MoveTimeWindowRight()
        {
            double minutesForTimeSpan = _zoomFactors[_currentZoomFactorIndex];
            MoveTimeWindowRight(minutesForTimeSpan);
        }

        public void MoveTimeWindowRight(double minutes)
        {
            DateTime targetWindowStart = _windowStartTime.Add(TimeSpan.FromMinutes(minutes));
            if (targetWindowStart >= DateTime.Now)
            {
                return;
            }
            
            DateTime targetWindowEnd = _windowEndTime.Add(TimeSpan.FromMinutes(minutes));

            this.SetWindow(targetWindowStart, targetWindowEnd, false);

            this.Invalidate();
        }

        public void ZoomIn()
        {
            if (_currentZoomFactorIndex == 0)
            {
                return;
            }

            double centerOfCurrentTimeWindow = (_windowEndTime - _windowStartTime).TotalMinutes / 2;

            DateTime targetWindowStart = _windowStartTime.AddMinutes(centerOfCurrentTimeWindow / 2);
            DateTime targetWindowEnd = _windowEndTime.Subtract(TimeSpan.FromMinutes(centerOfCurrentTimeWindow / 2));

            if (targetWindowStart <= _worldStartTime)
            {
                targetWindowStart = _worldStartTime;
            }

            if (targetWindowEnd >= _worldEndTime)
            {
                targetWindowEnd = _worldEndTime;
            }

            if (targetWindowEnd >= _itemDateMax)
            {
                targetWindowEnd = _itemDateMax;
            }

            SetZoom(_currentZoomFactorIndex - 1);
            CenterWindow(targetWindowStart, targetWindowEnd);

            this.Invalidate();
        }

        public void ZoomOut()
        {
            if (_currentZoomFactorIndex == _zoomFactors.Length - 1)
            {
                return;
            }

            double centerOfCurrentTimeWindow = (_windowEndTime - _windowStartTime).TotalMinutes / 2;

            DateTime targetWindowStart = _windowStartTime.Subtract(TimeSpan.FromMinutes(centerOfCurrentTimeWindow / 2));
            DateTime targetWindowEnd = _windowEndTime.AddMinutes(centerOfCurrentTimeWindow / 2);

            if (targetWindowStart <= _worldStartTime)
            {
                targetWindowStart = _worldStartTime;
            }

            if (targetWindowEnd >= _worldEndTime)
            {
                targetWindowEnd = _worldEndTime;
            }

            if (targetWindowEnd >= _itemDateMax)
            {
                targetWindowEnd = _itemDateMax;
            }

            SetZoom(_currentZoomFactorIndex + 1);
            CenterWindow(targetWindowStart, targetWindowEnd);

            this.Invalidate();
        }

        public void SetZoom(int zoomFactorIndex)
        {
            if (zoomFactorIndex < 0 || zoomFactorIndex > _zoomFactors.Length - 1)
            {
                return;
            }

            CurrentZoomFactorIndex = zoomFactorIndex;
            double zoomFactor = _zoomFactors[_currentZoomFactorIndex];

            // Restrict to world time end, otherwise, draw as many timezones as possible.
            _timeZonesCount = (int)Math.Min(
                Math.Ceiling((_worldEndTime - _windowStartTime).TotalMinutes / zoomFactor),
                Math.Ceiling((this.Width - _timeScalePainter.Left) / _timeZoneWidth)
            );

            _windowEndTime = _windowStartTime.AddMinutes(_timeZonesCount * zoomFactor);
        }

        private void CenterWindow(DateTime targetWindowStart, DateTime targetWindowEnd)
        {
            double absoluteMiddle = targetWindowEnd.Subtract(targetWindowStart).TotalMinutes;
            DateTime middleTime = targetWindowStart.AddMinutes(absoluteMiddle);

            int maxTimeZones = (int)((this.Width - _timeScalePainter.Left) / _timeZoneWidth);
            int timeZonesOnEitherSide = maxTimeZones / 2;

            DateTime start = middleTime.Subtract(TimeSpan.FromMinutes(timeZonesOnEitherSide * _zoomFactors[_currentZoomFactorIndex]));
            DateTime end = middleTime.Add(TimeSpan.FromMinutes(timeZonesOnEitherSide * _zoomFactors[_currentZoomFactorIndex]));
            SetWindow(start, end, false);
        }

        // 時刻からXの位置を計算する
        // [0]はその時刻のX位置、[1]はタイムゾーンの左側、[2]はタイムゾーンの右側
        private float[] GetXCoordinateFromTime(DateTime time)
        {
            double diff = (time - _windowStartTime).TotalMinutes / _zoomFactors[_currentZoomFactorIndex];
            float timezoneLeft = (float)(Math.Floor(diff) * _timeZoneWidth) + _timeScalePainter.Left;
            float timezoneRight = (float)(Math.Ceiling(diff) * _timeZoneWidth) + _timeScalePainter.Left;
            float x = (float)(diff * _timeZoneWidth) + _timeScalePainter.Left;

            return new float[] { x, timezoneLeft, timezoneRight };
            // return (float)((time - windowStart).TotalMinutes / _zoomFactors[_currentZoomFactorIndex] * _timeZoneWidth) + _timeScalePainter.Left;
        }

        private readonly Pen _penBlack = new Pen(Color.Black, 2);

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            // 座標の意味については、ここが詳しい
            // http://www.atmarkit.co.jp/fdotnet/practprog/wisearch02/wisearch02_03.html

            double zoomFactor = _zoomFactors[_currentZoomFactorIndex];

            // タイムスケールを描く
            _timeScalePainter.DrawTimeScale(e.Graphics, _windowStartTime, zoomFactor, _timeZonesCount, _timeZoneWidth, this.Height);

            // 各グラフの枠と名前を描画する
            lock (_axes)
            {
                foreach (IAxis axis in _axes)
                {
                    axis.Draw(e.Graphics, this.ClientRectangle.Width);
                }
            }

            // 各ITimelineItemに「描け」という指令を送る
            lock (_timelineItems)
            {
                foreach (ITimelineItem timelineItem in _timelineItems)
                {
                    // アイテムの作成時間を見て、枠内に入るアイテムだけを描画する
                    if (timelineItem.ItemEndTime >= _windowStartTime && timelineItem.ItemStartTime <= _windowEndTime)
                    {
                        if (timelineItem.Axis != null)
                        {
                            timelineItem.Draw(e.Graphics, GetXCoordinateFromTime, timelineItem.Axis.Y, _windowStartTime, _windowEndTime);
                        }
                    }
                }
            }

        }
    }
}