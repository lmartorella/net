namespace Lucky.Home.Solar;

public static class Constants
{
    /// <summary>
    /// JSON data published by the inverter, <see cref="PowerData"/>
    /// </summary>
    public const string SolarDataTopicId = "solar/data";

    /// <summary>
    /// String enum published by the inverter, <see cref="DeviceState"/>
    /// </summary>
    public const string SolarStateTopicId = "solar/state";

    /// <summary>
    /// Double value as string published by the current sensor, in Ampere RMS, home usage
    /// </summary>
    public const string CurrentSensorHomeDataTopicId = "currentSensor/home";

    /// <summary>
    /// Double value as string published by the current sensor, in Ampere RMS, exported to grid
    /// </summary>
    public const string CurrentSensorExportDataTopicId = "currentSensor/export";

    /// <summary>
    /// String enum published by the inverter, <see cref="DeviceState"/>
    /// </summary>
    public const string CurrentSensorStateTopicId = "currentSensor/state";
}
