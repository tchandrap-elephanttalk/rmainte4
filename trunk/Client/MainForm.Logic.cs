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
        /// �����[�e�B���O�֌W�����������܂��B
        /// </summary>
        private ServerEnv InitializeRemoting()
        {
            /*
            if (AppSettings.Instance.Server.Equals("localhost"))
            {
                try
                {
                    // IPC�`���l�����N������
                    RemotingServer.Instance.Register(true, false, false);
                }
                catch (RemotingException) { }
            }
            */
            
            // �T�[�o����ۑ����邽�߂̃N���X�I�u�W�F�N�g�𐶐�
            ServerEnv env = new ServerEnv();

            // ��������ServerEnv�I�u�W�F�N�g��List�ɕۑ����Ă����B
            _serverEnvList.Add(env);

            // IP�A�h���X��ڑ����@�ȂǁA�T�[�o�֘A�̃v���p�e�B��ۑ�����B
            env.Address = AppSettings.Instance.Server;
            
            // HTTP�`���l����TCP�`���l�����B
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

            // IRmainte�����������I�u�W�F�N�g���擾����
            env.GetIRmainteImplOrNull();

            if (env.IRmainte == null)
            {
                // �ʐM�G���[�H
                // �T�[�o���N�����Ă��Ȃ��H
                OfflineDetected(env);
            }
            else
            {
                // �܂��T�[�o�̏������ɍs���ĕۑ��B
                env.GetVersion();

                // �f�[�^�x�[�X�̃f�[�^�Z�b�g���擾���ĕۑ��B
                env.GetDataSet();
            }

            return env;
        }

        /// <summary>
        /// GUI���b�N�����p
        /// </summary>
        public readonly object _guiLock = new object();

        /// <summary>
        /// �N���C�A���g�����ʂ���GUID
        /// </summary>
        /// 
        public string Guid
        {
            get { return _guid; }
        }
        private string _guid = System.Guid.NewGuid().ToString();

        #region ServerEnv�֘A

        /// <summary>
        /// ServerEnv�I�u�W�F�N�g��ۑ����Ă������߂̃��X�g
        /// </summary>
        private List<ServerEnv> _serverEnvList = new List<ServerEnv>();

        /// <summary>
        /// ���X�g����w�肵���A�h���X�̕���Ԃ��܂��B
        /// TreeNode��ListViewItem��NodeData�I�u�W�F�N�g���^�O�Ɏ��̂ŁA�T���K�v�͂Ȃ��B
        /// </summary>
        /// <param name="serverAddress">�T�[�o�̃A�h���X</param>
        /// <returns>�����A�h���X�̂��̂��������ServerEnv��Ԃ��܂��B</returns>
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


        #region �T�[�o�Ƃ̐ڑ����؂ꂽ�Ƃ��̏���
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

            this.Text = "�I�t���C�����";

            // TODO:
            // �c���[�r���[������Ƃ��������B
        }
        #endregion


        /// <summary>
        /// Notification����M�����Ƃ��̏����B
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

            // �ύX�����̂������Ȃ���Ȃ�
            if (_guid.Equals(Database.GetGUID(ds)))
            {
                return;
            }

            // ���̐l���u���������̂Ȃ�A�����̃f�[�^�Z�b�g���u��������B
            env.DataSet = ds;

            // �f�[�^�Z�b�g���ς���Ă��܂����̂ŁA�c���[�r���[���\�z�������B
            LoadTree(_treeView_Node, env);
        }

        private void OnDatabaseMerged(ServerEnv env, DataSet diff)
        {
            if (diff == null)
            {
                return;
            }

            // �������ύX�����̂Ȃ�A�������Ȃ��Ă����B
            if (_guid.Equals(Database.GetGUID(diff)))
            {
                return;
            }

            // �����������Ă���f�[�^�Z�b�g�Ƀ}�[�W����B
            env.DataSet.Merge(diff);
            env.DataSet.AcceptChanges();

            // TODO:
            // ������悤�ɁA�\������

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
                    item.Text = IsJapanese ? "�f�[�^�x�[�X���ύX����܂���" : "Database changed"; // 0
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