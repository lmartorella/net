using System.Collections.ObjectModel;

namespace Lucky.Home
{
    public abstract class Connection : ViewModelBase
    {
        private string _statusText;
        private ObservableCollection<UiNode> _nodes;

        protected Connection()
        {
            Nodes = new ObservableCollection<UiNode>();
        }

        public string StatusText
        {
            get
            {
                return _statusText;
            }
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
        
        public ObservableCollection<UiNode> Nodes
        {
            get
            {
                return _nodes;
            }
            set
            {
                _nodes = value;
                OnPropertyChanged();
            }
        }
    }
}
