using FluentModbus;
using Lucky.Home.Services.FluentModbus;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Net;

namespace Lucky.Home.Services;

public class ModbusClient(ILogger<ModbusClientFactory> logger, string deviceHostName, ModbusEndianness endianness)
{
    private readonly ModbusTcpClient client = new ModbusTcpClient();
    private bool _connecting;
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);

    public ModbusEndianness Endianness => endianness;

    private async Task StartConnect()
    {
        if (_connecting)
        {
            return;
        }
        _connecting = true;

        try
        {
            IPAddress address = null;
            try
            {
                address = (await Dns.GetHostEntryAsync(deviceHostName)).AddressList.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).FirstOrDefault();
            }
            catch (Exception err)
            {
                logger.LogInformation("DnsErr: resolving {0}, err {1}", deviceHostName, err.Message);
                return;
            }

            if (address == null)
            {
                logger.LogInformation("DnsErr: resolving {0}, no result", deviceHostName);
                return;
            }

            client.ConnectTimeout = (int)ConnectTimeout.TotalMilliseconds;

            try
            {
                client.Connect(address, endianness);
            }
            catch (Exception err)
            {
                logger.LogInformation("ModbusConnect: connectingTo {0}, err {1}, type {2}", address, err.Message, err.GetType().Name);
                return;
            }
        }
        finally
        {
            _connecting = false;
        }
    }

    public bool CheckConnected()
    {
        // Check TCP MODBUS connection
        if (!client.IsConnected)
        {
            _ = StartConnect();
            return false;
        }
        else
        {
            _connecting = false;
            return true;
        }
    }

    public void Disconnect()
    {
        client.Disconnect();
    }

    private CancellationToken Timeout
    {
        get
        {
            // Use a longer timeout than the one used in the gateway to avoid many disconnections
            return new CancellationTokenSource(2000).Token;
        }
    }

    public async Task<ushort[]> ReadHoldingRegistries(int unitId, int addressStart, int count)
    {
        // Buggy API not passing cancellation token. Switch to ReadHoldingRegistersAsync<T>
        // after https://github.com/Apollo3zehn/FluentModbus/pull/100 merge
        var dataset = SpanExtensions.Cast<byte, ushort>(await client.ReadHoldingRegistersAsync((byte)unitId, (ushort)addressStart, (ushort)count, Timeout));
        if (/*client.SwapBytes*/ true)
        {
            ModbusUtils.SwitchEndianness(dataset);
        }
        return dataset.ToArray();
    }

    public async Task<float[]> ReadHoldingRegistriesFloat(int unitId, int addressStart, int count)
    {
        // Buggy API not passing cancellation token. Switch to ReadHoldingRegistersAsync<T>
        // after https://github.com/Apollo3zehn/FluentModbus/pull/100 merge
        var dataset = SpanExtensions.Cast<byte, float>(await client.ReadHoldingRegistersAsync((byte)unitId, (ushort)addressStart, (ushort)count, Timeout));
        if (/*client.SwapBytes*/ true)
        {
            ModbusUtils.SwitchEndianness(dataset);
        }
        return dataset.ToArray();
    }
}
