﻿using Lucky.Home.Services;
using System.Windows;

namespace Lucky.Home
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Manager.Register<JsonIsolatedStorageService, IIsolatedStorageService>();
            Manager.Register<GuiConfigurationService, IConfigurationService>();
            Manager.Register<GuiLoggerFactory, ILoggerFactory>();

            Manager.GetService<IIsolatedStorageService>().InitAppRoot("Manager.UI");
            Manager.GetService<ILoggerFactory>().Create("App").Log("Started");
        }

        private class GuiConfigurationService : IConfigurationService
        {
            public void Dispose()
            {
            }

            public string GetConfig(string key)
            {
                return null;
            }
        }
    }
}