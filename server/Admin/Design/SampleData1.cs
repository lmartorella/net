using System;
using Lucky.Home.Admin;
using Lucky.Home.Devices;

namespace Lucky.Home.Design
{
    public class SampleData1 : Connection
    {
        public SampleData1()
        {
            StatusText = "Connected";
            var root = new Node { Id = Guid.NewGuid(), Children = new[]
                {
                    new Node { Id = Guid.NewGuid(), Status = new NodeStatus { ResetReason = ResetReason.Brownout }},
                    new Node { Id = Guid.NewGuid(), Status = new NodeStatus { ResetReason = ResetReason.Exception, ExceptionMessage = "EXC.CODE1"} },
                }
            };
            Nodes.Add(new UiNode(root, null));

            Devices.Add(new UiDevice(new DeviceDescriptor { DeviceType = "Type"}));
        }
    }
}
