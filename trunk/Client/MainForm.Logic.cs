using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Remoting;

namespace rmainte4
{
    public partial class MainForm
    {
        /// <summary>
        /// リモーティング関係を初期化します。
        /// </summary>
        private ServerEnv InitializeRemoting()
        {
            /*
            if (AppSettings.Instance.Server.Equals("localhost"))
            {
                try
                {
                    // IPCチャネルを起動する
                    RemotingServer.Instance.Register(true, false, false);
                }
                catch (RemotingException) { }
            }
            */
            
            // サーバ情報を保存するためのクラスオブジェクトを生成
            ServerEnv env = new ServerEnv();

            // 生成したServerEnvオブジェクトをListに保存しておく。
            _serverEnvList.Add(env);

            // IPアドレスや接続方法など、サーバ関連のプロパティを保存する。
            env.Address = AppSettings.Instance.Server;
            
            // HTTPチャネルかTCPチャネルか。
            if (AppSettings.Instance.ConnectType.ToUpper().Equals("HTTP"))
            {
                env.ConnectType = ServerEnv.ServerConnectType.HTTP;
                env.HttpPort = AppSettings.Instance.HttpPort;
            }
            else if (AppSettings.Instance.ConnectType.ToUpper().Equals("TCP"))
            {
                env.ConnectType = ServerEnv.ServerConnectType.TCP;
                env.TcpPort = AppSettings.Instance.TcpPort;
            }

            // URI
            env.ObjectUri = AppSettings.Instance.ObjectUri;

            // IRmainteを実装したオブジェクトを取得する
            env.GetIRmainteImplOrNull();

            if (env.IRmainte == null)
            {
                // 通信エラー？
                // サーバが起動していない？
                OfflineDetected(env);
            }
            else
            {
                // まずサーバの情報を取りに行って保存。
                env.GetVersion();

                // データベースのデータセットを取得して保存。
                env.GetDataSet();
            }

            return env;
        }

        /// <summary>
        /// GUIロック処理用
        /// </summary>
        public readonly object _guiLock = new object();

        /// <summary>
        /// クライアントを識別するGUID
        /// </summary>
        /// 
        public string Guid
        {
            get { return _guid; }
        }
        private string _guid = System.Guid.NewGuid().ToString();

        #region ServerEnv関連

        /// <summary>
        /// ServerEnvオブジェクトを保存しておくためのリスト
        /// </summary>
        private List<ServerEnv> _serverEnvList = new List<ServerEnv>();

        /// <summary>
        /// リストから指定したアドレスの物を返します。
        /// TreeNodeやListViewItemはNodeDataオブジェクトをタグに持つので、探す必要はない。
        /// </summary>
        /// <param name="serverAddress">サーバのアドレス</param>
        /// <returns>同じアドレスのものが見つかればServerEnvを返します。</returns>
        private ServerEnv GetServerEnv(string serverAddress)
        {
            foreach (ServerEnv env in _serverEnvList)
            {
                if (env.Address.Equals(serverAddress))
                {
                    return env;
                }
            }
            return null;
        }
        #endregion


        #region サーバとの接続が切れたときの処理
        private void OnOffline(object sender, OfflineEventArgs e)
        {
            if (e == null || e.ServerEnv == null)
            {
                return;
            }

            OfflineDetected(e.ServerEnv);
        }

        private delegate void OfflineDetectedDelegate(ServerEnv env);
        private void OfflineDetected(ServerEnv env)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new OfflineDetectedDelegate(OfflineDetected), new object[] { env });
                return;
            }

            env.IRmainte = null;

            this.Text = "オフライン状態";

            // TODO:
            // ツリービュー上も何とかしたい。
        }
        #endregion


        /// <summary>
        /// Notificationを受信したときの処理。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReceiveNotification(object sender, NotificationEventArgs e)
        {
            if (e == null || e.ServerEnv == null || e.NotificationList == null)
            {
                return;
            }

            ServerEnv env = e.ServerEnv;
            ArrayList list = e.NotificationList;

            foreach (Notification notif in list)
            {
                switch (notif.Type)
                {
                    case NotificationType.DatabaseReplaced:
                        DataSet ds_full = (DataSet)notif.Tag;
                        OnDatabaseReplaced(env, ds_full);
                        break;
                    case NotificationType.DatabaseMerged:
                        DataSet ds_diff = (DataSet)notif.Tag;
                        OnDatabaseMerged(env, ds_diff);
                        break;
                    case NotificationType.Log:
                        LogInfo info = (LogInfo)notif.Tag;
                        break;
                    default:
                        break;
                }

                RefreshLog(GetListViewItemFromNotification(notif));
            }
        }

        private void OnDatabaseReplaced(ServerEnv env, DataSet ds)
        {
            if (ds == null)
            {
                return;
            }

            // 変更したのが自分なら問題ない
            if (_guid.Equals(Database.GetGUID(ds)))
            {
                return;
            }

            // 他の人が置き換えたのなら、自分のデータセットも置き換える。
            env.DataSet = ds;

            // データセットが変わってしまったので、ツリービューを構築し直す。
            LoadTree(_treeView_Node, env);
        }

        private void OnDatabaseMerged(ServerEnv env, DataSet diff)
        {
            if (diff == null)
            {
                return;
            }

            // 自分が変更したのなら、何もしなくていい。
            if (_guid.Equals(Database.GetGUID(diff)))
            {
                return;
            }

            // 自分が持っているデータセットにマージする。
            env.DataSet.Merge(diff);
            env.DataSet.AcceptChanges();

            // TODO:
            // 分かるように、表示する

        }

        private ListViewItem GetListViewItemFromNotification(Notification notif)
        {
            ListViewItem item = new ListViewItem();

            switch (notif.Type)
            {
                case NotificationType.Log:
                    LogInfo log = (LogInfo)notif.Tag;
                    item.Text = log.Name;                                    // 0
                    item.SubItems.Add(log.Target);                           // 1
                    item.SubItems.Add(log.Status);                           // 2
                    item.SubItems.Add(log.UpdatedTime.ToString());           // 3
                    item.SubItems.Add(log.StartedTime.ToString());           // 4
                    if (log.CompletedTime.Equals(DateTime.MinValue))
                    {
                        item.SubItems.Add("-");                              // 5
                    }
                    else
                    {
                        item.SubItems.Add(log.CompletedTime.ToString());     // 5
                    }
                    item.Tag = log.Id;

                    break;

                case NotificationType.DatabaseReplaced:
                case NotificationType.DatabaseMerged:
                    item.Text = IsJapanese ? "データベースが変更されました" : "Database changed"; // 0
                    item.SubItems.Add("-"); // 1
                    item.SubItems.Add("-"); // 2
                    item.SubItems.Add("-"); // 3
                    item.SubItems.Add("-"); // 4
                    item.SubItems.Add("-"); // 5
                    break;

                default:
                    break;
            }

            return item;
        }




    
    }
}