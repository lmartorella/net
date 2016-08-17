using System;
using System.Collections.Generic;

namespace Lucky.Home.Db
{
    class PeriodData<T>
    {
        private List<Sample> _data = new List<Sample>();

        private T Sum;

        class Sample
        {
            public DateTime TimeStamp;
            public T Data;
        }

        public PeriodData()
        {

        }
    }
}
