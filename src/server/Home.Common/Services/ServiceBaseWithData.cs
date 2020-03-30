using System.Resources;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Base class for singleton services that have a persisted state
    /// </summary>
    public abstract class ServiceBaseWithData<T> : ServiceBase where T : class, new()
    {
        private T _state;

        protected ServiceBaseWithData(bool verboseLog = false)
            :base(verboseLog)
        { }

        /// <summary>
        /// Serializable state
        /// </summary>
        public T State
        {
            get
            {
                if (_state == null)
                {
                    _state = Manager.GetService<IIsolatedStorageService>().GetState<T>(LogName, () => DefaultState);
                }
                return _state;
            }
            set
            {
                _state = value;
                Save();
            }
        }

        protected virtual T DefaultState
        {
            get
            {
                return new T();
            }
        }

        protected void Save()
        {
            Manager.GetService<IIsolatedStorageService>().SetState(LogName, State);
        }
    }
}