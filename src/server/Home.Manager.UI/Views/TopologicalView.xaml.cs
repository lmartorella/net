using Lucky.Home.Models;
using Lucky.Home.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Lucky.Home.Views
{
    /// <summary>
    /// Interaction logic for TopologicalView.xaml
    /// </summary>
    public partial class TopologicalView
    {
        private UiNode _inEditItem;

        internal static readonly DependencyProperty NodesProperty = DependencyProperty.Register(
            "Nodes", typeof (ObservableCollection<UiNode>), typeof (TopologicalView), new PropertyMetadata(default(ObservableCollection<UiNode>)));

        internal ObservableCollection<UiNode> Nodes
        {
            get { return (ObservableCollection<UiNode>) GetValue(NodesProperty); }
            set { SetValue(NodesProperty, value); }
        }

        public static readonly DependencyProperty RenameCommandProperty = DependencyProperty.Register(
            "RenameCommand", typeof(UiCommand), typeof(TopologicalView), new PropertyMetadata(default(ICommand)));

        public UiCommand RenameCommand
        {
            get { return (UiCommand)GetValue(RenameCommandProperty); }
            set { SetValue(RenameCommandProperty, value); }
        }

        public static readonly DependencyProperty ResetCommandProperty = DependencyProperty.Register(
            "ResetCommand", typeof(UiCommand), typeof(TopologicalView), new PropertyMetadata(default(ICommand)));

        public UiCommand ResetCommand
        {
            get { return (UiCommand)GetValue(ResetCommandProperty); }
            set { SetValue(ResetCommandProperty, value); }
        }

        public TopologicalView()
        {
            InitializeComponent();
            TreeView.DataContext = this;

            RenameCommand = new UiCommand(() =>
            {
                if (SelectedUiNode != null)
                {
                    _inEditItem = SelectedUiNode;
                    _inEditItem.InRename = true;
                    RenameCommand.RaiseCanExecuteChanged();
                }
            }, () => SelectedUiNode != null && !SelectedUiNode.InRename);

            ResetCommand = new UiCommand(async () =>
            {
                if (SelectedUiNode != null)
                {
                    await TcpConnection.ResetNode(SelectedUiNode.Node);
                }
            }, () => SelectedUiNode != null);
        }

        private SinkNode[] SelectedSinks
        {
            get
            {
                return GetAllNodes<SinkNode>().Where(sn => sn.IsSelected).ToArray();
            }
        }

        private IEnumerable<T> GetAllNodes<T>(TreeNode root = null) where T: TreeNode
        {
            if (root != null)
            {
                return root.Children.OfType<T>().Concat(root.Children.OfType<TreeNode>().SelectMany(GetAllNodes<T>));
            }
            else
            {
                return TcpConnection.Nodes.SelectMany(GetAllNodes<T>);
            }
        }

        private UiNode SelectedUiNode
        {
            get
            {
                return TreeView.SelectedItem as UiNode;
            }
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                RenameCommand.Execute(null);
            }
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

        public static readonly DependencyProperty TcpConnectionProperty = DependencyProperty.Register(
            "TcpConnection", typeof (Connection), typeof (TopologicalView), new PropertyMetadata(null, HandleConnectionChanged));

        private static void HandleConnectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                ((Connection)e.NewValue).NodeSelectionChanged += (o, e1) =>
                {
                    ((TopologicalView) d).UpdateMenuItems();
                };
            }
        }

        internal Connection TcpConnection
        {
            get { return (Connection) GetValue(TcpConnectionProperty); }
            set { SetValue(TcpConnectionProperty, value); }
        }

        private async void EndRename(bool commit)
        {
            if (_inEditItem != null)
            {
                // Rename the node
                if (commit)
                {
                    NodeId newId;
                    var inEditItem = _inEditItem;
                    _inEditItem = null;
                    inEditItem.InRename = false;
                    RenameCommand.RaiseCanExecuteChanged();
                    if (NodeId.TryParse(inEditItem.Name, out newId))
                    {
                        inEditItem.Name = "Renaming...";
                        if (await TcpConnection.RenameNode(inEditItem.Node, newId))
                        {
                            inEditItem.Name = newId.ToString();
                        }
                        else
                        {
                            inEditItem.Name = "Error in connection!";
                            await Task.Delay(TimeSpan.FromSeconds(2));
                            inEditItem.Name = inEditItem.Node.NodeId.ToString();
                        }
                    }
                    else
                    {
                        inEditItem.Name = "Invalid GUID!";
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        inEditItem.Name = inEditItem.Node.NodeId.ToString();
                    }
                }
            }
        }

        private void HandleSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            UpdateMenuItems();
        }

        private void UpdateMenuItems()
        {
            RenameCommand.RaiseCanExecuteChanged();
        }

        private object[] ParseArguments(string argumentStr, Type[] types)
        {
            if (string.IsNullOrEmpty(argumentStr))
            {
                return new object[0];
            }
            else
            {
                return argumentStr.Split(',').Select((str, i) => Convert.ChangeType(str, types[i])).ToArray();
            }
        }
    }
}
