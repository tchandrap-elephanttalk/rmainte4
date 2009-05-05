using System;
using System.Collections;
using System.Windows.Forms;

namespace rmainte4.Controls
{

    /// <summary>
    /// ListView�̍��ڂ̕��ёւ��Ɏg�p����N���X
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
        /// ��r������@
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
        /// ���ёւ���ListView��̔ԍ�
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
        /// �������~����
        /// </summary>
        public SortOrder Order
        {
            set { _order = value; }
            get { return (_order); }
        }

        /// <summary>
        /// ���ёւ��̕��@
        /// </summary>
        public ComparerMode Mode
        {
            set { _mode = value; }
            get { return (_mode); }
        }

        /// <summary>
        /// �񂲂Ƃ̕��ёւ��̕��@
        /// </summary>
        public ComparerMode[] ColumnModes
        {
            set { _columnModes = value; }
        }

        /// <summary>
        /// ListViewItemComparer�N���X�̃R���X�g���N�^
        /// </summary>
        /// <param name="col">���ёւ����ԍ�</param>
        /// <param name="ord">�������~����</param>
        /// <param name="mthd">���ёւ��̕��@</param>
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

        //x��y��菬�����Ƃ��̓}�C�i�X�̐��A�傫���Ƃ��̓v���X�̐��A
        //�����Ƃ���0��Ԃ�
        public int Compare(object x, object y)
        {
            int result = 0;
            //ListViewItem�̎擾
            ListViewItem itemx = (ListViewItem)x;
            ListViewItem itemy = (ListViewItem)y;

            //���בւ��̕��@������
            if (_columnModes != null && _columnModes.Length > _column)
            {
                _mode = _columnModes[_column];
            }

            //���ёւ��̕��@�ʂɁAx��y���r����
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

            // �~���̎��͌��ʂ�+-�t�ɂ���
            if (_order == SortOrder.Descending)
            {
                result = -result;
            }
            else if (_order == SortOrder.None)
            {
                result = 0;
            }

            //���ʂ�Ԃ�
            return (result);
        }
    }
}
