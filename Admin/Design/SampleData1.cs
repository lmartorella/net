using System.Collections.Generic;
using Lucky.Home.Admin;

namespace Lucky.Home.Design
{
    public class SampleData1 : Connection
    {
        public SampleData1()
        {
            StatusText = "Connected";
            Nodes.Add(new Node { Id = "root1", Children = new List<Node>(
                new[]
                {
                    new Node { Id = "Child1" },
                    new Node { Id = "Child2" },
                })
            });
        }
    }
}
