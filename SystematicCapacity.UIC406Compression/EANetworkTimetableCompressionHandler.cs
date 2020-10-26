using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using SystematicCapacity.Core;

namespace SystematicCapacity.UIC406Compression
{
    /// <summary>
    /// Using a event-activity network with a longest path algorithm to compress the timetable
    /// </summary>
    public class EANTimetableCompressionHandler : TimetableCompressionHandler
    {
        /// <summary>
        /// Global event list
        /// </summary>
        private List<EANEvent> eventList = new List<EANEvent>();

        /// <summary>
        /// Global activity list
        /// </summary>
        private List<EANActivity> activityList = new List<EANActivity>();

        /// <summary>
        /// The original event (the first (virtual) event for topological ordering)
        /// </summary>
        private EANEvent globalOriginEvent;

        /// <summary>
        /// Initialize the timetable compression method
        /// </sumary>
        protected override void Initialize()
        {
            GenerateEANetwork();
            OutputDebugFiles();
        }

        /// <summary>
        /// Compress the timetable using event-activity network's longest path algorithm
        /// </summary>
        protected override void Compress()
        {
            DoTopologicalOrdering();
        }

        /// <summary>
        /// Parse compressed event-activity data back to DataRepository
        /// </summary>
        protected override void ParseCompressedTimetable()
        {
            ParseCompressedData();
        }

        /// <summary>
        /// Generate the event-activity network by basic data
        /// </summary>
        private void GenerateEANetwork()
        {
            // generate the global origin event
            globalOriginEvent = new EANEvent()
            {
                Type = EventTypes.Origin
            };
            eventList.Add(globalOriginEvent);

            // generate train-path events and activities
            foreach (Train tr in Program.DataRepository.serviceRepo.TrainList)
            {
                // temporal event and activity list for train 'tr'
                List<EANEvent> _eventList = new List<EANEvent>();
                List<EANActivity> _activityList = new List<EANActivity>();

                // generate events and running activities by enumerating its passing through segments
                GenerateEventsAndRunningActivity(tr, _eventList, _activityList);

                // generate dwelling activity by enumerating its passing through stations
                GenerateDwellingActivity(tr, _eventList, _activityList);

                // generate activity connecting the global origin event
                GenerateOriginActivity(_eventList, _activityList);

                // merge the temporall event and activity list to the global ones
                eventList = eventList.Union(_eventList).ToList();
                activityList = activityList.Union(_activityList).ToList();
            }

            // generate headway activities
            foreach (SegmentTrack seg in Program.DataRepository.infraRepo.SegmentTrackSet)
            {
                GenerateHeadwayActivity(seg, EventTypes.Entering);
                GenerateHeadwayActivity(seg, EventTypes.Leaving);
            }
        }

        /// <summary>
        /// Generate headway activities
        /// </summary>
        /// <param name="seg"></param>
        /// <param name="type"></param>
        private void GenerateHeadwayActivity(SegmentTrack seg, EventTypes type)
        {
            // for segment 'seg', get all entering events ordered by their original time
            var orderedEventList = from EANEvent e in eventList
                                   where e.BindingSegmentTrack == seg && e.Type == type
                                   orderby e.OriginalTime
                                   select e;

            // generate headway activities by enumerating the relative events by time order
            IEnumerator<EANEvent> orderedEnteringEventEnum = orderedEventList.GetEnumerator();
            EANEvent lastEvent = null;
            while (orderedEnteringEventEnum.MoveNext())
            {
                if (lastEvent == null)
                {
                    lastEvent = orderedEnteringEventEnum.Current;
                    continue;
                }

                EANEvent currentEvent = orderedEnteringEventEnum.Current;

                EANActivity headwayActivity = new EANActivity()
                {
                    FromEvent = lastEvent,
                    ToEvent = currentEvent,
                    Type = ActivityTypes.Headway,
                    MinDuration = CalculateMinHeadway(seg, lastEvent, currentEvent)
                };
                lastEvent.OutActivityList.Add(headwayActivity);
                currentEvent.InActivityList.Add(headwayActivity);
                activityList.Add(headwayActivity);
                lastEvent = currentEvent;
            }
        }

        /// <summary>
        /// Calculate the minimum headway
        /// </summary>
        /// <param name="seg"></param>
        /// <param name="lastEvent"></param>
        /// <param name="currentEvent"></param>
        /// <returns></returns>
        private int CalculateMinHeadway(SegmentTrack seg, EANEvent lastEvent, EANEvent currentEvent)
        {
            // TODO
            return 6;
        }

        /// <summary>
        /// Generate Events, and running activities by enumerating segments
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="_eventList"></param>
        /// <param name="_activityList"></param>
        private void GenerateEventsAndRunningActivity(Train tr, List<EANEvent> _eventList, List<EANActivity> _activityList)
        {
            foreach (SegmentTrack seg in tr.RouteSet)
            {
                // event entering a segment (departing from a station)
                EANEvent eanEnteringEvent = new EANEvent()
                {
                    BindingSegmentTrack = seg,
                    BindingTrain = tr,
                    Type = EventTypes.Entering,
                    OriginalTime = tr.Timetable[seg.FromStation]["departure"]
                };
                _eventList.Add(eanEnteringEvent);

                // event leaving a segment (arriving at a station)
                EANEvent eanLeavingEvent = new EANEvent()
                {
                    BindingSegmentTrack = seg,
                    BindingTrain = tr,
                    Type = EventTypes.Leaving,
                    OriginalTime = tr.Timetable[seg.ToStation]["arrival"]
                };
                _eventList.Add(eanLeavingEvent);

                // activity running through a segment
                EANActivity runningActivity = new EANActivity()
                {
                    FromEvent = eanEnteringEvent,
                    ToEvent = eanLeavingEvent,
                    MinDuration = tr.Timetable[seg.ToStation]["arrival"] - tr.Timetable[seg.FromStation]["departure"],
                };
                eanEnteringEvent.OutActivityList.Add(runningActivity);
                eanLeavingEvent.InActivityList.Add(runningActivity);
                _activityList.Add(runningActivity);
            }
        }

        /// <summary>
        /// Generate dwelling activities by enumerating staions
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="_eventList"></param>
        /// <param name="_activityList"></param>
        private void GenerateDwellingActivity(Train tr, List<EANEvent> _eventList, List<EANActivity> _activityList)
        {
            foreach (Station sta in tr.Timetable.Keys)
            {
                // find the events associated with station 'sta'
                EANEvent arrivalEvent = _eventList.Find(e => e.BindingSegmentTrack.ToStation == sta && e.Type == EventTypes.Leaving);
                EANEvent departureEvent = _eventList.Find(e => e.BindingSegmentTrack.FromStation == sta && e.Type == EventTypes.Entering);

                if (arrivalEvent != null && departureEvent != null)
                {
                    // dwelling activity at station
                    EANActivity dwellingActivity = new EANActivity()
                    {
                        FromEvent = arrivalEvent,
                        ToEvent = departureEvent,
                        Type = ActivityTypes.Dwelling,
                        MinDuration = tr.MinStoppingTime[sta],
                    };
                    arrivalEvent.OutActivityList.Add(dwellingActivity);
                    departureEvent.InActivityList.Add(dwellingActivity);
                    _activityList.Add(dwellingActivity);
                }
            }
        }

        /// <summary>
        /// Generate the original activities (connecting the originalEvent and the first event of trains)
        /// </summary>
        /// <param name="_eventList"></param>
        /// <param name="_activityList"></param>
        private void GenerateOriginActivity(List<EANEvent> _eventList, List<EANActivity> _activityList)
        {
            EANEvent firstEvent = _eventList.First();
            EANActivity originActivity = new EANActivity()
            {
                FromEvent = globalOriginEvent,
                ToEvent = firstEvent,
                Type = ActivityTypes.Origin,
                MinDuration = 0,
            };
            _activityList.Add(originActivity);
            globalOriginEvent.OutActivityList.Add(originActivity);
            firstEvent.InActivityList.Add(originActivity);
        }

        /// <summary>
        /// Outpu events and activities debug files (if the OutputLevel > 0)
        /// </summary>
        private void OutputDebugFiles()
        {
            if (Program.OutputLevel <= 0)
                return;
            System.IO.StreamWriter sw = new System.IO.StreamWriter(Program.SolutionDataDirectory + "debug_event.csv", false);
            sw.WriteLine("TrainID,SegmentID,EventType,OriginalTime");
            foreach (EANEvent e in eventList)
            {
                sw.WriteLine("{0},{1},{2},{3}",
                 e.BindingTrain != null ? e.BindingTrain.ID : "null", e.BindingSegmentTrack != null ? e.BindingSegmentTrack.ID : "null", e.Type.ToString(), e.OriginalTime);
            }
            sw.Close();

            sw = new System.IO.StreamWriter(Program.SolutionDataDirectory + "debug_activity.csv", false);
            sw.WriteLine("FromEvent,ToEvent,ActivityType,MinDuration");
            foreach (EANActivity a in activityList)
            {
                sw.WriteLine("{0},{1},{2},{3}",
                a.FromEvent.ToString(), a.ToEvent.ToString(), a.Type.ToString(), a.MinDuration.ToString());
            }
            sw.Close();
        }

        /// <summary>
        /// Topological ordering to calculate the longest paths (labling the earliest happening time for each event)
        /// </summary>
        private void DoTopologicalOrdering()
        {
            // using a stack to implement the depth first searching
            Stack<EANEvent> topologicalOrderStack = new Stack<EANEvent>();
            topologicalOrderStack.Push(globalOriginEvent);

            while (topologicalOrderStack.Count > 0)
            {
                EANEvent currentEvent = topologicalOrderStack.Pop();
                foreach (EANActivity a in currentEvent.OutActivityList)
                {
                    EANEvent toEvent = a.ToEvent;
                    if (toEvent.CompressedTime < currentEvent.CompressedTime + a.MinDuration)
                    {
                        toEvent.CompressedTime = currentEvent.CompressedTime + a.MinDuration;
                    }
                    topologicalOrderStack.Push(toEvent);
                }
            }
        }

        /// <summary>
        /// Parse event-activity solution to global DataRepository
        /// </summary>
        private void ParseCompressedData()
        {
            StreamWriter sw = null;
            if (Program.OutputLevel > 0)
            {
                sw = new StreamWriter(Program.SolutionDataDirectory + "debug_compressedEventTime.csv");
                sw.WriteLine("Event,OriginalTime,CompressedTime");
            }

            foreach (EANEvent e in eventList)
            {
                Train tr = e.BindingTrain;

                Station sta = null;
                string operation = "";
                if (e.Type == EventTypes.Entering)
                {
                    sta = e.BindingSegmentTrack.FromStation;
                    operation = "departure";
                }
                else if (e.Type == EventTypes.Leaving)
                {
                    sta = e.BindingSegmentTrack.ToStation;
                    operation = "arrival";
                }
                else if (e.Type == EventTypes.Origin)
                    continue;

                tr.Timetable[sta][operation] = e.CompressedTime;
                sw?.WriteLine("{0},{1},{2}", e.ToString(), e.OriginalTime, e.CompressedTime);
            }

            sw?.Close();
        }
    }

    /// <summary>
    /// Event in event-activity network
    /// </summary>
    internal class EANEvent
    {
        internal List<EANActivity> InActivityList { get; set; } = new List<EANActivity>();

        internal List<EANActivity> OutActivityList { get; set; } = new List<EANActivity>();

        internal Train BindingTrain { get; set; }

        internal SegmentTrack BindingSegmentTrack { get; set; }

        internal EventTypes Type { get; set; }

        internal int OriginalTime { get; set; }

        internal int CompressedTime { get; set; } = 0;

        public override string ToString()
        {
            return string.Format("{0}|{1}|{2}", BindingTrain != null ? BindingTrain.ID : "null",
             BindingSegmentTrack != null ? BindingSegmentTrack.ID : "null", Type.ToString());
        }
    }

    /// <summary>
    /// Activity in event-activity network
    /// </summary>
    internal class EANActivity
    {
        internal EANEvent FromEvent { get; set; }

        internal EANEvent ToEvent { get; set; }

        internal ActivityTypes Type { get; set; }

        internal int MinDuration { get; set; }

        public override string ToString()
        {
            return string.Format("{0}|{1}|{2}", FromEvent.ToString(), ToEvent.ToString(), Type.ToString());
        }
    }

    /// <summary>
    /// Event types
    /// </summary>
    internal enum EventTypes
    {
        /// <summary>
        /// The global origin event
        /// </summary>
        Origin,

        /// <summary>
        /// The event entering a segment (departing from a station)
        /// </summary>
        Entering,

        /// <summary>
        /// The event leaving a segment (arriving at a station)
        /// </summary>
        Leaving,
    }

    /// <summary>
    /// Activity types
    /// </summary>
    internal enum ActivityTypes
    {
        /// <summary>
        /// The activity running through a segment
        /// </summary>
        Running,

        /// <summary>
        /// The activity dwelling at a station
        /// </summary>
        Dwelling,

        /// <summary>
        /// The activity keeping the necessary headway between two events
        /// </summary>
        Headway,

        /// <summary>
        /// The activity connecting the global origin event and the first event of a train
        /// </summary>
        Origin,
    }
}
