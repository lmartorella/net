using System;
using System.Net;
using System.Threading.Tasks;
using Lucky.Home.Core.Protocol;

namespace Lucky.Home.Core
{
    interface INodeRegistrar : IService
    {
        /// <summary>
        /// A new node with name logs in
        /// </summary>
        INode LoginNode(Guid guid, IPAddress address);

        /// <summary>
        /// A unnamed node wants to log in
        /// </summary>
        /// <param name="address">Node address</param>
        /// <returns>After the successful registration protocol completion</returns>
        Task<INode> RegisterBlankNode(IPAddress address);
    }
}
