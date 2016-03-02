namespace Lucky.Home.Protocol
{
    public interface IConnectionWriter
    {
        void Write<T>(T data);
        void WriteBytes(byte[] bytes);
    }
}