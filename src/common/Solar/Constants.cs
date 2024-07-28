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
    /// JSON values published by the current sensor, in Ampere RMS, home usage and export
    /// </summary>
    public const string CurrentSensorDataTopicId = "currentSensor/data";

    /// <summary>
    /// String enum published by the inverter, <see cref="DeviceState"/>
    /// </summary>
    public const string CurrentSensorStateTopicId = "currentSensor/state";
}
