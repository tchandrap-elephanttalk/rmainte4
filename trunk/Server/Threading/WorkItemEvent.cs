/* *-*-mode:java; -*- */

// Copyright Takamitsu IIDA <gsi@nifty.com>
// CCIE4288
// 2004.12.27
// Last modified at 2006.07.07
// Last modified at 2009.01.07 for rmainte4

using System;

namespace rmainte4.Threading
{

    public delegate void WorkItemEvent(object sender, WorkItemEventArgs e);
    public delegate void WorkItemStateChangedEvent(object sender, WorkItemStateChangedEventArgs e);
    public delegate void WorkThreadPoolEvent(object sender, WorkThreadPoolEventArgs e);

    public class WorkItemEventArgs : EventArgs
    {
        public WorkItemEventArgs(IWorkItem workItem)
            : base()
        {
            _workItem = workItem;
        }

        public IWorkItem WorkItem
        {
            get { return (_workItem); }
        }
        private IWorkItem _workItem;
    }

    public class WorkItemStateChangedEventArgs : WorkItemEventArgs
    {
        public WorkItemStateChangedEventArgs(IWorkItem workItem, WorkItemState previousState)
            : base(workItem)
        {
            _previousState = previousState;
        }

        public WorkItemState PreviousState
        {
            get { return (_previousState); }
        }
        private WorkItemState _previousState;
    }

    public sealed class WorkThreadPoolEventArgs : EventArgs
    {

        private WorkThreadPoolEventArgs()
        {
        }

        public WorkThreadPoolEventArgs(WorkThreadPool workThreadPool, Exception exception)
            : base()
        {
            _workThreadPool = workThreadPool;
            _exception = exception;
        }

        public WorkThreadPoolEventArgs(WorkThreadPool workThreadPool, IWorkItem workItem, Exception exception)
            : base()
        {
            _workThreadPool = workThreadPool;
            _workItem = workItem;
            _exception = exception;
        }

        public System.Exception Exception
        {
            get { return (_exception); }
        }
        private System.Exception _exception = null;

        public IWorkItem WorkItem
        {
            get { return (_workItem); }
        }
        private IWorkItem _workItem;

        public WorkThreadPool WorkThreadPool
        {
            get { return (_workThreadPool); }
        }
        private WorkThreadPool _workThreadPool;
    }

}
