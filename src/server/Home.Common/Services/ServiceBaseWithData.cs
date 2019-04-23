namespace Lucky.Home.Services
{
    /// <summary>
    /// Base class for singleton services that have a persisted state
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
                    _state = Manager.GetService<IIsolatedStorageService>().GetState<T>(LogName);
                }
                return _state;
            }
            set
            {
                _state = value;
                Manager.GetService<IIsolatedStorageService>().SetState(LogName, value);
            }
        }
    }
}