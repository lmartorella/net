using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;

namespace Lucky.Home.Model
{
    public class TimeProgram<TCycle> where TCycle : TimeProgram<TCycle>.Cycle
    {
        // Cover the timelapse during DST
        private static TimeSpan RefreshTimerPeriod = TimeSpan.FromHours(6);

        private ProgramData _program;
        private Timer _refreshTimer;

        public static ProgramData DefaultProgram
        {
            get
            {
                return new ProgramData { Cycles = new TCycle[0] };
            }
        }

        public TimeProgram(ProgramData program = null)
        {
            Program = program ?? DefaultProgram;
        }

        public void Dispose()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Dispose();
                _refreshTimer = null;
            }
        }

        public ProgramData Program
        {
            get
            {
                return _program;
            }
            set
            {
                _program = value;
                Validate();
                InitTimers();
            }
        }

        private void Validate()
        {
            if (_program == null || _program.Cycles == null)
            {
                throw new ArgumentNullException("Missing program");
            }
            foreach (var t in _program.Cycles.Select((cycle, idx) => new { cycle, idx }))
            {
                var name = t.cycle.Name ?? t.idx.ToString();
                if (t.cycle.End.HasValue && t.cycle.Start.HasValue && t.cycle.Start > t.cycle.End)
                {
                    throw new ArgumentOutOfRangeException("End before start: cycle " + name);
                }
                if (t.cycle.WeekDays != null && t.cycle.WeekDays.Length == 0)
                {
                    t.cycle.WeekDays = null;
                }
                if (t.cycle.DayPeriod > 0 && t.cycle.WeekDays != null)
                {
                    throw new ArgumentOutOfRangeException("Both day period and week days specified: cycle " + name);
                }
                if (t.cycle.DayPeriod <= 0 && t.cycle.WeekDays == null)
                {
                    throw new ArgumentOutOfRangeException("No day period nor week days specified: cycle " + name);
                }
                if (t.cycle.DayPeriod > 0 && !t.cycle.Start.HasValue)
                {
                    throw new ArgumentOutOfRangeException("No start day for periodic table: cycle " + name);
                }
                if (t.cycle.StartTime < TimeSpan.Zero || t.cycle.StartTime > TimeSpan.FromDays(1))
                {
                    throw new ArgumentOutOfRangeException("Invalid start time: cycle " + name);
                }
            }
        }

        /// <summary>
        /// The program data
        /// </summary>
        [DataContract]
        public class ProgramData
        {
            /// <summary>
            /// List of programs (if requested)
            /// </summary>
            [DataMember(Name = "cycles")]
            public TCycle[] Cycles { get; set; }
        }

        /// <summary>
        /// One cycle program
        /// </summary>
        [DataContract]
        public class Cycle
        {
            /// <summary>
            /// Friendly name
            /// </summary>
            [DataMember(Name = "name")]
            public string Name { get; set; }

            /// <summary>
            /// Enable/Disabled
            /// </summary>
            [DataMember(Name = "disabled")]
            public bool Disabled { get; set; }

            /// <summary>
            /// Start date-time
            /// </summary>
            [DataMember(Name = "start")]
            public string StartStr { get { return ToIso(Start);  } set { Start = FromIso(value);  } }

            /// <summary>
            /// Start date-time
            /// </summary>
            [IgnoreDataMember]
            public DateTime? Start { get; set; }

            /// <summary>
            /// End date-time
            /// </summary>
            [DataMember(Name = "end")]
            public string EndStr { get { return ToIso(End); } set { End = FromIso(value); } }

            /// <summary>
            /// End date-time
            /// </summary>
            [IgnoreDataMember]
            public DateTime? End { get; set; }

            /// <summary>
            /// If > 0, period in number of days
            /// </summary>
            [DataMember(Name = "dayPeriod")]
            public int DayPeriod { get; set; }

            /// <summary>
            /// If not null, week days of activity (0 = Sunday, 6 = Saturday) 
            /// </summary>
            [DataMember(Name = "weekDays")]
            public DayOfWeek[] WeekDays { get; set; }

            /// <summary>
            /// Time of day of start activity
            /// </summary>
            [DataMember(Name = "startTime")]
            public string StartTimeStr { get { return ToIso(StartTime); } set { StartTime = FromIsoT(value); } }

            /// <summary>
            /// Time of day of start activity
            /// </summary>
            [IgnoreDataMember]
            public TimeSpan StartTime { get; set; }
        }

        private static string ToIso(DateTime? start)
        {
            if (start.HasValue)
            {
                return start.Value.ToString("o");
            }
            else
            {
                return null;
            }
        }

        private static DateTime? FromIso(string str)
        {
            if (str != null)
            {
                return DateTime.ParseExact(str, "o", null);
            }
            else
            {
                return null;
            }
        }

        private static string ToIso(TimeSpan? timeOfDay)
        {
            if (timeOfDay.HasValue)
            {
                return timeOfDay.Value.ToString("c");
            }
            else
            {
                return null;
            }
        }

        private static TimeSpan FromIsoT(string str)
        {
            return TimeSpan.ParseExact(str, "c", null);
        }

        public class CycleTriggeredEventArgs : EventArgs
        {
            public TCycle Cycle;
        }

        /// <summary>
        /// Event raised when a cycle program kicks in
        /// </summary>
        public event EventHandler<CycleTriggeredEventArgs> CycleTriggered;

        private void InitTimers()
        {
            // Building a straightforward C# timer for each cycle pointing to the next step is limited to 49 days.
            // This will require an additional refresh timer for longer periods.
            if (_refreshTimer != null)
            {
                _refreshTimer.Dispose();
            }

            // Make sure that we don't lose ticks in between poll calc
            var lastRefreshTime = DateTime.Now;
            // To avoid GC
            Timer[] intermediateTimers;
            _refreshTimer = new Timer(o =>
            {
                intermediateTimers = PollTimers(lastRefreshTime, RefreshTimerPeriod);
                lastRefreshTime += RefreshTimerPeriod;
            }
            , null, TimeSpan.Zero, RefreshTimerPeriod);
        }

        /// <summary>
        /// Create shorter C# timers for each cycle that triggers between now and pollPeriod
        /// </summary>
        private Timer[] PollTimers(DateTime now, TimeSpan pollPeriod)
        {
            return _program.Cycles.Select(cycle =>
            {
                // Calc the next event for each cycle. If exceeding the refresh timer, let's wait for the next poll cycle
                var nextTick = GetNextTick(cycle, now);
                if (nextTick.HasValue)
                {
                    var period = nextTick.Value - now;
                    if (period <= pollPeriod)
                    {
                        // Ok, schedule the timer for this period
                        return new Timer(o => RaiseEvent(cycle), null, (int)period.TotalMilliseconds, Timeout.Infinite);
                    }
                }
                return null;
            }).ToArray();
        }

        private void RaiseEvent(TCycle cycle)
        {
            CycleTriggered?.Invoke(this, new CycleTriggeredEventArgs { Cycle = cycle });
        }

        /// <summary>
        /// Get the next event timestamp for that cycle starting from now
        /// </summary>
        public static DateTime? GetNextTick(Cycle cycle, DateTime now)
        {
            if (cycle.Disabled)
            {
                return null;
            }

            // Check begin / end
            if (cycle.Start.HasValue && now < cycle.Start)
            {
                return null;
            }
            if (cycle.End.HasValue && now > cycle.End)
            {
                return null;
            }

            // Calc the next valid starting day 
            var nextDay = (now.TimeOfDay > cycle.StartTime) ? (now.Date + TimeSpan.FromDays(1)) : now.Date;
            if (cycle.WeekDays != null)
            {
                // Get the next weekday
                nextDay = GetNextValidWeekday(cycle.WeekDays, nextDay);
            }
            else
            {
                // Get the next periodic day
                nextDay = GetNextValidPeriodicDay(cycle.DayPeriod, nextDay, cycle.Start.Value.Date);
            }

            return nextDay + cycle.StartTime;
        }

        private static DateTime GetNextValidPeriodicDay(int dayPeriod, DateTime today, DateTime startDate)
        {
            int elapsedDays = (int)Math.Round((today.Subtract(startDate)).TotalDays);
            // If elapsedDays is multiple of dayPeriod, don't add any day
            // If elapsedDays is (multiple of dayPeriod + 1), add (dayPeriod - 1), etc.. 
            int missingDays = (dayPeriod - (elapsedDays % dayPeriod)) % dayPeriod;
            return today + TimeSpan.FromDays(missingDays);
        }

        private static DateTime GetNextValidWeekday(DayOfWeek[] weekDays, DateTime today)
        {
            for (var i = 0; i < 7; i++)
            {
                if (weekDays.Contains(today.DayOfWeek))
                {
                    break;
                }
                today += TimeSpan.FromDays(1);
            }
            return today;
        }
    }
}
