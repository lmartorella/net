using FluentModbus;
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

    /// <summary>
    /// Wraps ModbusClient.ReadHoldingRegistries and catch communication exceptions and return null
    /// </summary>
    public async Task<T[]> ReadHoldingRegistries<T>(int unitId, int addressStart, int count) where T : unmanaged
    {
        try
        {
            var dataset = await client.ReadHoldingRegistersAsync<T>(unitId, addressStart, count, Timeout);
            return dataset.ToArray();
        }
        catch (OperationCanceledException exc)
        {
            // Bridge gone down?
            logger.LogError(exc, "canceled");
            return null;
        }
        catch (ModbusException exc)
        {
            if (exc.ExceptionCode != ModbusExceptionCode.GatewayTargetDeviceFailedToRespond)
            {
                logger.LogError(exc, "ModbusExc");
            }
            // The bridge RTU-to-TCP responded with some error that is not managed, so it is alive
            // Even the RTU timeout is managed by the gateway and translated to a modbus error
            return null;
        }
    }
}
