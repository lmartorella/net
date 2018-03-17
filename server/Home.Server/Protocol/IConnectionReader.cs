namespace Lucky.Home.Protocol
{
    public interface IConnectionReader
    {
        T Read<T>();
        byte[] ReadBytes(int byteCount);
    }
}