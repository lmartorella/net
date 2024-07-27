using FluentModbus;
using Microsoft.Extensions.Logging;

namespace Lucky.Home.Services;

public class ModbusClientFactory(ILogger<ModbusClientFactory> logger)
{
    private readonly Dictionary<string, ModbusClient> clients = new Dictionary<string, ModbusClient>();

    public ModbusClient Get(string deviceHostName, ModbusEndianness endianness)
    {
        ModbusClient client;
        if (!clients.TryGetValue(deviceHostName, out client))
        {
            client = new ModbusClient(logger, deviceHostName, endianness);
            clients[deviceHostName] = client;
        }
        else
        {
            if (client.Endianness != endianness)
            {
                throw new InvalidOperationException("Endianness cannot be changed");
            }
        }
        return client;
    }
}
