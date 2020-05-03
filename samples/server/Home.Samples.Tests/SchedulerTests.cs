using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lucky.Home.Model;
using System.Linq;

namespace UTs
{
    [TestClass]
    public class SchedulerTests
    {
        public class C : TimeProgram<C>.Cycle
        {
        }

        [TestMethod]
        public void DayPeriodTests()
        {
            var cycle = new C { DayPeriod = 3, Start = new DateTime(2010, 5, 5), StartTime = new TimeSpan(12, 0, 0) };
            var program = new TimeProgram<C>.ProgramData { Cycles = new[] { cycle } };

            // some min before
            Assert.AreEqual(new DateTime(2010, 5, 5, 12, 0, 0), TimeProgram<C>.GetNextTick(cycle, new DateTime(2010, 5, 5, 11, 0, 0)));
            // some min after
            Assert.AreEqual(new DateTime(2010, 5, 8, 12, 0, 0), TimeProgram<C>.GetNextTick(cycle, new DateTime(2010, 5, 5, 13, 0, 0)));

            // Around midnight
            cycle = new C { DayPeriod = 3, Start = new DateTime(2010, 5, 2), StartTime = new TimeSpan(0, 0, 0) };
            program = new TimeProgram<C>.ProgramData { Cycles = new[] { cycle } };

            // some min before
            Assert.AreEqual(new DateTime(2010, 5, 5, 0, 0, 0), TimeProgram<C>.GetNextTick(cycle, new DateTime(2010, 5, 4, 23, 59, 0)));
            // some min after
            Assert.AreEqual(new DateTime(2010, 5, 8, 0, 0, 0), TimeProgram<C>.GetNextTick(cycle, new DateTime(2010, 5, 5, 0, 0, 2)));

            // Around DST change 1
            // DST changes 26/mar/2017 2:00 (Italy) -> 3:00
            cycle = new C { DayPeriod = 3, Start = new DateTime(2017, 3, 26), StartTime = new TimeSpan(1, 59, 59) };
            program = new TimeProgram<C>.ProgramData { Cycles = new[] { cycle } };

            // some min before
            Assert.AreEqual(new DateTime(2017, 3, 26, 1, 59, 59, DateTimeKind.Local), TimeProgram<C>.GetNextTick(cycle, new DateTime(2017, 3, 26, 1, 59, 50, DateTimeKind.Local)));
            // some min after
            Assert.AreEqual(new DateTime(2017, 3, 29, 1, 59, 59, DateTimeKind.Local), TimeProgram<C>.GetNextTick(cycle, new DateTime(2017, 3, 26, 2, 00, 02, DateTimeKind.Local)));

            // Around DST change 2
            // DST changes 29/oct/2017 3:00 (Italy) -> 2:00
            cycle = new C { DayPeriod = 3, Start = new DateTime(2017, 10, 29), StartTime = new TimeSpan(2, 59, 59) };
            program = new TimeProgram<C>.ProgramData { Cycles = new[] { cycle } };

            // some min before
            Assert.AreEqual(new DateTime(2017, 10, 29, 2, 59, 59, DateTimeKind.Local), TimeProgram<C>.GetNextTick(cycle, new DateTime(2017, 10, 29, 2, 59, 50, DateTimeKind.Local)));
            // some min after
            Assert.AreEqual(new DateTime(2017, 11, 1, 2, 59, 59, DateTimeKind.Local), TimeProgram<C>.GetNextTick(cycle, new DateTime(2017, 10, 29, 3, 00, 02, DateTimeKind.Local)));

            var cycles = new C[] {
                new C { DayPeriod = 3, Start = new DateTime(2010, 1, 1), StartTime = new TimeSpan(12, 0, 0) },
                new C { DayPeriod = 1, Start = new DateTime(2010, 1, 2), StartTime = new TimeSpan(14, 0, 0) },
            };
            program = new TimeProgram<C>.ProgramData { Cycles = cycles };

            CollectionAssert.AreEqual(new[] {
                new DateTime(2010, 1, 1, 12, 0, 0),
                new DateTime(2010, 1, 2, 14, 0, 0),
                new DateTime(2010, 1, 3, 14, 0, 0),
                new DateTime(2010, 1, 4, 12, 0, 0),
                new DateTime(2010, 1, 4, 14, 0, 0),
                new DateTime(2010, 1, 5, 14, 0, 0)
            }, TimeProgram<C>.GetNextCycles(program, new DateTime(2010, 1, 1, 11, 0, 0)).Take(6).Select(c => c.Item2).ToArray(), "Test 2");
        }

        /// <summary>
        /// Test that re-enabling after suspension will pick the closer day of period
        /// </summary>
        [TestMethod]
        public void TestMostCloseDay()
        {
            var c1 = new C { DayPeriod = 2, Start = new DateTime(2020, 4, 29, 9, 50, 0), StartTime = new TimeSpan(3, 0, 4) };
            var c2 = new C { DayPeriod = 3, Start = new DateTime(2020, 4, 29, 9, 50, 0), StartTime = new TimeSpan(2, 0, 4) };
            var cycles = new C[] { c1, c2 };
            var program = new TimeProgram<C>.ProgramData { Cycles = cycles };

            var nextCycles = TimeProgram<C>.GetNextCycles(program, new DateTime(2020, 4, 29, 10, 50, 0)).Take(7);
            CollectionAssert.AreEqual(new[] {
                Tuple.Create(c2, new DateTime(2020, 4, 30, 2, 0, 4)),
                Tuple.Create(c1, new DateTime(2020, 4, 30, 3, 0, 4)),
                Tuple.Create(c1, new DateTime(2020, 5, 2, 3, 0, 4)),
                Tuple.Create(c2, new DateTime(2020, 5, 3, 2, 0, 4)),
                Tuple.Create(c1, new DateTime(2020, 5, 4, 3, 0, 4)),
                Tuple.Create(c2, new DateTime(2020, 5, 6, 2, 0, 4)),
                Tuple.Create(c1, new DateTime(2020, 5, 6, 3, 0, 4))
            }, nextCycles.ToArray(), "Start time after cycle time");

            nextCycles = TimeProgram<C>.GetNextCycles(program, new DateTime(2020, 4, 30, 0, 50, 0)).Take(7);
            CollectionAssert.AreEqual(new[] {
                Tuple.Create(c2, new DateTime(2020, 4, 30, 2, 0, 4)),
                Tuple.Create(c1, new DateTime(2020, 4, 30, 3, 0, 4)),
                Tuple.Create(c1, new DateTime(2020, 5, 2, 3, 0, 4)),
                Tuple.Create(c2, new DateTime(2020, 5, 3, 2, 0, 4)),
                Tuple.Create(c1, new DateTime(2020, 5, 4, 3, 0, 4)),
                Tuple.Create(c2, new DateTime(2020, 5, 6, 2, 0, 4)),
                Tuple.Create(c1, new DateTime(2020, 5, 6, 3, 0, 4))
            }, nextCycles.ToArray(), "Start time before cycle time");
        }
    }
}
