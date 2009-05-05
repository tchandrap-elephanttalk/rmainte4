using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace rmainte4
{

    
    /// <summary>
    /// �T�[�o�̏���ۑ����Ă������߂̃N���X�B
    /// </summary>
    public class ServerEnv
    {
        public const string DEFAULT_SERVER_ADDRESS = "localhost";
        public const int DEFAULT_TCP_PORT = 8338;
        public const int DEFAULT_HTTP_PORT = 8339;
        public const string DEFAULT_OBJECT_URI = "rmainte4";
        public const string DEFAULT_CONNECT_TYPE = "Http";

        public ServerEnv()
        {
        }

        /// <summary>
        /// �T�[�o�̐ڑ��^�C�v�̗񋓌^�B
        /// </summary>
        public enum ServerConnectType { IPC = 0, TCP = 1, HTTP = 2 };

        public ServerConnectType ConnectType
        {
            get { return _serverConnectType; }
            set { _serverConnectType = value; }
        }
        private ServerConnectType _serverConnectType = ServerConnectType.TCP;

        public int TcpPort
        {
            get { return _tcpPort; }
            set { _tcpPort = value; }
        }
        private int _tcpPort = DEFAULT_TCP_PORT;

        public int HttpPort
        {
            get { return _httpPort; }
            set { _httpPort = value; }
        }
        private int _httpPort = DEFAULT_HTTP_PORT;

        public string Address
        {
            get { return _serverAddress; }
            set { _serverAddress = value; }
        }
        private string _serverAddress = DEFAULT_SERVER_ADDRESS;

        public string ObjectUri
        {
            get { return _objectUri; }
            set { _objectUri = value; }
        }
        private string _objectUri = DEFAULT_OBJECT_URI;

        public IRmainte IRmainte
        {
            get { return _ir; }
            set { _ir = value; }
        }
        private IRmainte _ir = null;

        public double Version
        {
            get { return _version; }
            set { _version = value; }
        }
        private double _version = 1.0;

        public DataSet DataSet
        {
            get { return _ds; }
            set { _ds = value; }
        }
        private DataSet _ds = null;

        // �Ō�Ɏ�M����Notification�̎�����ۑ����Ă���
        public DateTime LastReceiveTime
        {
            get { return _lastReceivedTime; }
            set { _lastReceivedTime = value; }
        }
        private DateTime _lastReceivedTime = DateTime.Now;

        public bool IsConnected
        {
            get { return (_ir != null); }
        }

        public void GetIRmainteImplOrNull()
        {
            // ���[�J���z�X�g�̏ꍇ�����͓��ʈ����ɂ���
            if (_serverAddress.Equals("localhost"))
            {
                _ir = GetIRmainteImplIpcOrNull();
                return;
            }

            if (_serverConnectType == ServerEnv.ServerConnectType.HTTP)
            {
                _ir = GetIRmainteImplHttpOrNull();
            }
            else
            {
                _ir = GetIRmainteImplTcpOrNull();
            }
        }

        private IRmainte GetIRmainteImplIpcOrNull()
        {
            string url = "ipc://" + _objectUri + "/" + _objectUri;

            return (IRmainte)Activator.GetObject(typeof(IRmainte), url);
        }

        private IRmainte GetIRmainteImplTcpOrNull()
        {
            if (ChannelServices.GetChannel("tcp") == null)
            {
                ChannelServices.RegisterChannel(new TcpClientChannel(), true);
            }
            string url = "tcp://" + _serverAddress + ":" + _tcpPort + "/" + _objectUri;

            return (IRmainte)Activator.GetObject(typeof(IRmainte), url);
        }

        private IRmainte GetIRmainteImplHttpOrNull()
        {
            ChannelServices.RegisterChannel(new HttpClientChannel(), true);
            string url = "http://" + _serverAddress + ":" + _httpPort + "/" + _objectUri;

            return (IRmainte)Activator.GetObject(typeof(IRmainte), url);
        }




        // -----------------------------------------------------------------------------------

        /// <summary>
        /// �񓯊��ɏ����擾���邽�߂̃f���Q�[�g
        /// </summary>
        /// <param name="newerThanThisDate">���̎��������V�����������ꂽNotification���擾���܂�</param>
        /// <returns>Notification�̋l�܂����A���C���X�g��Ԃ��܂�</returns>
        public delegate ArrayList AsyncReceiveNotificationDelegate(DateTime newerThanThisDate);

        private IAsyncResult _ar = null;

        /// <summary>
        /// Notification�̎�M���J�n���܂��B
        /// </summary>
        /// <param name="serverAddress"></param>
        public void StartAsyncReceiveNotification()
        {
            if (IsConnected == false)
            {
                return;
            }

            _lastReceivedTime = DateTime.Now;

            AsyncCallback acb = new AsyncCallback(this.AsyncReceiveNotificationCallBack);
            AsyncReceiveNotificationDelegate d = new AsyncReceiveNotificationDelegate(_ir.ReceiveNotification);
            _ar = d.BeginInvoke(_lastReceivedTime, acb, null);
        }

        /// <summary>
        /// �񓯊���Notification����M����ۂɌĂ΂��R�[���o�b�N���\�b�h�B
        /// </summary>
        /// <param name="ar">IAsyncResult�I�u�W�F�N�g</param>
        private void AsyncReceiveNotificationCallBack(IAsyncResult ar)
        {
            // string serverAddress = (string)ar.AsyncState;

            AsyncReceiveNotificationDelegate d = (AsyncReceiveNotificationDelegate)((AsyncResult)ar).AsyncDelegate;
            ArrayList list;
            try
            {
                list = (ArrayList)d.EndInvoke(ar);
            }
            catch (Exception ex)
            {
                // �����ŗ�O���o����Ă��Ƃ́A�ԈႢ�Ȃ��l�b�g���[�N�̃G���[
                Debug.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);

                OnOfflineEventHandler(new OfflineEventArgs(this));
                return;
            }

            if (list == null || list.Count == 0)
            {
                // �l�b�g���[�N�̃G���[���Ȃ��H
            }
            else
            {
                Notification notif = (Notification)list[list.Count - 1];
                _lastReceivedTime = notif.CreatedTime;
                OnNotificationEventHandler(new NotificationEventArgs(this, list));
            }

            // �ēx��M���J�n����
            AsyncCallback acb = new AsyncCallback(this.AsyncReceiveNotificationCallBack);
            _ar = d.BeginInvoke(_lastReceivedTime, acb, null);
        }

        /// <summary>
        /// Notification����M�����Ƃ��ɔ�������C�x���g�n���h���̎��́B
        /// </summary>
        public event NotificationEvent NotificationEventHandler
        {
            add
            {
                lock (this)
                {
                    _notificationEventHandler += value;
                }
            }
            remove
            {
                lock (this)
                {
                    _notificationEventHandler -= value;
                }
            }
        }
        private event NotificationEvent _notificationEventHandler;

        /// <summary>
        /// �C�x���g�𔭓����郁�\�b�h�B
        /// </summary>
        /// <param name="e"></param>
        private void OnNotificationEventHandler(NotificationEventArgs e)
        {
            NotificationEvent handler = null;
            lock (this)
            {
                handler = _notificationEventHandler;
            }

            if (handler != null)
            {
                handler(this, e);
            }
        }

        // -----------------------------------------------------------------------------------


        public event OfflineEvent OfflineEventHandler
        {
            add
            {
                lock (this)
                {
                    _offlineEventHandler += value;
                }
            }
            remove
            {
                lock (this)
                {
                    _offlineEventHandler -= value;
                }
            }
        }
        private event OfflineEvent _offlineEventHandler;


        private void OnOfflineEventHandler(OfflineEventArgs e)
        {
            OfflineEvent handler = null;
            lock (this)
            {
                handler = _offlineEventHandler;
            }

            if (handler != null)
            {
                handler(this, e);
            }
        }

        // -----------------------------------------------------------------------------------

        private static void WriteException(Exception ex)
        {
            Debug.WriteLine(ex.Message);
            Debug.WriteLine(ex.StackTrace);
            Debug.Flush();
        }

        /// <summary>
        /// �T�[�o�̃��b�N���擾���܂��B
        /// ���b���u���b�N���܂��B
        /// </summary>
        /// <param name="env"></param>
        /// <returns>���b�N�������True</returns>
        public bool GetLock()
        {
            // if (!IRmainteAttribute.ConfirmVersion("GetLock", MainForm.CLIENT_VERSION))
            //     return false;

            if (_ir == null)
                return false;

            try
            {
                return _ir.GetLock(MainForm.Instance.Guid);
            }
            catch (System.Net.Sockets.SocketException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.IO.IOException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.Net.WebException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.Runtime.Remoting.RemotingException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (Exception ex)
            {
                WriteException(ex);
                OnOfflineEventHandler(new OfflineEventArgs(this));
            }

            return false;
        }

        public void ReleaseLock()
        {
            // if (!IRmainteAttribute.ConfirmVersion("ReleaseLock", CLIENT_VERSION))
            //     return;

            if (_ir == null)
                return;
            
            try
            {
                _ir.ReleaseLock(MainForm.Instance.Guid);
            }
            catch (System.Net.Sockets.SocketException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.IO.IOException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.Net.WebException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.Runtime.Remoting.RemotingException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (Exception ex)
            {
                WriteException(ex);
                OnOfflineEventHandler(new OfflineEventArgs(this));
            }
        }

        public void ReplaceDataSet(Database.ChangeReason reason)
        {
            // if (!IRmainteAttribute.ConfirmVersion("ReplaceDataSet", MainForm.CLIENT_VERSION))
            //     return;

            if (_ir == null)
                return;

            if (_ds.HasChanges() == false)
                return;

            try
            {
                // �f�[�^�Z�b�g�ɃN���C�A���g�̎��ʎq���Z�b�g����B
                Database.SetGUID(_ds, MainForm.Instance.Guid);

                // �ύX���R�~�b�g���āA
                DataSet.AcceptChanges();

                // �T�[�o�ɒʒm
                _ir.ReplaceDataSet(DataSet, reason);
            }
            catch (System.Net.Sockets.SocketException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.IO.IOException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.Net.WebException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.Runtime.Remoting.RemotingException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (Exception ex)
            {
                WriteException(ex);
                OnOfflineEventHandler(new OfflineEventArgs(this));
            }
        }

        public void MergeDataSet(Database.ChangeReason reason)
        {
            // if (!IRmainteAttribute.ConfirmVersion("MergeDataSet", MainForm.Instance.CLIENT_VERSION))
            //     return;

            if (_ir == null)
                return;

            if (_ds.HasChanges() == false)
                return;

            try
            {
                // ���������o��
                DataSet diff = DataSet.GetChanges();

                // �����ɃN���C�A���g�̎��ʎq���Z�b�g����B
                Database.SetGUID(diff, MainForm.Instance.Guid);

                // �f�[�^�Z�b�g�ւ̕ύX�͂��̎��_�ŃR�~�b�g�B
                DataSet.AcceptChanges();
                
                // �T�[�o�ɒʒm
                _ir.MergeDataSet(diff, reason);
            }
            catch (System.Net.Sockets.SocketException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.IO.IOException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.Net.WebException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.Runtime.Remoting.RemotingException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (Exception ex)
            {
                WriteException(ex);
                OnOfflineEventHandler(new OfflineEventArgs(this));
            }
        }

        public DataSet GetDataSet()
        {
            if (_ir == null)
                return null;

            _ds = null;

            try
            {
                _ds = _ir.GetDataSet();
            }
            catch (System.Net.Sockets.SocketException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.IO.IOException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.Net.WebException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.Runtime.Remoting.RemotingException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (Exception ex)
            {
                WriteException(ex);
                OnOfflineEventHandler(new OfflineEventArgs(this));
            }

            return _ds;
        }

        public double GetVersion()
        {
            if (_ir == null)
                return 0.0;

            _version = 0.0;

            try
            {
                _version = _ir.GetVersion();
            }
            catch (System.Net.Sockets.SocketException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.IO.IOException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.Net.WebException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.Runtime.Remoting.RemotingException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (Exception ex)
            {
                WriteException(ex);
                OnOfflineEventHandler(new OfflineEventArgs(this));
            }
            return _version;
        }



        // �e�X�g�R�[�h
        public void RegisterJob(int jobId)
        {
            try
            {
                _ir.RegisterJob(jobId);
            }
            catch (System.Net.Sockets.SocketException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.IO.IOException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.Net.WebException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (System.Runtime.Remoting.RemotingException) { OnOfflineEventHandler(new OfflineEventArgs(this)); }
            catch (Exception ex)
            {
                WriteException(ex);
                OnOfflineEventHandler(new OfflineEventArgs(this));
            }
        }

    
    
    
    
    }


    /// <summary>
    /// �I�t���C�������o�����Ƃ��̃C�x���g�̒�`
    /// </summary>
    /// <param name="sender">ServerEnv�I�u�W�F�N�g</param>
    /// <param name="e"></param>
    public delegate void OfflineEvent(object sender, OfflineEventArgs e);

    public class OfflineEventArgs : EventArgs
    {
        public OfflineEventArgs(ServerEnv env)
        {
            _env = env;
        }

        public ServerEnv ServerEnv
        {
            get { return _env; }
            set { _env = value; }
        }
        private ServerEnv _env = null;
    }

    /// <summary>
    /// Notification����M�����Ƃ��̃C�x���g�̒�`�B
    /// </summary>
    /// <param name="sender">ServerEnv�I�u�W�F�N�g</param>
    /// <param name="e"></param>
    public delegate void NotificationEvent(object sender, NotificationEventArgs e);

    /// <summary>
    /// Notification����M�����Ƃ��ɔ�������C�x���g�̈����ł��B
    /// �T�[�o�̃A�h���X�ANotification�̋l�܂������X�g���i�[����Ă��܂��B
    /// </summary>
    public class NotificationEventArgs : EventArgs
    {
        public NotificationEventArgs(ServerEnv env, ArrayList list)
            : base()
        {
            _env = env;
            _notificationList = list;
        }

        public ArrayList NotificationList
        {
            get { return _notificationList; }
            set { _notificationList = value; }
        }
        private ArrayList _notificationList = null;

        public ServerEnv ServerEnv
        {
            get { return _env; }
            set { _env = value; }
        }
        private ServerEnv _env = null;
    }


}
