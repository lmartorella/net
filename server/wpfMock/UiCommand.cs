using System;
using System.Windows.Input;

namespace Lucky.HomeMock
{
    public class UiCommand : ICommand
    {
        private readonly Action _action;
        private readonly Func<bool> _canExec;

        public UiCommand(Action action, Func<bool> canExec = null)
        {
            _action = action;
            _canExec = canExec;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExec != null)
            {
                return _canExec();
            }
            return true;
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}