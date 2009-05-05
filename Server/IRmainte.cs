using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data;

namespace rmainte4
{
    public interface IRmainte
    {
        [IRmainte(RequiredVersion = MyVersion.INITIAL_VERSION)]
        string Echo(string message);

        [IRmainte(RequiredVersion = MyVersion.INITIAL_VERSION)]
        bool KeepAlive();

        [IRmainte(RequiredVersion = MyVersion.INITIAL_VERSION)]
        double GetVersion();

        // 非常に残念な事に、HTTPチャネルだとGeneric型のListは運べないため、ArrayListで運ぶようにする。
        [IRmainte(RequiredVersion = MyVersion.INITIAL_VERSION)]
        ArrayList ReceiveNotification(DateTime newerThanThisDate);

        [IRmainte(RequiredVersion = MyVersion.INITIAL_VERSION)]
        void RegisterJob(int jobId);

        [IRmainte(RequiredVersion = MyVersion.INITIAL_VERSION)]
        DataSet GetDataSet();

        [IRmainte(RequiredVersion = MyVersion.INITIAL_VERSION)]
        void SaveDataSet();

        [IRmainte(RequiredVersion = MyVersion.INITIAL_VERSION)]
        void ReplaceDataSet(DataSet ds, Database.ChangeReason reason);

        [IRmainte(RequiredVersion = MyVersion.INITIAL_VERSION)]
        void MergeDataSet(DataSet ds, Database.ChangeReason reason);

        [IRmainte(RequiredVersion = MyVersion.INITIAL_VERSION)]
        bool GetLock(string guid);

        [IRmainte(RequiredVersion = MyVersion.INITIAL_VERSION)]
        void ReleaseLock(string guid);

    }

    /// <summary>
    /// 属性を定義します。
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class IRmainteAttribute : Attribute
    {
        /// <summary>
        /// 動作させるのに必要なバージョンです。
        /// </summary>
        public double RequiredVersion
        {
            get { return _requiredVersion; }
            set { _requiredVersion = value; }
        }
        private double _requiredVersion = 1.0;

        /// <summary>
        /// メソッドにひもづけられたバージョンと、クライアント側のバージョンを比較します。
        /// </summary>
        /// <param name="name">実行したいメソッド名</param>
        /// <param name="myVersion">クライアント側のバージョン</param>
        /// <returns>実行できるならtrue</returns>
        public static bool ConfirmVersion(string name, double myVersion)
        {
            MethodInfo info = typeof(IRmainte).GetMethod(name);
            if (info == null)
            {
                return false;
            }

            foreach (Object o in info.GetCustomAttributes(false))
            {
                IRmainteAttribute att = o as IRmainteAttribute;
                if (att != null)
                {
                    return (myVersion >= att.RequiredVersion);
                }
            }
            return (true);
        }
    }

    /// <summary>
    /// ログ情報を格納します。
    /// </summary>
    [Serializable]
    public class LogInfo
    {
        public LogInfo()
        {
        }

        public long Id
        {
            get { return (_id); }
            set { _id = value; }
        }
        private long _id = 0;

        public string Name
        {
            get { return (_name); }
            set { _name = value; }
        }
        private string _name = string.Empty;

        public string Target
        {
            get { return (_target); }
            set { _target = value; }
        }
        private string _target = string.Empty;

        public string Status
        {
            get { return (_status); }
            set { _status = value; }
        }
        private string _status = string.Empty;

        public DateTime StartedTime
        {
            get { return (_startedTime); }
            set { _startedTime = value; }
        }
        private DateTime _startedTime = DateTime.Now;

        public DateTime CompletedTime
        {
            get { return (_completedTime); }
            set { _completedTime = value; }
        }
        private DateTime _completedTime = DateTime.MinValue;

        public DateTime UpdatedTime
        {
            get { return (_updatedTime); }
            set { _updatedTime = value; }
        }
        private DateTime _updatedTime = DateTime.Now;

    }


    public enum NotificationType { KeepAlive = 0, DatabaseReplaced = 1, DatabaseMerged = 2, Log = 3, };

    [Serializable]
    public class Notification
    {
        public Notification()
        {
        }

        public NotificationType Type
        {
            get { return (_type); }
            set { _type = value; }
        }
        private NotificationType _type = NotificationType.KeepAlive;

        public int Code
        {
            get { return (_code); }
            set { _code = value; }
        }
        private int _code = 0;

        public DateTime CreatedTime
        {
            get { return (_createdTime); }
            set { _createdTime = value; }
        }
        private DateTime _createdTime = DateTime.Now;

        public Object Tag
        {
            get { return (_tag); }
            set { _tag = value; }
        }
        private Object _tag = null;
    }




}
