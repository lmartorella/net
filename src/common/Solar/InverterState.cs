﻿using System.Reflection;
using System.Runtime.Serialization;

namespace Lucky.Home.Solar;

public enum OperatingState
{
    [CsvValue("WAIT")]
    Waiting = 0,
    [CsvValue("CHK")]
    Checking = 1,
    [CsvValue("")]
    Normal = 2,
    [CsvValue("FAULT")]
    Fault = 3,
    [CsvValue("PERM_FAULT")]
    PermanentFault = 4,

    FirstUnknownValue = 5,

    [CsvValue("UNKNOWN")]
    Unknown = -1
}

/// <summary>
/// Known inverter states. Unknown state will be logged with original flags
/// </summary>
[DataContract]
public class InverterState
{
    private static Dictionary<OperatingState, string> s_valueToCsvValue = new Dictionary<OperatingState, string>();
    private static Dictionary<string, OperatingState> s_csvValueToValue = new Dictionary<string, OperatingState>();
    static InverterState()
    {
        var memberInfos = typeof(OperatingState).GetMembers();
        foreach (var info in memberInfos)
        {
            var valueAttribute = info.GetCustomAttribute<CsvValueAttribute>();
            if (valueAttribute != null)
            {
                s_valueToCsvValue[Enum.Parse<OperatingState>(info.Name)] = valueAttribute.Value;
                s_csvValueToValue[valueAttribute.Value] = Enum.Parse<OperatingState>(info.Name);
            }
        }
    }

    public InverterState()
    {

    }

    public InverterState(OperatingState operatingState)
    {
        OperatingState = operatingState;
    }

    public InverterState(OperatingState operatingState, string faultCode)
    {
        OperatingState = operatingState;
        FaultCode = faultCode;
    }

    [DataMember]
    public OperatingState OperatingState { get; set; }
    [DataMember]
    public string FaultCode { get; set; }

    internal string ToCsv()
    {
        string value;
        if (!s_valueToCsvValue.TryGetValue(OperatingState, out value))
        {
            value = "UNKNOWN";
        }
        if (OperatingState == OperatingState.Normal || !IsFault)
        {
            return value;
        }
        else
        {
            return value + ":" + FaultCode;
        }
    }

    public InverterState FromCsv(string value)
    {
        var parts = value.Split(":");
        OperatingState enumValue;
        if (!s_csvValueToValue.TryGetValue(parts[0], out enumValue))
        {
            enumValue = OperatingState.Unknown;
        }
        if (parts.Length == 2)
        {
            return new InverterState(enumValue, parts[1]);
        }
        else
        {
            return new InverterState(enumValue);
        }
    }

    public string ToUserInterface(NightState inverterNightState) 
    {
        if (inverterNightState == NightState.Night)
        {
            return "Off";
        }
        else
        {
            return OperatingState.ToString();
        }
    }

    public bool IsFault
    {
        get
        {
            return !string.IsNullOrEmpty(FaultCode);
        }
    }

    /// <summary>
    /// Return a fault representation that won't change until the fault changes or it resets
    /// </summary>
    public string IsFaultToNotify()
    {
        return IsFault ? FaultCode : null;
    }
}
