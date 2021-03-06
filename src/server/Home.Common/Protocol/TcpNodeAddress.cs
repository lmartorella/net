using System;
using System.Net;
using System.Text.RegularExpressions;

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// A TCP address
    /// </summary>
    internal class TcpNodeAddress : IEquatable<TcpNodeAddress>
    {
        /// <summary>
        /// The remote end-point IP address of the node or its master (in case of non-eth subnode)
        /// </summary>
        public readonly IPAddress Address;

        /// <summary>
        /// The remote end-point IP port of the node or its master (in case of non-eth subnode)
        /// </summary>
        public readonly ushort ControlPort;

        /// <summary>
        /// The child subindex. 0 means the node itself (IP), >= 1 are child sub-indexes
        /// </summary>
        public readonly int Index;

        public TcpNodeAddress(IPAddress address, ushort controlPort, int index)
        {
            Address = address;
            ControlPort = controlPort;
            Index = index;
        }

        public bool IsSubNode
        {
            get
            {
                return Index > 0;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals((TcpNodeAddress)obj);
        }

        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }

        public bool Equals(TcpNodeAddress other)
        {
            return other.Address.Equals(Address) && ControlPort == other.ControlPort && Index == other.Index;
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}[{2}]", Address, ControlPort, Index);
        }

        public TcpNodeAddress Clone()
        {
            return new TcpNodeAddress(Address, ControlPort, Index);
        }

        public TcpNodeAddress SubNode(int index)
        {
            return new TcpNodeAddress(Address, ControlPort, index);
        }

        public IPEndPoint IPEndPoint
        {
            get
            {
                return new IPEndPoint(Address, ControlPort);
            }
        }

        public static TcpNodeAddress Parse(string nodeAddress)
        {
            var groups = new Regex("(?<address>.*):(?<port>.*)\\[(?<index>.*)\\]").Match(nodeAddress).Groups;
            return new TcpNodeAddress(IPAddress.Parse(groups["address"].Value), ushort.Parse(groups["port"].Value), int.Parse(groups["index"].Value));
        }
    }
}