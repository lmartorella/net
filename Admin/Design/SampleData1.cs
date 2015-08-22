using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lucky.Home.Admin;

namespace Lucky.Home.Design
{
    public class SampleData1 : Connection
    {
        public SampleData1()
        {
            StatusText = "Connected";
            Nodes.Add(new Node { Id = Guid.NewGuid(), Children = new List<Node>(
                new[]
                {
                    new Node { Id = Guid.NewGuid(), Status = new NodeStatus { ResetReason = ResetReason.Brownout }},
                    new Node { Id = Guid.NewGuid(), Status = new NodeStatus { ResetReason = ResetReason.Exception, ExceptionMessage = "EXC.CODE1"} },
                })
            });
        }

        public override async Task RenameNode(Node node, Guid newName)
        {
            node.Id = newName;
        }
    }
}
