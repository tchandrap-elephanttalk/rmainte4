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
        // Singletonの作り方はここを参照。
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
        // MarshalByRefObjectのメソッドをオーバーライドする。
        // GCに収集されないように、無限に有効にする。
        // http://msdn.microsoft.com/en-us/magazine/cc300474.aspx
        public override object InitializeLifetimeService()
        {
            return null;
        }
        #endregion

        /// <summary>
        /// サーバのバージョンです。
        /// </summary>
        public const double SERVER_VERSION = MyVersion.CURRENT_VERSION;


        // ロックしているクライアントのGUID
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
                // 古いロックは死んだクライアントと見なして、勝手にロックを解除する
                // RemoveAllを使うと、foreachを回さなくても一発で消せる。
                _lockClientList.RemoveAll(CheckLife);

                // 誰もロックをしていないなら、ロックの取得成功。
                // 誰かにロックを取られていたら、最大1秒間だけ待ってみる。
                // その間に誰かがロックを解放してくれればtrueが帰る。
                if (_lockClientList.Count == 0 || Monitor.Wait(_lockClientList, 1000))
                {
                    // ロック所有者として自分の情報を格納しておく
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

        // 同期処理のためのオブジェクト
        private static readonly object _notificationLock = new object();

        // ログ情報を格納するアレイリスト
        private ArrayList _notificationList = ArrayList.Synchronized(new ArrayList());

        /// <summary>
        /// ログ情報LogInfoオブジェクトを格納したリストを返す。
        /// 引数で指定した時刻よりも新しいログが無ければ、ブロッキングする。
        /// </summary>
        /// <param name="newerThanThisDate">この時刻よりも新しい物だけを取得する</param>
        /// <returns></returns>
        public ArrayList ReceiveNotification(DateTime newerThanThisDate)
        {
            // _syncObjectをロックしてクリティカルセクションに入る
            lock (_notificationLock)
            {
                // 新しい物をリストから検索する
                ArrayList list = SearchNotification(newerThanThisDate);

                if (list.Count == 0)
                {
                    // 空っぽの場合は追加されるまで待機する。
                    // 別のスレッドで追加したら、パルスしてもらう
                    Monitor.Wait(_notificationLock);

                    // 追加されているはずなので、もう一度検索して取得する
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

        // 最大1分だけ保存する
        public static TimeSpan MaxNotificationLife = new TimeSpan(0, 1, 0);

        private void AddNotification(Notification notif)
        {
            lock (_notificationLock)
            {
                // 古いのはここで削除してしまう。
                for (int i = _notificationList.Count - 1; i >= 0; i--)
                {
                    Notification n = (Notification)_notificationList[i];
                    if (DateTime.Now - n.CreatedTime > MaxNotificationLife)
                    {
                        _notificationList.RemoveAt(i);
                    }
                }

                // 追加して、
                _notificationList.Add(notif);

                // 教えてあげる
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

            // 理由に応じて、Notificationを飛ばす。
            Notification notif = new Notification();
            notif.Type = NotificationType.DatabaseReplaced;

            // タグにはデータセットそのものを入れる。
            notif.Tag = ds; 
           
            AddNotification(notif);
        }

        public void MergeDataSet(DataSet ds, Database.ChangeReason reason)
        {
            Database.Instance.Merge(ds);

            // 理由に応じて、Notificationを飛ばす。
            Notification notif = new Notification();
            notif.Type = NotificationType.DatabaseMerged;
            
            // タグにはデータセットそのものを入れる。
            notif.Tag = ds;

            AddNotification(notif);
        }


    }

}
