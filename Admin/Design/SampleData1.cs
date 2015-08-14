using System.Collections.Generic;
using Lucky.Home.Admin;
using Lucky.Home.Sinks;

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
                    new Node { Id = "Child1", Status = new NodeStatus { ResetReason = ResetReason.Brownout }},
                    new Node { Id = "Child2", Status = new NodeStatus { ResetReason = ResetReason.Exception, ExceptionMessage = "EXC.CODE1"} },
                })
            });
        }
    }
}
