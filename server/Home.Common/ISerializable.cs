namespace Lucky
{
    public interface ISerializable
    {
        byte[] Serialize();

        void Deserialize(byte[] data);

        int DataSize { get; }
    }
}
