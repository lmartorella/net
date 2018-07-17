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

            // Exactly equal to now
            Assert.AreEqual(new DateTime(2010, 5, 5, 12, 0, 0), TimeProgram<C>.GetNextTick(program, cycle, new DateTime(2010, 5, 5, 12, 0, 0)));
            // some min before
            Assert.AreEqual(new DateTime(2010, 5, 5, 12, 0, 0), TimeProgram<C>.GetNextTick(program, cycle, new DateTime(2010, 5, 5, 11, 0, 0)));
            // some min after
            Assert.AreEqual(new DateTime(2010, 5, 8, 12, 0, 0), TimeProgram<C>.GetNextTick(program, cycle, new DateTime(2010, 5, 5, 13, 0, 0)));

            // Around midnight
            cycle = new C { DayPeriod = 3, Start = new DateTime(2010, 5, 2), StartTime = new TimeSpan(0, 0, 0) };
            program = new TimeProgram<C>.ProgramData { Cycles = new[] { cycle } };
            
            // Exactly equal to now
            Assert.AreEqual(new DateTime(2010, 5, 5, 0, 0, 0), TimeProgram<C>.GetNextTick(program, cycle, new DateTime(2010, 5, 5, 0, 0, 0)));
            // some min before
            Assert.AreEqual(new DateTime(2010, 5, 5, 0, 0, 0), TimeProgram<C>.GetNextTick(program, cycle, new DateTime(2010, 5, 4, 23, 59, 0)));
            // some min after
            Assert.AreEqual(new DateTime(2010, 5, 8, 0, 0, 0), TimeProgram<C>.GetNextTick(program, cycle, new DateTime(2010, 5, 5, 0, 0, 2)));

            // Around DST change 1
            // DST changes 26/mar/2017 2:00 (Italy) -> 3:00
            cycle = new C { DayPeriod = 3, Start = new DateTime(2017, 3, 26), StartTime = new TimeSpan(1, 59, 59) };
            program = new TimeProgram<C>.ProgramData { Cycles = new[] { cycle } };

            // Exactly equal to now
            Assert.AreEqual(new DateTime(2017, 3, 26, 1, 59, 59, DateTimeKind.Local), TimeProgram<C>.GetNextTick(program, cycle, new DateTime(2017, 3, 26, 1, 59, 59, DateTimeKind.Local)));
            // some min before
            Assert.AreEqual(new DateTime(2017, 3, 26, 1, 59, 59, DateTimeKind.Local), TimeProgram<C>.GetNextTick(program, cycle, new DateTime(2017, 3, 26, 1, 59, 50, DateTimeKind.Local)));
            // some min after
            Assert.AreEqual(new DateTime(2017, 3, 29, 1, 59, 59, DateTimeKind.Local), TimeProgram<C>.GetNextTick(program, cycle, new DateTime(2017, 3, 26, 2, 00, 02, DateTimeKind.Local)));

            // Around DST change 2
            // DST changes 29/oct/2017 3:00 (Italy) -> 2:00
            cycle = new C { DayPeriod = 3, Start = new DateTime(2017, 10, 29), StartTime = new TimeSpan(2, 59, 59) };
            program = new TimeProgram<C>.ProgramData { Cycles = new[] { cycle } };

            // Exactly equal to now
            Assert.AreEqual(new DateTime(2017, 10, 29, 2, 59, 59, DateTimeKind.Local), TimeProgram<C>.GetNextTick(program, cycle, new DateTime(2017, 10, 29, 2, 59, 59, DateTimeKind.Local)));
            // some min before
            Assert.AreEqual(new DateTime(2017, 10, 29, 2, 59, 59, DateTimeKind.Local), TimeProgram<C>.GetNextTick(program, cycle, new DateTime(2017, 10, 29, 2, 59, 50, DateTimeKind.Local)));
            // some min after
            Assert.AreEqual(new DateTime(2017, 11, 1, 2, 59, 59, DateTimeKind.Local), TimeProgram<C>.GetNextTick(program, cycle, new DateTime(2017, 10, 29, 3, 00, 02, DateTimeKind.Local)));

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
    }
}