using Lucky.Home.Services;
using System;
using System.Threading.Tasks;

namespace Lucky.Home
{
    /// <summary>
    /// Use this attribute to define an application assembly
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ApplicationAttribute : Attribute
    {
        public ApplicationAttribute(Type applicationType)
        {
            ApplicationType = applicationType;
        }

        public Type ApplicationType { get; }
    }

    public interface IApplication : IService
    {
        /// <summary>
        /// Override to implement the start logic
        /// </summary>
        Task Start();
    }
}
