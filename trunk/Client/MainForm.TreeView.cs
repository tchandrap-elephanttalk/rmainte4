using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics; 

// TreeView関係だけ、ここに独立。

namespace rmainte4
{
    public partial class MainForm
    {
        private void OnLoad_TreeView()
        {
            _treeView_Node.AllowDrop = true;
            _treeView_Node.HideSelection = false;
            _treeView_Node.ImageList = _imageList;
            _treeView_Node.ShowRootLines = false;

            // メニュー構築
            _contextMenuStrip_TreeView.Items.Add(MENU_STRING_INSERT_SITE, null, new EventHandler(treeViewRightClickInsertGroup));  // 0
            _contextMenuStrip_TreeView.Items.Add(MENU_STRING_INSERT_NODE, null, new EventHandler(treeViewRightClickInsertNode));   // 1
            _contextMenuStrip_TreeView.Items.Add(MENU_STRING_EDIT_LABEL, null, new EventHandler(treeViewRightClickEdit));          // 2
            _contextMenuStrip_TreeView.Items.Add(MENU_STRING_UP, null, new EventHandler(treeViewRightClickNudgeUp));               // 3
            _contextMenuStrip_TreeView.Items.Add(MENU_STRING_DOWN, null, new EventHandler(treeViewRightClickNudgeDown));           // 4
            _contextMenuStrip_TreeView.Items.Add(MENU_STRING_DELETE, null, new EventHandler(treeViewRightClickDelete));            // 5
            _contextMenuStrip_TreeView.Items.Add(MENU_STRING_RECONNECT, null, new EventHandler((treeViewRightClickReconnect)));    // 6

            // イベントハンドラを仕込む
            _treeView_Node.AfterLabelEdit += new NodeLabelEditEventHandler(_treeView_Node_AfterLabelEdit);
            _treeView_Node.DragDrop += new DragEventHandler(_treeView_Node_DragDrop);
            _treeView_Node.DragEnter += new DragEventHandler(_treeView_Node_DragEnter);
            _treeView_Node.DragOver += new DragEventHandler(_treeView_Node_DragOver);
            _treeView_Node.ItemDrag += new ItemDragEventHandler(_treeView_Node_ItemDrag);
            _treeView_Node.MouseDown += new MouseEventHandler(_treeView_Node_MouseDown);
            _treeView_Node.MouseUp += new MouseEventHandler(_treeView_Node_MouseUp);
            _treeView_Node.AfterSelect += new TreeViewEventHandler(_treeView_Node_AfterSelect);

            ShowWaitCursor(true);
            try
            {
                foreach (ServerEnv env in _serverEnvList)
                {
                    if (env.IsConnected == false || env.DataSet == null)
                    {
                        continue;
                    }

                    // データセットからTreeViewを復元する
                    LoadTree(_treeView_Node, env);
                }
            }
            catch (Exception err)
            {
                ShowWaitCursor(false);
                ShowError(err.Message + Environment.NewLine + err.StackTrace);
            }
            finally
            {
                ShowWaitCursor(false);
            }
        }


        /// <summary>
        /// ツリービューに表示するイメージのインデックスです。
        /// </summary>
        public enum TreeViewImageIndex { Root = 0, Site = 1, Router = 2 };
        
        /// <summary>
        /// ツリービューに表示するメニューのインデックスです。
        /// </summary>
        public enum TreeViewMenuIndex { INSERT_SITE = 0, INSERT_NODE, EDIT_LABEL, UP, DOWN, DELETE, RECONNECT };

        private static string MENU_STRING_NEW_NODE_NAME = IsJapanese ? "新しいノード" : "New Site";
        private static string MENU_STRING_NEW_SITE_NAME = IsJapanese ? "新しいグループ" : "New Group";
        private static string MENU_STRING_INSERT_SITE = IsJapanese ? "グループの挿入" : "Insert Group";
        private static string MENU_STRING_INSERT_NODE = IsJapanese ? "ノードの挿入" : "Insert Node";
        private static string MENU_STRING_EDIT_LABEL = IsJapanese ? "名前の変更" : "Edit Label";
        private static string MENU_STRING_UP = IsJapanese ? "上に移動" : "Up";
        private static string MENU_STRING_DOWN = IsJapanese ? "下に移動" : "Down";
        private static string MENU_STRING_DELETE = IsJapanese ? "削除" : "Delete";
        private static string MENU_STRING_RECONNECT = IsJapanese ? "再接続" : "Reconnect";


        #region LoadTree(TreeView tv, ServerEnv env)
        private delegate void LoadTreeDelegate(TreeView tv, ServerEnv env);
        public void LoadTree(ServerEnv env)
        {
            LoadTree(_treeView_Node, env);
        }
        private void LoadTree(TreeView tv, ServerEnv env)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new LoadTreeDelegate(LoadTree), new object[] { tv, env });
                return;
            }

            lock (_guiLock)
            {
                try
                {
                    tv.BeginUpdate();
                    ShowWaitCursor(true);

                    // プロパティ情報が表示されているなら、それを消す。
                    HideNodeProperty();

                    // rootノードのエントリがないと処理できない
                    if (env.DataSet.Tables[Database.NODE_TABLE].Rows.Count < 1)
                    {
                        return;
                    }

                    // 最初に既に構築されているツリーを削除する。
                    // TreeViewを全部消去するのではなく、そのサーバに該当するルートノードだけを削除する
                    TreeNode root = FindRootByServerEnvOrNull(tv, env);
                    if (root != null)
                    {
                        root.Remove(); // もちろんデータベース上は消えない
                    }

                    // ルートノードに対応するデータベースのエントリを取得。常にテーブルの0行目。
                    DataRow rootRow = env.DataSet.Tables[Database.NODE_TABLE].Rows[0];

                    // ルートノードに対応するツリーノードを新たに作成
                    root = GetTreeNodeFromDataRow(rootRow, env);
                    if (root == null)
                    {
                        return;
                    }

                    // ルートノードのテキストをサーバのアドレスに変更する。データベース上は変わらない。
                    root.Text = env.Address;

                    // ルートノードをツリービューに追加
                    tv.Nodes.Add(root);

                    if (env.DataSet.Tables[Database.NODE_TABLE].Rows.Count > 1)
                    {
                        // ルートノード以外にノードが存在するなら、子ノード達を追加。
                        // ここから再起処理。
                        AddNodeFromDataRow(root, rootRow, env);
                    }

                    root.Expand();
                }
                finally
                {
                    tv.EndUpdate();
                    ShowWaitCursor(false);
                }
            }

        }
        #endregion

        public void FixTreeNodeText()
        {
            // TreeNodeのタグにはNodeDataが入っている。
            // そのDataRowのhostname値と実際のラベルが食い違っていたら修正する。

            foreach (TreeNode node in _treeView_Node.Nodes)
            {
                FixTreeNodeText(node);
            }
        }

        private void FixTreeNodeText(TreeNode node)
        {
            NodeData data = (NodeData)node.Tag;
            if (data == null || data.Row == null)
            {
                return;
            }

            string newHostname = (string)data.Row[Database.NODE_COLUMN_HOSTNAME];

            if (node.Text.Equals(newHostname) == false)
            {
                // 食い違っているなら、直す。
                ChangeTreeNodeText(node, newHostname);
            }

            foreach (TreeNode child in node.Nodes)
            {
                FixTreeNodeText(child);
            }
        }

        private delegate void ChangeTreeNodeTextDelegate(TreeNode node, string newLabel);
        private void ChangeTreeNodeText(TreeNode node, string newLabel)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new ChangeTreeNodeTextDelegate(ChangeTreeNodeText), new object[] { node, newLabel });
                return;
            }

            lock (_guiLock)
            {
                _treeView_Node.BeginUpdate();
                node.Text = newLabel;
                _treeView_Node.EndUpdate();
            }
        }

        /// <summary>
        /// ツリービューの各ノードはタグにServerEnvオブジェクトを所有しています。
        /// ツリービューのルートノード群から、指定したServerEnvをタグに持つものを返します。
        /// </summary>
        /// <param name="tv">対象となるツリービュー</param>
        /// <param name="env">ServerEnvオブジェクトでこれと同じ物をタグに持つノードを返します。</param>
        /// <returns></returns>
        private static TreeNode FindRootByServerEnvOrNull(TreeView tv, ServerEnv env)
        {
            foreach (TreeNode node in tv.Nodes)
            {
                NodeData data = (NodeData)node.Tag;
                if (data.ServerEnv == null)
                {
                    continue;
                }
                if (data.ServerEnv == env)
                {
                    return node;
                }
            }
            return null;
        }

        #region AddNodeFromDataRow(TreeNode parentNode, DataRow row, ServerEnv env)
        private static void AddNodeFromDataRow(TreeNode parentNode, DataRow parentRow, ServerEnv env)
        {
            try
            {
                string dataRelationName = Database.FindForeignKeyRelationName(parentRow.Table);
                if (String.IsNullOrEmpty(dataRelationName))
                {
                    return;
                }

                // データベースから子ノードを取り出す
                DataRow[] rows = parentRow.GetChildRows(dataRelationName);
                if (rows == null || rows.Length == 0)
                {
                    return;
                }

                // "sortOrder"に順番が入っているので、配列をソートする
                Array.Sort(rows, new DataRowComparer());

                foreach (DataRow childRow in rows)
                {
                    if ((Int32)childRow[Database.NODE_COLUMN_NODE_ID] == 0)
                    {
                        // ルートノードは、親が自分になってしまうので、パス。
                        continue;
                    }

                    TreeNode node = GetTreeNodeFromDataRow(childRow, env);
                    if (node == null)
                    {
                        continue;
                    }
                    parentNode.Nodes.Add(node);

                    // 再帰呼び出し。
                    AddNodeFromDataRow(node, childRow, env);
                }
            }
            catch (Exception) { throw; }
        }
        #endregion

        #region GetTreeNodeFromDataRow(DataRow row, string textColumnName)
        private static TreeNode GetTreeNodeFromDataRow(DataRow row, ServerEnv env)
        {
            // グループの場合のみ作成してみる。
            if ((bool)row[Database.NODE_COLUMN_ISGROUP] == false)
            {
                return null;
            }

            TreeNode node = new TreeNode();

            node.Tag = new NodeData(row, env, node);

            node.Text = ((String)row[Database.NODE_COLUMN_HOSTNAME]).Trim();

            if (row.Table.Columns.Contains(Database.NODE_COLUMN_IMAGEINDEX))
            {
                node.ImageIndex = (Int32)row[Database.NODE_COLUMN_IMAGEINDEX];
            }

            if (row.Table.Columns.Contains(Database.NODE_COLUMN_SELECTED_IMAGEINDEX))
            {
                node.SelectedImageIndex = (Int32)row[Database.NODE_COLUMN_SELECTED_IMAGEINDEX];
            }
            return node;
        }
        #endregion

        #region InsertNewNode(TreeNode node, DataSet ds, bool isGroup)
        private static void InsertNewNode(TreeNode parentNode, DataSet ds, bool isGroup)
        {
            NodeData parentTag = (NodeData)parentNode.Tag;
            if (parentTag == null || parentTag.ServerEnv == null || parentTag.Row == null)
            {
                return;
            }

            DataRow parentRow = parentTag.Row;

            // データベースに新しい行を追加する
            DataRow newRow = ds.Tables[Database.NODE_TABLE].NewRow();

            newRow[Database.NODE_COLUMN_PARENT_ID] = (Int32)parentRow[ds.Tables[Database.NODE_TABLE].PrimaryKey[0].ColumnName];
            newRow[Database.NODE_COLUMN_MODELID] = (Int32)parentRow[Database.NODE_COLUMN_MODELID];
            newRow[Database.NODE_COLUMN_SORT_ORDER] = (Int32)parentRow[Database.NODE_COLUMN_SORT_ORDER] + 1;
            newRow[Database.NODE_COLUMN_ISGROUP] = isGroup;
            if (isGroup)
            {
                newRow[Database.NODE_COLUMN_HOSTNAME] = MENU_STRING_NEW_SITE_NAME;
                newRow[Database.NODE_COLUMN_IMAGEINDEX] = TreeViewImageIndex.Site;
                newRow[Database.NODE_COLUMN_SELECTED_IMAGEINDEX] = TreeViewImageIndex.Site;
            }
            else
            {
                newRow[Database.NODE_COLUMN_HOSTNAME] = MENU_STRING_NEW_NODE_NAME;
                newRow[Database.NODE_COLUMN_IMAGEINDEX] = TreeViewImageIndex.Router;
                newRow[Database.NODE_COLUMN_SELECTED_IMAGEINDEX] = TreeViewImageIndex.Router;
            }

            ds.Tables[Database.NODE_TABLE].Rows.Add(newRow);

            if (isGroup)
            {
                // グループの場合だけ、ツリービューに表示する
                parentNode.Nodes.Add(GetTreeNodeFromDataRow(newRow, parentTag.ServerEnv));
            }
            else
            {
                // ノードはツリービューに表示しないことにする
            }
        }
    
        #endregion

        #region treeViewRightClickInsertGroup
        private void treeViewRightClickInsertGroup(object sender, System.EventArgs e)
        {
            lock (_guiLock)
            {
                try
                {
                    ShowWaitCursor(true);

                    TreeNode selected = _treeView_Node.SelectedNode;
                    if (selected == null)
                    {
                        return;
                    }

                    NodeData data = (NodeData)selected.Tag;
                    if (data == null || data.ServerEnv == null || data.ServerEnv.IsConnected == false)
                    {
                        return;
                    }

                    ServerEnv env = data.ServerEnv;

                    if (env.GetLock())
                    {
                        _treeView_Node.BeginUpdate();
                        InsertNewNode(selected, env.DataSet, true);
                        _treeView_Node.EndUpdate();

                        // 広げて見せる
                        selected.Expand();

                        // サーバ側にアップロードする
                        env.ReplaceDataSet(Database.ChangeReason.NodeAdded);

                        env.ReleaseLock();
                    }
                    else
                    {
                        ShowTip(MESSAGE_LOCK_FAILED);
                    }
                }
                finally
                {
                    ShowWaitCursor(false);
                }
            }
        }
        #endregion

        #region treeViewRightClickInsertNode
        private void treeViewRightClickInsertNode(object sender, System.EventArgs e)
        {
            try
            {
                lock (_guiLock)
                {
                    ShowWaitCursor(true);

                    TreeNode selected = _treeView_Node.SelectedNode;
                    if (selected == null)
                    {
                        return;
                    }

                    NodeData data = (NodeData)selected.Tag;
                    if (data == null || data.ServerEnv == null || data.ServerEnv.IsConnected == false)
                    {
                        return;
                    }

                    ServerEnv env = data.ServerEnv;
                    if (env.GetLock())
                    {
                        InsertNewNode(selected, env.DataSet, false);

                        // 選択しなおさないとリストビューに追加したノードが表示されない
                        _treeView_Node_AfterSelect(_treeView_Node, new TreeViewEventArgs(selected));

                        // サーバ側にアップロードする
                        env.ReplaceDataSet(Database.ChangeReason.NodeAdded);

                        env.ReleaseLock();
                    }
                    else
                    {
                        ShowTip(MESSAGE_LOCK_FAILED);
                    }
                }
            }
            finally
            {
                ShowWaitCursor(false);
            }
        }
        #endregion

        #region treeViewRightClickEdit
        private void treeViewRightClickEdit(object sender, System.EventArgs e)
        {
            lock (_guiLock)
            {
                try
                {
                    ShowWaitCursor(true);

                    TreeNode selected = _treeView_Node.SelectedNode;
                    if (selected == null)
                    {
                        return;
                    }

                    selected.TreeView.LabelEdit = true;
                    selected.BeginEdit();
                }
                finally
                {
                    ShowWaitCursor(false);
                }
            }
        }
        #endregion

        #region treeViewRightClickNudgeUp
        private void treeViewRightClickNudgeUp(object sender, System.EventArgs e)
        {
            lock (_guiLock)
            {
                try
                {
                    ShowWaitCursor(true);

                    TreeNode selected = _treeView_Node.SelectedNode;
                    if (selected == null)
                    {
                        return;
                    }

                    NodeData data = (NodeData)selected.Tag;
                    if (data == null)
                    {
                        return;
                    }

                    ServerEnv env = data.ServerEnv;
                    if (env == null || env.IsConnected == false)
                    {
                        return;
                    }

                    if (env.GetLock())
                    {
                        NudgeUp(selected);

                        env.ReplaceDataSet(Database.ChangeReason.NodeMoved);

                        env.ReleaseLock();
                    }
                    else
                    {
                        ShowTip(MESSAGE_LOCK_FAILED);
                    }
                }
                finally
                {
                    ShowWaitCursor(false);
                }
            }
        }
        #endregion

        #region NudgeUp(TreeNode node) このメソッドはデータベースを変更します。
        private static void NudgeUp(TreeNode node)
        {
            if (node == null)
            {
                return;
            }

            if (node.Index == 0)
            {
                return;
            }

            int newIndex = node.Index - 1;

            TreeNode nodeClone = (TreeNode)node.Clone();

            node.Parent.Nodes.Insert(newIndex, nodeClone);
            node.Parent.Nodes.Remove(node);

            ReOrderSiblings(nodeClone);

            nodeClone.TreeView.SelectedNode = nodeClone;
        }
        #endregion

        #region treeViewRightClickNudgeDown
        private void treeViewRightClickNudgeDown(object sender, System.EventArgs e)
        {
            lock (_guiLock)
            {
                try
                {
                    ShowWaitCursor(true);

                    TreeNode selected = _treeView_Node.SelectedNode;
                    if (selected == null)
                    {
                        return;
                    }

                    NodeData data = (NodeData)selected.Tag;
                    if (data == null)
                    {
                        return;
                    }

                    ServerEnv env = (ServerEnv)data.ServerEnv;
                    if (env == null || env.IsConnected == false)
                    {
                        return;
                    }

                    if (env.GetLock())
                    {
                        NudgeDown(selected);

                        env.ReplaceDataSet(Database.ChangeReason.NodeMoved);

                        env.ReleaseLock();
                    }
                    else
                    {
                        ShowTip(MESSAGE_LOCK_FAILED);
                    }
                }
                finally
                {
                    ShowWaitCursor(false);
                }
            }
        }
        #endregion

        #region NudgeDown(TreeNode node) このメソッドはデータベースを変更します。
        private static void NudgeDown(TreeNode node)
        {
            if (node == null)
            {
                return;
            }

            int newIndex = node.Index + 2;
            if (newIndex > node.Parent.Nodes.Count)
            {
                return;
            }

            TreeNode nodeClone = (TreeNode)node.Clone();
            node.Parent.Nodes.Insert(newIndex, nodeClone);
            node.Parent.Nodes.Remove(node);

            ReOrderSiblings(nodeClone);

            nodeClone.TreeView.SelectedNode = nodeClone;
        }
        #endregion

        #region ReOrderSiblings(TreeNode node)
        private static void ReOrderSiblings(TreeNode node)
        {
            for (int i = 0; i < node.Parent.Nodes.Count; i++)
            {
                TreeNode child = node.Parent.Nodes[i];
                NodeData childTag = (NodeData)child.Tag;
                if (childTag != null && childTag.Row != null)
                {
                    childTag.Row[Database.NODE_COLUMN_SORT_ORDER] = i;
                }
            }
        }
        #endregion

        private void treeViewRightClickReconnect(object sender, System.EventArgs e)
        {
            TreeNode selected = _treeView_Node.SelectedNode;
            if (selected == null || selected.Parent != null)
            {
                return;
            }

            NodeData data = (NodeData)selected.Tag;
            if (data == null || data.ServerEnv == null)
            {
                return;
            }

            // iida
            // todo: 再接続のテスト
            ServerEnv env = data.ServerEnv;

            env.GetIRmainteImplOrNull();
            if (env.IRmainte == null)
            {
                return;
            }

            env.GetDataSet();

            if (env.DataSet == null)
            {
                return;
            }

            LoadTree(env);
        }

        #region treeViewRightClickDelete
        private void treeViewRightClickDelete(object sender, System.EventArgs e)
        {
            lock (_guiLock)
            {
                try
                {
                    ShowWaitCursor(true);

                    TreeNode selected = _treeView_Node.SelectedNode;
                    if (selected == null)
                    {
                        return;
                    }

                    NodeData data = (NodeData)selected.Tag;
                    if (data == null || data.Row == null)
                    {
                        return;
                    }

                    ServerEnv env = data.ServerEnv;
                    if (env == null || env.IsConnected == false)
                    {
                        return;
                    }

                    // ルートノードを選択している場合
                    // 何もしない。
                    bool permitRootNodeDeletion = false;
                    if (selected.Parent == null)
                    {
                        if (permitRootNodeDeletion)
                        {
                            data.Row.Delete();
                            selected.Nodes.Clear();
                        }
                        return;
                    }

                    if (env.GetLock())
                    {
                        data.Row.Delete();

                        selected.Remove();

                        env.ReplaceDataSet(Database.ChangeReason.NodeDeleted);

                        env.ReleaseLock();
                    }
                    else
                    {
                        ShowTip(MESSAGE_LOCK_FAILED);
                    }

                }
                finally
                {
                    ShowWaitCursor(false);
                }
            }
        }
        #endregion

        #region _treeView_Node_AfterLabelEdit
        void _treeView_Node_AfterLabelEdit(object sender, System.Windows.Forms.NodeLabelEditEventArgs e)
        {
            lock (_guiLock)
            {
                TreeNode selected = _treeView_Node.SelectedNode;
                try
                {
                    if (selected == null || selected.Parent == null)
                    {
                        e.CancelEdit = true;
                        return;
                    }

                    NodeData data = (NodeData)selected.Tag;
                    if (data == null || data.Row == null)
                    {
                        e.CancelEdit = true;
                        return;
                    }

                    ServerEnv env = data.ServerEnv;
                    if (env == null || env.IsConnected == false)
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

                    if (e.Label.Trim().Length < 1)
                    {
                        e.CancelEdit = true;
                        return;
                    }

                    if (env.GetLock())
                    {
                        data.Row[Database.NODE_COLUMN_HOSTNAME] = e.Label;

                        env.MergeDataSet(Database.ChangeReason.NodePropertyChanged);
                        // env.ReplaceDataSet(Database.ChangeReason.NodePropertyChanged);

                        env.ReleaseLock();
                    }
                    else
                    {
                        ShowTip(MESSAGE_LOCK_FAILED);
                        e.CancelEdit = true;
                        return;
                    }
                }
                finally
                {
                    selected.EndEdit(false);
                    _treeView_Node.LabelEdit = false;
                }
            }
        }
        #endregion

        #region _treeView_Node_DragDrop
        void _treeView_Node_DragDrop(object sender, DragEventArgs e)
        {
            lock (_guiLock)
            {
                ShowWaitCursor(true);
                try
                {
                    _treeView_Node.BeginUpdate();

                    TreeNode selected = _treeView_Node.SelectedNode;
                    if (selected == null)
                    {
                        return;
                    }

                    NodeData data = (NodeData)selected.Tag;
                    if (data == null || data.Row == null)
                    {
                        return;
                    }

                    ServerEnv env = data.ServerEnv;
                    if (env == null || env.IsConnected == false)
                    {
                        return;
                    }

                    if (env.GetLock())
                    {
                        bool needUpdate = TreeViewDragDrop((TreeView)sender, e);
                        if (needUpdate)
                        {
                            env.ReplaceDataSet(Database.ChangeReason.NodeMoved);

                            // AfterSelectを呼ぶ
                            _treeView_Node_AfterSelect(_treeView_Node, new TreeViewEventArgs(selected));
                        }

                        env.ReleaseLock();
                    }
                    else
                    {
                        ShowTip(MESSAGE_LOCK_FAILED);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    _treeView_Node.EndUpdate();
                    ShowWaitCursor(false);
                }
            }
        }
        #endregion
        
        #region DragDrop(TreeView tv, System.Windows.Forms.DragEventArgs e) このメソッドはデータベースを変更します。
        private static bool TreeViewDragDrop(TreeView tv, System.Windows.Forms.DragEventArgs e)
        {
            // ツリービューとリストビュー、どっちからのデータも乗ってなければ、何もしない
            if (e.Data.GetDataPresent(typeof(TreeNode)) == false && e.Data.GetDataPresent(typeof(ListViewDragDropData)) == false)
            {
                return false;
            }

            // ターゲットのノード
            TreeNode target = tv.SelectedNode;
            if (target == null)
            {
                // DragOverで選択しているはずだけど、変なタイミングでこうなる？
                return false;
            }

            // ターゲットノードにひも付いたデータベースの行を取得
            NodeData targetTag = (NodeData)target.Tag;
            if (targetTag == null || targetTag.Row == null)
            {
                return false;
            }
            DataRow targetRow = targetTag.Row;

            // ツリービュー内での移動の場合
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                TreeNode source = (TreeNode)e.Data.GetData(typeof(TreeNode));
                return MoveTreeNode(source, target);
            }

            // 外部(リストビュー)からのドロップの場合
            if (e.Data.GetDataPresent(typeof(ListViewDragDropData)))
            {
                // データベースの変更あり？
                bool needUpdate = false;

                // リストビューでドラッグしている物を取り出す
                ListViewDragDropData data = (ListViewDragDropData)e.Data.GetData(typeof(ListViewDragDropData));

                if ((e.KeyState & 8) == 8)
                {
                    // コピーの場合
                    needUpdate = true;
                    foreach (ListViewItem item in data.DragItems)
                    {
                        NodeData sourceTag = (NodeData)item.Tag;
                        if (sourceTag == null || sourceTag.Row == null)
                        {
                            continue;
                        }
                        // この行を元にして、データベースに追加する。
                        CopyNode(target, sourceTag.Row);
                    }
                }
                else
                {
                    // 移動の場合

                    // 子供の行を取ってくる
                    string dataRelationName = Database.FindForeignKeyRelationName(targetRow.Table);
                    DataRow[] childRows = targetRow.GetChildRows(dataRelationName);

                    // 順番を整理しなおして、
                    Array.Sort(childRows, new DataRowComparer());

                    // 最後に追加していく
                    int sortOrder = childRows.Length + 1;

                    foreach (ListViewItem item in data.DragItems)
                    {
                        NodeData sourceTag = (NodeData)item.Tag;
                        sourceTag.Row[Database.NODE_COLUMN_SORT_ORDER] = sortOrder++;
                        if (item.ImageIndex == (int)TreeViewImageIndex.Router)
                        {
                            // ソースとなっているノードのデータベース上の親を付け替える
                            sourceTag.Row[Database.FindForeignKeyColumnName(targetRow.Table)] = targetRow[targetRow.Table.PrimaryKey[0].ColumnName];
                            needUpdate = true;
                        }
                        else
                        {
                            // グループの場合、TreeNodeを取ってきて、それを移動してしまえばいいんじゃないか。
                            TreeNode source = FindNodeWithNodeId(data.ParentTreeNode.Nodes, (Int32)sourceTag.Row[Database.NODE_COLUMN_NODE_ID]);
                            if (MoveTreeNode(source, target))
                            {
                                needUpdate = true;
                            }
                        }
                    }
                }

                // 見えるようにする
                target.EnsureVisible();

                // 選択する
                target.TreeView.SelectedNode = target;

                return needUpdate;
            }

            return true;
        }
        #endregion

        #region CopyNode(TreeNode node, DataSet ds, bool isGroup)
        private static void CopyNode(TreeNode parentNode, DataRow sourceRow)
        {
            NodeData parentTag = (NodeData)parentNode.Tag;
            if (parentTag == null || parentTag.ServerEnv == null || parentTag.Row == null)
            {
                return;
            }

            if (sourceRow == null || sourceRow.Table == null || sourceRow.Table.DataSet == null)
            {
                return;
            }

            DataRow parentRow = parentTag.Row;
            int parentNodeId = (Int32)parentRow[Database.NODE_COLUMN_NODE_ID];

            DataRow newRow = CopyAndAddRow(sourceRow, sourceRow.Table);
            newRow[Database.NODE_COLUMN_PARENT_ID] = parentNodeId;

            string dataRelationName = Database.FindForeignKeyRelationName(parentRow.Table);
            DataRow[] siblings = parentRow.GetChildRows(dataRelationName);
            newRow[Database.NODE_COLUMN_SORT_ORDER] = siblings.Length + 1;

            bool isGroup = (bool)sourceRow[Database.NODE_COLUMN_ISGROUP];
            if (isGroup)
            {
                // グループの場合だけ、ツリービューに表示する
                parentNode.Nodes.Add(GetTreeNodeFromDataRow(newRow, parentTag.ServerEnv));
            }
            else
            {
                // ノードはツリービューに表示しないことにする
            }
        }
        #endregion

        #region FindNodeWithNodeId(TreeNodeCollection nodes, int nodeId)
        private static TreeNode FindNodeWithNodeId(TreeNodeCollection nodes, int nodeId)
        {
            foreach (TreeNode node in nodes)
            {
                NodeData nodeTag = (NodeData)node.Tag;
                if (nodeTag == null || nodeTag.Row == null)
                {
                    continue;
                }
                int id = (Int32)nodeTag.Row[Database.NODE_COLUMN_NODE_ID];
                if (nodeId == id)
                {
                    return node;
                }
            }
            return null;
        }
        #endregion

        #region MoveTreeNode(TreeNode source, TreeNode target)
        private static bool MoveTreeNode(TreeNode source, TreeNode target)
        {
            if (source.Parent == null)
            {
                // ルートの場合は移動できない
                return false;
            }

            if (source == target)
            {
                // 同じノードには移動できない
                return false;
            }

            if (source.Nodes.Contains(target))
            {
                // 子供に移動することはできない
                return false;
            }

            if (source.Parent == target)
            {
                // 親に移動することは意味がない
                return false;
            }

            NodeData targetTag = (NodeData)target.Tag;
            DataRow targetRow = targetTag.Row;
            if ((Boolean)targetRow[Database.NODE_COLUMN_ISGROUP] == false)
            {
                // 移動先はグループのみ
                return false;
            }

            DataTable targetTable = targetRow.Table;
            NodeData sourceTag = (NodeData)source.Tag;
            DataRow sourceRow = sourceTag.Row;

            // ソースとなっているノードのデータベース上の親を付け替える
            sourceRow[Database.FindForeignKeyColumnName(targetTable)] = targetRow[targetTable.PrimaryKey[0].ColumnName];

            // ソースとなっているツリーノードを削除する。
            // ツリービューから削除されるだけで、データベースのエントリが消えるわけではない。
            source.Remove();

            // ターゲットにソースを追加する
            target.Nodes.Add(source);

            // 兄弟関係の順序を保存し、
            ReOrderSiblings(source);

            // 見えるようにする
            source.EnsureVisible();

            // 選択する
            source.TreeView.SelectedNode = source;

            return true;
        }
        #endregion

        #region _treeView_Node_DragEnter
        void _treeView_Node_DragEnter(object sender, DragEventArgs e)
        {
            // ツリービュー内でのドラッグエンター
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                // 移動しか考えない
                e.Effect = DragDropEffects.Move;
                return;
            }

            // 外部(リストビュー)からのドラッグエンター
            if (e.Data.GetDataPresent(typeof(ListViewDragDropData)))
            {
                if ((e.KeyState & 8) == 8)
                {
                    // コントロールキーが押されていたらコピー
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.Move;
                }
                return;
            }

            e.Effect = DragDropEffects.None;
        }
        #endregion

        #region _treeView_Node_DragOver
        void _treeView_Node_DragOver(object sender, DragEventArgs e)
        {
            // TreeViewUtil.DragOver((TreeView)sender, e);

            if (e.Data.GetDataPresent(typeof(TreeNode)) == false && e.Data.GetDataPresent(typeof(ListViewDragDropData)) == false)
            {
                return;
            }

            TreeView tv = (TreeView)sender;
            Point pt = tv.PointToClient(new Point(e.X, e.Y));
            TreeNode target = tv.GetNodeAt(pt);

            if (target != null && target.Nodes.Count > 0)
            {
                target.Expand();
            }

            if (tv.SelectedNode != target)
            {
                tv.SelectedNode = target;
            }

            // ツリービュー内でのドラッグの場合、
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                // 親子関係を調べて、不適切な移動の場合はDragDropEffectsを直す
                TreeNode source = (TreeNode)e.Data.GetData(typeof(TreeNode));

                while (target != null)
                {
                    if (target == source)
                    {
                        e.Effect = DragDropEffects.None;
                        return;
                    }

                    target = target.Parent;
                }
                e.Effect = DragDropEffects.Move;
                return;
            }

            // 外部(リストビュー)からやってきた場合
            if (e.Data.GetDataPresent(typeof(ListViewDragDropData)))
            {
                if ((e.KeyState & 8) == 8)
                {
                    // Controlキーが押されている場合はコピーなので、あまり気にしなくて良い。
                    e.Effect = DragDropEffects.Copy;
                    return;
                }

                ListViewDragDropData data = (ListViewDragDropData)e.Data.GetData(typeof(ListViewDragDropData));
                TreeNode parent = data.ParentTreeNode;
                if (parent != null)
                {
                    // 移動先が親ノードと一致するかどうかだけ確認
                    if (target == parent)
                    {
                        e.Effect = DragDropEffects.None;
                        return;
                    }
                }
            }

            e.Effect = DragDropEffects.Move;
        }
        #endregion

        #region _treeView_Node_ItemDrag
        void _treeView_Node_ItemDrag(object sender, ItemDragEventArgs e)
        {
            DoDragDrop(e.Item, DragDropEffects.Move);
        }
        #endregion

        #region _treeView_Node_MouseDown
        void _treeView_Node_MouseDown(object sender, MouseEventArgs e)
        {
            // クリックした場所のツリーノードを選択する
            Point pt = new Point(e.X, e.Y);
            _treeView_Node.PointToClient(pt);
            _treeView_Node.SelectedNode = _treeView_Node.GetNodeAt(pt);

        }
        #endregion

        #region _treeView_Node_MouseUp
        void _treeView_Node_MouseUp(object sender, MouseEventArgs e)
        {
            lock (_guiLock)
            {
                TreeNode selected = _treeView_Node.SelectedNode;
                if (selected == null || selected.Tag == null)
                {
                    return;
                }

                NodeData data = (NodeData)selected.Tag;

                if (e.Button == MouseButtons.Right)
                {
                    // メニューを一度全部enableにする
                    for (int i = 0; i < _contextMenuStrip_TreeView.Items.Count; i++)
                    {
                        _contextMenuStrip_TreeView.Items[i].Enabled = true;
                    }

                    if (selected.Parent == null)
                    {
                        // ルートノードの場合。編集できないし、削除もできない。アップ・ダウンもできない。
                        _contextMenuStrip_TreeView.Items[(int)TreeViewMenuIndex.EDIT_LABEL].Enabled = false;
                        _contextMenuStrip_TreeView.Items[(int)TreeViewMenuIndex.UP].Enabled = false;
                        _contextMenuStrip_TreeView.Items[(int)TreeViewMenuIndex.DOWN].Enabled = false;
                        _contextMenuStrip_TreeView.Items[(int)TreeViewMenuIndex.DELETE].Enabled = false;

                        _contextMenuStrip_TreeView.Items[(int)TreeViewMenuIndex.RECONNECT].Enabled = !data.ServerEnv.IsConnected;
                    }
                    else
                    {
                        if (data.Row != null && data.Row.Table.Columns.Contains(Database.NODE_COLUMN_ISGROUP))
                        {
                            // 普通のノードだったら、挿入はできない。
                            bool isGroup = (Boolean)data.Row[Database.NODE_COLUMN_ISGROUP];
                            if (isGroup == false)
                            {
                                _contextMenuStrip_TreeView.Items[(int)TreeViewMenuIndex.INSERT_SITE].Enabled = false;
                                _contextMenuStrip_TreeView.Items[(int)TreeViewMenuIndex.INSERT_NODE].Enabled = false;
                            }
                        }

                        _contextMenuStrip_TreeView.Items[(int)TreeViewMenuIndex.RECONNECT].Enabled = false;

                    }
                    _contextMenuStrip_TreeView.Show(_treeView_Node, new Point(e.X, e.Y));
                }
            }
        }
        #endregion

        #region _treeView_Node_AfterSelect

        private void _treeView_Node_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode node = e.Node;
            if (node == null)
            {
                return;
            }

            NodeData nodeTag = (NodeData)node.Tag;
            if (nodeTag == null || nodeTag.Row == null)
            {
                return;
            }

            ShowWaitCursor(true);
            try
            {
                _listView_Node.BeginUpdate();
                _listView_Node.Items.Clear();

                if ((bool)nodeTag.Row[Database.NODE_COLUMN_ISGROUP] == false)
                {
                    // グループ以外がここに現れることはないのだが、念のため。
                    return;
                }

                string dataRelationName = Database.FindForeignKeyRelationName(nodeTag.Row.Table);
                DataRow[] rows = nodeTag.Row.GetChildRows(dataRelationName);

                // "sortOrder"に順番が入っているのでソートする
                Array.Sort(rows, new DataRowComparer());

                lock (_guiLock)
                {
                    // 選択したノードのプロパティを表示する。
                    if (node.Parent == null)
                    {   // ルートの場合は編集できないので、表示しない。
                        HideNodeProperty();
                    }
                    else
                    {
                        ShowNodeProperty(nodeTag);
                    }

                    
                    // List<ListViewItem> groupList = new List<ListViewItem>();
                    List<ListViewItem> nodeList = new List<ListViewItem>();
                    foreach (DataRow dr in rows)
                    {
                        // ルートノードは作成しない。
                        if ((Int32)dr[Database.NODE_COLUMN_NODE_ID] == 0)
                        {
                            continue;
                        }

                        ListViewItem item = new ListViewItem((string)dr[Database.NODE_COLUMN_HOSTNAME]);
                        item.Tag = new NodeData(dr, nodeTag.ServerEnv, item);
                        bool isGroup = (bool)dr[Database.NODE_COLUMN_ISGROUP];
                        if (isGroup)
                        {
                            // ツリービュー上にTreeNodeが存在するので、それをTagにセットする。
                            // item.ImageIndex = (int)TreeViewImageIndex.Site;
                            // groupList.Add(item);
                        }
                        else
                        {
                            // ツリービュー上には存在しないので、データベースの行をTagにセットする。
                            item.ImageIndex = (int)TreeViewImageIndex.Router;
                            nodeList.Add(item);
                        }
                    }
                    // _listView_Node.Items.AddRange(groupList.ToArray());
                    _listView_Node.Items.AddRange(nodeList.ToArray());
                }
            }
            finally
            {
                _listView_Node.EndUpdate();
                ShowWaitCursor(false);
            }
        }

        #endregion

        #region GetRootNodeOrNull(TreeNode node)
        private static TreeNode GetRootNodeOrNull(TreeNode node)
        {
            if (node == null)
            {
                return null;
            }

            while (node.Parent != null)
            {
                node = node.Parent;
            }

            return node;
        }
        #endregion


        // 未使用
        #region CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
        private static void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
        {

            foreach (TreeNode node in treeNode.Nodes)
            {

                node.Checked = nodeChecked;

                if (node.Nodes.Count > 0)
                {
                    CheckAllChildNodes(node, nodeChecked);
                }
                if (!node.Checked)
                {
                    node.Collapse();
                }
                else
                {
                    node.ExpandAll();
                }
            }
        }
        #endregion

        // 未使用
        #region CollapseTreeBranch(TreeNode parentNode)
        private static void CollapseTreeBranch(TreeNode parentNode)
        {
            try
            {
                if (parentNode.Nodes.Count < 1) { return; }

                for (int i = 0; i < parentNode.Nodes.Count; i++)
                {
                    parentNode.Nodes[i].Collapse();
                }
            }
            catch (Exception) { throw; }
        }
        #endregion


        #region CopyRowToNewTable
        private static DataRow CopyAndAddRow(DataRow oldRow, DataTable table)
        {
            DataRow newRow = null;
            try
            {
                newRow = CopyRow(oldRow, table);
                table.Rows.Add(newRow);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return newRow;
        }
        
        private static DataRow CopyRow(DataRow oldRow, DataTable table)
        {
            DataRow newRow = table.NewRow();
            int nodeId = (Int32)newRow[table.PrimaryKey[0].ColumnName];
            newRow.ItemArray = oldRow.ItemArray;

            // 主キーは元に戻す
            newRow[table.PrimaryKey[0].ColumnName] = nodeId;

            return newRow;
        }
        #endregion


    }


}