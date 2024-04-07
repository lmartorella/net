
namespace Lucky.Home.Serialization
{
    /// <summary>
    /// Message with a single variable-length string in it
    /// </summary>
    public class DynamicString
    {
        [SerializeAsDynString]
        public string Str;
    }
}