using System.IO;

namespace Lucky.Home.Simulator
{
    public interface ISinkMock
    {
        void Init(ISimulatedNode node);
        void Read(BinaryReader reader);
        void Write(BinaryWriter writer);
    }
}