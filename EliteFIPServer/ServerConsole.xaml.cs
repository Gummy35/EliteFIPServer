
using EliteFIPServer.Logging;
using ExoScan.StellarStructs;
using NLog;
using NLog.Config;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace EliteFIPServer
{
    /// <summary>
    /// Interaction logic for ServerConsole.xaml
    /// </summary>
    public partial class ServerConsole : Window
    {


        private CoreServer ServerCore;

        private delegate void ImageSafeCallDelegate(Image target, bool newstate);
        private delegate void ButtonSafeCallDelegate(Button target, bool newstate);

        private bool EDCPActive = false;
        public ExoViewModel ExoView { get; set; } = new();


        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private NLog.Targets.MethodCallTarget _nlogMemoryTarget;

        private int maxRange = 500;
        public int MaxRange
        {
            get => maxRange;
            set
            {
                if (maxRange != value)
                {
                    maxRange = value;
                    UpdateSystems(ExoView.SelectedSystem?.SystemName);
                }
            }
        }

        public ServerConsole()
        {
            InitializeComponent();
            tbRange.DataContext = this;
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
            ServerCore.EDCPClient.exoDataChanged += HandleExoDataChange;
            ServerCore.EDCPClient.locationChanged += HandleLocationChange;
            ServerCore.ExoView = ExoView;
            DataContext = ExoView;

            ServerCore.EDCPClient.Start();
            ServerCore.Start();
        }

        public void UpdateSystems(string currentSystemName, int maxRange)
        {
            var systems = ServerCore.EliteAPIIntegration.GetSystems(ExobiologyData.ReferencePosition, maxRange);
            ExoView.SetSystems(systems);
            if (!string.IsNullOrEmpty(currentSystemName))
            {
                var existing = ExoView.Systems.FirstOrDefault(x => x.SystemName == currentSystemName);

                ExoView.SelectedSystem = existing;
                ExoView.RefreshSelectedSystem();
            }
        }

        private void UpdateSystems(string currentSystemName)
        {
            UpdateSystems(currentSystemName, maxRange);
        }

        private void HandleLocationChange(object sender, LocationData e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ExobiologyData.ReferencePosition = new Position(e.StarPos);
                UpdateSystems(e.SystemName);
            }));
        }



        private void HandleExoDataChange(object sender, ExobiologyData e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var existing = ExoView.Systems.FirstOrDefault(x => x.SystemName == e.SystemName);

                if (existing != null)
                {
                    existing.Timestamp = e.Timestamp;
                    existing.LastBodyId = e.LastBodyId;
                    existing.LastBodyName = e.LastBodyName;
                    existing.Scans = e.Scans;
                }
                else
                {
                    ExoView.Systems.Add(e);
                    existing = e;
                }

                //exoView.SelectedSystem = existing;
                ExoView.RefreshSelectedSystem();

                var exoViewBody = ExoView.Bodies.FirstOrDefault(b => b.BodyId == e.LastBodyId);
                if (exoViewBody != null)
                {
                    var tvi = BodiesTreeView.ItemContainerGenerator.ContainerFromItem(exoViewBody) as TreeViewItem;
                    if (tvi != null)
                    {
                        tvi.IsSelected = true;
                        tvi.BringIntoView();
                        tvi.Focus();
                    }
                }
            }));
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

        private void HandleCoreServerStateChange(object sender, RunState newState)
        {
            Dispatcher.Invoke(new Action(() => setStatusImage(imgCoreServerStatus, newState)));
        }


        public void HandleEDCPStateChange(object sender, RunState newState)
        {
            Dispatcher.Invoke(new Action(() => setStatusImage(imgEDCPStatus, newState)));
            Dispatcher.Invoke(new Action(() => setButtonText(cmdEDCP, newState)));
            EDCPActive = newState == RunState.Started ? true : false;
        }

        public void updateInfoText(string newInfoText)
        {
            Dispatcher.Invoke(new Action(() => setInfoText(newInfoText)));
        }

        private void setInfoText(string newInfoText)
        {
            txtInfoText.Text = newInfoText;
        }

        private void setButtonText(Button target, RunState newState)
        {

            switch (newState)
            {
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

        private void setStatusImage(Image target, RunState newState)
        {

            switch (newState)
            {
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ServerCore.EliteAPIIntegration.ImportAll();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //ServerCore.EliteAPIIntegration.GetSystems();
        }

    }
}
