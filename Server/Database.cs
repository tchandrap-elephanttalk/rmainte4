/* *-*-mode:java; -*- */

// Copyright Takamitsu IIDA <gsi@nifty.com>
// CCIE4288
// 2004.11.26
// 2006.07.20 .NET2.0�Ή�
// 2006.07.26 Poderosa��Plugin�Ή��ŕۑ����ύX

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
    /// �m�[�h��Ԃ�\���N���X�ł��B
    /// </summary>
    public class NodeStatus
    {
        /// <summary>
        /// �s����Ԃ�\���萔�B
        /// </summary>
        public const int Unknown = 0;

        /// <summary>
        /// �A�b�v��Ԃ�\���萔�B
        /// </summary>
        public const int Up = 1;

        /// <summary>
        /// �_�E����Ԃ�\���萔�B
        /// </summary>
        public const int Down = 2;

        /// <summary>
        /// �Ǘ���̃_�E����\���萔�B
        /// </summary>
        public const int AdminDown = 3;

        /// <summary>
        /// ���ꂼ��̏�Ԃ̐��������i�[���镶����z��B
        /// </summary>
        public static string[] Description = { "������", "�A�b�v", "�_�E��", "�Ǘ��_�E��" };
    }

    /// <summary>
    /// Database�N���X�́A���[�^�̏����i�[����f�[�^�Z�b�g��ۗL����Singleton�N���X�ł��B
    /// </summary>
    public sealed class Database
    {
        // �O����̓C���X�^���X�������Ȃ�
        private Database()
        {
            // ���ꕨ�����̉�
            _ds = new DataSet();
        }

        // �O������C���X�^���X���擾
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
        /// �f�[�^�Z�b�g�̕ύX���R
        /// </summary>
        public enum ChangeReason { NodeAdded = 0, NodeDeleted, NodeMoved, NodePropertyChanged, };

        private static string Filename;
        static Database()
        {
            FileInfo fi = new FileInfo(Application.ExecutablePath);
            // ���s�t�@�C�����̊g���q��xml�ɂ������̂�T���A�Ȃ����rmainte4.xml�Ƃ���
            Filename = Path.ChangeExtension(fi.Name, "xml");
            if (File.Exists(Filename) == false)
            {
                Filename = Path.Combine(fi.DirectoryName, "rmainte4.xml");
            }
        }

        /// <summary>
        /// �}�X�^�[�f�[�^�x�[�X�������f�[�^�Z�b�g�I�u�W�F�N�g
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
        /// ���̃f�[�^�Z�b�g�̃o�[�W����
        /// </summary>
        public const int VERSION = 1;

        /// <summary>
        /// �g���v���p�e�B���g���ăf�[�^�Z�b�g�Ƀo�[�W��������t�^���܂��B
        /// </summary>
        /// <param name="ds">�ΏۂƂȂ�f�[�^�Z�b�g</param>
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
        /// �o�[�W�������擾���܂��B
        /// </summary>
        /// <param name="ds">�ΏۂƂȂ�f�[�^�Z�b�g</param>
        /// <returns>�f�[�^�x�[�X�ɕt�^���ꂽ�o�[�W����</returns>
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
        /// �t�@�C���ɕۑ�����Ă���f�[�^�Z�b�g�𕜌����܂��B
        /// </summary>
        /// <param name="filename">�ۑ�����Ă���t�@�C���̖��O</param>
        public void Load(string filename)
        {

            // �t�@�C������ǂ�Œ��g���l�߂�
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

            // �t�@�C������ǂ񂾕����Â��ꍇ�̓R���o�[�g����
            if (GetVersion(_ds) < VERSION)
            {
                // Log.WriteLine("�f�[�^�x�[�X���ŐV�ɕϊ����܂��B");
                try
                {
                    Convert();
                    WriteXml();
                }
                catch (Exception ex) { System.Windows.Forms.MessageBox.Show(ex.ToString()); }
                // Log.WriteLine("�f�[�^�x�[�X�̕ϊ����I�����܂����B");
            }
        }

        /// <summary>
        /// �f�[�^�Z�b�g�I�u�W�F�N�g��XML�ŕۑ����܂��B
        /// </summary>
        public void WriteXml()
        {
            WriteXml(Filename);
        }

        /// <summary>
        /// �f�[�^�Z�b�g�I�u�W�F�N�g��XML�ŕۑ����܂��B
        /// </summary>
        /// <param name="filename">�ۑ���̃t�@�C����</param>
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
        /// �S�e�[�u���̑S�G���g�����폜���܂��B
        /// </summary>
        public void Clear()
        {
            lock (_ds)
            {
                _ds.Clear();
            }
        }

        /// <summary>
        /// �f�[�^�Z�b�g���쐬���܂��B
        /// </summary>
        public void Create()
        {
            // �o�[�W�������Z�b�g
            SetVersion(_ds, VERSION);

            // �e�[�u�����쐬
            MakeTables(_ds);
        }

        /// <summary>
        /// �V�����f�[�^�Z�b�g�Œu�������܂��B
        /// </summary>
        /// <param name="ds">�V����DataSet</param>
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
        /// �o�[�W�����̌Â�DataSet��V����DataSet�ɈڐA���܂��B
        /// </summary>
        private void Convert()
        {
            // �f�[�^�Z�b�g����V�K�ɍ쐬
            DataSet newds = new DataSet();

            // �V�����f�[�^�Z�b�g�Ƀo�[�W�������Z�b�g
            SetVersion(newds, VERSION);

            // �e�[�u�����쐬
            MakeTables(newds);

            // �S�Ă̌Â��e�[�u���Ɋւ���
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
                    // �Â��e�[�u���̑S�Ă̍s�ɂ���
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
            // ���g����̃e�[�u�����\�z
            MakeNodeTable(ds);
            MakeActionTable(ds);
            MakeConfigTable(ds);
        }

        private string IntToString(int i)
        {
            Char[] c = new Char[] { (char)((int)'a' + i) };
            return (new String(c));
        }

        #region DataColumn���擾
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
            // �e�[�u����V�K�쐬
            DataTable table = new DataTable("Action");

            // actionId��
            // ���̃e�[�u���̎�L�[�ƂȂ��B���ׂĂ̎��{�v���actionId�ŋ�ʂ����B
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
            // ����Action��ۗL���Ă���NodeProperty��nodeId
            table.Columns.Add(GetInt32Column("ownerId", 0));

            // ���ݖ��g�p�B
            // string name
            table.Columns.Add(GetStringColumn("name", String.Empty));

            // string description
            table.Columns.Add(GetStringColumn("description", String.Empty));

            // Int32 sceduleType
            table.Columns.Add(GetInt32Column("scheduleType", 0));

            // string status
            table.Columns.Add(GetStringColumn("status", "�v��쐬"));

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

            // �V�i���I1
            // Int32 scenario1SimultaneousConnection
            table.Columns.Add(GetInt32Column("scenario1SimultaneousConnection", 4));

            // bool scenario1DoCiscoEnable
            table.Columns.Add(GetBooleanColumn("scenario1DoCiscoEnable", true));

            // �V�i���I2
            // �ڑ����ăR�}���h��@��

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

            // �V�i���I3

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

            // �V�i���I4

            // string scenario4Filename
            table.Columns.Add(GetStringColumn("scenario4Filename", String.Empty));

            // string scenario4Dllfile
            table.Columns.Add(GetStringColumn("scenario4Dllfile", String.Empty));

            // �V�i���I5

            // string scenario5Filename
            table.Columns.Add(GetStringColumn("scenario5Filename", String.Empty));

            // string scenario5Arg
            table.Columns.Add(GetStringColumn("scenario5Arg", String.Empty));

            // actionId�����L�[�Ƃ��Đݒ�
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
        /// �m�[�h�e�[�u���̃e�[�u����
        /// </summary>
        public const string NODE_TABLE = "Node";

        /// <summary>
        /// �m�[�hID�̗�
        /// </summary>
        public const string NODE_COLUMN_NODE_ID = "nodeId";

        /// <summary>
        /// �e�m�[�h��nodeId��\����
        /// </summary>
        public const string NODE_COLUMN_PARENT_ID = "parentId";

        /// <summary>
        /// �m�[�h�̃\�[�g������\����
        /// </summary>
        public const string NODE_COLUMN_SORT_ORDER = "sortOrder";

        /// <summary>
        /// �c���[�r���[��ID
        /// </summary>
        public const string NODE_COLUMN_MODELID = "modelId";
        
        /// <summary>
        /// �c���[�r���[�ɕ\������C���[�W�̃C���f�b�N�X��\����
        /// </summary>
        public const string NODE_COLUMN_IMAGEINDEX = "imageIndex";

        /// <summary>
        /// �c���[�r���[�ɕ\������I�����ꂽ��Ԃ̃C���[�W�̃C���f�b�N�X��\����
        /// </summary>
        public const string NODE_COLUMN_SELECTED_IMAGEINDEX = "selectedImageIndex";

        /// <summary>
        /// �O���[�v���ǂ�����\����
        /// </summary>
        public const string NODE_COLUMN_ISGROUP = "isGroup";

        /// <summary>
        /// �z�X�g���B�c���[�r���[�͂�����e�L�X�g�Ƃ��ĕ\������B
        /// </summary>
        public const string NODE_COLUMN_HOSTNAME = "hostname";

        /// <summary>
        /// IP�A�h���X�B
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
            // �e�[�u����V�K�쐬
            DataTable table = new DataTable(NODE_TABLE);

            // nodeId��
            // ���̃e�[�u���̎�L�[�ƂȂ��B���ׂẴm�[�h��nodeId�ŋ�ʂ����B
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
            // ��������TreeView�A�g�p
            // --------

            // �e��nodeId
            // 0�̓��[�g�m�[�h��\�����Ƃɂ���
            table.Columns.Add(GetInt32Column(NODE_COLUMN_PARENT_ID, 0));

            // �V�[�P���X�ԍ��B�c���[�r���[�̕��בւ��p�B
            table.Columns.Add(GetInt32Column(NODE_COLUMN_SORT_ORDER, 0));

            // �ǂ�TreeView�ɑ����邩�����ʂ���ԍ��B
            table.Columns.Add(GetInt32Column(NODE_COLUMN_MODELID, 0));

            // ImageIndex
            table.Columns.Add(GetInt32Column(NODE_COLUMN_IMAGEINDEX, 0));

            // SelectedImageIndex
            table.Columns.Add(GetInt32Column(NODE_COLUMN_SELECTED_IMAGEINDEX, 0));

            // �c���[�r���[�̃O���[�v�H
            table.Columns.Add(GetBooleanColumn(NODE_COLUMN_ISGROUP, false));

            // �z�X�g��
            // string hostname
            table.Columns.Add(GetStringColumn(NODE_COLUMN_HOSTNAME, "New Node"));

            // --------
            // �����܂�TreeView�A�g�p
            // --------

            // �T�C�g�̐ݒ�ɏ]���H
            table.Columns.Add(GetBooleanColumn(NODE_COLUMN_FOLLOW_GROUP, true));

            // �Ǘ��ԍ�
            table.Columns.Add(GetInt32Column(NODE_COLUMN_DEVICE_NO, 0));

            // �@��
            table.Columns.Add(GetStringColumn(NODE_COLUMN_DEVICE_TYPE, String.Empty));

            // IP�A�h���X
            table.Columns.Add(GetStringColumn(NODE_COLUMN_ADDRESS, String.Empty));

            // TCP�|�[�g
            table.Columns.Add(GetInt32Column(NODE_COLUMN_TCP_PORT, 23));

            // SSH���g���H
            table.Columns.Add(GetBooleanColumn(NODE_COLUMN_USE_SSH, false));

            // ���[�U��
            table.Columns.Add(GetStringColumn(NODE_COLUMN_CONNECT_USERNAME, String.Empty));

            // telnet�ڑ��̃p�X���[�h
            table.Columns.Add(GetStringColumn(NODE_COLUMN_CONNECT_SECRET, String.Empty));

            // telnet�ڑ��̃p�X���[�h(�V)
            table.Columns.Add(GetStringColumn(NODE_COLUMN_CONNECT_SECRET_NEW, String.Empty));

            // �Ǘ��҃��[�h�ڍs���̃R�}���h
            table.Columns.Add(GetStringColumn(NODE_COLUMN_PRIVILEDGE_COMMAND, "enable"));

            // �Ǘ��҃p�X���[�h
            table.Columns.Add(GetStringColumn(NODE_COLUMN_ENABLE_SECRET, String.Empty));

            // �Ǘ��҃p�X���[�h(�V)
            table.Columns.Add(GetStringColumn(NODE_COLUMN_ENABLE_SECRET_NEW, String.Empty));

            // �G���R�[�f�B���O�^�C�v
            table.Columns.Add(GetStringColumn(NODE_COLUMN_ENCODING_TYPE, "shift-jis"));

            // -------------------------
            // �������瓥�ݑ� 2006.08.10
            // -------------------------

            // bool useGateway
            // ���ݑ���g���H
            table.Columns.Add(GetBooleanColumn("useGateway", false));

            // string gwAddress
            // ���ݑ��IP�A�h���X
            table.Columns.Add(GetStringColumn("gwAddress", String.Empty));

            // int gwPort
            // ���ݑ��TCP�|�[�g
            table.Columns.Add(GetInt32Column("gwPort", 23));

            // string gwUsername
            // ���ݑ�ɐڑ����郆�[�U��
            table.Columns.Add(GetStringColumn("gwUsername", String.Empty));

            // string connectSecret
            // ���ݑ�̐ڑ��̃p�X���[�h
            table.Columns.Add(GetStringColumn("gwConnectSecret", String.Empty));

            // -------------------------
            // �����܂œ��ݑ�
            // -------------------------

            // SNMP�R�~���j�e�B(READ)
            // string snmpReadCommunity
            table.Columns.Add(GetStringColumn(NODE_COLUMN_SNMP_READ_COMMUNITY, "public"));

            // SNMP�R�~���j�e�B(WRITE)
            // string snmpWriteCommunity
            table.Columns.Add(GetStringColumn(NODE_COLUMN_SNMP_WRITE_COMMUNITY, "private"));

            // string osVersion
            table.Columns.Add(GetStringColumn(NODE_COLUMN_OS_VERSION, String.Empty));

            // string osFile
            table.Columns.Add(GetStringColumn(NODE_COLUMN_OS_FILE, String.Empty));

            // nodeId�����L�[�Ƃ��Đݒ�
            DataColumn[] PK = new DataColumn[1];
            PK[0] = table.Columns[NODE_COLUMN_NODE_ID]; // "nodeId"
            table.PrimaryKey = PK;

            // �O���L�[����
            DataColumn FK = table.Columns[NODE_COLUMN_PARENT_ID]; // "parentId"
            ForeignKeyConstraint FKC = new ForeignKeyConstraint(PK[0], FK);
            FKC.DeleteRule = Rule.Cascade;
            table.Constraints.Add(FKC);
            table.AcceptChanges();

            // �f�[�^�Z�b�g�ɒǉ�
            ds.Tables.Add(table);

            // �e�q�֌W�̃����[�V�������f�[�^�Z�b�g�ɕt�^
            DataRelation DR = new DataRelation("ParentChild", PK[0], FK, false);
            ds.Relations.Add(DR);

            // ���[�g�m�[�h���쐬���Ă����B
            DataRow row = table.NewRow();
            row[Database.NODE_COLUMN_HOSTNAME] = "Root";
            row[Database.NODE_COLUMN_ISGROUP] = true;
            row[Database.NODE_COLUMN_IMAGEINDEX] = 0;              //�C���[�W�C���f�b�N�X�ɒ���
            row[Database.NODE_COLUMN_SELECTED_IMAGEINDEX] = 0;     //�C���[�W�C���f�b�N�X�ɒ���
            table.Rows.Add(row);

            //�e�X�g
            /*
            for (int i = 0; i < 1000; i++)
            {
                row = table.NewRow();
                row[Database.NODE_COLUMN_HOSTNAME] = "�V�����m�[�h";
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

            // id��
            // ���̃e�[�u���̎�L�[�ƂȂ��B
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

            // �P�s�쐬�B
            table.Rows.Add(table.NewRow());

            ds.Tables.Add(table);

            return table;
        }

        /// <summary>
        /// Node�e�[�u����CSV�̓��e��ǉ����܂��B
        /// </summary>
        /// <param name="csv">CSV�ŋ�؂�ꂽ������z��</param>
        public int AddRowCsv(int parentId, string[] csv)
        {
            try
            {
                bool created = false;
                DataRow dr;
                if (csv[0].Equals(string.Empty))
                {
                    // �Ǘ��ԍ������ݒ�Ȃ�A�������ō쐬
                    dr = _ds.Tables["Node"].NewRow();
                    created = true;
                }
                else
                {
                    DataRow[] rows = _ds.Tables["Node"].Select("deviceNo='" + csv[0] + "' AND parentId='" + parentId + "'");
                    if (rows == null || rows.Length == 0)
                    {
                        // �e�̒����ɂ��̊Ǘ��ԍ��̃m�[�h���܂��Ȃ��Ȃ�쐬
                        dr = _ds.Tables["Node"].NewRow();
                        created = true;
                    }
                    else if ((int)rows[0]["parentId"] != parentId)
                    {
                        // ���̊Ǘ��ԍ��̃m�[�h�����ɑ��݂���񂾂��ǁA�ʂ̐e�ɂȂ��Ă���ꍇ�A�V�K�쐬
                        dr = _ds.Tables["Node"].NewRow();
                        created = true;
                    }
                    else
                    {
                        // ���ɑ��݁B
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
        /// CSV�`���ōs���擾���܂��B
        /// </summary>
        /// <param name="dr">�ΏۂƂȂ�s</param>
        /// <returns>CSV�`���̕�����</returns>
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

        // CSV�̃J������
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

    // "sortOrder"��Ń\�[�g
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
