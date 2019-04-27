using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Lucky.Home.Models
{
    /// <summary>
    /// Base class for tree objects
    /// </summary>
    class TreeNode : DependencyObject
    {
        public ObservableCollection<object> Children { get; set; }

        public TreeNode()
        {
            Children = new ObservableCollection<object>();
        }

        public static readonly DependencyProperty NameProperty = DependencyProperty.Register("Name", typeof(string), typeof(UiNode), new PropertyMetadata(default(string)));

        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        internal TreeNode Parent { get; set; }

        protected TreeNode Root
        {
            get
            {
                return Parent != null ? Parent.Root : this;
            }
        }

        public void RaiseSelectionChanged()
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler SelectionChanged;
    }
}