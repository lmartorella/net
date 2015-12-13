using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Lucky.Home
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register(
            "Connection", typeof (Connection), typeof (MainWindow), new PropertyMetadata(default(Connection)));

        public Connection Connection
        {
            get { return (Connection) GetValue(ConnectionProperty); }
            set { SetValue(ConnectionProperty, value); }
        }

        public static readonly DependencyProperty RenameCommandProperty = DependencyProperty.Register(
            "RenameCommand", typeof(UiCommand), typeof(MainWindow), new PropertyMetadata(default(ICommand)));

        private UiNode _inEditItem;

        public UiCommand RenameCommand
        {
            get { return (UiCommand)GetValue(RenameCommandProperty); }
            set { SetValue(RenameCommandProperty, value); }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            RenameCommand = new UiCommand(() =>
            {
                UiNode node = TreeView.SelectedItem as UiNode;
                if (node != null)
                {
                    _inEditItem = node;
                    node.InRename = true;
                    RenameCommand.RaiseCanExecuteChanged();
                }
            }, () =>
            {
                UiNode node = TreeView.SelectedItem as UiNode;
                return node != null && !node.InRename;
            });

            Connection = new TcpConnection();

            //Connection = new SampleData1();
        }

        private void RenameBoxKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    EndRename(true);
                    break;
                case Key.Escape:
                    EndRename(false);
                    break;
            }
        }

        private void RenameBoxLostFocus(object sender, RoutedEventArgs e)
        {
            EndRename(true);
        }

        private async void EndRename(bool commit)
        {
            if (_inEditItem != null)
            {
                // Rename the node
                if (commit)
                {
                    Guid newId;
                    var inEditItem = _inEditItem;
                    _inEditItem = null;
                    inEditItem.InRename = false;
                    RenameCommand.RaiseCanExecuteChanged();
                    if (Guid.TryParse(inEditItem.Name, out newId))
                    {
                        inEditItem.Name = "Renaming...";
                        if (await Connection.RenameNode(inEditItem.Node, newId))
                        {
                            inEditItem.Name = newId.ToString();
                        }
                        else
                        {
                            inEditItem.Name = "Error in connection!";
                            await Task.Delay(TimeSpan.FromSeconds(2));
                            inEditItem.Name = inEditItem.Node.Id.ToString();
                        }
                    }
                    else
                    {
                        inEditItem.Name = "Invalid GUID!";
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        inEditItem.Name = inEditItem.Node.Id.ToString();
                    }
                }
            }
        }

        private void HandleSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            RenameCommand.RaiseCanExecuteChanged();
        }

        private void Node_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var frameworkElement = ((FrameworkElement)sender);
            var cm = frameworkElement.ContextMenu = new ContextMenu();
            var node = (UiNode)frameworkElement.DataContext;

            if (node.Node.Sinks.Contains("SWAR"))
            {
                // Read switch array    
                var menuItem = new MenuItem { Header = "Read Switches" };
                menuItem.Click += (o, args) =>
                {
                    Console.WriteLine("uno");
                };
                cm.Items.Add(menuItem);
            }
        }
    }
}
