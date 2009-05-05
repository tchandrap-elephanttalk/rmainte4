using System;
using System.Collections;
using System.Windows.Forms;

namespace rmainte4.Controls
{

    /// <summary>
    /// ListViewの項目の並び替えに使用するクラス
    /// </summary>
    //
    //	ListViewItemComparer scheduleListViewSorter = new ListViewItemComparer();
    // 	this.scheduleListViewSorter.ColumnModes = new ListViewItemComparer.ComparerMode[] {
    //                                                ListViewItemComparer.ComparerMode.String,
    //                                                ListViewItemComparer.ComparerMode.DateTime, 
    //                                                ListViewItemComparer.ComparerMode.DateTime };
    // 	this.scheduleListView.ListViewItemSorter = this.scheduleListViewSorter;
    // 
    internal class ListViewItemComparer : IComparer
    {
        /// <summary>
        /// 比較する方法
        /// </summary>
        public enum ComparerMode
        {
            String,
            Integer,
            DateTime
        };

        private int _column;
        private SortOrder _order;
        private ComparerMode _mode;
        private ComparerMode[] _columnModes;

        /// <summary>
        /// 並び替えるListView列の番号
        /// </summary>
        public int Column
        {
            set
            {
                if (_column == value)
                {
                    if (_order == SortOrder.Ascending)
                    {
                        _order = SortOrder.Descending;
                    }
                    else if (_order == SortOrder.Descending)
                    {
                        _order = SortOrder.Ascending;
                    }
                }
                _column = value;
            }
            get
            {
                return _column;
            }
        }

        /// <summary>
        /// 昇順か降順か
        /// </summary>
        public SortOrder Order
        {
            set { _order = value; }
            get { return (_order); }
        }

        /// <summary>
        /// 並び替えの方法
        /// </summary>
        public ComparerMode Mode
        {
            set { _mode = value; }
            get { return (_mode); }
        }

        /// <summary>
        /// 列ごとの並び替えの方法
        /// </summary>
        public ComparerMode[] ColumnModes
        {
            set { _columnModes = value; }
        }

        /// <summary>
        /// ListViewItemComparerクラスのコンストラクタ
        /// </summary>
        /// <param name="col">並び替える列番号</param>
        /// <param name="ord">昇順か降順か</param>
        /// <param name="mthd">並び替えの方法</param>
        public ListViewItemComparer(int col, SortOrder ord, ComparerMode cmod)
        {
            _column = col;
            _order = ord;
            _mode = cmod;
        }

        public ListViewItemComparer()
        {
            _column = 0;
            _order = SortOrder.Ascending;
            _mode = ComparerMode.String;
        }

        //xがyより小さいときはマイナスの数、大きいときはプラスの数、
        //同じときは0を返す
        public int Compare(object x, object y)
        {
            int result = 0;
            //ListViewItemの取得
            ListViewItem itemx = (ListViewItem)x;
            ListViewItem itemy = (ListViewItem)y;

            //並べ替えの方法を決定
            if (_columnModes != null && _columnModes.Length > _column)
            {
                _mode = _columnModes[_column];
            }

            //並び替えの方法別に、xとyを比較する
            try
            {
                switch (_mode)
                {
                    case ComparerMode.String:
                        result = string.Compare(itemx.SubItems[_column].Text, itemy.SubItems[_column].Text);
                        break;
                    case ComparerMode.Integer:
                        result = int.Parse(itemx.SubItems[_column].Text) - int.Parse(itemy.SubItems[_column].Text);
                        break;
                    case ComparerMode.DateTime:

                        result = DateTime.Compare(DateTime.Parse(itemx.SubItems[_column].Text), DateTime.Parse(itemy.SubItems[_column].Text));
                        break;
                }
            }
            catch (Exception) { }

            // 降順の時は結果を+-逆にする
            if (_order == SortOrder.Descending)
            {
                result = -result;
            }
            else if (_order == SortOrder.None)
            {
                result = 0;
            }

            //結果を返す
            return (result);
        }
    }
}
