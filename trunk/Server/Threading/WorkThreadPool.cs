/* *-*-mode:java; -*- */

// Copyright Takamitsu IIDA <gsi@nifty.com>
// CCIE4288
// 2005.01.02
// Last modified at 2006.07.07 for rmainte3
// Last modified at 2009.01.05 for rmainte4

using System;
using System.Collections.Generic;
using System.Threading;

namespace rmainte4.Threading
{
    /// <summary>
    /// WorkThreadPool の概要の説明です。
    /// </summary>
    public class WorkThreadPool : IDisposable
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="maxThreads">同時実行するスレッド数。1以上の整数。</param>
        public WorkThreadPool(int maxThreads)
        {
            if (0 >= maxThreads)
            {
                throw new ArgumentOutOfRangeException("MaxThreads", maxThreads, "正の値でなければなりません");
            }

            _maxThreads = maxThreads;
        }

        public void Dispose()
        {
            Shutdown();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 緊急停止するときはこれ。
        /// </summary>
        public void Shutdown()
        {
            // 実行中のワークアイテムを全てシャットダウン
            lock (this)
            {
                // 実行待ちのワークキューを空っぽにする
                while (_workQueue.Count != 0)
                {
                    _workQueue.Dequeue();
                }

                // 実行中のワークアイテムを止める
                foreach (WorkThread worker in _workersList)
                {
                    worker.Stop();
                }
                Monitor.PulseAll(this);

                // ワーカースレッドの停止を待つ
                while (_workersList.Count > 0)
                {
                    Thread thread = null;

                    thread = DeleteThread();
                    Monitor.PulseAll(this);

                    if (thread != null)
                    {
                        // 自然に終わるのを待つ？
                        // thread.Join();

                        // それとも強制的に終了する？
                        thread.Abort();
                    }
                }
            }

            lock (_completed)
            {
                Monitor.PulseAll(_completed);
            }
        }

        // キュー
        private Queue<IWorkItem> _queue = new Queue<IWorkItem>();

        // スケジュール化されたワークアイテムのキュー
        private Queue<IWorkItem> _workQueue = new Queue<IWorkItem>();


        //        running数がMaxThreadでなければ
        //        queueから取りだす。
        //                      (Pausing)
        //┌───┐    ┌───┐　  ┌─────┐
        //│Add() ├──┤queue ├──┤workQueue ├─→実行
        //└───┘    └───┘　  └─────┘

        // 内部エラー
        private volatile Exception _internalException;

        // 実行中のワークアイテムの数
        private int _numOfRunning;

        // 実際に動作しているワークアイテムを格納するリスト
        private List<WorkThread> _workersList = new List<WorkThread>();

        // 全てのワークアイテムが完了するまで保持するロックオブジェクト
        private object _completed = new object();

        // イベントを発動する際のロックオブジェクト
        private readonly object _eventLock = new object();

        /// <summary>
        /// 実行中のワークアイテムとキューに貯められたワークアイテムの合計値
        /// </summary>
        public int Count
        {
            get { return (_numOfRunning + _queue.Count); }
        }

        /// <summary>
        /// 統計情報。
        /// Stats[Created = 0,Scheduled,Queued,Running,Failed,Completed]
        /// </summary>
        public long[] Stats
        {
            get { return _stats; }
        }
        private long[] _stats = new long[(int)WorkItemState.Completed + 1];

        public void ClearStats()
        {
            lock (this)
            {
                Array.Clear(_stats, 0, _stats.Length);
            }
        }

        // 実行中のワークアイテムの名前とか取れるようにしたいなぁ。






        /// <summary>
        /// 利用するときは、WorkThreadPoolを作って、このメソッドでワークアイテム(IWorkItem)を追加するだけでよい。
        /// 後は自動で走り出す。
        /// </summary>
        /// <param name="workItem">IWorkItemを実装したオブジェクト</param>
        public void Add(IWorkItem workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException("NULLオブジェクトは追加できません");
            }

            if (_internalException != null)
            {
                throw new NotSupportedException("内部エラー", _internalException);
            }

            // このワークアイテムを管理するスレッドプールは自分。
            workItem.WTPool = this;

            // まずはキューに入れる。
            lock (this)
            {
                // キューに入れる
                _queue.Enqueue(workItem);

                // 状態をセット
                workItem.State = WorkItemState.Queued;

                // 少なくとも一つはキューに入っているので、
                // スケジュール化を試みる
                DoNextWorkItem();
            }
        }

        // queueに待機しているワークアイテムを一つ取り出してSchedule状態にする
        // 条件が合えばイベントハンドラ経由でキューから取り出され、実行される
        private bool DoNextWorkItem()
        {
            lock (this)
            {
                if (!_pausing && _numOfRunning < _maxThreads && _queue.Count != 0)
                {
                    IWorkItem item = (IWorkItem)_queue.Dequeue();
                    item.State = WorkItemState.Scheduled;
                    return (true);
                }
            }
            return (false);
        }

        /// <summary>
        /// 一時停止。新規の実行ができなくなる。
        /// </summary>
        public void Pause()
        {
            Pausing = true;
        }

        /// <summary>
        /// 再開。
        /// </summary>
        public void Resume()
        {
            Pausing = false;
        }

        /// <summary>
        /// 一時停止しているかどうか。初期値はfalse。
        /// </summary>
        public bool Pausing
        {
            get { return (_pausing); }
            set
            {
                if (_pausing != value)
                {
                    _pausing = value;

                    // ポーズが解除されたなら、キューのワークアイテムを実行する
                    while (!_pausing && DoNextWorkItem())
                    {
                        // 2006.10.10 SocketFactoryを導入したので、Sleep()はコメントアウト。
                        // WinSockに負担をかけないように、実行前にちょっとだけ待ちます。
                        // Thread.Sleep(20);
                    }
                }
            }
        }
        private bool _pausing = false;

        /// <summary>
        /// キューで待機しているワークアイテムを削除する。
        /// 実行中のワークアイテムには影響しない。
        /// </summary>
        public void ClearQueue()
        {
            lock (this)
            {
                _queue.Clear();
            }
        }

        /// <summary>
        /// Add()した全てのワークアイテムが終わるまで待つ。
        /// </summary>
        public void WaitAll()
        {
            lock (this)
            {
                if (_internalException != null)
                {
                    throw _internalException;
                }

                if (_pausing)
                {
                    throw new InvalidOperationException("一時停止中です。");
                }

                if (_numOfRunning == 0 && _queue.Count == 0)
                {
                    return;
                }
            }

            lock (_completed)
            {
                if (_internalException != null)
                {
                    throw _internalException;
                }

                if (_numOfRunning == 0 && _queue.Count == 0)
                {
                    return;
                }

                Monitor.Wait(_completed);

                if (_internalException != null)
                {
                    throw _internalException;
                }
            }
        }

        /// <summary>
        /// Add()した全てのワークアイテムが終わるまで待つ。
        /// タイムアウト付き。
        /// </summary>
        /// <param name="timeout">最大待機時間をTimeSpanで指定</param>
        /// <returns></returns>
        public bool WaitAll(TimeSpan timeout)
        {
            lock (this)
            {
                if (_internalException != null)
                    throw _internalException;
            }

            lock (_completed)
            {
                if (!Monitor.Wait(_completed, timeout))
                {
                    return false;
                }

                if (_internalException != null)
                {
                    throw _internalException;
                }
            }

            return (true);
        }

        // スレッド管理をしているときに例外が発生したときの処理
        protected virtual void HandleThreadPoolException(WorkThreadPoolEventArgs e)
        {
            lock (_completed)
            {
                Pause();
                _internalException = e.Exception;

                // この先でアプリケーションを終了して欲しい。
                OnWorkThreadPoolEventHandler(e);

                Monitor.PulseAll(_completed);
            }
        }

        /// <summary>
        /// スレッド管理をしているときに例外が発生した場合のイベント
        /// </summary>
        public event WorkThreadPoolEvent WorkThreadPoolEventHandler
        {
            add
            {
                lock (_eventLock)
                {
                    _workThreadPoolEventHandler += value;
                }
            }
            remove
            {
                lock (_eventLock)
                {
                    _workThreadPoolEventHandler -= value;
                }
            }
        }
        private event WorkThreadPoolEvent _workThreadPoolEventHandler;

        protected virtual void OnWorkThreadPoolEventHandler(WorkThreadPoolEventArgs e)
        {
            WorkThreadPoolEvent handler = null;

            lock (_eventLock)
            {
                handler = _workThreadPoolEventHandler;
            }

            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// ワークアイテムの状態が変化したときのイベント
        /// </summary>
        public event WorkItemStateChangedEvent WorkItemStateChangedEventHandler
        {
            add
            {
                lock (_eventLock)
                {
                    _workItemStateChangedEventHandler += value;
                }
            }
            remove
            {
                lock (_eventLock)
                {
                    _workItemStateChangedEventHandler -= value;
                }
            }
        }
        private WorkItemStateChangedEvent _workItemStateChangedEventHandler;

        // イベント発動
        protected virtual void OnWorkItemStateChangedEventHandler(IWorkItem workItem, WorkItemState previousState)
        {
            WorkItemStateChangedEvent handler = null;

            lock (_eventLock)
            {
                handler = _workItemStateChangedEventHandler;
            }

            if (handler != null)
            {
                handler(this, new WorkItemStateChangedEventArgs(workItem, previousState));
            }
        }

        /// <summary>
        /// Add()した全てのワークアイテムが完了したときのイベント
        /// </summary>
        public event EventHandler AllWorkItemCompletedEventHandler
        {
            add
            {
                lock (_eventLock)
                {
                    _allWorkItemCompletedEventHandler += value;
                }
            }
            remove
            {
                lock (_eventLock)
                {
                    _allWorkItemCompletedEventHandler -= value;
                }
            }
        }
        private EventHandler _allWorkItemCompletedEventHandler;

        // イベント発動
        protected virtual void OnAllWorkItemCompletedEventHandler(EventArgs e)
        {
            EventHandler handler = null;

            lock (_eventLock)
            {
                handler = _allWorkItemCompletedEventHandler;
            }

            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// ワークアイテムが実行されたときのイベント
        /// </summary>
        public event WorkItemEvent WorkItemRunningEventHandler
        {
            add
            {
                lock (_eventLock)
                {
                    _workItemRunningEventHandler += value;
                }
            }
            remove
            {
                lock (_eventLock)
                {
                    _workItemRunningEventHandler -= value;
                }
            }
        }
        private event WorkItemEvent _workItemRunningEventHandler;

        // イベント発動
        protected virtual void OnWorkItemRunningEventHandler(IWorkItem workItem)
        {
            WorkItemEvent handler = null;

            lock (_eventLock)
            {
                handler = _workItemRunningEventHandler;
            }

            if (handler != null)
            {
                handler(this, new WorkItemEventArgs(workItem));
            }
        }

        /// <summary>
        /// ワークアイテムが完了したときのイベント
        /// </summary>
        public event WorkItemEvent WorkItemCompletedEventHandler
        {
            add
            {
                lock (_eventLock)
                {
                    _workItemCompletedEventHandler += value;
                }
            }
            remove
            {
                lock (_eventLock)
                {
                    _workItemCompletedEventHandler -= value;
                }
            }
        }
        private event WorkItemEvent _workItemCompletedEventHandler;

        // イベント発動
        protected virtual void OnWorkItemCompletedEventHandler(IWorkItem workItem)
        {
            WorkItemEvent handler = null;

            lock (_eventLock)
            {
                handler = _workItemCompletedEventHandler;
            }

            if (handler != null)
            {
                handler(this, new WorkItemEventArgs(workItem));
            }
        }

        /// <summary>
        /// ワークアイテムが失敗したときのイベント
        /// </summary>
        public event WorkItemEvent WorkItemFailedEventHandler
        {
            add
            {
                lock (_eventLock)
                {
                    _workItemFailedEventHandler += value;
                }
            }
            remove
            {
                lock (_eventLock)
                {
                    _workItemFailedEventHandler -= value;
                }
            }
        }
        private event WorkItemEvent _workItemFailedEventHandler;

        // イベント発動
        protected virtual void OnWorkItemFailedEventHandler(IWorkItem workItem)
        {
            WorkItemEvent handler = null;

            lock (_eventLock)
            {
                handler = _workItemFailedEventHandler;
            }

            if (handler != null)
            {
                handler(this, new WorkItemEventArgs(workItem));
            }
        }

        /// <summary>
        /// ワークアイテムの状態は実行状況により勝手に変化するため、
        /// IWorkItem側からこのメソッドを呼ぶようにしている。
        /// 通常は使わなくてよい。
        /// </summary>
        /// <param name="workItem"></param>
        /// <param name="previousState"></param>
        public void WorkItemStateChanged(IWorkItem workItem, WorkItemState previousState)
        {
            // 統計値を変える
            lock (this)
            {
                _stats[(int)previousState] -= 1;
                _stats[(int)workItem.State] += 1;
            }

            // イベントを発動
            OnWorkItemStateChangedEventHandler(workItem, previousState);

            // ワークアイテムの現在の状態に応じて、
            switch (workItem.State)
            {
                case WorkItemState.Scheduled:
                    // スケジュール化された場合
                    lock (this)
                    {
                        // 実行する
                        ++_numOfRunning;
                        BeginWork(workItem);
                    }
                    break;

                case WorkItemState.Running:
                    // ワークアイテムが開始された場合
                    // イベントを発動
                    OnWorkItemRunningEventHandler(workItem);
                    break;

                case WorkItemState.Failed:
                    // ワークアイテムが失敗した場合
                    // イベントを発動
                    OnWorkItemFailedEventHandler(workItem);
                    break;

                case WorkItemState.Completed:
                    // ワークアイテムが完了した場合
                    bool allDone = false;
                    lock (this)
                    {
                        --_numOfRunning;
                        allDone = _queue.Count == 0 && _numOfRunning == 0;
                    }

                    // イベントを発動してこのワークアイテムが完了したことを通知
                    OnWorkItemCompletedEventHandler(workItem);

                    if (allDone)
                    {
                        // イベントを発動して全部終わったことを通知
                        OnAllWorkItemCompletedEventHandler(EventArgs.Empty);
                        lock (_completed)
                        {
                            Monitor.PulseAll(_completed);
                        }
                    }
                    else
                    {
                        DoNextWorkItem();
                    }
                    break;
            }
        }

        /// <summary>
        /// 同時実行スレッドの数。コンストラクタで指定する。
        /// </summary>
        public int MaxThreads
        {
            get { return (_maxThreads); }
            set
            {
                if (0 >= value)
                {
                    throw new ArgumentOutOfRangeException("MaxThreads", value, "正の値でなければなりません");
                }

                _maxThreads = value;
                lock (this)
                {
                    while (_workersList.Count > _maxThreads)
                    {
                        DeleteThread();
                    }
                }
            }
        }
        private int _maxThreads = 1;

        private void CreateThread()
        {
            WorkThread worker = new WorkThread(this);
            _workersList.Add(worker);

            Thread thread = new Thread(new ThreadStart(worker.Start));
            
            // 親スレッドが停止したら、子スレッドも停止する。
            thread.IsBackground = true;

            // STAにセットしないと、FormアプリケーションのSaveFileDialog等の各種ダイアログが起動しない。           
            thread.SetApartmentState(ApartmentState.STA);

            worker.Thread = thread;
            thread.Start();
        }

        private Thread DeleteThread()
        {
            int i = _workersList.Count - 1;
            WorkThread worker = (WorkThread)_workersList[i];

            worker.Stop();
            _workersList.RemoveAt(i);

            return (worker.Thread);
        }

        private void BeginWork(IWorkItem workItem)
        {
            if (workItem == null)
                throw new ArgumentNullException();

            lock (this)
            {
                // キューに入れる
                _workQueue.Enqueue(workItem);

                // 最大スレッド数に達してなければスレッドを作成する
                if (_workersList.Count < _maxThreads)
                {
                    CreateThread();
                }

                // 待機しているWorkItemを一つ起動
                // GetIWorkItemOrNull()がMonitor.Wait()してるので、通知してあげる
                Monitor.Pulse(this);
            }
        }

        // ワークアイテムを実行するためのプライベートクラス
        private class WorkThread
        {
            // 管理元のスレッドプール
            private WorkThreadPool _wtPool;

            // 停止するときはこれをtrueにする
            // 最適化で削除されないようにvolatile指定。
            private volatile bool _stopping = false;

            // コンストラクタ
            public WorkThread(WorkThreadPool threadPool)
            {
                _wtPool = threadPool;
            }

            // 実際に実行するスレッド
            public Thread Thread
            {
                get { return (_thread); }
                set { _thread = value; }
            }
            private Thread _thread = null;

            public void Start()
            {
                // stoppingが変更されてないか、毎回確実に見るためにvolatile変数にしている
                while (!_stopping)
                {
                    try
                    {
                        IWorkItem workItem = null;

                        // ワークアイテムを取得
                        // BeginWork()がMonitor.Pulse()するまでブロッキング
                        workItem = GetIWorkItemOrNull();

                        // ワークアイテムを実行
                        if (workItem != null)
                        {
                            DoWork(workItem);
                        }

                        // 順番的には一番最後に実行したいので
                        // 他のスレッドにCPU処理を譲る
                        Thread.Sleep(0);
                    }
                    catch (ThreadAbortException)
                    {
                        Thread.ResetAbort();
                        _stopping = true;
                    }
                }
            }

            // ワークキューが空っぽの場合は、ワークアイテムが格納されるまでブロッキング
            public IWorkItem GetIWorkItemOrNull()
            {
                IWorkItem workItem = null;

                lock (_wtPool)
                {
                    if (_stopping)
                    {
                        return (null);
                    }

                    if (_wtPool._workQueue.Count == 0)
                    {
                        Monitor.Wait(_wtPool);
                    }

                    // 停止が指示されてたらnullを返す
                    if (_stopping)
                    {
                        return (null);
                    }

                    // ワークキューにエントリがあればそれを取り出す
                    if (_wtPool._workQueue.Count > 0)
                    {
                        workItem = (IWorkItem)_wtPool._workQueue.Dequeue();
                    }
                }

                return (workItem);
            }

            public void DoWork(IWorkItem workItem)
            {
                ThreadPriority originalPriority = Thread.CurrentThread.Priority;
                try
                {
                    if (workItem.Priority != originalPriority)
                    {
                        Thread.CurrentThread.Priority = workItem.Priority;
                    }

                    workItem.State = WorkItemState.Running;
                    workItem.Work();
                    workItem.State = WorkItemState.Completed;
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                    workItem.Terminate();
                    return;
                }
                catch (Exception e)
                {
                    // 実行中のワークアイテムの例外はここで捕捉する。
                    // ここで例外が発生することは大した問題ではない。
                    // 後から参照できるようにワークアイテムの中に保存しておく。
                    workItem.FailedException = e;
                    workItem.State = WorkItemState.Failed;
                    workItem.State = WorkItemState.Completed;
                }
                finally
                {
                    if (Thread.CurrentThread.Priority != originalPriority)
                    {
                        Thread.CurrentThread.Priority = originalPriority;
                    }
                }
            }

            public void Stop()
            {
                _stopping = true;
            }
        }
    }
}
