
using EliteFIPServer.Logging;
using NLog;
using NLog.Config;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace EliteFIPServer {
    /// <summary>
    /// Interaction logic for ServerConsole.xaml
    /// </summary>
    public partial class ServerConsole : Window {

        private CoreServer ServerCore;

        private delegate void ImageSafeCallDelegate(Image target, bool newstate);
        private delegate void ButtonSafeCallDelegate(Button target, bool newstate);

        private bool EDCPActive = false;

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private NLog.Targets.MethodCallTarget _nlogMemoryTarget;

        public ServerConsole() {
            InitializeComponent();
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            txtVersion.Text = version.ToString();

            Log.LogEnabled(Properties.Settings.Default.EnableLog);

            _nlogMemoryTarget = new NLog.Targets.MethodCallTarget("LogTextbox", LogText);
            var _nlogMemoryRule = new LoggingRule("*", LogLevel.Trace, _nlogMemoryTarget);
            LogManager.Configuration.AddTarget("logTextBoxTarget", _nlogMemoryTarget);
            LogManager.Configuration.LoggingRules.Add(_nlogMemoryRule);
            LogManager.ReconfigExistingLoggers();

            chkEnableLog.IsChecked = Properties.Settings.Default.EnableLog;

            ServerCore = new CoreServer(this);
            ServerCore.CurrentState.onStateChange += HandleCoreServerStateChange;
            ServerCore.EDCPClient.CurrentState.onStateChange += HandleEDCPStateChange;

            ServerCore.EDCPClient.Start();
            ServerCore.Start();

        }

        private void LogText(LogEventInfo info, object[] arg2)
        {
            Dispatcher.BeginInvoke(new Action(() => LogTextbox.AppendText($"{info.Message}\n")));
        }

        private void LogText(object sender, string message)
        {
        }


        private void CmdEDCP_onClick(object sender, RoutedEventArgs e)
        {
            if (EDCPActive)
            {
                ServerCore.EDCPClient.Stop();
            }
            else
            {
                ServerCore.EDCPClient.Start();
            }
        }

        private void HandleCoreServerStateChange(object sender, RunState newState) {
            Dispatcher.Invoke(new Action(() => setStatusImage(imgCoreServerStatus, newState)));
        }


        public void HandleEDCPStateChange(object sender, RunState newState)
        {
            Dispatcher.Invoke(new Action(() => setStatusImage(imgEDCPStatus, newState)));
            Dispatcher.Invoke(new Action(() => setButtonText(cmdEDCP, newState)));
            EDCPActive = newState == RunState.Started ? true : false;
        }

        public void updateInfoText(string newInfoText) {
            Dispatcher.Invoke(new Action(() => setInfoText(newInfoText)));
        }

        private void setInfoText(string newInfoText) {
            txtInfoText.Text = newInfoText;
        }

        private void setButtonText(Button target, RunState newState) {
            
            switch (newState) {
                case RunState.Stopped:
                    target.Content = "Start";
                    target.IsEnabled = true;
                    break;

                case RunState.Starting:
                    target.Content = "Starting...";
                    target.IsEnabled = false;
                    break;

                case RunState.Started:
                    target.Content = "Stop";
                    target.IsEnabled = true;
                    break;

                case RunState.Stopping:
                    target.Content = "Stopping...";
                    target.IsEnabled = false;
                    break;
            }
        }

        private void setStatusImage(Image target, RunState newState) {

            switch (newState) {
                case RunState.Stopped:
                    target.Source = new BitmapImage(new Uri("pack://application:,,,/Images/minus32.png"));
                    break;

                case RunState.Starting:
                case RunState.Stopping:
                    target.Source = new BitmapImage(new Uri("pack://application:,,,/Images/refresh32.png"));
                    break;

                case RunState.Started:
                    target.Source = new BitmapImage(new Uri("pack://application:,,,/Images/yes32.png"));
                    break;
            }
        }

        private void chkEnableLog_Checked(object sender, RoutedEventArgs e)
        {
            Log.Instance.Info("Saving settings");
            Properties.Settings.Default.EnableLog = (bool)chkEnableLog.IsChecked;
            Properties.Settings.Default.Save();
            Log.LogEnabled(Properties.Settings.Default.EnableLog);
        }
    }
}
