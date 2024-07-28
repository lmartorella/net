﻿namespace Lucky.Home.Solar;

/// <summary>
/// CSV for tick-by-tick power data
/// </summary>
public class PowerData : TimeSample
{
    [Csv("0")]
    public double PowerW { get; set; }
    [Csv("0")]
    public double TotalEnergyKWh { get; set; }

    public InverterState InverterState = new InverterState(OperatingState.Normal);

    [Csv("", Name = "InverterState")]
    public string InverterStateStr
    {
        get
        {
            return InverterState.ToCsv();
        }
        set
        {
            InverterState = InverterState.FromCsv(value);
        }
    }

    [Csv("0")]
    public double EnergyTodayWh { get; set; }
    [Csv("0.00")]
    public double GridCurrentA { get; set; }
    [Csv("0.0")]
    public double GridVoltageV { get; set; }
    [Csv("0.00")]
    public double GridFrequencyHz { get; set; }
    [Csv("0.00")]
    public double String1CurrentA { get; set; }
    [Csv("0.00")]
    public double String1VoltageV { get; set; }
    [Csv("0.00")]
    public double String2CurrentA { get; set; }
    [Csv("0.00")]
    public double String2VoltageV { get; set; }
    // Home usage current, to calculate Net Energy Metering
    [Csv("0.00")]
    public double HomeUsageCurrentA { get; set; }
    // Measured Inverted produced current at the meter, to calibrate HomeUsageCurrentA
    [Csv("0.00")]
    public double GridCurrentA2 { get; set; }

    /// <summary>
    /// For old CSV formats
    /// </summary>
    [Csv("0.00", OnlyForParsing = true)]
    public int Fault { get; set; }
}
