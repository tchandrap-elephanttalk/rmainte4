/* *-*-mode:java; -*- */

// Copyright Takamitsu IIDA <gsi@nifty.com>
// CCIE4288
// 2004.12.27
// Last modified at 2006.07.07
// Last modified at 2009.01.07 for rmainte4

using System;
using System.Threading;

namespace rmainte4.Threading
{
    /// <summary>
    /// IWorkItem の概要の説明です。
    /// </summary>
    public interface IWorkItem
    {
        /// <summary>
        /// 実行するメソッド
        /// </summary>
        void Work();

        /// <summary>
        /// スレッドを強制停止したときの処理
        /// </summary>
        void Terminate();

        /// <summary>
        /// ワークアイテムを管理するスレッドプール
        /// </summary>
        WorkThreadPool WTPool { get; set; }

        /// <summary>
        /// ワークアイテムの状態
        /// </summary>
        WorkItemState State { get; set; }

        /// <summary>
        /// 例外
        /// </summary>
        Exception FailedException { get; set; }

        /// <summary>
        /// スレッドプライオリティ
        /// </summary>
        ThreadPriority Priority { get; set; }

        /// <summary>
        /// 開始時刻
        /// </summary>
        DateTime StartedTime { get; set; }

        /// <summary>
        /// 終了時刻
        /// </summary>
        DateTime CompletedTime { get; set; }

        /// <summary>
        /// ワークID
        /// </summary>
        int WorkId { get; set; }
    }

    /// <summary>
    /// WorkItemのサンプル実装。
    /// IWorkItemImplを継承するのが難しい場面では、この中身をコピーしてIWorkItem, ICompararableを実装すればよい
    /// </summary>
    public abstract class WorkItemImpl : IWorkItem, IComparable
    {
        // 抽象クラスなので、継承して使う必要がある。
        protected WorkItemImpl()
        {
        }

        // ワークアイテムを実行する抽象メソッド
        // これをオーバーライドして使用
        public abstract void Work();

        // スレッドを強制終了したときに実行するメソッド
        public abstract void Terminate();

        // このワークアイテムが格納されているキュー
        // WorkPoolにAdd()されたときにセットされる
        public WorkThreadPool WTPool
        {
            get { return (_workPool); }
            set { _workPool = value; }
        }
        private WorkThreadPool _workPool;

        // このワークアイテムの状態
        public WorkItemState State
        {
            get { return (_state); }
            set
            {
                WorkItemState prev = _state;
                _state = value;

                switch (value)
                {
                    case WorkItemState.Running:
                        StartedTime = DateTime.Now;
                        break;

                    case WorkItemState.Completed:
                        CompletedTime = DateTime.Now;
                        break;
                }

                // これを管理しているキューに状態の変化を通知
                if (WTPool != null)
                {
                    WTPool.WorkItemStateChanged(this, prev);
                }
            }
        }
        private WorkItemState _state = WorkItemState.Created;

        // 実行中に発生した例外を保存
        public Exception FailedException
        {
            get { return (_failedException); }
            set { _failedException = value; }
        }
        private Exception _failedException = null;

        // プライオリティ。
        // この値でソートする。
        public ThreadPriority Priority
        {
            get { return (_priority); }
            set { _priority = value; }
        }
        private ThreadPriority _priority = ThreadPriority.Normal;

        // 開始時刻
        public DateTime StartedTime
        {
            get { return (_startedTime); }
            set { _startedTime = value; }
        }
        private DateTime _startedTime = DateTime.MaxValue;

        // 実行完了時刻
        public DateTime CompletedTime
        {
            get { return (_completedTime); }
            set { _completedTime = value; }
        }
        private DateTime _completedTime = DateTime.MaxValue;

        // 作成時刻
        public DateTime CreatedTime
        {
            get { return (_createdTime); }
            set { _createdTime = value; }
        }
        private DateTime _createdTime = DateTime.Now;

        public int WorkId
        {
            get { return (_workId); }
            set { _workId = value; }
        }
        private int _workId = 0;

        // ---------------

        // IComparableの実装
        public int CompareTo(object obj)
        {
            IWorkItem wi = (IWorkItem)obj;
            if (wi == null)
            {
                throw new ArgumentException("WorkItemオブジェクトではありません");
            }

            return (Priority - wi.Priority);
        }

    }
}
