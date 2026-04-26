namespace Meshfrantic.Models;

public class NodeTelemetry
{
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    // Device metrics
    public uint? BatteryLevel { get; set; }
    public float? Voltage { get; set; }
    public float? ChannelUtilization { get; set; }
    public float? AirUtilTx { get; set; }
    public uint? UptimeSeconds { get; set; }

    // Environment metrics
    public float? Temperature { get; set; }
    public float? RelativeHumidity { get; set; }
    public float? BarometricPressure { get; set; }
    public float? WindSpeed { get; set; }
    public uint? WindDirection { get; set; }
    public float? Lux { get; set; }
}
