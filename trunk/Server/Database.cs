/* *-*-mode:java; -*- */

// Copyright Takamitsu IIDA <gsi@nifty.com>
// CCIE4288
// 2004.11.26
// 2006.07.20 .NET2.0対応
// 2006.07.26 PoderosaのPlugin対応で保存先を変更

using System;
using System.Drawing;
using System.Collections;
using System.Data;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace rmainte4
{
    /// <summary>
    /// ノード状態を表すクラスです。
    /// </summary>
    public class NodeStatus
    {
        /// <summary>
        /// 不明状態を表す定数。
        /// </summary>
        public const int Unknown = 0;

        /// <summary>
        /// アップ状態を表す定数。
        /// </summary>
        public const int Up = 1;

        /// <summary>
        /// ダウン状態を表す定数。
        /// </summary>
        public const int Down = 2;

        /// <summary>
        /// 管理上のダウンを表す定数。
        /// </summary>
        public const int AdminDown = 3;

        /// <summary>
        /// それぞれの状態の説明文を格納する文字列配列。
        /// </summary>
        public static string[] Description = { "未調査", "アップ", "ダウン", "管理ダウン" };
    }

    /// <summary>
    /// Databaseクラスは、ルータの情報を格納するデータセットを保有するSingletonクラスです。
    /// </summary>
    public sealed class Database
    {
        // 外からはインスタンス化させない
        private Database()
        {
            // 入れ物を実体化
            _ds = new DataSet();
        }

        // 外部からインスタンスを取得
        /*
        public static Database GetInstance()
        {
            if (instance == null)
            {
                instance = new Database();
            }
            return instance;
        }
        */

        public static Database GetInstance()
        {
            return _instance;
        }

        public static Database Instance
        {
            get { return _instance; }
        }

        private static readonly Database _instance = new Database();

        /// <summary>
        /// データセットの変更理由
        /// </summary>
        public enum ChangeReason { NodeAdded = 0, NodeDeleted, NodeMoved, NodePropertyChanged, };

        private static string Filename;
        static Database()
        {
            FileInfo fi = new FileInfo(Application.ExecutablePath);
            // 実行ファイル名の拡張子をxmlにしたものを探し、なければrmainte4.xmlとする
            Filename = Path.ChangeExtension(fi.Name, "xml");
            if (File.Exists(Filename) == false)
            {
                Filename = Path.Combine(fi.DirectoryName, "rmainte4.xml");
            }
        }

        /// <summary>
        /// マスターデータベースを扱うデータセットオブジェクト
        /// </summary>
        public DataSet GetDataSet()
        {
            return _ds;
        }

        public DataSet DataSet
        {
            get { return _ds; }
        }
        private DataSet _ds;

        /// <summary>
        /// このデータセットのバージョン
        /// </summary>
        public const int VERSION = 1;

        /// <summary>
        /// 拡張プロパティを使ってデータセットにバージョン情報を付与します。
        /// </summary>
        /// <param name="ds">対象となるデータセット</param>
        public static void SetVersion(DataSet ds, int version)
        {
            SetProperty(ds, "VERSION", version.ToString());
        }

        public static void SetGUID(DataSet ds, string guid)
        {
            SetProperty(ds, "GUID", guid);
        }

        private static void SetProperty(DataSet ds, string propertyName, string propertyValue)
        {
            if (ds.ExtendedProperties.Contains(propertyName))
            {
                ds.ExtendedProperties.Remove(propertyName);
            }
            ds.ExtendedProperties.Add(propertyName, propertyValue);
        }

        /// <summary>
        /// バージョンを取得します。
        /// </summary>
        /// <param name="ds">対象となるデータセット</param>
        /// <returns>データベースに付与されたバージョン</returns>
        public static int GetVersion(DataSet ds)
        {
            int v = -1;
            try
            {
                v = Int32.Parse(ds.ExtendedProperties["VERSION"].ToString());
            }
            catch (Exception) { }
            return (v);
        }

        public static string GetGUID(DataSet ds)
        {
            string guid = "";
            try
            {
                guid = (string) ds.ExtendedProperties["GUID"];
            }
            catch (Exception ex) { Debug.WriteLine(ex.StackTrace); Debug.Flush(); }
            return guid;
        }

        public void Commit()
        {
            if (_ds.HasChanges())
            {
                _ds.AcceptChanges();
            }
        }

        public void Load()
        {
            Load(Filename);
        }

        /// <summary>
        /// ファイルに保存されているデータセットを復元します。
        /// </summary>
        /// <param name="filename">保存されているファイルの名前</param>
        public void Load(string filename)
        {

            // ファイルから読んで中身を詰める
            try
            {
                using (StreamReader sr = new StreamReader(filename, System.Text.Encoding.UTF8))
                {
                    DataSet dataset = new DataSet();
                    dataset.ReadXml(sr);
                    dataset.AcceptChanges();
                    lock (_ds)
                    {
                        _ds = dataset;
                    }

                    sr.Close();
                }
            }
            catch (Exception)
            {
                Clear();
                Create();
                return;
            }

            // ファイルから読んだ物が古い場合はコンバートする
            if (GetVersion(_ds) < VERSION)
            {
                // Log.WriteLine("データベースを最新に変換します。");
                try
                {
                    Convert();
                    WriteXml();
                }
                catch (Exception ex) { System.Windows.Forms.MessageBox.Show(ex.ToString()); }
                // Log.WriteLine("データベースの変換を終了しました。");
            }
        }

        /// <summary>
        /// データセットオブジェクトをXMLで保存します。
        /// </summary>
        public void WriteXml()
        {
            WriteXml(Filename);
        }

        /// <summary>
        /// データセットオブジェクトをXMLで保存します。
        /// </summary>
        /// <param name="filename">保存先のファイル名</param>
        public void WriteXml(string filename)
        {
            lock (_ds)
            {
                if (_ds.HasChanges())
                {
                    _ds.AcceptChanges();
                }
                try
                {
                    using (StreamWriter sw = new StreamWriter(filename, false, System.Text.Encoding.UTF8))
                    {
                        _ds.WriteXml(sw, XmlWriteMode.WriteSchema);
                        sw.Close();
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// 全テーブルの全エントリを削除します。
        /// </summary>
        public void Clear()
        {
            lock (_ds)
            {
                _ds.Clear();
            }
        }

        /// <summary>
        /// データセットを作成します。
        /// </summary>
        public void Create()
        {
            // バージョンをセット
            SetVersion(_ds, VERSION);

            // テーブルを作成
            MakeTables(_ds);
        }

        /// <summary>
        /// 新しいデータセットで置き換えます。
        /// </summary>
        /// <param name="ds">新しいDataSet</param>
        public void Replace(DataSet ds)
        {
            lock (_ds)
            {
                _ds.Clear();
                _ds = ds;
            }
        }

        public void Merge(DataSet diff)
        {
            lock (_ds)
            {
                _ds.Merge(diff);
                _ds.AcceptChanges();
            }
        }

        /// <summary>
        /// バージョンの古いDataSetを新しいDataSetに移植します。
        /// </summary>
        private void Convert()
        {
            // データセットを一つ新規に作成
            DataSet newds = new DataSet();

            // 新しいデータセットにバージョンをセット
            SetVersion(newds, VERSION);

            // テーブルを作成
            MakeTables(newds);

            // 全ての古いテーブルに関して
            foreach (DataTable oldTable in _ds.Tables)
            {
                DataTable newTable = null;
                try
                {
                    newTable = newds.Tables[oldTable.TableName];
                }
                catch (Exception) { }
                if (newTable == null)
                {
                    continue;
                }

                if (oldTable.TableName.Equals("Config"))
                {
                    DataRow newdr = newTable.Rows[0];
                    DataRow olddr = oldTable.Rows[0];
                    foreach (DataColumn oldCol in oldTable.Columns)
                    {
                        try
                        {
                            newdr[oldCol.ColumnName] = olddr[oldCol];
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                else
                {
                    // 古いテーブルの全ての行について
                    foreach (DataRow olddr in oldTable.Rows)
                    {
                        DataRow newdr = newTable.NewRow();

                        foreach (DataColumn oldCol in oldTable.Columns)
                        {
                            try
                            {
                                newdr[oldCol.ColumnName] = olddr[oldCol];
                            }
                            catch (Exception)
                            {
                            }
                        }

                        newTable.Rows.Add(newdr);
                    }
                }
            }
            newds.AcceptChanges();

            Clear();
            _ds = newds;
        }


        public static string FindForeignKeyColumnName(DataTable table)
        {
            DataSet ds = table.DataSet;
            DataRelation relation = ds.Relations[0];

            return relation.ChildColumns[0].ColumnName;
        }

        public static string FindForeignKeyRelationName(DataTable table)
        {
            DataSet ds = table.DataSet;
            DataRelation rel = ds.Relations[0];
            return rel.RelationName;
        }

        private void MakeTables(DataSet ds)
        {
            // 中身が空のテーブルを構築
            MakeNodeTable(ds);
            MakeActionTable(ds);
            MakeConfigTable(ds);
        }

        private string IntToString(int i)
        {
            Char[] c = new Char[] { (char)((int)'a' + i) };
            return (new String(c));
        }

        #region DataColumnを取得
        private static DataColumn GetStringColumn(string name, string defaultValue)
        {
            DataColumn column = new DataColumn();
            column.DataType = Type.GetType("System.String");
            column.AllowDBNull = true;
            column.ColumnName = name;
            column.DefaultValue = defaultValue;
            return (column);
        }

        private static DataColumn GetDateTimeColumn(string name, DateTime defaultValue)
        {
            DataColumn column = new DataColumn();
            column.DataType = Type.GetType("System.DateTime");
            column.AllowDBNull = true;
            column.ColumnName = name;
            column.DefaultValue = defaultValue;
            return (column);
        }

        private static DataColumn GetTimeSpanColumn(string name, TimeSpan defaultValue)
        {
            DataColumn column = new DataColumn();
            column.DataType = Type.GetType("System.TimeSpan");
            column.AllowDBNull = true;
            column.ColumnName = name;
            column.DefaultValue = defaultValue;
            return (column);
        }

        private static DataColumn GetInt32Column(string name, Int32 defaultValue)
        {
            DataColumn column = new DataColumn();
            column.DataType = Type.GetType("System.Int32");
            column.AllowDBNull = true;
            column.ColumnName = name;
            column.DefaultValue = defaultValue;
            return (column);
        }

        private static DataColumn GetInt64Column(string name, Int64 defaultValue)
        {
            DataColumn column = new DataColumn();
            column.DataType = Type.GetType("System.Int64");
            column.AllowDBNull = true;
            column.ColumnName = name;
            column.DefaultValue = defaultValue;
            return (column);
        }

        private static DataColumn GetBooleanColumn(string name, bool defaultValue)
        {
            DataColumn column = new DataColumn();
            column.DataType = Type.GetType("System.Boolean");
            column.AllowDBNull = false;
            column.ColumnName = name;
            column.DefaultValue = defaultValue;
            return (column);
        }
        #endregion

        #region MakeActionTable
        private static DataTable MakeActionTable(DataSet ds)
        {
            // テーブルを新規作成
            DataTable table = new DataTable("Action");

            // actionId列
            // このテーブルの主キーとなる列。すべての実施計画はactionIdで区別される。
            DataColumn column = new DataColumn();
            column.DataType = Type.GetType("System.Int32");
            column.ColumnName = "actionId";
            column.ReadOnly = true;
            column.AllowDBNull = false;
            column.Unique = true;
            column.AutoIncrement = true;
            column.AutoIncrementSeed = 0;
            column.AutoIncrementStep = 1;
            table.Columns.Add(column);

            // Int32 ownerId
            // このActionを保有しているNodePropertyのnodeId
            table.Columns.Add(GetInt32Column("ownerId", 0));

            // 現在未使用。
            // string name
            table.Columns.Add(GetStringColumn("name", String.Empty));

            // string description
            table.Columns.Add(GetStringColumn("description", String.Empty));

            // Int32 sceduleType
            table.Columns.Add(GetInt32Column("scheduleType", 0));

            // string status
            table.Columns.Add(GetStringColumn("status", "計画作成"));

            // Boolean startNow
            table.Columns.Add(GetBooleanColumn("startNow", true));

            // DateTime startTime
            table.Columns.Add(GetDateTimeColumn("startTime", DateTime.MaxValue));

            // DateTime nextInvokeTime
            table.Columns.Add(GetDateTimeColumn("nextInvokeTime", DateTime.MaxValue));

            // DateTime toTime
            table.Columns.Add(GetDateTimeColumn("toTime", DateTime.MaxValue));

            // DateTime fromTime
            table.Columns.Add(GetDateTimeColumn("fromTime", DateTime.MaxValue));

            // Int64 interval
            table.Columns.Add(GetInt64Column("interval", Int64.MaxValue));

            // bool sun
            table.Columns.Add(GetBooleanColumn("sun", false));

            // bool mon
            table.Columns.Add(GetBooleanColumn("mon", true));

            // bool tue
            table.Columns.Add(GetBooleanColumn("tue", true));

            // bool wed
            table.Columns.Add(GetBooleanColumn("wed", true));

            // bool thu
            table.Columns.Add(GetBooleanColumn("thu", true));

            // bool fri
            table.Columns.Add(GetBooleanColumn("fri", true));

            // bool sat
            table.Columns.Add(GetBooleanColumn("sat", false));

            // bool isScheduled
            table.Columns.Add(GetBooleanColumn("isScheduled", false));

            // string target
            table.Columns.Add(GetStringColumn("target", String.Empty));

            // string lastLog
            table.Columns.Add(GetStringColumn("lastLog", String.Empty));

            // DateTime lastStarted
            table.Columns.Add(GetDateTimeColumn("lastStarted", DateTime.MaxValue));

            // DateTime lastFinished
            table.Columns.Add(GetDateTimeColumn("lastFinished", DateTime.MaxValue));

            // Int32 scenario
            table.Columns.Add(GetInt32Column("scenario", 1));

            // シナリオ1
            // Int32 scenario1SimultaneousConnection
            table.Columns.Add(GetInt32Column("scenario1SimultaneousConnection", 4));

            // bool scenario1DoCiscoEnable
            table.Columns.Add(GetBooleanColumn("scenario1DoCiscoEnable", true));

            // シナリオ2
            // 接続してコマンドを叩く

            // bool scenario2PingBeforeConnect
            table.Columns.Add(GetBooleanColumn("scenario2PingBeforeConnect", true));

            // Int32 scenario2SimultaneousConnection
            table.Columns.Add(GetInt32Column("scenario2SimultaneousConnection", 4));

            // Int32 scenario2IdleTimeout
            table.Columns.Add(GetInt32Column("scenario2IdleTimeout", 10000));

            // bool scenario2DoEnable
            table.Columns.Add(GetBooleanColumn("scenario2DoEnable", true));

            // bool scenario2DoTerminalLength
            table.Columns.Add(GetBooleanColumn("scenario2DoTerminalLength", true));

            // bool scenario2Cmd1
            table.Columns.Add(GetBooleanColumn("scenario2Cmd1", false));

            // bool scenario2Cmd2
            table.Columns.Add(GetBooleanColumn("scenario2Cmd2", false));

            // bool scenario2Cmd3
            table.Columns.Add(GetBooleanColumn("scenario2Cmd3", false));

            // string scenario2Cmd1Command
            table.Columns.Add(GetStringColumn("scenario2Cmd1Command", String.Empty));

            // string scenario2Cmd2Command
            table.Columns.Add(GetStringColumn("scenario2Cmd2Command", String.Empty));

            // string scenario2Cmd3Command
            table.Columns.Add(GetStringColumn("scenario2Cmd3Command", String.Empty));

            // bool scenario2Cmd1AutoPrompt
            table.Columns.Add(GetBooleanColumn("scenario2Cmd1AutoPrompt", true));

            // bool scenario2Cmd2AutoPrompt
            table.Columns.Add(GetBooleanColumn("scenario2Cmd2AutoPrompt", true));

            // bool scenario2Cmd3AutoPrompt
            table.Columns.Add(GetBooleanColumn("scenario2Cmd3AutoPrompt", true));

            // string scenario2Cmd1Prompt
            table.Columns.Add(GetStringColumn("scenario2Cmd1Prompt", String.Empty));

            // string scenario2Cmd2Prompt
            table.Columns.Add(GetStringColumn("scenario2Cmd2Prompt", String.Empty));

            // string scenario2Cmd3Prompt
            table.Columns.Add(GetStringColumn("scenario2Cmd3Prompt", String.Empty));

            // bool scenario2Cmd1Repeat
            table.Columns.Add(GetBooleanColumn("scenario2Cmd1Repeat", false));

            // bool scenario2Cmd2Repeat
            table.Columns.Add(GetBooleanColumn("scenario2Cmd2Repeat", false));

            // bool scenario2Cmd1Repeat
            table.Columns.Add(GetBooleanColumn("scenario2Cmd3Repeat", false));

            // Int32 scenario2Cmd1RepeatInterval
            table.Columns.Add(GetInt32Column("scenario2Cmd1RepeatInterval", 10));

            // Int32 scenario2Cmd2RepeatInterval
            table.Columns.Add(GetInt32Column("scenario2Cmd2RepeatInterval", 10));

            // Int32 scenario2Cmd3RepeatInterval
            table.Columns.Add(GetInt32Column("scenario2Cmd3RepeatInterval", 10));

            // Int32 scenario2Cmd1Repeatcount
            table.Columns.Add(GetInt32Column("scenario2Cmd1RepeatCount", 1));

            // Int32 scenario2Cmd2Repeatcount
            table.Columns.Add(GetInt32Column("scenario2Cmd2RepeatCount", 1));

            // Int32 scenario2Cmd3Repeatcount
            table.Columns.Add(GetInt32Column("scenario2Cmd3RepeatCount", 1));

            // bool scenario2CloseOnFinish
            table.Columns.Add(GetBooleanColumn("scenario2CloseOnFinish", false));

            // bool scenario2CollectLogEnabled
            table.Columns.Add(GetBooleanColumn("scenario2CollectLogEnabled", false));

            // シナリオ3

            // bool scenario3PingBeforeConnect
            table.Columns.Add(GetBooleanColumn("scenario3PingBeforeConnect", true));

            // Int32 scenario2SimultaneousConnection
            table.Columns.Add(GetInt32Column("scenario3SimultaneousConnection", 4));

            // bool scenario3BackupBeforeChange
            table.Columns.Add(GetBooleanColumn("scenario3BackupBeforeChange", true));

            // bool scenario3BackupAfterChange
            table.Columns.Add(GetBooleanColumn("scenario3BackupAfterChange", true));

            // bool scenario3ChangeConfig
            table.Columns.Add(GetBooleanColumn("scenario3ChangeConfig", false));

            // bool scenario3CopyToStartup
            table.Columns.Add(GetBooleanColumn("scenario3CopyToStartup", false));

            // bool scenario3RunCommandBeforeChange
            table.Columns.Add(GetBooleanColumn("scenario3RunCommandBeforeChange", false));

            // bool scenario3RunCommandAfterChange
            table.Columns.Add(GetBooleanColumn("scenario3RunCommandAfterChange", false));

            // string scenario3RunCommandTextBeforeChange
            table.Columns.Add(GetStringColumn("scenario3RunCommandTextBeforeChange", String.Empty));

            // string scenario3RunCommandTextAfterChange
            table.Columns.Add(GetStringColumn("scenario3RunCommandTextAfterChange", String.Empty));

            // bool scenario3WaitAfterChange
            table.Columns.Add(GetBooleanColumn("scenario3WaitAfterChange", false));

            // Int32 scenario3WaitSeconds
            table.Columns.Add(GetInt32Column("scenario3WaitSeconds", 60));

            // bool scenario3SameConfig
            table.Columns.Add(GetBooleanColumn("scenario3SameConfig", true));

            // bool scenario3IndividualConfig
            table.Columns.Add(GetBooleanColumn("scenario3IndividualConfig", false));

            // string scenario3ConfigDir
            table.Columns.Add(GetStringColumn("scenario3ConfigDir", "Config"));

            // string scenario3ConfigText
            table.Columns.Add(GetStringColumn("scenario3ConfigText", DEFAULT_CONFIG));

            // bool scenario3loseOnFinish
            table.Columns.Add(GetBooleanColumn("scenario3CloseOnFinish", false));

            // bool scenario3CollectLogEnabled
            table.Columns.Add(GetBooleanColumn("scenario3CollectLogEnabled", false));

            // シナリオ4

            // string scenario4Filename
            table.Columns.Add(GetStringColumn("scenario4Filename", String.Empty));

            // string scenario4Dllfile
            table.Columns.Add(GetStringColumn("scenario4Dllfile", String.Empty));

            // シナリオ5

            // string scenario5Filename
            table.Columns.Add(GetStringColumn("scenario5Filename", String.Empty));

            // string scenario5Arg
            table.Columns.Add(GetStringColumn("scenario5Arg", String.Empty));

            // actionId列を主キーとして設定
            DataColumn[] PK = new DataColumn[1];
            PK[0] = table.Columns["actionId"];
            table.PrimaryKey = PK;

            ds.Tables.Add(table);

            return table;
        }
        #endregion

        public const string DEFAULT_CONFIG = @"!
service password-encryption
service timestamps debug datetime msec
no ip domain-lookup
scheduler allocate
clock timezone JST 9
snmp-server queue-length 200
!
";
        /// <summary>
        /// ノードテーブルのテーブル名
        /// </summary>
        public const string NODE_TABLE = "Node";

        /// <summary>
        /// ノードIDの列名
        /// </summary>
        public const string NODE_COLUMN_NODE_ID = "nodeId";

        /// <summary>
        /// 親ノードのnodeIdを表す列名
        /// </summary>
        public const string NODE_COLUMN_PARENT_ID = "parentId";

        /// <summary>
        /// ノードのソート順序を表す列名
        /// </summary>
        public const string NODE_COLUMN_SORT_ORDER = "sortOrder";

        /// <summary>
        /// ツリービューのID
        /// </summary>
        public const string NODE_COLUMN_MODELID = "modelId";
        
        /// <summary>
        /// ツリービューに表示するイメージのインデックスを表す列名
        /// </summary>
        public const string NODE_COLUMN_IMAGEINDEX = "imageIndex";

        /// <summary>
        /// ツリービューに表示する選択された状態のイメージのインデックスを表す列名
        /// </summary>
        public const string NODE_COLUMN_SELECTED_IMAGEINDEX = "selectedImageIndex";

        /// <summary>
        /// グループかどうかを表す列名
        /// </summary>
        public const string NODE_COLUMN_ISGROUP = "isGroup";

        /// <summary>
        /// ホスト名。ツリービューはこれをテキストとして表示する。
        /// </summary>
        public const string NODE_COLUMN_HOSTNAME = "hostname";

        /// <summary>
        /// IPアドレス。
        /// </summary>
        public const string NODE_COLUMN_ADDRESS = "ipAddress";

        public const string NODE_COLUMN_DEVICE_NO = "deviceNo";
        public const string NODE_COLUMN_DEVICE_TYPE = "deviceType";
        public const string NODE_COLUMN_FOLLOW_GROUP = "followGroupConfig";
        public const string NODE_COLUMN_TCP_PORT = "tcpPort";
        public const string NODE_COLUMN_USE_SSH = "useSSH";
        public const string NODE_COLUMN_CONNECT_USERNAME = "connectUsername";
        public const string NODE_COLUMN_CONNECT_SECRET = "connectSecret";
        public const string NODE_COLUMN_CONNECT_SECRET_NEW = "connectSecretNew";
        public const string NODE_COLUMN_PRIVILEDGE_COMMAND = "privilegeCommand";
        public const string NODE_COLUMN_ENABLE_SECRET = "enableSecret";
        public const string NODE_COLUMN_ENABLE_SECRET_NEW = "enableSecretNew";
        public const string NODE_COLUMN_ENCODING_TYPE = "encodingType";
        public const string NODE_COLUMN_SNMP_READ_COMMUNITY = "snmpReadCommunity";
        public const string NODE_COLUMN_SNMP_WRITE_COMMUNITY = "snmpWriteCommunity";
        public const string NODE_COLUMN_OS_VERSION = "osVersion";
        public const string NODE_COLUMN_OS_FILE = "osFile";


        private static DataTable MakeNodeTable(DataSet ds)
        {
            // テーブルを新規作成
            DataTable table = new DataTable(NODE_TABLE);

            // nodeId列
            // このテーブルの主キーとなる列。すべてのノードはnodeIdで区別される。
            DataColumn column = new DataColumn();
            column.DataType = Type.GetType("System.Int32");
            column.ColumnName = NODE_COLUMN_NODE_ID;
            column.ReadOnly = true;
            column.AllowDBNull = false;
            column.Unique = true;
            column.AutoIncrement = true;
            column.AutoIncrementSeed = 0;
            column.AutoIncrementStep = 1;
            table.Columns.Add(column);

            // --------
            // ここからTreeView連携用
            // --------

            // 親のnodeId
            // 0はルートノードを表すことにする
            table.Columns.Add(GetInt32Column(NODE_COLUMN_PARENT_ID, 0));

            // シーケンス番号。ツリービューの並べ替え用。
            table.Columns.Add(GetInt32Column(NODE_COLUMN_SORT_ORDER, 0));

            // どのTreeViewに属するかを識別する番号。
            table.Columns.Add(GetInt32Column(NODE_COLUMN_MODELID, 0));

            // ImageIndex
            table.Columns.Add(GetInt32Column(NODE_COLUMN_IMAGEINDEX, 0));

            // SelectedImageIndex
            table.Columns.Add(GetInt32Column(NODE_COLUMN_SELECTED_IMAGEINDEX, 0));

            // ツリービューのグループ？
            table.Columns.Add(GetBooleanColumn(NODE_COLUMN_ISGROUP, false));

            // ホスト名
            // string hostname
            table.Columns.Add(GetStringColumn(NODE_COLUMN_HOSTNAME, "New Node"));

            // --------
            // ここまでTreeView連携用
            // --------

            // サイトの設定に従う？
            table.Columns.Add(GetBooleanColumn(NODE_COLUMN_FOLLOW_GROUP, true));

            // 管理番号
            table.Columns.Add(GetInt32Column(NODE_COLUMN_DEVICE_NO, 0));

            // 機種
            table.Columns.Add(GetStringColumn(NODE_COLUMN_DEVICE_TYPE, String.Empty));

            // IPアドレス
            table.Columns.Add(GetStringColumn(NODE_COLUMN_ADDRESS, String.Empty));

            // TCPポート
            table.Columns.Add(GetInt32Column(NODE_COLUMN_TCP_PORT, 23));

            // SSHを使う？
            table.Columns.Add(GetBooleanColumn(NODE_COLUMN_USE_SSH, false));

            // ユーザ名
            table.Columns.Add(GetStringColumn(NODE_COLUMN_CONNECT_USERNAME, String.Empty));

            // telnet接続のパスワード
            table.Columns.Add(GetStringColumn(NODE_COLUMN_CONNECT_SECRET, String.Empty));

            // telnet接続のパスワード(新)
            table.Columns.Add(GetStringColumn(NODE_COLUMN_CONNECT_SECRET_NEW, String.Empty));

            // 管理者モード移行時のコマンド
            table.Columns.Add(GetStringColumn(NODE_COLUMN_PRIVILEDGE_COMMAND, "enable"));

            // 管理者パスワード
            table.Columns.Add(GetStringColumn(NODE_COLUMN_ENABLE_SECRET, String.Empty));

            // 管理者パスワード(新)
            table.Columns.Add(GetStringColumn(NODE_COLUMN_ENABLE_SECRET_NEW, String.Empty));

            // エンコーディングタイプ
            table.Columns.Add(GetStringColumn(NODE_COLUMN_ENCODING_TYPE, "shift-jis"));

            // -------------------------
            // ここから踏み台 2006.08.10
            // -------------------------

            // bool useGateway
            // 踏み台を使う？
            table.Columns.Add(GetBooleanColumn("useGateway", false));

            // string gwAddress
            // 踏み台のIPアドレス
            table.Columns.Add(GetStringColumn("gwAddress", String.Empty));

            // int gwPort
            // 踏み台のTCPポート
            table.Columns.Add(GetInt32Column("gwPort", 23));

            // string gwUsername
            // 踏み台に接続するユーザ名
            table.Columns.Add(GetStringColumn("gwUsername", String.Empty));

            // string connectSecret
            // 踏み台の接続のパスワード
            table.Columns.Add(GetStringColumn("gwConnectSecret", String.Empty));

            // -------------------------
            // ここまで踏み台
            // -------------------------

            // SNMPコミュニティ(READ)
            // string snmpReadCommunity
            table.Columns.Add(GetStringColumn(NODE_COLUMN_SNMP_READ_COMMUNITY, "public"));

            // SNMPコミュニティ(WRITE)
            // string snmpWriteCommunity
            table.Columns.Add(GetStringColumn(NODE_COLUMN_SNMP_WRITE_COMMUNITY, "private"));

            // string osVersion
            table.Columns.Add(GetStringColumn(NODE_COLUMN_OS_VERSION, String.Empty));

            // string osFile
            table.Columns.Add(GetStringColumn(NODE_COLUMN_OS_FILE, String.Empty));

            // nodeId列を主キーとして設定
            DataColumn[] PK = new DataColumn[1];
            PK[0] = table.Columns[NODE_COLUMN_NODE_ID]; // "nodeId"
            table.PrimaryKey = PK;

            // 外部キー制約
            DataColumn FK = table.Columns[NODE_COLUMN_PARENT_ID]; // "parentId"
            ForeignKeyConstraint FKC = new ForeignKeyConstraint(PK[0], FK);
            FKC.DeleteRule = Rule.Cascade;
            table.Constraints.Add(FKC);
            table.AcceptChanges();

            // データセットに追加
            ds.Tables.Add(table);

            // 親子関係のリレーションをデータセットに付与
            DataRelation DR = new DataRelation("ParentChild", PK[0], FK, false);
            ds.Relations.Add(DR);

            // ルートノードを作成しておく。
            DataRow row = table.NewRow();
            row[Database.NODE_COLUMN_HOSTNAME] = "Root";
            row[Database.NODE_COLUMN_ISGROUP] = true;
            row[Database.NODE_COLUMN_IMAGEINDEX] = 0;              //イメージインデックスに注意
            row[Database.NODE_COLUMN_SELECTED_IMAGEINDEX] = 0;     //イメージインデックスに注意
            table.Rows.Add(row);

            //テスト
            /*
            for (int i = 0; i < 1000; i++)
            {
                row = table.NewRow();
                row[Database.NODE_COLUMN_HOSTNAME] = "新しいノード";
                row[Database.NODE_COLUMN_IMAGEINDEX] = TreeViewImageIndex.Router;
                row[Database.NODE_COLUMN_SELECTEDIMAGEINDEX] = TreeViewImageIndex.Router;
                row[Database.NODE_COLUMN_ISGROUP] = false;
                table.Rows.Add(row);
            }
            */

            ds.AcceptChanges();

            return (table);
        }


        private static DataTable MakeConfigTable(DataSet ds)
        {
            DataTable table = new DataTable("Config");

            // id列
            // このテーブルの主キーとなる列。
            DataColumn column = new DataColumn();
            column.DataType = Type.GetType("System.Int32");
            column.ColumnName = "id";
            column.ReadOnly = true;
            column.AllowDBNull = false;
            column.Unique = true;
            column.AutoIncrement = true;
            column.AutoIncrementSeed = 0;
            column.AutoIncrementStep = 1;
            table.Columns.Add(column);

            // string LogPath
            table.Columns.Add(GetStringColumn("logPath", String.Empty));

            // bool isTimestampEnabled
            table.Columns.Add(GetBooleanColumn("isTimestampEnabled", false));

            // int width
            table.Columns.Add(GetInt32Column("width", 0));

            // int height
            table.Columns.Add(GetInt32Column("height", 0));

            // int mainFormLocationX
            table.Columns.Add(GetInt32Column("mainFormLocationX", 0));

            // int mainFormLocationY
            table.Columns.Add(GetInt32Column("mainFormLocationY", 0));

            // int splitterDistance1
            table.Columns.Add(GetInt32Column("splitterDistance1", 0));

            // int splitterDistance2
            table.Columns.Add(GetInt32Column("splitterDistance2", 0));

            // int myshortcutWidth
            table.Columns.Add(GetInt32Column("myShortcutWidth", 0));

            // int myShortcutHeight
            table.Columns.Add(GetInt32Column("myShortcutHeight", 0));

            // int myshortcutLocationX
            table.Columns.Add(GetInt32Column("myShortcutLocationX", 0));

            // int myShortcutLocationY
            table.Columns.Add(GetInt32Column("myShortcutLocationY", 0));

            // string myShortcutHome
            table.Columns.Add(GetStringColumn("myShortcutHome", string.Empty));

            // bool isAutoexecMyShortcut
            table.Columns.Add(GetBooleanColumn("isAutoexecMyShortcut", false));

            // string myShortcutMyFavorite
            table.Columns.Add(GetStringColumn("myShortcutMyFavorite", string.Empty));

            // int sendPasswordDelay
            table.Columns.Add(GetInt32Column("sendPasswordDelay", 200));

            // bool toolbarEnabled
            table.Columns.Add(GetBooleanColumn("toolbarEnabled", true));

            // int pingFormWidth
            table.Columns.Add(GetInt32Column("pingFormWidth", 0));

            // int pingFormHeight
            table.Columns.Add(GetInt32Column("pingFormHeight", 0));

            // int pingFormLocationX
            table.Columns.Add(GetInt32Column("pingFormLocationX", 0));

            // int mainFormLocationY
            table.Columns.Add(GetInt32Column("pingFormLocationY", 0));

            // string pingFormTarget
            table.Columns.Add(GetStringColumn("pingFormTarget", string.Empty));

            // １行作成。
            table.Rows.Add(table.NewRow());

            ds.Tables.Add(table);

            return table;
        }

        /// <summary>
        /// NodeテーブルにCSVの内容を追加します。
        /// </summary>
        /// <param name="csv">CSVで区切られた文字列配列</param>
        public int AddRowCsv(int parentId, string[] csv)
        {
            try
            {
                bool created = false;
                DataRow dr;
                if (csv[0].Equals(string.Empty))
                {
                    // 管理番号が未設定なら、無条件で作成
                    dr = _ds.Tables["Node"].NewRow();
                    created = true;
                }
                else
                {
                    DataRow[] rows = _ds.Tables["Node"].Select("deviceNo='" + csv[0] + "' AND parentId='" + parentId + "'");
                    if (rows == null || rows.Length == 0)
                    {
                        // 親の直下にその管理番号のノードがまだないなら作成
                        dr = _ds.Tables["Node"].NewRow();
                        created = true;
                    }
                    else if ((int)rows[0]["parentId"] != parentId)
                    {
                        // その管理番号のノードが既に存在するんだけど、別の親になっている場合、新規作成
                        dr = _ds.Tables["Node"].NewRow();
                        created = true;
                    }
                    else
                    {
                        // 既に存在。
                        dr = rows[0];
                    }
                }

                dr.BeginEdit();
                for (int i = 0; i < CsvColumn.Length; i++)
                {
                    string column = CsvColumn[i].Substring(CsvColumn[i].IndexOf(".") + 1);
                    if (CsvColumn[i].StartsWith("String."))
                    {
                        dr[column] = csv[i];
                    }
                    else if (CsvColumn[i].StartsWith("Int32"))
                    {
                        dr[column] = Int32.Parse(csv[i]);
                    }
                }
                dr.EndEdit();

                if (created)
                {
                    _ds.Tables["Node"].Rows.Add(dr);
                }

                return (int)dr["nodeId"];
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// CSV形式で行を取得します。
        /// </summary>
        /// <param name="dr">対象となる行</param>
        /// <returns>CSV形式の文字列</returns>
        public static string GetRowCsv(DataRow dr)
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                foreach (string col in CsvColumn)
                {
                    string column = col.Substring(col.IndexOf(".") + 1);

                    if (col.StartsWith("String"))
                    {
                        sb.Append((string)dr[column]);
                    }
                    else if (col.StartsWith("Int32"))
                    {
                        sb.Append(((Int32)dr[column]).ToString());
                    }

                    sb.Append(",");
                }

                return (sb.ToString());
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static string GetRowCsvHeader()
        {
            StringBuilder sb = new StringBuilder();

            foreach (string col in CsvColumn)
            {
                string column = col.Substring(col.IndexOf(".") + 1);
                sb.Append("/Node/");
                sb.Append(column);
                sb.Append(",");
            }

            return sb.ToString();
        }

        // CSVのカラム名
        private static string[] CsvColumn = new string[] {
            "Int32.deviceNo", 
            "String.hostname", 
            "String.ipaddress", 
            "Int32.tcpPort", 
            "String.username", 
            "String.connectSecret",
            "String.privilegeCommand",
            "String.enableSecret"
        };
    }


    #region DataRowComparer

    // "sortOrder"列でソート
    // Array.Sort(rows, new DataRowComparer());
    public class DataRowComparer : System.Collections.IComparer
    {
        public int Compare(object x, object y)
        {
            DataRow rowA = x as DataRow;
            DataRow rowB = y as DataRow;

            int a = (Int32)rowA[Database.NODE_COLUMN_SORT_ORDER];
            int b = (Int32)rowB[Database.NODE_COLUMN_SORT_ORDER];
            return a.CompareTo(b);
        }
    }
    #endregion


}
// Local Variables:
// tab-width: 4
// End:
