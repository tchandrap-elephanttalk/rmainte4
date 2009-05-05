using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics; 

// TreeView�֌W�����A�����ɓƗ��B

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

            // ���j���[�\�z
            _contextMenuStrip_TreeView.Items.Add(MENU_STRING_INSERT_SITE, null, new EventHandler(treeViewRightClickInsertGroup));  // 0
            _contextMenuStrip_TreeView.Items.Add(MENU_STRING_INSERT_NODE, null, new EventHandler(treeViewRightClickInsertNode));   // 1
            _contextMenuStrip_TreeView.Items.Add(MENU_STRING_EDIT_LABEL, null, new EventHandler(treeViewRightClickEdit));          // 2
            _contextMenuStrip_TreeView.Items.Add(MENU_STRING_UP, null, new EventHandler(treeViewRightClickNudgeUp));               // 3
            _contextMenuStrip_TreeView.Items.Add(MENU_STRING_DOWN, null, new EventHandler(treeViewRightClickNudgeDown));           // 4
            _contextMenuStrip_TreeView.Items.Add(MENU_STRING_DELETE, null, new EventHandler(treeViewRightClickDelete));            // 5
            _contextMenuStrip_TreeView.Items.Add(MENU_STRING_RECONNECT, null, new EventHandler((treeViewRightClickReconnect)));    // 6

            // �C�x���g�n���h�����d����
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

                    // �f�[�^�Z�b�g����TreeView�𕜌�����
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
        /// �c���[�r���[�ɕ\������C���[�W�̃C���f�b�N�X�ł��B
        /// </summary>
        public enum TreeViewImageIndex { Root = 0, Site = 1, Router = 2 };
        
        /// <summary>
        /// �c���[�r���[�ɕ\�����郁�j���[�̃C���f�b�N�X�ł��B
        /// </summary>
        public enum TreeViewMenuIndex { INSERT_SITE = 0, INSERT_NODE, EDIT_LABEL, UP, DOWN, DELETE, RECONNECT };

        private static string MENU_STRING_NEW_NODE_NAME = IsJapanese ? "�V�����m�[�h" : "New Site";
        private static string MENU_STRING_NEW_SITE_NAME = IsJapanese ? "�V�����O���[�v" : "New Group";
        private static string MENU_STRING_INSERT_SITE = IsJapanese ? "�O���[�v�̑}��" : "Insert Group";
        private static string MENU_STRING_INSERT_NODE = IsJapanese ? "�m�[�h�̑}��" : "Insert Node";
        private static string MENU_STRING_EDIT_LABEL = IsJapanese ? "���O�̕ύX" : "Edit Label";
        private static string MENU_STRING_UP = IsJapanese ? "��Ɉړ�" : "Up";
        private static string MENU_STRING_DOWN = IsJapanese ? "���Ɉړ�" : "Down";
        private static string MENU_STRING_DELETE = IsJapanese ? "�폜" : "Delete";
        private static string MENU_STRING_RECONNECT = IsJapanese ? "�Đڑ�" : "Reconnect";


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

                    // �v���p�e�B��񂪕\������Ă���Ȃ�A����������B
                    HideNodeProperty();

                    // root�m�[�h�̃G���g�����Ȃ��Ə����ł��Ȃ�
                    if (env.DataSet.Tables[Database.NODE_TABLE].Rows.Count < 1)
                    {
                        return;
                    }

                    // �ŏ��Ɋ��ɍ\�z����Ă���c���[���폜����B
                    // TreeView��S����������̂ł͂Ȃ��A���̃T�[�o�ɊY�����郋�[�g�m�[�h�������폜����
                    TreeNode root = FindRootByServerEnvOrNull(tv, env);
                    if (root != null)
                    {
                        root.Remove(); // �������f�[�^�x�[�X��͏����Ȃ�
                    }

                    // ���[�g�m�[�h�ɑΉ�����f�[�^�x�[�X�̃G���g�����擾�B��Ƀe�[�u����0�s�ځB
                    DataRow rootRow = env.DataSet.Tables[Database.NODE_TABLE].Rows[0];

                    // ���[�g�m�[�h�ɑΉ�����c���[�m�[�h��V���ɍ쐬
                    root = GetTreeNodeFromDataRow(rootRow, env);
                    if (root == null)
                    {
                        return;
                    }

                    // ���[�g�m�[�h�̃e�L�X�g���T�[�o�̃A�h���X�ɕύX����B�f�[�^�x�[�X��͕ς��Ȃ��B
                    root.Text = env.Address;

                    // ���[�g�m�[�h���c���[�r���[�ɒǉ�
                    tv.Nodes.Add(root);

                    if (env.DataSet.Tables[Database.NODE_TABLE].Rows.Count > 1)
                    {
                        // ���[�g�m�[�h�ȊO�Ƀm�[�h�����݂���Ȃ�A�q�m�[�h�B��ǉ��B
                        // ��������ċN�����B
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
            // TreeNode�̃^�O�ɂ�NodeData�������Ă���B
            // ����DataRow��hostname�l�Ǝ��ۂ̃��x�����H������Ă�����C������B

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
                // �H������Ă���Ȃ�A�����B
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
        /// �c���[�r���[�̊e�m�[�h�̓^�O��ServerEnv�I�u�W�F�N�g�����L���Ă��܂��B
        /// �c���[�r���[�̃��[�g�m�[�h�Q����A�w�肵��ServerEnv���^�O�Ɏ����̂�Ԃ��܂��B
        /// </summary>
        /// <param name="tv">�ΏۂƂȂ�c���[�r���[</param>
        /// <param name="env">ServerEnv�I�u�W�F�N�g�ł���Ɠ��������^�O�Ɏ��m�[�h��Ԃ��܂��B</param>
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

                // �f�[�^�x�[�X����q�m�[�h�����o��
                DataRow[] rows = parentRow.GetChildRows(dataRelationName);
                if (rows == null || rows.Length == 0)
                {
                    return;
                }

                // "sortOrder"�ɏ��Ԃ������Ă���̂ŁA�z����\�[�g����
                Array.Sort(rows, new DataRowComparer());

                foreach (DataRow childRow in rows)
                {
                    if ((Int32)childRow[Database.NODE_COLUMN_NODE_ID] == 0)
                    {
                        // ���[�g�m�[�h�́A�e�������ɂȂ��Ă��܂��̂ŁA�p�X�B
                        continue;
                    }

                    TreeNode node = GetTreeNodeFromDataRow(childRow, env);
                    if (node == null)
                    {
                        continue;
                    }
                    parentNode.Nodes.Add(node);

                    // �ċA�Ăяo���B
                    AddNodeFromDataRow(node, childRow, env);
                }
            }
            catch (Exception) { throw; }
        }
        #endregion

        #region GetTreeNodeFromDataRow(DataRow row, string textColumnName)
        private static TreeNode GetTreeNodeFromDataRow(DataRow row, ServerEnv env)
        {
            // �O���[�v�̏ꍇ�̂ݍ쐬���Ă݂�B
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

            // �f�[�^�x�[�X�ɐV�����s��ǉ�����
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
                // �O���[�v�̏ꍇ�����A�c���[�r���[�ɕ\������
                parentNode.Nodes.Add(GetTreeNodeFromDataRow(newRow, parentTag.ServerEnv));
            }
            else
            {
                // �m�[�h�̓c���[�r���[�ɕ\�����Ȃ����Ƃɂ���
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

                        // �L���Č�����
                        selected.Expand();

                        // �T�[�o���ɃA�b�v���[�h����
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

                        // �I�����Ȃ����Ȃ��ƃ��X�g�r���[�ɒǉ������m�[�h���\������Ȃ�
                        _treeView_Node_AfterSelect(_treeView_Node, new TreeViewEventArgs(selected));

                        // �T�[�o���ɃA�b�v���[�h����
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

        #region NudgeUp(TreeNode node) ���̃��\�b�h�̓f�[�^�x�[�X��ύX���܂��B
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

        #region NudgeDown(TreeNode node) ���̃��\�b�h�̓f�[�^�x�[�X��ύX���܂��B
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
            // todo: �Đڑ��̃e�X�g
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

                    // ���[�g�m�[�h��I�����Ă���ꍇ
                    // �������Ȃ��B
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

                    // �ς���ĂȂ��B
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

                            // AfterSelect���Ă�
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
        
        #region DragDrop(TreeView tv, System.Windows.Forms.DragEventArgs e) ���̃��\�b�h�̓f�[�^�x�[�X��ύX���܂��B
        private static bool TreeViewDragDrop(TreeView tv, System.Windows.Forms.DragEventArgs e)
        {
            // �c���[�r���[�ƃ��X�g�r���[�A�ǂ�������̃f�[�^������ĂȂ���΁A�������Ȃ�
            if (e.Data.GetDataPresent(typeof(TreeNode)) == false && e.Data.GetDataPresent(typeof(ListViewDragDropData)) == false)
            {
                return false;
            }

            // �^�[�Q�b�g�̃m�[�h
            TreeNode target = tv.SelectedNode;
            if (target == null)
            {
                // DragOver�őI�����Ă���͂������ǁA�ςȃ^�C�~���O�ł����Ȃ�H
                return false;
            }

            // �^�[�Q�b�g�m�[�h�ɂЂ��t�����f�[�^�x�[�X�̍s���擾
            NodeData targetTag = (NodeData)target.Tag;
            if (targetTag == null || targetTag.Row == null)
            {
                return false;
            }
            DataRow targetRow = targetTag.Row;

            // �c���[�r���[���ł̈ړ��̏ꍇ
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                TreeNode source = (TreeNode)e.Data.GetData(typeof(TreeNode));
                return MoveTreeNode(source, target);
            }

            // �O��(���X�g�r���[)����̃h���b�v�̏ꍇ
            if (e.Data.GetDataPresent(typeof(ListViewDragDropData)))
            {
                // �f�[�^�x�[�X�̕ύX����H
                bool needUpdate = false;

                // ���X�g�r���[�Ńh���b�O���Ă��镨�����o��
                ListViewDragDropData data = (ListViewDragDropData)e.Data.GetData(typeof(ListViewDragDropData));

                if ((e.KeyState & 8) == 8)
                {
                    // �R�s�[�̏ꍇ
                    needUpdate = true;
                    foreach (ListViewItem item in data.DragItems)
                    {
                        NodeData sourceTag = (NodeData)item.Tag;
                        if (sourceTag == null || sourceTag.Row == null)
                        {
                            continue;
                        }
                        // ���̍s�����ɂ��āA�f�[�^�x�[�X�ɒǉ�����B
                        CopyNode(target, sourceTag.Row);
                    }
                }
                else
                {
                    // �ړ��̏ꍇ

                    // �q���̍s������Ă���
                    string dataRelationName = Database.FindForeignKeyRelationName(targetRow.Table);
                    DataRow[] childRows = targetRow.GetChildRows(dataRelationName);

                    // ���Ԃ𐮗����Ȃ����āA
                    Array.Sort(childRows, new DataRowComparer());

                    // �Ō�ɒǉ����Ă���
                    int sortOrder = childRows.Length + 1;

                    foreach (ListViewItem item in data.DragItems)
                    {
                        NodeData sourceTag = (NodeData)item.Tag;
                        sourceTag.Row[Database.NODE_COLUMN_SORT_ORDER] = sortOrder++;
                        if (item.ImageIndex == (int)TreeViewImageIndex.Router)
                        {
                            // �\�[�X�ƂȂ��Ă���m�[�h�̃f�[�^�x�[�X��̐e��t���ւ���
                            sourceTag.Row[Database.FindForeignKeyColumnName(targetRow.Table)] = targetRow[targetRow.Table.PrimaryKey[0].ColumnName];
                            needUpdate = true;
                        }
                        else
                        {
                            // �O���[�v�̏ꍇ�ATreeNode������Ă��āA������ړ����Ă��܂��΂����񂶂�Ȃ����B
                            TreeNode source = FindNodeWithNodeId(data.ParentTreeNode.Nodes, (Int32)sourceTag.Row[Database.NODE_COLUMN_NODE_ID]);
                            if (MoveTreeNode(source, target))
                            {
                                needUpdate = true;
                            }
                        }
                    }
                }

                // ������悤�ɂ���
                target.EnsureVisible();

                // �I������
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
                // �O���[�v�̏ꍇ�����A�c���[�r���[�ɕ\������
                parentNode.Nodes.Add(GetTreeNodeFromDataRow(newRow, parentTag.ServerEnv));
            }
            else
            {
                // �m�[�h�̓c���[�r���[�ɕ\�����Ȃ����Ƃɂ���
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
                // ���[�g�̏ꍇ�͈ړ��ł��Ȃ�
                return false;
            }

            if (source == target)
            {
                // �����m�[�h�ɂ͈ړ��ł��Ȃ�
                return false;
            }

            if (source.Nodes.Contains(target))
            {
                // �q���Ɉړ����邱�Ƃ͂ł��Ȃ�
                return false;
            }

            if (source.Parent == target)
            {
                // �e�Ɉړ����邱�Ƃ͈Ӗ����Ȃ�
                return false;
            }

            NodeData targetTag = (NodeData)target.Tag;
            DataRow targetRow = targetTag.Row;
            if ((Boolean)targetRow[Database.NODE_COLUMN_ISGROUP] == false)
            {
                // �ړ���̓O���[�v�̂�
                return false;
            }

            DataTable targetTable = targetRow.Table;
            NodeData sourceTag = (NodeData)source.Tag;
            DataRow sourceRow = sourceTag.Row;

            // �\�[�X�ƂȂ��Ă���m�[�h�̃f�[�^�x�[�X��̐e��t���ւ���
            sourceRow[Database.FindForeignKeyColumnName(targetTable)] = targetRow[targetTable.PrimaryKey[0].ColumnName];

            // �\�[�X�ƂȂ��Ă���c���[�m�[�h���폜����B
            // �c���[�r���[����폜����邾���ŁA�f�[�^�x�[�X�̃G���g����������킯�ł͂Ȃ��B
            source.Remove();

            // �^�[�Q�b�g�Ƀ\�[�X��ǉ�����
            target.Nodes.Add(source);

            // �Z��֌W�̏�����ۑ����A
            ReOrderSiblings(source);

            // ������悤�ɂ���
            source.EnsureVisible();

            // �I������
            source.TreeView.SelectedNode = source;

            return true;
        }
        #endregion

        #region _treeView_Node_DragEnter
        void _treeView_Node_DragEnter(object sender, DragEventArgs e)
        {
            // �c���[�r���[���ł̃h���b�O�G���^�[
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                // �ړ������l���Ȃ�
                e.Effect = DragDropEffects.Move;
                return;
            }

            // �O��(���X�g�r���[)����̃h���b�O�G���^�[
            if (e.Data.GetDataPresent(typeof(ListViewDragDropData)))
            {
                if ((e.KeyState & 8) == 8)
                {
                    // �R���g���[���L�[��������Ă�����R�s�[
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

            // �c���[�r���[���ł̃h���b�O�̏ꍇ�A
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                // �e�q�֌W�𒲂ׂāA�s�K�؂Ȉړ��̏ꍇ��DragDropEffects�𒼂�
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

            // �O��(���X�g�r���[)�������Ă����ꍇ
            if (e.Data.GetDataPresent(typeof(ListViewDragDropData)))
            {
                if ((e.KeyState & 8) == 8)
                {
                    // Control�L�[��������Ă���ꍇ�̓R�s�[�Ȃ̂ŁA���܂�C�ɂ��Ȃ��ėǂ��B
                    e.Effect = DragDropEffects.Copy;
                    return;
                }

                ListViewDragDropData data = (ListViewDragDropData)e.Data.GetData(typeof(ListViewDragDropData));
                TreeNode parent = data.ParentTreeNode;
                if (parent != null)
                {
                    // �ړ��悪�e�m�[�h�ƈ�v���邩�ǂ��������m�F
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
            // �N���b�N�����ꏊ�̃c���[�m�[�h��I������
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
                    // ���j���[����x�S��enable�ɂ���
                    for (int i = 0; i < _contextMenuStrip_TreeView.Items.Count; i++)
                    {
                        _contextMenuStrip_TreeView.Items[i].Enabled = true;
                    }

                    if (selected.Parent == null)
                    {
                        // ���[�g�m�[�h�̏ꍇ�B�ҏW�ł��Ȃ����A�폜���ł��Ȃ��B�A�b�v�E�_�E�����ł��Ȃ��B
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
                            // ���ʂ̃m�[�h��������A�}���͂ł��Ȃ��B
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
                    // �O���[�v�ȊO�������Ɍ���邱�Ƃ͂Ȃ��̂����A�O�̂��߁B
                    return;
                }

                string dataRelationName = Database.FindForeignKeyRelationName(nodeTag.Row.Table);
                DataRow[] rows = nodeTag.Row.GetChildRows(dataRelationName);

                // "sortOrder"�ɏ��Ԃ������Ă���̂Ń\�[�g����
                Array.Sort(rows, new DataRowComparer());

                lock (_guiLock)
                {
                    // �I�������m�[�h�̃v���p�e�B��\������B
                    if (node.Parent == null)
                    {   // ���[�g�̏ꍇ�͕ҏW�ł��Ȃ��̂ŁA�\�����Ȃ��B
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
                        // ���[�g�m�[�h�͍쐬���Ȃ��B
                        if ((Int32)dr[Database.NODE_COLUMN_NODE_ID] == 0)
                        {
                            continue;
                        }

                        ListViewItem item = new ListViewItem((string)dr[Database.NODE_COLUMN_HOSTNAME]);
                        item.Tag = new NodeData(dr, nodeTag.ServerEnv, item);
                        bool isGroup = (bool)dr[Database.NODE_COLUMN_ISGROUP];
                        if (isGroup)
                        {
                            // �c���[�r���[���TreeNode�����݂���̂ŁA�����Tag�ɃZ�b�g����B
                            // item.ImageIndex = (int)TreeViewImageIndex.Site;
                            // groupList.Add(item);
                        }
                        else
                        {
                            // �c���[�r���[��ɂ͑��݂��Ȃ��̂ŁA�f�[�^�x�[�X�̍s��Tag�ɃZ�b�g����B
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


        // ���g�p
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

        // ���g�p
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

            // ��L�[�͌��ɖ߂�
            newRow[table.PrimaryKey[0].ColumnName] = nodeId;

            return newRow;
        }
        #endregion


    }


}