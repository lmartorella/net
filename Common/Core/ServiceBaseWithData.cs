namespace Lucky.Home.Core
{
    public abstract class ServiceBaseWithData<T> : ServiceBase where T : class, new()
    {
        private T _state;

        protected ServiceBaseWithData(string logName)
            : base(logName)
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
                    _state = Manager.GetService<IPersistenceService>().GetState<T>(LogName);
                }
                return _state;
            }
            set
            {
                _state = value;
                Manager.GetService<IPersistenceService>().SetState<T>(LogName, value);
            }
        }
    }
}