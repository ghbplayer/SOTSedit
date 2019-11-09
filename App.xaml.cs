using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace SOTSEdit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App() :base()
        {
            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
            return;
        }
        
        void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if(!alerted)
            {
                MessageBox.Show(e.Exception.Message, "Exit", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                alerted = true;
                Shutdown(1);
            }
            // Prevent default unhandled exception processing
            e.Handled = true;
        }

        bool alerted = false;

    }
}
