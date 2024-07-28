using ModbusClient = Lucky.Home.Services.ModbusClient;

namespace Lucky.Home.Sofar;

internal class RegistryValues(AddressRange addresses, int modbusNodeId)
{
    private ushort[] Data;

    /// <summary>
    /// Returns false in case of timeout or other Modbus errors.
    /// In night mode, silence logging of timeout errors
    /// </summary>
    public async Task<bool> ReadData(ModbusClient client)
    {
        var data = await client.ReadHoldingRegistries<ushort>(modbusNodeId, addresses.Start, addresses.End - addresses.Start + 1);
        if (data != null)
        {
            Data = data;
            return true;
        }
        else
        {
            return false;
        }
    }

    public ushort GetValueAt(int address)
    {
        return Data[address - addresses.Start];
    }
}
