using System;
using System.Collections.Generic;
using System.Text;

namespace rmainte4.TimelineControl
{
    public delegate void AxisFoundEventHandler(IAxis axis);
    public delegate void TimelineItemFoundEventHandler(ITimelineItem timelineItem);
    
    public interface ITimelineDataProvider
    {
        event AxisFoundEventHandler AxisFound;
        event TimelineItemFoundEventHandler TimelineItemFound;
    }
}
