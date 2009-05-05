using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using System.Data;
using System.Collections;

namespace rmainte4
{
    public sealed class IRmainteImpl : MarshalByRefObject , IRmainte
    {
        // Singleton�̍����͂������Q�ƁB
        // http://msdn.microsoft.com/ja-jp/library/ms998558.aspx

        // Singleton
        private IRmainteImpl()
        {
        }

        public static IRmainteImpl GetInstance()
        {
            return _instance;
        }

        public static IRmainteImpl Instance
        {
            get { return _instance; }
        }
        private static readonly IRmainteImpl _instance = new IRmainteImpl();

        #region public override object InitializeLifetimeService()
        // MarshalByRefObject�̃��\�b�h���I�[�o�[���C�h����B
        // GC�Ɏ��W����Ȃ��悤�ɁA�����ɗL���ɂ���B
        // http://msdn.microsoft.com/en-us/magazine/cc300474.aspx
        public override object InitializeLifetimeService()
        {
            return null;
        }
        #endregion

        /// <summary>
        /// �T�[�o�̃o�[�W�����ł��B
        /// </summary>
        public const double SERVER_VERSION = MyVersion.CURRENT_VERSION;


        // ���b�N���Ă���N���C�A���g��GUID
        private List<LockClient> _lockClientList = new List<LockClient>();

        private class LockClient
        {
            public LockClient(string guid)
            {
                _guid = guid;
                _time = DateTime.Now;
            }

            public DateTime Time
            {
                get { return _time; }
            }
            private DateTime _time;

            public string Guid
            {
                get { return _guid; }
            }
            private string _guid;
        }

        public bool GetLock(string guid)
        {
            lock (_lockClientList)
            {
                // �Â����b�N�͎��񂾃N���C�A���g�ƌ��Ȃ��āA����Ƀ��b�N����������
                // RemoveAll���g���ƁAforeach���񂳂Ȃ��Ă��ꔭ�ŏ�����B
                _lockClientList.RemoveAll(CheckLife);

                // �N�����b�N�����Ă��Ȃ��Ȃ�A���b�N�̎擾�����B
                // �N���Ƀ��b�N������Ă�����A�ő�1�b�Ԃ����҂��Ă݂�B
                // ���̊ԂɒN�������b�N��������Ă�����true���A��B
                if (_lockClientList.Count == 0 || Monitor.Wait(_lockClientList, 1000))
                {
                    // ���b�N���L�҂Ƃ��Ď����̏����i�[���Ă���
                    _lockClientList.Add(new LockClient(guid));
                    return true;
                }

                return false;
            }
        }

        private static TimeSpan MAX_LOCK_LIFETIME = new TimeSpan(0, 1, 0);
        private static bool CheckLife(LockClient client)
        {
            return (DateTime.Now - client.Time > MAX_LOCK_LIFETIME);
        }

        public void ReleaseLock(string guid)
        {
            lock (_lockClientList)
            {
                if (_lockClientList.Count == 0)
                {
                    return;
                }

                for (int i = _lockClientList.Count - 1; i >= 0; i--)
                {
                    LockClient client = _lockClientList[i];
                    if (client.Guid.Equals(guid))
                    {
                        _lockClientList.Remove(client);
                        Monitor.Pulse(_lockClientList);
                    }
                }
            }
        }

        public string Echo(string message)
        {
            return ("Hello! " + message);
        }

        public bool KeepAlive()
        {
            return true;
        }

        public double GetVersion()
        {
            return SERVER_VERSION;
        }

        // ���������̂��߂̃I�u�W�F�N�g
        private static readonly object _notificationLock = new object();

        // ���O�����i�[����A���C���X�g
        private ArrayList _notificationList = ArrayList.Synchronized(new ArrayList());

        /// <summary>
        /// ���O���LogInfo�I�u�W�F�N�g���i�[�������X�g��Ԃ��B
        /// �����Ŏw�肵�����������V�������O��������΁A�u���b�L���O����B
        /// </summary>
        /// <param name="newerThanThisDate">���̎��������V�������������擾����</param>
        /// <returns></returns>
        public ArrayList ReceiveNotification(DateTime newerThanThisDate)
        {
            // _syncObject�����b�N���ăN���e�B�J���Z�N�V�����ɓ���
            lock (_notificationLock)
            {
                // �V�����������X�g���猟������
                ArrayList list = SearchNotification(newerThanThisDate);

                if (list.Count == 0)
                {
                    // ����ۂ̏ꍇ�͒ǉ������܂őҋ@����B
                    // �ʂ̃X���b�h�Œǉ�������A�p���X���Ă��炤
                    Monitor.Wait(_notificationLock);

                    // �ǉ�����Ă���͂��Ȃ̂ŁA������x�������Ď擾����
                    list = SearchNotification(newerThanThisDate);
                }

                return list;
            }
        }

        private ArrayList SearchNotification(DateTime newerThanThisDate)
        {
            ArrayList list = new ArrayList();

            lock (_notificationList)
            {
                foreach (Notification notif in _notificationList)
                {
                    if (notif.CreatedTime > newerThanThisDate)
                    {
                        list.Add(notif);
                    }
                }
            }
            return list;
        }

        // �ő�1�������ۑ�����
        public static TimeSpan MaxNotificationLife = new TimeSpan(0, 1, 0);

        private void AddNotification(Notification notif)
        {
            lock (_notificationLock)
            {
                // �Â��̂͂����ō폜���Ă��܂��B
                for (int i = _notificationList.Count - 1; i >= 0; i--)
                {
                    Notification n = (Notification)_notificationList[i];
                    if (DateTime.Now - n.CreatedTime > MaxNotificationLife)
                    {
                        _notificationList.RemoveAt(i);
                    }
                }

                // �ǉ����āA
                _notificationList.Add(notif);

                // �����Ă�����
                Monitor.PulseAll(_notificationLock);
            }
        }

        public void RegisterJob(int jobId)
        {
            Notification notif = new Notification();
            notif.Type = NotificationType.Log;
            LogInfo info = new LogInfo();
            info.Name = "RegisterJob";
            notif.Tag = info;
            AddNotification(notif);
        }

        public DataSet GetDataSet()
        {
            return Database.Instance.DataSet;
        }

        public void SaveDataSet()
        {
            Database.Instance.WriteXml();
        }

        public void ReplaceDataSet(DataSet ds, Database.ChangeReason reason)
        {
            Database.Instance.Replace(ds);

            // ���R�ɉ����āANotification���΂��B
            Notification notif = new Notification();
            notif.Type = NotificationType.DatabaseReplaced;

            // �^�O�ɂ̓f�[�^�Z�b�g���̂��̂�����B
            notif.Tag = ds; 
           
            AddNotification(notif);
        }

        public void MergeDataSet(DataSet ds, Database.ChangeReason reason)
        {
            Database.Instance.Merge(ds);

            // ���R�ɉ����āANotification���΂��B
            Notification notif = new Notification();
            notif.Type = NotificationType.DatabaseMerged;
            
            // �^�O�ɂ̓f�[�^�Z�b�g���̂��̂�����B
            notif.Tag = ds;

            AddNotification(notif);
        }


    }

}
