using Lucky.Home.Serialization;
using System.Threading.Tasks;

namespace Lucky.Home.Sinks
{
    [SinkId("TCMD")]
    class MockCommandSink : SinkBase
    {
        public string ReadCommand()
        {
            string command = null;
            Read(reader =>
            {
                command = reader.Read<DynamicString>()?.Str;
            });
            return command;
        }

        public void WriteResponse(string response)
        {
            Write(writer =>
            {
                writer.Write(new DynamicString() { Str = response });
            });
        }
    }
}
