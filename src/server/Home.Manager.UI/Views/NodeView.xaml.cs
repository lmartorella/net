﻿using System.Windows;
using System.Windows.Controls;
using Lucky.Home.Models;
using Lucky.Home.Services;
using Lucky.Home.Simulator;

namespace Lucky.Home.Views
{
    public partial class NodeView
    {
        public NodeView()
        {
            InitializeComponent();

            DataContext = this;
        }

        internal void Init(NodeBase node)
        {
            var sinkManager = Manager.GetService<MockSinkManager>();
            foreach (var sink in node.Sinks)
            {
                if (sink is UIElement)
                {
                    TabItem tabItem = new TabItem { Content = sink, Header = sinkManager.GetDisplayName(sink) };
                    TabControl.Items.Add(tabItem);
                }

                if (sink is SystemSinkView)
                {
                    ((SystemSinkView)sink).AddSlaveCommand = new UiCommand(() =>
                    {
                        var slaveNode = Manager.GetService<SimulatorNodesService>().CreateNewSlaveNode((MasterNode)node, Manager.GetService<MockSinkManager>().GetAllSinks());
                        AddSlaveView(slaveNode);
                    });
                }
            }

            if (node is MasterNode)
            {
                foreach (var slave in ((MasterNode)node).Children)
                {
                    AddSlaveView(slave);
                }
            }
        }

        private void AddSlaveView(SlaveNode slaveNode)
        {
            var slaveView = new NodeView();
            slaveView.Init(slaveNode);
            TabItem tabItem2 = new TabItem { Content = slaveView, Header = "Slave Node" };
            TabControl.Items.Add(tabItem2);
        }
    }
}
