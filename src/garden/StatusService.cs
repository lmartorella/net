
using System.Runtime.Serialization;
using Lucky.Garden.Services;

namespace Lucky.Garden
{
    [DataContract]
    public class StatusType
    {
        [DataMember(Name = "error")]
        public string Error;

        [DataMember(Name = "status")]
        public StatusCode StatusCode;

        [DataMember(Name = "isRunning")]
        public bool isRunning;

        [DataMember(Name = "config")]
        public ProgramConfig Config;
    }

    [DataContract]
    public class ProgramConfig
    {
        [DataMember(Name = "zones")]
        public string[] Zones;

        [DataMember(Name = "programCycles")]
        public ProgramCycle[] ProgramCycles;
    }

    ///          name: string;
    ///          start: ISO-string;
    ///          startTime: HH:mm:ss;
    ///          suspended: boolean;
    ///          disabled: boolean;
    ///          minutes: number;
    [DataContract]
    public class ProgramCycle
    {

    }

    public enum StatusCode
    {
        Online = 1,
        Offline = 2,
        PartiallyOnline = 3
    }

    public class StatusService
    {
        public StatusService(MqttService mqttService)
        {
            mqttService.JsonPublish("garden/status", Status);
        }

        public StatusType Status
        {
            get
            {
                return new StatusType
                {
                    StatusCode = StatusCode.Offline,
                    Config = new ProgramConfig
                    {
                        ProgramCycles = []
                    }
                };
            }
        }
    }
}
