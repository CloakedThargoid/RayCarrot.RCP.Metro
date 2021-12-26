﻿using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using ControlzEx.Theming;

namespace RayCarrot.RCP.Updater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Constructor

        public App()
        {
            WC = new WebClient();

            // Get temp file names
            LocalTempPath = Path.GetTempFileName();
            ServerTempPath = Path.GetTempFileName();
        }

        #endregion

        #region Event Handlers

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            //
            // Arguments:
            //  - RCP filePath (string)
            //  - Dark mode (bool)
            //  - User level (UserLevel)
            //  - Update URL (string)
            //  - Culture (string)
            //  - Web security protocol (int)
            //

            // Retrieve the arguments
            string[] args = e.Args;

            // Make sure we have 5 arguments
            if (args.Length != 6)
            {
                ShutdownApplication("The number of launch arguments need to be 6");
                return;
            }

            // Attempt to get the file path
            RCPFilePath = args[0];

            // Make sure the file exists
            if (!File.Exists(RCPFilePath))
            {
                ShutdownApplication("The file specified in the first launch argument does not exist");
                return;
            }

            // Get the dark mode value
            bool darkMode = !Boolean.TryParse(args[1], out bool dm) || dm;

            // Set the app theme
            ThemeManager.Current.ChangeTheme(this, $"{(darkMode ? "Dark" : "Light")}.Purple");

            // Get the user level
            CurrentUserLevel = Enum.TryParse(args[2], out UserLevel ule) ? ule : UserLevel.Normal;

            // Get the update URL
            UpdateURL = args[3];

            try
            {
                // Get the culture info
                var ci = new CultureInfo(args[4]);

                // Update the current thread cultures
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.DefaultThreadCurrentCulture = ci;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error setting culture", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Get the web security protocol
            int webSecurityProtocol = Int32.TryParse(args[5], out int s) ? s : 0;

            // Set if not default (0)
            if (webSecurityProtocol != 0)
            {
                try
                {
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)webSecurityProtocol;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error setting web security protocol", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Create and show the main window
            new MainWindow().Show();
        }

        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            ShutdownApplication("Unhandled exception", e.Exception);
            WC?.Dispose();
            ClearTemp();
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            WC?.Dispose();
            ClearTemp();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shuts down the application with an optional exception
        /// </summary>
        /// <param name="debugMessage">A debug message explaining the reason for the shutdown</param>
        /// <param name="ex">The exception which caused the shutdown</param>
        public void ShutdownApplication(string debugMessage, Exception ex = null)
        {
            if (Dispatcher == null)
            {
                Shutdown();
                return;
            }

            Dispatcher.Invoke(() =>
            {
                var win = new ErrorWindow(debugMessage, ex)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                if (win != MainWindow && MainWindow != null)
                {
                    win.Owner = MainWindow;
                    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }

                win.ShowDialog();

                Shutdown();
            });
        }

        /// <summary>
        /// Clears the temporary files for this program
        /// </summary>
        public void ClearTemp()
        {
            if (File.Exists(ServerTempPath))
            {
                try
                {
                    File.Delete(ServerTempPath);
                }
                catch
                {
                    // Ignore exception
                }
            }

            if (File.Exists(LocalTempPath))
            {
                try
                {
                    File.Delete(LocalTempPath);
                }
                catch
                {
                    // Ignore exception
                }
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The current user level
        /// </summary>
        public UserLevel CurrentUserLevel { get; set; }

        /// <summary>
        /// The Rayman Control Panel file path
        /// </summary>
        public string RCPFilePath { get; set; }

        /// <summary>
        /// The temporary path for the server update
        /// </summary>
        public string ServerTempPath { get; }

        /// <summary>
        /// The temporary path for the local program
        /// </summary>
        public string LocalTempPath { get; }

        /// <summary>
        /// The update download URL
        /// </summary>
        public string UpdateURL { get; set; }

        /// <summary>
        /// The web client for downloading the update
        /// </summary>
        public WebClient WC { get; }

        /// <summary>
        /// The current stage in the update process
        /// </summary>
        public UpdateStage Stage { get; set; }

        #endregion

        #region Public Static Properties

        /// <summary>
        /// The current application
        /// </summary>
        public new static App Current => Application.Current as App;

        #endregion
    }
}