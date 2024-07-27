namespace Lucky.Home.Solar;

public static class Constants
{
    /// <summary>
    /// JSON data published by the inverter, <see cref="PowerData"/>
    /// </summary>
    public const string SolarDataTopicId = "solar/data";

    /// <summary>
    /// String enum published by the inverter, <see cref="PollStrategyManager.StateEnum"/>
    /// </summary>
    public const string SolarStateTopicId = "solar/state";
}
