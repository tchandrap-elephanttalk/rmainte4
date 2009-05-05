using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;

namespace rmainte4
{
    /// <summary>
    /// TreeNodeのTagに入れる情報
    /// </summary>
    [Serializable] 
    public class NodeData
    {
        public NodeData(DataRow row, ServerEnv env, Object owner)
        {
            _env = env;
            _row = row;
            _owner = owner;
        }

        public ServerEnv ServerEnv
        {
            get { return _env; }
        }
        private ServerEnv _env = null;

        public DataRow Row
        {
            get { return _row; }
        }
        private DataRow _row = null;

        public Object Owner
        {
            get { return _owner; }
        }
        private object _owner = null;


        private static string NullToEmpty(string str)
        {
            if (str == null)
            {
                return string.Empty;
            }
            return str;
        }

        public string Hostname
        {
            get { return NullToEmpty((string)GetValueOrNull(Database.NODE_COLUMN_HOSTNAME)); }
            set
            {
                if (SetValue(Database.NODE_COLUMN_HOSTNAME, NullToEmpty(value)))
                {
                    if (_owner.GetType().Equals(typeof(TreeNode)))
                    {
                        TreeNode node = (TreeNode)_owner;
                        node.Text = value;
                    }
                }
            }
        }

        public string Address
        {
            get { return NullToEmpty((string)GetValueOrNull(Database.NODE_COLUMN_ADDRESS)); }
            set { SetValue(Database.NODE_COLUMN_ADDRESS, NullToEmpty(value)); }
        }

        public bool IsGroup
        {
            get { return GetBooleanValueOrFalse(Database.NODE_COLUMN_ISGROUP); }
            set { SetValue(Database.NODE_COLUMN_ISGROUP, value); }
        }



        public bool GetBooleanValueOrFalse(string columnString)
        {
            if (_row == null || _row.Table.Columns.Contains(columnString) == false)
            {
                return false;
            }

            return (bool)_row[columnString];
        }

        public object GetValueOrNull(string columnString)
        {
            if (_row == null || _row.Table.Columns.Contains(columnString) == false)
            {
                return null;
            }

            if (_row[columnString] == DBNull.Value)
            {
                return null;
            }

            return (object)_row[columnString];
        }

        public bool SetValue(string columnString, object data)
        {
            if (_row == null || _row.Table.Columns.Contains(columnString) == false)
            {
                return false;
            }

            if (_row[columnString] == data)
            {
                return false;
            }

            if (_env == null || _env.IsConnected == false)
            {
                return false;
            }

            if (_env.GetLock())
            {
                // 中身が変わっているので、データベースを変更する。
                _row[columnString] = data;

                // サーバにアップロードする
                _env.MergeDataSet(Database.ChangeReason.NodePropertyChanged);

                _env.ReleaseLock();

                return true;
            }
            else
            {
                // MessageBox.Show("ロックの取得に失敗しました。時間をあけてから再度実行してください。");
                return false;
            }
        }

    
    }
}
