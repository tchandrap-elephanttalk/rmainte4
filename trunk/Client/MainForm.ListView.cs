using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Data;
using System.Drawing;

namespace rmainte4
{
    public partial class MainForm
    {

        private void OnLoad_ListView_Node()
        {
            _listView_Node.SmallImageList = _imageList;
            _listView_Node.LabelEdit = false;
            _listView_Node.View = View.Details;
            _listView_Node.HideSelection = false;
            _listView_Node.FullRowSelect = true;
            _listView_Node.GridLines = true;
            _listView_Node.AllowDrop = true;

            // イベントハンドラを仕込む
            _listView_Node.ItemDrag += new ItemDragEventHandler(_listView_Node_ItemDrag);
            _listView_Node.MouseUp += new MouseEventHandler(_listView_Node_MouseUp);
            _listView_Node.SelectedIndexChanged += new EventHandler(_listView_Node_SelectedIndexChanged);


            // ラベル編集させない
            // _listView_Node.AfterLabelEdit += new LabelEditEventHandler(_listView_Node_AfterLabelEdit);
        }

        private void _listView_Node_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_listView_Node.SelectedIndices.Count == 1)
            {
                NodeData data = (NodeData)_listView_Node.SelectedItems[0].Tag;
                if (data != null && data.Row != null)
                {
                    _panel_Property.Show();
                    ShowNodeProperty(data);

                    return;
                }
            }

            // 複数選んだり、選択されているアイテムがなくなったりしたら、
            HideNodeProperty();
        }

        private void HideNodeProperty()
        {
            foreach (Control c in _panel_Property.Controls)
            {
                c.DataBindings.Clear();
            }
            _panel_Property.Hide();
        }

        private void ShowNodeProperty(NodeData data)
        {
            _textBox_Hostname.DataBindings.Clear();
            _textBox_Hostname.DataBindings.Add("Text", data, "Hostname");
            _textBox_Hostname.Tag = data;

            _textBox_Address.DataBindings.Clear();
            _textBox_Address.DataBindings.Add("Text", data, "Address");
            _textBox_Address.Tag = data;

            _panel_Property.Show();
        }

        // リストビュー上のアイテムをドラッグしたとき
        private void _listView_Node_ItemDrag(object sender, ItemDragEventArgs e)
        {
            _listView_Node.DoDragDrop(GetDataForDragDrop(), DragDropEffects.Copy | DragDropEffects.Move);
        }

        // 掴んでいる物を入れ物に収める
        private ListViewDragDropData GetDataForDragDrop()
        {
            ListViewDragDropData data = new ListViewDragDropData(_listView_Node);

            foreach (ListViewItem item in _listView_Node.SelectedItems)
            {
                data.DragItems.Add((ListViewItem)item.Clone());
            }

            data.ParentTreeNode = _treeView_Node.SelectedNode;

            return data;
        }


        private void _listView_Node_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            // 単体ノードしか選んでいない
            if (_listView_Node.SelectedItems.Count == 1)
            {

            }



            /*
            if (_treeView_Node.SelectedNode != null)
            {

                // メニューを一度全部enableにする
                for (int i = 0; i < _contextMenuStrip_TreeView.Items.Count; i++)
                {
                    _contextMenuStrip_TreeView.Items[i].Enabled = true;
                }

                if (_treeView_Node.SelectedNode.Parent == null)
                {
                    // ルートノードの場合。編集できないし、削除もできない。アップ・ダウンもできない。
                    _contextMenuStrip_TreeView.Items[(int)TreeViewMenuIndex.EDIT_LABEL].Enabled = false;
                    _contextMenuStrip_TreeView.Items[(int)TreeViewMenuIndex.UP].Enabled = false;
                    _contextMenuStrip_TreeView.Items[(int)TreeViewMenuIndex.DOWN].Enabled = false;
                    _contextMenuStrip_TreeView.Items[(int)TreeViewMenuIndex.DELETE].Enabled = false;
                }
                else
                {
                    DataRow row = (DataRow)_treeView_Node.SelectedNode.Tag;
                    if (row != null && row.Table.Columns.Contains(Database.NODE_COLUMN_ISGROUP))
                    {
                        // 普通のノードだったら、挿入はできない。
                        bool isGroup = (Boolean)row[Database.NODE_COLUMN_ISGROUP];
                        if (isGroup == false)
                        {
                            _contextMenuStrip_TreeView.Items[(int)TreeViewMenuIndex.INSERT_SITE].Enabled = false;
                            _contextMenuStrip_TreeView.Items[(int)TreeViewMenuIndex.INSERT_NODE].Enabled = false;
                        }
                    }
                }
                _contextMenuStrip_TreeView.Show(_treeView_Node, new Point(e.X, e.Y));
            }
            */

        }


        private void _listView_Node_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            ListView lv = (ListView)sender;

            ShowWaitCursor(true);
            try
            {
                lock (_guiLock)
                {
                    lv.BeginUpdate();

                    if (lv.SelectedItems == null || lv.SelectedItems.Count != 1)
                    {
                        e.CancelEdit = true;
                        return;
                    }
                    ListViewItem selected = lv.SelectedItems[0];

                    NodeData data = (NodeData)selected.Tag;
                    if (data == null || data.Row == null)
                    {
                        e.CancelEdit = true;
                        return;
                    }

                    ServerEnv env = data.ServerEnv;
                    if (env == null)
                    {
                        e.CancelEdit = true;
                        return;
                    }

                    // 変わってない。
                    if (e.Label == null)
                    {
                        e.CancelEdit = true;
                        return;
                    }

                    // 消せない
                    if (e.Label.Trim().Length < 1)
                    {
                        e.CancelEdit = true;
                        return;
                    }

                    data.Row[Database.NODE_COLUMN_HOSTNAME] = e.Label;

                    env.ReplaceDataSet(Database.ChangeReason.NodePropertyChanged);
                }
            }
            finally
            {
                lv.EndUpdate();
                ShowWaitCursor(false);
            }
        }


    
    }

    /// <summary>
    /// ドラッグドロップするときに保存する入れ物
    /// </summary>
    public class ListViewDragDropData
    {
        public ListViewDragDropData(ListView listView)
        {
            _listView = listView;
        }

        public ListView ListView
        {
            get { return _listView; }
        }
        private ListView _listView;

        public List<ListViewItem> DragItems
        {
            get { return _dragItems; }
        }
        private List<ListViewItem> _dragItems = new List<ListViewItem>();


        public TreeNode ParentTreeNode
        {
            get { return _parentTreeNode; }
            set { _parentTreeNode = value; }
        }
        private TreeNode _parentTreeNode = null;
    }

}
