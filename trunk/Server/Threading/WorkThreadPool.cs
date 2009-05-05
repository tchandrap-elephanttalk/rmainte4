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
    /// WorkThreadPool �̊T�v�̐����ł��B
    /// </summary>
    public class WorkThreadPool : IDisposable
    {
        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="maxThreads">�������s����X���b�h���B1�ȏ�̐����B</param>
        public WorkThreadPool(int maxThreads)
        {
            if (0 >= maxThreads)
            {
                throw new ArgumentOutOfRangeException("MaxThreads", maxThreads, "���̒l�łȂ���΂Ȃ�܂���");
            }

            _maxThreads = maxThreads;
        }

        public void Dispose()
        {
            Shutdown();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// �ً}��~����Ƃ��͂���B
        /// </summary>
        public void Shutdown()
        {
            // ���s���̃��[�N�A�C�e����S�ăV���b�g�_�E��
            lock (this)
            {
                // ���s�҂��̃��[�N�L���[������ۂɂ���
                while (_workQueue.Count != 0)
                {
                    _workQueue.Dequeue();
                }

                // ���s���̃��[�N�A�C�e�����~�߂�
                foreach (WorkThread worker in _workersList)
                {
                    worker.Stop();
                }
                Monitor.PulseAll(this);

                // ���[�J�[�X���b�h�̒�~��҂�
                while (_workersList.Count > 0)
                {
                    Thread thread = null;

                    thread = DeleteThread();
                    Monitor.PulseAll(this);

                    if (thread != null)
                    {
                        // ���R�ɏI���̂�҂H
                        // thread.Join();

                        // ����Ƃ������I�ɏI������H
                        thread.Abort();
                    }
                }
            }

            lock (_completed)
            {
                Monitor.PulseAll(_completed);
            }
        }

        // �L���[
        private Queue<IWorkItem> _queue = new Queue<IWorkItem>();

        // �X�P�W���[�������ꂽ���[�N�A�C�e���̃L���[
        private Queue<IWorkItem> _workQueue = new Queue<IWorkItem>();


        //        running����MaxThread�łȂ����
        //        queue�����肾���B
        //                      (Pausing)
        //����������    �����������@  ��������������
        //��Add() ��������queue ��������workQueue ���������s
        //����������    �����������@  ��������������

        // �����G���[
        private volatile Exception _internalException;

        // ���s���̃��[�N�A�C�e���̐�
        private int _numOfRunning;

        // ���ۂɓ��삵�Ă��郏�[�N�A�C�e�����i�[���郊�X�g
        private List<WorkThread> _workersList = new List<WorkThread>();

        // �S�Ẵ��[�N�A�C�e������������܂ŕێ����郍�b�N�I�u�W�F�N�g
        private object _completed = new object();

        // �C�x���g�𔭓�����ۂ̃��b�N�I�u�W�F�N�g
        private readonly object _eventLock = new object();

        /// <summary>
        /// ���s���̃��[�N�A�C�e���ƃL���[�ɒ��߂�ꂽ���[�N�A�C�e���̍��v�l
        /// </summary>
        public int Count
        {
            get { return (_numOfRunning + _queue.Count); }
        }

        /// <summary>
        /// ���v���B
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

        // ���s���̃��[�N�A�C�e���̖��O�Ƃ�����悤�ɂ������Ȃ��B






        /// <summary>
        /// ���p����Ƃ��́AWorkThreadPool������āA���̃��\�b�h�Ń��[�N�A�C�e��(IWorkItem)��ǉ����邾���ł悢�B
        /// ��͎����ő���o���B
        /// </summary>
        /// <param name="workItem">IWorkItem�����������I�u�W�F�N�g</param>
        public void Add(IWorkItem workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException("NULL�I�u�W�F�N�g�͒ǉ��ł��܂���");
            }

            if (_internalException != null)
            {
                throw new NotSupportedException("�����G���[", _internalException);
            }

            // ���̃��[�N�A�C�e�����Ǘ�����X���b�h�v�[���͎����B
            workItem.WTPool = this;

            // �܂��̓L���[�ɓ����B
            lock (this)
            {
                // �L���[�ɓ����
                _queue.Enqueue(workItem);

                // ��Ԃ��Z�b�g
                workItem.State = WorkItemState.Queued;

                // ���Ȃ��Ƃ���̓L���[�ɓ����Ă���̂ŁA
                // �X�P�W���[���������݂�
                DoNextWorkItem();
            }
        }

        // queue�ɑҋ@���Ă��郏�[�N�A�C�e��������o����Schedule��Ԃɂ���
        // �����������΃C�x���g�n���h���o�R�ŃL���[������o����A���s�����
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
        /// �ꎞ��~�B�V�K�̎��s���ł��Ȃ��Ȃ�B
        /// </summary>
        public void Pause()
        {
            Pausing = true;
        }

        /// <summary>
        /// �ĊJ�B
        /// </summary>
        public void Resume()
        {
            Pausing = false;
        }

        /// <summary>
        /// �ꎞ��~���Ă��邩�ǂ����B�����l��false�B
        /// </summary>
        public bool Pausing
        {
            get { return (_pausing); }
            set
            {
                if (_pausing != value)
                {
                    _pausing = value;

                    // �|�[�Y���������ꂽ�Ȃ�A�L���[�̃��[�N�A�C�e�������s����
                    while (!_pausing && DoNextWorkItem())
                    {
                        // 2006.10.10 SocketFactory�𓱓������̂ŁASleep()�̓R�����g�A�E�g�B
                        // WinSock�ɕ��S�������Ȃ��悤�ɁA���s�O�ɂ�����Ƃ����҂��܂��B
                        // Thread.Sleep(20);
                    }
                }
            }
        }
        private bool _pausing = false;

        /// <summary>
        /// �L���[�őҋ@���Ă��郏�[�N�A�C�e�����폜����B
        /// ���s���̃��[�N�A�C�e���ɂ͉e�����Ȃ��B
        /// </summary>
        public void ClearQueue()
        {
            lock (this)
            {
                _queue.Clear();
            }
        }

        /// <summary>
        /// Add()�����S�Ẵ��[�N�A�C�e�����I���܂ő҂B
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
                    throw new InvalidOperationException("�ꎞ��~���ł��B");
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
        /// Add()�����S�Ẵ��[�N�A�C�e�����I���܂ő҂B
        /// �^�C���A�E�g�t���B
        /// </summary>
        /// <param name="timeout">�ő�ҋ@���Ԃ�TimeSpan�Ŏw��</param>
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

        // �X���b�h�Ǘ������Ă���Ƃ��ɗ�O�����������Ƃ��̏���
        protected virtual void HandleThreadPoolException(WorkThreadPoolEventArgs e)
        {
            lock (_completed)
            {
                Pause();
                _internalException = e.Exception;

                // ���̐�ŃA�v���P�[�V�������I�����ė~�����B
                OnWorkThreadPoolEventHandler(e);

                Monitor.PulseAll(_completed);
            }
        }

        /// <summary>
        /// �X���b�h�Ǘ������Ă���Ƃ��ɗ�O�����������ꍇ�̃C�x���g
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
        /// ���[�N�A�C�e���̏�Ԃ��ω������Ƃ��̃C�x���g
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

        // �C�x���g����
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
        /// Add()�����S�Ẵ��[�N�A�C�e�������������Ƃ��̃C�x���g
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

        // �C�x���g����
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
        /// ���[�N�A�C�e�������s���ꂽ�Ƃ��̃C�x���g
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

        // �C�x���g����
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
        /// ���[�N�A�C�e�������������Ƃ��̃C�x���g
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

        // �C�x���g����
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
        /// ���[�N�A�C�e�������s�����Ƃ��̃C�x���g
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

        // �C�x���g����
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
        /// ���[�N�A�C�e���̏�Ԃ͎��s�󋵂ɂ�菟��ɕω����邽�߁A
        /// IWorkItem�����炱�̃��\�b�h���ĂԂ悤�ɂ��Ă���B
        /// �ʏ�͎g��Ȃ��Ă悢�B
        /// </summary>
        /// <param name="workItem"></param>
        /// <param name="previousState"></param>
        public void WorkItemStateChanged(IWorkItem workItem, WorkItemState previousState)
        {
            // ���v�l��ς���
            lock (this)
            {
                _stats[(int)previousState] -= 1;
                _stats[(int)workItem.State] += 1;
            }

            // �C�x���g�𔭓�
            OnWorkItemStateChangedEventHandler(workItem, previousState);

            // ���[�N�A�C�e���̌��݂̏�Ԃɉ����āA
            switch (workItem.State)
            {
                case WorkItemState.Scheduled:
                    // �X�P�W���[�������ꂽ�ꍇ
                    lock (this)
                    {
                        // ���s����
                        ++_numOfRunning;
                        BeginWork(workItem);
                    }
                    break;

                case WorkItemState.Running:
                    // ���[�N�A�C�e�����J�n���ꂽ�ꍇ
                    // �C�x���g�𔭓�
                    OnWorkItemRunningEventHandler(workItem);
                    break;

                case WorkItemState.Failed:
                    // ���[�N�A�C�e�������s�����ꍇ
                    // �C�x���g�𔭓�
                    OnWorkItemFailedEventHandler(workItem);
                    break;

                case WorkItemState.Completed:
                    // ���[�N�A�C�e�������������ꍇ
                    bool allDone = false;
                    lock (this)
                    {
                        --_numOfRunning;
                        allDone = _queue.Count == 0 && _numOfRunning == 0;
                    }

                    // �C�x���g�𔭓����Ă��̃��[�N�A�C�e���������������Ƃ�ʒm
                    OnWorkItemCompletedEventHandler(workItem);

                    if (allDone)
                    {
                        // �C�x���g�𔭓����đS���I��������Ƃ�ʒm
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
        /// �������s�X���b�h�̐��B�R���X�g���N�^�Ŏw�肷��B
        /// </summary>
        public int MaxThreads
        {
            get { return (_maxThreads); }
            set
            {
                if (0 >= value)
                {
                    throw new ArgumentOutOfRangeException("MaxThreads", value, "���̒l�łȂ���΂Ȃ�܂���");
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
            
            // �e�X���b�h����~������A�q�X���b�h����~����B
            thread.IsBackground = true;

            // STA�ɃZ�b�g���Ȃ��ƁAForm�A�v���P�[�V������SaveFileDialog���̊e��_�C�A���O���N�����Ȃ��B           
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
                // �L���[�ɓ����
                _workQueue.Enqueue(workItem);

                // �ő�X���b�h���ɒB���ĂȂ���΃X���b�h���쐬����
                if (_workersList.Count < _maxThreads)
                {
                    CreateThread();
                }

                // �ҋ@���Ă���WorkItem����N��
                // GetIWorkItemOrNull()��Monitor.Wait()���Ă�̂ŁA�ʒm���Ă�����
                Monitor.Pulse(this);
            }
        }

        // ���[�N�A�C�e�������s���邽�߂̃v���C�x�[�g�N���X
        private class WorkThread
        {
            // �Ǘ����̃X���b�h�v�[��
            private WorkThreadPool _wtPool;

            // ��~����Ƃ��͂����true�ɂ���
            // �œK���ō폜����Ȃ��悤��volatile�w��B
            private volatile bool _stopping = false;

            // �R���X�g���N�^
            public WorkThread(WorkThreadPool threadPool)
            {
                _wtPool = threadPool;
            }

            // ���ۂɎ��s����X���b�h
            public Thread Thread
            {
                get { return (_thread); }
                set { _thread = value; }
            }
            private Thread _thread = null;

            public void Start()
            {
                // stopping���ύX����ĂȂ����A����m���Ɍ��邽�߂�volatile�ϐ��ɂ��Ă���
                while (!_stopping)
                {
                    try
                    {
                        IWorkItem workItem = null;

                        // ���[�N�A�C�e�����擾
                        // BeginWork()��Monitor.Pulse()����܂Ńu���b�L���O
                        workItem = GetIWorkItemOrNull();

                        // ���[�N�A�C�e�������s
                        if (workItem != null)
                        {
                            DoWork(workItem);
                        }

                        // ���ԓI�ɂ͈�ԍŌ�Ɏ��s�������̂�
                        // ���̃X���b�h��CPU����������
                        Thread.Sleep(0);
                    }
                    catch (ThreadAbortException)
                    {
                        Thread.ResetAbort();
                        _stopping = true;
                    }
                }
            }

            // ���[�N�L���[������ۂ̏ꍇ�́A���[�N�A�C�e�����i�[�����܂Ńu���b�L���O
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

                    // ��~���w������Ă���null��Ԃ�
                    if (_stopping)
                    {
                        return (null);
                    }

                    // ���[�N�L���[�ɃG���g��������΂�������o��
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
                    // ���s���̃��[�N�A�C�e���̗�O�͂����ŕߑ�����B
                    // �����ŗ�O���������邱�Ƃ͑債�����ł͂Ȃ��B
                    // �ォ��Q�Ƃł���悤�Ƀ��[�N�A�C�e���̒��ɕۑ����Ă����B
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
