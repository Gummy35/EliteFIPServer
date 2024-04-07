using EliteFIPProtocol;
using EliteFIPServer.Logging;
using Matric.Integration;
using Newtonsoft.Json;
using System.IO;

namespace EliteFIPServer {
    public class MatricApiClient {

        public ComponentState CurrentState { get; private set; } = new ComponentState();

        public List<ClientInfo> ConnectedClients = new List<ClientInfo>();
        private Dictionary<string, MatricButton> MatricButtonList;

        private string AppName = "Elite FIP Server";
        private string CLIENT_ID;
        private Matric.Integration.Matric matric;
        
        private static DateTime lastEventUpdate = DateTime.UtcNow;


        // Matric Flash Worker
        private CancellationTokenSource MatricFlashWorkerCTS;
        private Task MatricFlashWorkerTask;

        // Data variables
        private double fuelCapacity = 0;
        private double fuelCurrent = 0;
        private double fuelReserve = 0;        
        private double fuelReserveMax = 0;
        private string commander = "";
        private string currentShipId = "";

        private bool resetMaxFuel = false;
        private bool resetMaxReservoirFuel = false;

        public MatricApiClient() {
            MatricButtonList = CreateButtonList();
        }

        private static Dictionary<string, MatricButton> CreateButtonList() {
            var buttonlist = new Dictionary<string, MatricButton>();

            // Create Button List 
            // For reference:
            // public MatricButton(string buttonName, string buttonLabel, bool isButton = true, bool isIndicator = true, bool isWarning = true , bool isSwitch = true, bool isSlider = false, bool isText = false, bool isPanel = false, 
            //                     string offText = "Off", string onText = "On", bool buttonState = false, int switchPosition = 1, int sliderPosition = 0)
            var templist = new List<MatricButton> {
                new MatricButton(MatricConstants.DOCKED, "Docked", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.LANDED, "Landed", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.LANDINGGEAR, "Landing Gear", offText: "Landing Gear", onText: "Landing Gear"),
                new MatricButton(MatricConstants.SHIELDS, "Shields", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.SUPERCRUISE, "Supercruise", offText: "Supercruise", onText: "Supercruise"),
                new MatricButton(MatricConstants.FLIGHTASSIST, "Flight Assist", offText: "Flight Assist", onText: "Flight Assist"),
                new MatricButton(MatricConstants.HARDPOINTS, "Hardpoints", offText: "Retracted", onText: "Deployed", updateButtonText: true),
                new MatricButton(MatricConstants.INWING, "Wing", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.LIGHTS, "Lights", offText: "Lights", onText: "Lights"),
                new MatricButton(MatricConstants.CARGOSCOOP, "Cargo Scoop", offText: "Cargo Scoop", onText: "Cargo Scoop"),
                new MatricButton(MatricConstants.SILENTRUNNING, "Silent Running", offText: "Silent Running", onText: "Silent Running"),
                new MatricButton(MatricConstants.SCOOPINGFUEL, "Scooping Fuel", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.SRVHANDBRAKE, "SRV Handbrake", offText: "SRV Handbrake", onText: "SRV Handbrake"),
                new MatricButton(MatricConstants.SRVTURRET, "SRV Turret", offText: "SRV Turret", onText: "SRV Turret"),
                new MatricButton(MatricConstants.SRVUNDERSHIP, "SRV Under Ship", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.SRVDRIVEASSIST, "SRV DriveAssist", offText: "SRV DriveAssist", onText: "SRV DriveAssist"),
                new MatricButton(MatricConstants.FSDMASSLOCK, "Mass Locked", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.FSDCHARGE, "FSD Charging", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.FSDCOOLDOWN, "FSD Cooldown", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.LOWFUEL, "Low Fuel", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.OVERHEAT, "Overheat", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.INDANGER, "Danger", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.INTERDICTION, "Interdiction", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.INMAINSHIP, "In Main Ship", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.INFIGHTER, "In Fighter", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.INSRV, "In SRV", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.HUDMODE, "HUD Mode", offText: "Combat", onText: "Analysis"),
                new MatricButton(MatricConstants.NIGHTVISION, "Night Vision", offText: "Night Vision", onText: "Night Vision"),
                new MatricButton(MatricConstants.FSDJUMP, "FSD Jump", offText: "FSD Jump", onText: "FSD Jump"),
                new MatricButton(MatricConstants.SRVHIGHBEAM, "SRV High Beam", offText: "SRV High Beam", onText: "SRV High Beam"),

                new MatricButton(MatricConstants.ONFOOT, "On Foot", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.INTAXI, "In Taxi", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.INMULTICREW, "In Multicrew", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.ONFOOTINSTATION, "On Foot In Station", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.ONFOOTONPLANET, "On Foot On Planet", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.AIMDOWNSIGHT, "Aim Down Sight", offText: "Sights", onText: "Sights"),
                new MatricButton(MatricConstants.LOWOXYGEN, "Low Oxygen", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.LOWHEALTH, "Low Health", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.COLD, "Cold", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.HOT, "Hot", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.VERYCOLD, "Very Cold", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.VERYHOT, "Very Hot", isButton: false, isSwitch: false),

                new MatricButton(MatricConstants.FUELMAIN, "Main Fuel", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isSlider: true, isText: true),
                new MatricButton(MatricConstants.FUELRESERVOIR, "Fuel Reservoir", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isSlider: true, isText: true),

                new MatricButton(MatricConstants.FUELMAIN2, "Main Fuel 2", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isSlider: true, isText: true),
                new MatricButton(MatricConstants.FUELRESERVOIR2, "Fuel Reservoir 2", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isSlider: true, isText: true),

                new MatricButton(MatricConstants.STATUS, "Status", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.STATUS_LABEL, "Ship Status:", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.STATUS2, "Status", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.STATUS_LABEL2, "Ship Status:", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.STATUS3, "Status", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.STATUS_LABEL3, "Ship Status:", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),

                new MatricButton(MatricConstants.TARGET, "Target", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.TARGET_LABEL, "Target Info:", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.TARGET2, "Target", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.TARGET_LABEL2, "Target Info:", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.TARGET3, "Target", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.TARGET_LABEL3, "Target Info:", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),

                new MatricButton(MatricConstants.LANDING, "Landing", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.LANDING_LABEL, "Landing Info:", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.LANDING2, "Landing 2", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.LANDING_LABEL2, "Landing Info 2:", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),

                new MatricButton(MatricConstants.GAMEINFO, "GameInfo", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),

                new MatricButton(MatricConstants.INFO, "Info", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.INFO_LABEL, "General Info:", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.INFO2, "Info", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.INFO_LABEL2, "General Info:", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.INFO3, "Info", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.INFO_LABEL3, "General Info:", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),

                new MatricButton(MatricConstants.LANDINGPAD, "Landing Pad", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),

                new MatricButton(MatricConstants.TARGETSHIELDVALUE, "Target Shield", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isSlider: true, isText: true),
                new MatricButton(MatricConstants.TARGETHULLVALUE, "Target Hull", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isSlider: true, isText: true),                
                new MatricButton(MatricConstants.TARGETSUBSYSVALUE, "Target Subsystem", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isSlider: true, isText: true),
                new MatricButton(MatricConstants.TARGETSUBSYSNAME, "Target Subsystem Name", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true)
            };

            foreach (MatricButton button in templist) {
                buttonlist.Add(button.ButtonName, button);
            }
            return buttonlist;
        }

        public void Start() {

            Log.Instance.Info("Starting Matric Integration");
            CurrentState.Set(RunState.Starting);

            if (matric == null) {
                try {
                    matric = new Matric.Integration.Matric(AppName, "", Properties.Settings.Default.MatricApiPort);
                    matric.OnConnectedClientsReceived += Matric_OnConnectedClientsReceived;
                    matric.OnError += Matric_OnError;
                } catch (Exception e) {
                    Log.Instance.Info("Matric Exception: {exception}", e.ToString());
                }
            }

            // There is a possible timing window where an exception will occur after a connection attempt, but before we get here.
            // In that event, matric integration will already have been stopped and matric set to null, so we need to guard against this
            // but no further action need be triggered.
            if (matric != null) {
                matric.GetConnectedClients();
            }
        }

        private void CompleteStart() {

            // Start Matric Flash Thread
            Log.Instance.Info("Starting Matric Flash Thread");
            MatricFlashWorkerCTS = new CancellationTokenSource();
            MatricFlashWorkerTask = new Task(new Action(MatricFlashWorkerThread), MatricFlashWorkerCTS.Token);
            MatricFlashWorkerTask.ContinueWith(MatricFlashWorkerThreadEnded);
            MatricFlashWorkerTask.Start();

            // Refesh Button Text Config 
            Log.Instance.Info("Refesh Button Text Config");
            try {
                string jsonButtonTextConfig = File.ReadAllText(Constants.ButtonTextConfigFilename);
                var buttonTextConfigList = JsonConvert.DeserializeObject<List<ButtonTextConfig>>(jsonButtonTextConfig);                
                foreach (ButtonTextConfig buttonConfig in buttonTextConfigList) {
                    if (MatricButtonList.ContainsKey(buttonConfig.ButtonName)) {
                        MatricButtonList[buttonConfig.ButtonName].OffText = buttonConfig.OffText;
                        MatricButtonList[buttonConfig.ButtonName].OnText = buttonConfig.OnText;
                        MatricButtonList[buttonConfig.ButtonName].UpdateButtonText = buttonConfig.UpdateButtonText;
                        Log.Instance.Info("Button updated: {name}, Offtext: {offtext}, Ontext: {Ontext},UpdateButtonText: {updatebuttontext}",
                            MatricButtonList[buttonConfig.ButtonName].ButtonName, MatricButtonList[buttonConfig.ButtonName].OffText, MatricButtonList[buttonConfig.ButtonName].OnText, MatricButtonList[buttonConfig.ButtonName].UpdateButtonText);
                    }
                }
            } catch {
                Log.Instance.Info("Unable to refesh Button Text Config");
            }
            CurrentState.Set(RunState.Started);
        }

        public void Stop() {
            Log.Instance.Info("Stopping Matric Integration");
            if (CurrentState.State == RunState.Started) {
                CurrentState.Set(RunState.Stopping);
                MatricFlashWorkerCTS.Cancel();
                MatricFlashWorkerTask.Wait();
            }
            matric = null;
            CurrentState.Set(RunState.Stopped);
        }

        public void UpdateGameState(GameEventType eventType, Object gameData) {
            // Only update if Matric Integration is running
            if (CurrentState.State == RunState.Started) {
                if (eventType == GameEventType.Status) {
                    StatusData currentStatus = gameData as StatusData;                      
                    UpdateStatus(currentStatus);
                    UpdateInfo(currentStatus);
                    updateLastEventUpdateTimeStamp(currentStatus.LastUpdate);
                } else if (eventType == GameEventType.Target) {
                    ShipTargetedData currentTarget = gameData as ShipTargetedData;                    
                    UpdateTarget(currentTarget);
                    updateLastEventUpdateTimeStamp(currentTarget.LastUpdate);
                } else if (eventType == GameEventType.DockingGranted) {
                    DockingGrantedData currentDockingGranted = gameData as DockingGrantedData;                    
                    UpdateLanding(currentDockingGranted, null, null, null, null);
                    updateLastEventUpdateTimeStamp(currentDockingGranted.Timestamp);
                } else if (eventType == GameEventType.DockingDenied) {
                    DockingDeniedData currentDockingDenied = gameData as DockingDeniedData;                    
                    UpdateLanding(null, currentDockingDenied, null, null, null);
                    updateLastEventUpdateTimeStamp(currentDockingDenied.Timestamp);
                } else if (eventType == GameEventType.DockingTimeout) {
                    DockingTimeoutData currentDockingTimeout = gameData as DockingTimeoutData;
                    UpdateLanding(null, null, currentDockingTimeout, null, null);
                    updateLastEventUpdateTimeStamp(currentDockingTimeout.Timestamp);
                } else if (eventType == GameEventType.DockingCancelled) {
                    DockingCancelledData currentDockingCancelled = gameData as DockingCancelledData;                    
                    UpdateLanding(null, null, null, currentDockingCancelled, null);
                    updateLastEventUpdateTimeStamp(currentDockingCancelled.Timestamp);
                } else if (eventType == GameEventType.Location) {
                    LocationData currentLocation = gameData as LocationData;                    
                    UpdateLanding(null, null, null, null, currentLocation);
                    updateLastEventUpdateTimeStamp(currentLocation.LastUpdate);
                } else if (eventType == GameEventType.LoadGame) {
                    LoadGameData currentLoadGameData = gameData as LoadGameData;                    
                    UpdateMaxFuelData(currentLoadGameData, null, null, null);
                    UpdateGameInfo(currentLoadGameData);
                    updateLastEventUpdateTimeStamp(currentLoadGameData.Timestamp);
                } else if (eventType == GameEventType.Loadout) {
                    LoadoutData currentLoadoutData = gameData as LoadoutData;                    
                    UpdateMaxFuelData(null, currentLoadoutData, null, null);
                    updateLastEventUpdateTimeStamp(currentLoadoutData.Timestamp);
                } else if (eventType == GameEventType.RefuelAll) {
                    RefuelAllData currentRefuelAllData = gameData as RefuelAllData;
                    UpdateMaxFuelData(null, null, currentRefuelAllData, null);
                    updateLastEventUpdateTimeStamp(currentRefuelAllData.Timestamp);
                } else if (eventType == GameEventType.ReservoirReplenished) {
                    ReservoirReplenishedData currentReservoirReplenishedData = gameData as ReservoirReplenishedData;                    
                    UpdateMaxFuelData(null, null, null, currentReservoirReplenishedData);
                    updateLastEventUpdateTimeStamp(currentReservoirReplenishedData.Timestamp);
                } else if (eventType == GameEventType.ShipyardNew) {
                    ShipyardNewData currentShipyardNewData = gameData as ShipyardNewData;                    
                    if (currentShipyardNewData != null && currentShipyardNewData.NewShipId != null && currentShipyardNewData.NewShipId != "" && !currentShipyardNewData.NewShipId.Equals(this.currentShipId) && currentShipyardNewData.Timestamp > lastEventUpdate)
                    {
                        Log.Instance.Info("Found a Ship Change (old id: " + this.currentShipId + ". new id: " + currentShipyardNewData.NewShipId + "), reset Max fuel with next status update");
                        this.currentShipId = currentShipyardNewData.NewShipId; 
                        this.resetMaxFuel = true;
                        this.resetMaxReservoirFuel = true;
                    }
                    updateLastEventUpdateTimeStamp(currentShipyardNewData.Timestamp);
                } else if (eventType == GameEventType.ShipyardSwap) {
                    ShipyardSwapData currentShipyardSwapData = gameData as ShipyardSwapData;                    
                    if (currentShipyardSwapData != null && currentShipyardSwapData.ShipId != null && currentShipyardSwapData.ShipId != "" && !currentShipyardSwapData.ShipId.Equals(this.currentShipId) && currentShipyardSwapData.Timestamp > lastEventUpdate)
                    {
                        Log.Instance.Info("Found a Ship Change (old id: " + this.currentShipId + ". new id: " + currentShipyardSwapData.ShipId + "), reset Max fuel with next status update");
                        this.currentShipId = currentShipyardSwapData.ShipId;                        
                        this.resetMaxFuel = true;
                        this.resetMaxReservoirFuel = true;
                    }
                    updateLastEventUpdateTimeStamp(currentShipyardSwapData.Timestamp);
                }
            }
        }

        public void Matric_OnConnectedClientsReceived(object source, List<ClientInfo> clients) {
            Log.Instance.Info("Matric client list updated");

            // If we get a client list (even empty) from Matric, we know we have connectivity
            if (CurrentState.State == RunState.Starting) {
                CompleteStart();                
            }
            
            ConnectedClients = clients;

            // Matric version 2 supports use of 'null' Client IDs, in which case the updates are set to all Clients. 
            // Previous logic to select first client, and store the ID for reuse is removed in favour of updating all.
            // But we can still log connected clients for info.
            CLIENT_ID = null;
            if (ConnectedClients.Count == 0) {
                Log.Instance.Info("No clients connected");
            } else {
                foreach (ClientInfo clientInfo in ConnectedClients) {
                    Log.Instance.Info("Client name: {name}, IP: {ip}, ID: {id}", clientInfo.Name, clientInfo.IP, clientInfo.Id);
                }
            }
        }

        private void Matric_OnError(Exception ex) {
            Log.Instance.Info("Matric Exception: {message}\r\n{exception}", ex.Message, ex.ToString());
            if (ex is System.Net.Sockets.SocketException) {
                if (ex.HResult == 10054) {
                    System.Threading.Thread.Sleep(Properties.Settings.Default.MatricRetryInterval*1000);
                    matric.GetConnectedClients();
                }
            }
        }

        private void MatricFlashWorkerThread() {
            Log.Instance.Info("Matric Flash Worker Thread started");

            CancellationToken token = MatricFlashWorkerCTS.Token;
            while (token.IsCancellationRequested == false) {
                List<SetButtonsVisualStateArgs> buttons = new List<SetButtonsVisualStateArgs>();
                foreach (MatricButton button in MatricButtonList.Values) {
                    if (button != null && button.IsWarning && button.GameState) {
                        buttons.Add(new SetButtonsVisualStateArgs(null, button.ButtonState ? "off" : "on", MatricConstants.WRN + button.ButtonName));
                        button.ButtonState = !button.ButtonState;
                    }
                }
                if (buttons.Count > 0) {
                    matric.SetButtonsVisualState(CLIENT_ID, buttons);
                }
                Thread.Sleep(500);
            }
            Log.Instance.Info("Matric Flash Worker Thread ending");
        }

        private void MatricFlashWorkerThreadEnded(Task task) {
            if (task.Exception != null) {
                Log.Instance.Info("Matric Flash Worker Thread Exception: {exception}", task.Exception.ToString());
            }
            if (CurrentState.State != RunState.Stopping) {
                Stop();
            }            
            Log.Instance.Info("Matric Flash Worker Thread ended");
        }


        public List<ClientInfo> GetConnectedClients() {
            return ConnectedClients;
        }

        public void UpdateStatus(StatusData currentStatus) {

            if (currentStatus != null) {
                Log.Instance.Info("Setting Matric state using: {gamestate}", System.Text.Json.JsonSerializer.Serialize(currentStatus));

                //Handle Fuel Stati calculation
                //If Reset should be triggered, set Max values based of current values (should show the correct max at that point)
                if (this.resetMaxFuel)
                {
                    this.fuelCapacity = currentStatus.FuelMain;
                    this.fuelReserveMax = currentStatus.FuelReservoir;
                    this.resetMaxFuel = false;
                }
                if (this.resetMaxReservoirFuel)
                {                    
                    this.fuelReserveMax = currentStatus.FuelReservoir;
                    this.resetMaxReservoirFuel = false;
                }

                //Update Fuel data variables
                this.fuelCurrent = currentStatus.FuelMain;
                this.fuelReserve = currentStatus.FuelReservoir;

                Log.Instance.Info("Current Main Fuel Percentage: " + calculatePercentageValue(currentStatus.FuelMain, this.fuelCapacity) + "%");
                Log.Instance.Info("Current Fuel Reservoir Percentage: " + calculatePercentageValue(currentStatus.FuelReservoir, this.fuelReserveMax) + "%");

                // Handle Indicators / Warnings first
                if (MatricButtonList.ContainsKey(MatricConstants.DOCKED)) { MatricButtonList[MatricConstants.DOCKED].GameState = currentStatus.Docked; }
                if (MatricButtonList.ContainsKey(MatricConstants.LANDED)) { MatricButtonList[MatricConstants.LANDED].GameState = currentStatus.Landed; }
                if (MatricButtonList.ContainsKey(MatricConstants.SHIELDS)) { MatricButtonList[MatricConstants.SHIELDS].GameState = !currentStatus.ShieldsUp; }
                if (MatricButtonList.ContainsKey(MatricConstants.INWING)) { MatricButtonList[MatricConstants.INWING].GameState = currentStatus.InWing; }
                if (MatricButtonList.ContainsKey(MatricConstants.SCOOPINGFUEL)) { MatricButtonList[MatricConstants.SCOOPINGFUEL].GameState = currentStatus.ScoopingFuel; }
                if (MatricButtonList.ContainsKey(MatricConstants.SRVUNDERSHIP)) { MatricButtonList[MatricConstants.SRVUNDERSHIP].GameState = currentStatus.SrvUnderShip; }
                if (MatricButtonList.ContainsKey(MatricConstants.FSDMASSLOCK)) { MatricButtonList[MatricConstants.FSDMASSLOCK].GameState = currentStatus.FsdMassLocked; }
                if (MatricButtonList.ContainsKey(MatricConstants.FSDCHARGE)) { MatricButtonList[MatricConstants.FSDCHARGE].GameState = currentStatus.FsdCharging; }
                if (MatricButtonList.ContainsKey(MatricConstants.FSDCOOLDOWN)) { MatricButtonList[MatricConstants.FSDCOOLDOWN].GameState = currentStatus.FsdCooldown; }
                if (MatricButtonList.ContainsKey(MatricConstants.LOWFUEL)) { MatricButtonList[MatricConstants.LOWFUEL].GameState = currentStatus.LowFuel; }
                if (MatricButtonList.ContainsKey(MatricConstants.OVERHEAT)) { MatricButtonList[MatricConstants.OVERHEAT].GameState = currentStatus.Overheating; }
                if (MatricButtonList.ContainsKey(MatricConstants.INDANGER)) { MatricButtonList[MatricConstants.INDANGER].GameState = currentStatus.InDanger; }
                if (MatricButtonList.ContainsKey(MatricConstants.INTERDICTION)) { MatricButtonList[MatricConstants.INTERDICTION].GameState = currentStatus.BeingInterdicted; }
                if (MatricButtonList.ContainsKey(MatricConstants.INMAINSHIP)) { MatricButtonList[MatricConstants.INMAINSHIP].GameState = currentStatus.InMainShip; }
                if (MatricButtonList.ContainsKey(MatricConstants.INFIGHTER)) { MatricButtonList[MatricConstants.INFIGHTER].GameState = currentStatus.InFighter; }
                if (MatricButtonList.ContainsKey(MatricConstants.INSRV)) { MatricButtonList[MatricConstants.INSRV].GameState = currentStatus.InSRV; }

                if (MatricButtonList.ContainsKey(MatricConstants.ONFOOT)) { MatricButtonList[MatricConstants.ONFOOT].GameState = currentStatus.OnFoot; }
                if (MatricButtonList.ContainsKey(MatricConstants.INTAXI)) { MatricButtonList[MatricConstants.INTAXI].GameState = currentStatus.InTaxi; }
                if (MatricButtonList.ContainsKey(MatricConstants.INMULTICREW)) { MatricButtonList[MatricConstants.INMULTICREW].GameState = currentStatus.InMulticrew; }
                if (MatricButtonList.ContainsKey(MatricConstants.ONFOOTINSTATION)) { MatricButtonList[MatricConstants.ONFOOTINSTATION].GameState = currentStatus.OnFootInStation; }
                if (MatricButtonList.ContainsKey(MatricConstants.ONFOOTONPLANET)) { MatricButtonList[MatricConstants.ONFOOTONPLANET].GameState = currentStatus.OnFootOnPlanet; }
                if (MatricButtonList.ContainsKey(MatricConstants.LOWOXYGEN)) { MatricButtonList[MatricConstants.LOWOXYGEN].GameState = currentStatus.LowOxygen; }
                if (MatricButtonList.ContainsKey(MatricConstants.LOWHEALTH)) { MatricButtonList[MatricConstants.LOWHEALTH].GameState = currentStatus.LowHealth; }
                if (MatricButtonList.ContainsKey(MatricConstants.COLD)) { MatricButtonList[MatricConstants.COLD].GameState = currentStatus.Cold; }
                if (MatricButtonList.ContainsKey(MatricConstants.HOT)) { MatricButtonList[MatricConstants.HOT].GameState = currentStatus.Hot; }
                if (MatricButtonList.ContainsKey(MatricConstants.VERYCOLD)) { MatricButtonList[MatricConstants.VERYCOLD].GameState = currentStatus.VeryCold; }
                if (MatricButtonList.ContainsKey(MatricConstants.VERYHOT)) { MatricButtonList[MatricConstants.VERYHOT].GameState = currentStatus.VeryHot; }


                // Buttons and switches need extra TLC
                if (MatricButtonList.ContainsKey(MatricConstants.LANDINGGEAR)) {
                    MatricButtonList[MatricConstants.LANDINGGEAR].GameState = currentStatus.LandingGearDown;
                    MatricButtonList[MatricConstants.LANDINGGEAR].SwitchPosition = currentStatus.LandingGearDown ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.SUPERCRUISE)) {
                    MatricButtonList[MatricConstants.SUPERCRUISE].GameState = currentStatus.Supercruise;
                    MatricButtonList[MatricConstants.SUPERCRUISE].SwitchPosition = currentStatus.Supercruise ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.FLIGHTASSIST)) {
                    MatricButtonList[MatricConstants.FLIGHTASSIST].GameState = currentStatus.FlightAssistOff;
                    MatricButtonList[MatricConstants.FLIGHTASSIST].SwitchPosition = currentStatus.FlightAssistOff ? 0 : 1;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.HARDPOINTS)) {
                    MatricButtonList[MatricConstants.HARDPOINTS].GameState = currentStatus.HardpointsDeployed;
                    MatricButtonList[MatricConstants.HARDPOINTS].SwitchPosition = currentStatus.HardpointsDeployed ? 1 : 0;                    
                }
                if (MatricButtonList.ContainsKey(MatricConstants.LIGHTS)) {
                    MatricButtonList[MatricConstants.LIGHTS].GameState = currentStatus.LightsOn;
                    MatricButtonList[MatricConstants.LIGHTS].SwitchPosition = currentStatus.LightsOn ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.CARGOSCOOP)) {
                    MatricButtonList[MatricConstants.CARGOSCOOP].GameState = currentStatus.CargoScoopDeployed;
                    MatricButtonList[MatricConstants.CARGOSCOOP].SwitchPosition = currentStatus.CargoScoopDeployed ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.SILENTRUNNING)) {
                    MatricButtonList[MatricConstants.SILENTRUNNING].GameState = currentStatus.SilentRunning;
                    MatricButtonList[MatricConstants.SILENTRUNNING].SwitchPosition = currentStatus.SilentRunning ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.SRVHANDBRAKE)) {
                    MatricButtonList[MatricConstants.SRVHANDBRAKE].GameState = currentStatus.SrvHandbrake;
                    MatricButtonList[MatricConstants.SRVHANDBRAKE].SwitchPosition = currentStatus.SrvHandbrake ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.SRVTURRET)) {
                    MatricButtonList[MatricConstants.SRVTURRET].GameState = currentStatus.SrvTurret;
                    MatricButtonList[MatricConstants.SRVTURRET].SwitchPosition = currentStatus.SrvTurret ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.SRVDRIVEASSIST)) {
                    MatricButtonList[MatricConstants.SRVDRIVEASSIST].GameState = currentStatus.SrvDriveAssist;
                    MatricButtonList[MatricConstants.SRVDRIVEASSIST].SwitchPosition = currentStatus.SrvDriveAssist ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.HUDMODE)) {
                    MatricButtonList[MatricConstants.HUDMODE].GameState = currentStatus.HudAnalysisMode;
                    MatricButtonList[MatricConstants.HUDMODE].SwitchPosition = currentStatus.HudAnalysisMode ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.NIGHTVISION)) {
                    MatricButtonList[MatricConstants.NIGHTVISION].GameState = currentStatus.NightVision;
                    MatricButtonList[MatricConstants.NIGHTVISION].SwitchPosition = currentStatus.NightVision ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.FSDJUMP)) {
                    MatricButtonList[MatricConstants.FSDJUMP].GameState = currentStatus.FsdJump;
                    MatricButtonList[MatricConstants.FSDJUMP].SwitchPosition = currentStatus.FsdJump ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.SRVHIGHBEAM)) {
                    MatricButtonList[MatricConstants.SRVHIGHBEAM].GameState = currentStatus.SrvHighBeam;
                    MatricButtonList[MatricConstants.SRVHIGHBEAM].SwitchPosition = currentStatus.SrvHighBeam ? 1 : 0;
                }

                if (MatricButtonList.ContainsKey(MatricConstants.AIMDOWNSIGHT)) {
                    MatricButtonList[MatricConstants.AIMDOWNSIGHT].GameState = currentStatus.AimDownSight;
                    MatricButtonList[MatricConstants.AIMDOWNSIGHT].SwitchPosition = currentStatus.AimDownSight ? 1 : 0;
                }                

                // Handle Sliders and text fields
                if (MatricButtonList.ContainsKey(MatricConstants.FUELMAIN)) {
                    //FormatNumberValues(MatricConstants.FUELMAIN, currentStatus.FuelMain, 1, true);
                    MatricButtonList[MatricConstants.FUELMAIN].SliderPosition = calculatePercentageValue(currentStatus.FuelMain, this.fuelCapacity);                                                            
                }

                if (MatricButtonList.ContainsKey(MatricConstants.FUELRESERVOIR)) {
                    //MatricButtonList[MatricConstants.FUELRESERVOIR].OffText = Math.Round((decimal)currentStatus.FuelReservoir, 2).ToString();
                    MatricButtonList[MatricConstants.FUELRESERVOIR].SliderPosition = calculatePercentageValue(currentStatus.FuelReservoir, this.fuelReserveMax);
                }

                if (MatricButtonList.ContainsKey(MatricConstants.FUELMAIN2))
                {
                    FormatNumberValues(MatricConstants.FUELMAIN2, currentStatus.FuelMain, 1);
                    MatricButtonList[MatricConstants.FUELMAIN2].SliderPosition = calculatePercentageValue(currentStatus.FuelMain, this.fuelCapacity);                    
                }

                if (MatricButtonList.ContainsKey(MatricConstants.FUELRESERVOIR2))
                {
                    //MatricButtonList[MatricConstants.FUELRESERVOIR2].OffText = Math.Round((decimal)currentStatus.FuelReservoir, 2).ToString();
                    MatricButtonList[MatricConstants.FUELRESERVOIR2].SliderPosition = calculatePercentageValue(currentStatus.FuelReservoir, this.fuelReserveMax);
                }

                // Handle Special Text fields
                if (MatricButtonList.ContainsKey(MatricConstants.STATUS_LABEL)) {
                    MatricButtonList[MatricConstants.STATUS_LABEL].GameState = false;
                    MatricButtonList[MatricConstants.STATUS_LABEL].OffText = FormatStatusLabel(currentStatus);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.STATUS)) {
                    MatricButtonList[MatricConstants.STATUS].GameState = false;
                    MatricButtonList[MatricConstants.STATUS].OffText = FormatStatusText(currentStatus, this.fuelCapacity, this.fuelReserveMax);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.STATUS_LABEL2))
                {
                    MatricButtonList[MatricConstants.STATUS_LABEL2].GameState = false;
                    MatricButtonList[MatricConstants.STATUS_LABEL2].OffText = FormatStatusLabel(currentStatus);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.STATUS2))
                {
                    MatricButtonList[MatricConstants.STATUS2].GameState = false;
                    MatricButtonList[MatricConstants.STATUS2].OffText = FormatStatusText(currentStatus, this.fuelCapacity, this.fuelReserveMax);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.STATUS_LABEL3))
                {
                    MatricButtonList[MatricConstants.STATUS_LABEL3].GameState = false;
                    MatricButtonList[MatricConstants.STATUS_LABEL3].OffText = FormatStatusLabel(currentStatus);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.STATUS3))
                {
                    MatricButtonList[MatricConstants.STATUS3].GameState = false;
                    MatricButtonList[MatricConstants.STATUS3].OffText = FormatStatusText(currentStatus, this.fuelCapacity, this.fuelReserveMax);
                }

                foreach (MatricButton button in MatricButtonList.Values) {
                    if (button != null) {
                        button.UpdateMatricState(matric, CLIENT_ID);
                    }
                }
            }
        }

        public void UpdateGameInfo(LoadGameData currentLoadGameData)
        {

            if (currentLoadGameData != null)
            {

                // Handle Text fields
                if (MatricButtonList.ContainsKey(MatricConstants.GAMEINFO))
                {
                    MatricButtonList[MatricConstants.GAMEINFO].GameState = false;
                    MatricButtonList[MatricConstants.GAMEINFO].OffText = LoadGameStatusInfo(currentLoadGameData);
                    this.commander = currentLoadGameData.Commander;
                }               

                foreach (MatricButton button in MatricButtonList.Values)
                {
                    if (button != null)
                    {
                        button.UpdateMatricState(matric, CLIENT_ID);
                    }
                }
            }
        }

        public void UpdateInfo(StatusData currentStatus)
        {

            if (currentStatus != null)
            {

                // Handle Text fields
                if (MatricButtonList.ContainsKey(MatricConstants.INFO_LABEL))
                {
                    MatricButtonList[MatricConstants.INFO_LABEL].GameState = false;
                    MatricButtonList[MatricConstants.INFO_LABEL].OffText = FormatInfoLabel(currentStatus);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.INFO))
                {
                    MatricButtonList[MatricConstants.INFO].GameState = false;
                    MatricButtonList[MatricConstants.INFO].OffText = FormatInfoText(currentStatus);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.INFO_LABEL2))
                {
                    MatricButtonList[MatricConstants.INFO_LABEL2].GameState = false;
                    MatricButtonList[MatricConstants.INFO_LABEL2].OffText = FormatInfoLabel(currentStatus);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.INFO2))
                {
                    MatricButtonList[MatricConstants.INFO2].GameState = false;
                    MatricButtonList[MatricConstants.INFO2].OffText = FormatInfoText(currentStatus);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.INFO_LABEL3))
                {
                    MatricButtonList[MatricConstants.INFO_LABEL3].GameState = false;
                    MatricButtonList[MatricConstants.INFO_LABEL3].OffText = FormatInfoLabel(currentStatus);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.INFO3))
                {
                    MatricButtonList[MatricConstants.INFO3].GameState = false;
                    MatricButtonList[MatricConstants.INFO3].OffText = FormatInfoText(currentStatus);
                }

                foreach (MatricButton button in MatricButtonList.Values)
                {
                    if (button != null)
                    {
                        button.UpdateMatricState(matric, CLIENT_ID);
                    }
                }
            }
        }

        public void UpdateTarget(ShipTargetedData currentTarget) {

            if (currentTarget != null) {

                // Handle Text fields
                if (MatricButtonList.ContainsKey(MatricConstants.TARGET_LABEL)) {
                    MatricButtonList[MatricConstants.TARGET_LABEL].GameState = false;
                    MatricButtonList[MatricConstants.TARGET_LABEL].OffText = FormatTargetLabel(currentTarget);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.TARGET)) {
                    MatricButtonList[MatricConstants.TARGET].GameState = false;
                    MatricButtonList[MatricConstants.TARGET].OffText = FormatTargetText(currentTarget);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.TARGET_LABEL2))
                {
                    MatricButtonList[MatricConstants.TARGET_LABEL2].GameState = false;
                    MatricButtonList[MatricConstants.TARGET_LABEL2].OffText = FormatTargetLabel(currentTarget);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.TARGET2))
                {
                    MatricButtonList[MatricConstants.TARGET2].GameState = false;
                    MatricButtonList[MatricConstants.TARGET2].OffText = FormatTargetText(currentTarget);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.TARGET_LABEL3))
                {
                    MatricButtonList[MatricConstants.TARGET_LABEL3].GameState = false;
                    MatricButtonList[MatricConstants.TARGET_LABEL3].OffText = FormatTargetLabel(currentTarget);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.TARGET3))
                {
                    MatricButtonList[MatricConstants.TARGET3].GameState = false;
                    MatricButtonList[MatricConstants.TARGET3].OffText = FormatTargetText(currentTarget);
                }

                if (MatricButtonList.ContainsKey(MatricConstants.TARGETSHIELDVALUE))
                    FormatPercentValues(MatricConstants.TARGETSHIELDVALUE, currentTarget.ShieldHealth, 34);

                if (MatricButtonList.ContainsKey(MatricConstants.TARGETHULLVALUE))
                    FormatPercentValues(MatricConstants.TARGETHULLVALUE, currentTarget.HullHealth, 34);

                if (MatricButtonList.ContainsKey(MatricConstants.TARGETSUBSYSVALUE))
                    FormatPercentValues(MatricConstants.TARGETSUBSYSVALUE, currentTarget.SubSystemHealth, 34);
                
                if (MatricButtonList.ContainsKey(MatricConstants.TARGETSUBSYSNAME))
                {
                    MatricButtonList[MatricConstants.TARGETSUBSYSNAME].GameState = false;
                    MatricButtonList[MatricConstants.TARGETSUBSYSNAME].OffText = FormatSubsysText(currentTarget);
                }              


                foreach (MatricButton button in MatricButtonList.Values) {
                    if (button != null) {
                        button.UpdateMatricState(matric, CLIENT_ID);
                    }
                }
            }
        }     

        public void UpdateLanding(DockingGrantedData currentDockingGranted, DockingDeniedData currentDockingDenied, DockingTimeoutData currentDockingTimeout, DockingCancelledData currentDockingCancelled, LocationData currentLocation)
        {

            if ((currentDockingGranted != null && currentDockingGranted.Timestamp > lastEventUpdate) || 
                (currentDockingDenied != null && currentDockingDenied.Timestamp > lastEventUpdate) || 
                (currentDockingTimeout != null && currentDockingTimeout.Timestamp > lastEventUpdate) || 
                (currentDockingCancelled != null && currentDockingCancelled.Timestamp > lastEventUpdate) || 
                currentLocation != null)
            {

                LandingDataCollect(MatricConstants.LANDING, MatricConstants.LANDING_LABEL, currentDockingGranted, currentDockingDenied, currentDockingTimeout, currentDockingCancelled, currentLocation);
                LandingDataCollect(MatricConstants.LANDING2, MatricConstants.LANDING_LABEL2, currentDockingGranted, currentDockingDenied, currentDockingTimeout, currentDockingCancelled, currentLocation);
            }

            if (MatricButtonList.ContainsKey(MatricConstants.LANDINGPAD))
            {
                MatricButtonList[MatricConstants.LANDINGPAD].UpdateButtonText = true;
                MatricButtonList[MatricConstants.LANDINGPAD].OffText = "<table><tr><td>&nbsp;</td></tr></table>";
                if (currentDockingGranted != null && currentDockingGranted.LandingPad > 0 && currentDockingGranted.Timestamp > lastEventUpdate)
                {
                    MatricButtonList[MatricConstants.LANDINGPAD].ButtonState = true;
                    MatricButtonList[MatricConstants.LANDINGPAD].GameState = true;                        
                    MatricButtonList[MatricConstants.LANDINGPAD].OnText = FormatLandingPadText(currentDockingGranted);
                    Log.Instance.Info("Landing Pad assigned. Pad: " + currentDockingGranted.LandingPad.ToString());
                }
                else
                    if (currentDockingDenied != null && currentDockingDenied.Reason != "" && currentDockingDenied.Timestamp > lastEventUpdate)
                    {
                        MatricButtonList[MatricConstants.LANDINGPAD].OnText = "<table><tr><td>&nbsp;</td></tr></table>";
                        MatricButtonList[MatricConstants.LANDINGPAD].OffText = "<table><tr><td><B>XX</B></td></tr></table>";
                        MatricButtonList[MatricConstants.LANDINGPAD].ButtonState = false;
                        MatricButtonList[MatricConstants.LANDINGPAD].GameState = false;                        
                        Log.Instance.Info("Landing Denied. Reason: " + currentDockingDenied.Reason);
                    }
                    else
                        if (currentDockingTimeout != null && currentDockingTimeout.Timestamp > lastEventUpdate)
                        {
                            MatricButtonList[MatricConstants.LANDINGPAD].OnText = "<table><tr><td>&nbsp;</td></tr></table>";
                            MatricButtonList[MatricConstants.LANDINGPAD].OffText = "<table><tr><td><B>--</B></td></tr></table>";
                            MatricButtonList[MatricConstants.LANDINGPAD].ButtonState = false;
                            MatricButtonList[MatricConstants.LANDINGPAD].GameState = false;                            
                            Log.Instance.Info("Landing Denied. Reason: Timeout.");
                        }
                        else
                        {                                                                   
                            MatricButtonList[MatricConstants.LANDINGPAD].OnText = "<table><tr><td>&nbsp;</td></tr></table>";
                            MatricButtonList[MatricConstants.LANDINGPAD].GameState = false;
                            MatricButtonList[MatricConstants.LANDINGPAD].ButtonState = false;
                            Log.Instance.Info("No Landing Pad assigned or reset.");
                        }                    
            }

            foreach (MatricButton button in MatricButtonList.Values)
            {
                if (button != null)
                {
                    button.UpdateMatricState(matric, CLIENT_ID);
                }
            }            
        }

        public void UpdateMaxFuelData(LoadGameData currentLoadGameData, LoadoutData currentLoadoutData, RefuelAllData currentRefuelAllData, ReservoirReplenishedData currentReservoirReplenishedData)
        {


            if (currentLoadGameData != null && (this.fuelCapacity == 0 || currentLoadGameData.Timestamp > lastEventUpdate))
            { 
                this.fuelCapacity = currentLoadGameData.FuelCapacity;
                this.fuelCurrent = currentLoadGameData.FuelLevel;
                this.currentShipId = currentLoadGameData.ShipId;
                Log.Instance.Info("Fuel Data found after loading game. Capacity: " + this.fuelCapacity + ". Level: " + this.fuelCurrent + ". Percentage: " + calculatePercentageValue(this.fuelCurrent, this.fuelCapacity) + "%");
            }
            if (currentLoadoutData != null && currentLoadoutData.Timestamp > lastEventUpdate)
            {
                this.fuelCapacity = currentLoadoutData.FuelCapacity.Main;
                this.fuelReserveMax = currentLoadoutData.FuelCapacity.Reserve;
                this.currentShipId = currentLoadoutData.ShipId;
                Log.Instance.Info("Fuel Data found after loading Loadout. Max Main: " + this.fuelCapacity + " (current level: " + calculatePercentageValue(this.fuelCurrent, this.fuelCapacity) + "%). Max Reserve: " + this.fuelReserveMax + " (current level: " + calculatePercentageValue(this.fuelReserve, this.fuelReserveMax) + "%).");
            }
            if (currentReservoirReplenishedData != null && currentReservoirReplenishedData.Timestamp > lastEventUpdate)
            {
                Log.Instance.Info("ReservoirReplenishedData Event found. Reset Max Reservoir Fuel with next Status update");
                this.resetMaxReservoirFuel = true;
            }
            if (currentRefuelAllData != null && currentRefuelAllData.Timestamp > lastEventUpdate)
            {
                this.resetMaxFuel = true;
                this.resetMaxReservoirFuel = true;
                Log.Instance.Info("RefuelAll Event found. Reset Max Fuel with next Status update");
            }
        }

        private void FormatPercentValues(string field, double value, double threshold)
        {
            if (value >= threshold)
            {
                MatricButtonList[field].GameState = false;
                MatricButtonList[field].ButtonState = false;
                MatricButtonList[field].OffText = FormatPercentValueText(value);
                MatricButtonList[field].OnText = "<table><tr><td>&nbsp;</td></tr></table>";
                MatricButtonList[field].SliderPosition = (int)Math.Round((decimal)value, 0);
            }
            else
                if (value >= 0 && value < threshold)
            {
                MatricButtonList[field].GameState = true;
                MatricButtonList[field].ButtonState = true;
                MatricButtonList[field].OffText = "<table><tr><td>&nbsp;</td></tr></table>";
                MatricButtonList[field].OnText = FormatPercentValueText(value);
                MatricButtonList[field].SliderPosition = (int)Math.Round((decimal)value, 0);
            }
            else
            {
                MatricButtonList[field].GameState = false;
                MatricButtonList[field].ButtonState = false;
                MatricButtonList[field].OffText = "<table><tr><td>&nbsp;</td></tr></table>";
                MatricButtonList[field].OnText = "<table><tr><td>&nbsp;</td></tr></table>";
                MatricButtonList[field].SliderPosition = 0;
            }

        }

        private void FormatNumberValues(string field, double value, double threshold)
        {
            if (value >= threshold)
            {
                MatricButtonList[field].GameState = false;
                MatricButtonList[field].ButtonState = false;
                MatricButtonList[field].OffText = FormatNumberValueText(value);
                MatricButtonList[field].OnText = "<table><tr><td>&nbsp;</td></tr></table>";
            }
            else
            {
                MatricButtonList[field].GameState = true;
                MatricButtonList[field].ButtonState = true;
                MatricButtonList[field].OffText = "<table><tr><td>&nbsp;</td></tr></table>";
                MatricButtonList[field].OnText = FormatNumberValueText(value);
            }

        }

        private void LandingDataCollect(string field, string fieldLabel, DockingGrantedData currentDockingGranted, DockingDeniedData currentDockingDenied, DockingTimeoutData currentDockingTimeout, DockingCancelledData currentDockingCancelled, LocationData currentLocation)
        {
            // Handle Text fields
            if (MatricButtonList.ContainsKey(fieldLabel))
            {
                MatricButtonList[fieldLabel].GameState = false;
                MatricButtonList[fieldLabel].OffText = FormatLandingLabel(currentDockingGranted, currentDockingDenied, currentDockingTimeout, currentDockingCancelled, currentLocation);
            }
            if (MatricButtonList.ContainsKey(field))
            {
                Log.Instance.Info("Landing Event triggered.");
                if (currentDockingDenied != null && currentDockingDenied.Reason != "" && currentDockingDenied.Timestamp > lastEventUpdate)
                {
                    MatricButtonList[field].ButtonState = false;
                    MatricButtonList[field].GameState = false;
                    MatricButtonList[field].UpdateButtonText = true;
                    MatricButtonList[field].OffText = FormatLandingText(currentDockingGranted, currentDockingDenied, currentDockingTimeout, currentDockingCancelled, currentLocation);
                    MatricButtonList[field].OnText = "<table><tr><td>&nbsp;</td></tr></table>";
                    Log.Instance.Info("Docking denied Event triggered.");
                }
                else
                    if (currentDockingGranted != null && currentDockingGranted.LandingPad > 0 && currentDockingGranted.Timestamp > lastEventUpdate)
                    {
                        MatricButtonList[field].ButtonState = true;
                        MatricButtonList[field].GameState = true;
                        MatricButtonList[field].UpdateButtonText = true;
                        MatricButtonList[field].OffText = "<table><tr><td>&nbsp;</td></tr></table>";
                        MatricButtonList[field].OnText = FormatLandingText(currentDockingGranted, currentDockingDenied, currentDockingTimeout, currentDockingCancelled, currentLocation);
                        Log.Instance.Info("Docking granted Event triggered.");
                    }
                    else
                        if ((currentDockingTimeout != null && currentDockingTimeout.Timestamp > lastEventUpdate) || (currentDockingCancelled != null && currentDockingCancelled.Timestamp > lastEventUpdate))
                        {
                            MatricButtonList[field].ButtonState = false;
                            MatricButtonList[field].GameState = false;
                            MatricButtonList[field].UpdateButtonText = true;
                            MatricButtonList[field].OffText = FormatLandingText(currentDockingGranted, currentDockingDenied, currentDockingTimeout, currentDockingCancelled, currentLocation);
                            MatricButtonList[field].OnText = "<table><tr><td>&nbsp;</td></tr></table>";
                            if (currentDockingTimeout != null)
                                Log.Instance.Info("Docking Timeout Event triggered.");
                            if (currentDockingCancelled != null)
                                Log.Instance.Info("Docking Cancelled Event triggered.");
                        }
                        else
                            if (currentLocation != null && currentLocation.LastUpdate > lastEventUpdate)
                            {
                                MatricButtonList[field].ButtonState = false;
                                MatricButtonList[field].GameState = false;
                                MatricButtonList[field].UpdateButtonText = true;
                                MatricButtonList[field].OffText = FormatLandingText(currentDockingGranted, currentDockingDenied, currentDockingTimeout, currentDockingCancelled, currentLocation);
                                MatricButtonList[field].OnText = "<table><tr><td>&nbsp;</td></tr></table>";
                                Log.Instance.Info("Location Event triggered. Data: " + currentLocation.ToString());
                            }
            }
        }

        private static string LoadGameStatusInfo(LoadGameData loadGameData)
        {

            string displayText = "";
            if (loadGameData != null)
            {
               
                    var targetTemplate = $"<table>" +
                        $"<tr><td>Fid:</td><td>{loadGameData.Fid}&nbsp;</td></tr>" +
                        $"<tr><td>Commander:</td><td>{loadGameData.Commander}&nbsp;</td></tr>" +
                        $"<tr><td>has Horizons:</td><td>{loadGameData.HasHorizons}&nbsp;</td></tr>" +
                        $"<tr><td>has Odyssey:</td><td>{loadGameData.HasOdyssey}&nbsp;</td></tr>" +
                        $"<tr><td>GameMode:</td><td>{loadGameData.GameMode}&nbsp;</td></tr>" +
                        $"<tr><td>Language:</td><td>{loadGameData.Language}&nbsp;</td></tr>" +
                        $"<tr><td>GameVersion:</td><td>{loadGameData.GameVersion}&nbsp;</td></tr>" +
                        $"<tr><td>Build:</td><td>{loadGameData.Build}&nbsp;</td></tr>" +
                        $"<tr><td>Group:</td><td>{loadGameData.Group}&nbsp;</td></tr>" +
                        $"</table>";
                    displayText = targetTemplate;
            }
            else
            {

                displayText = "<table><tr><td>&nbsp;</td></tr></table>";
            }
            
            return displayText;
        }

        private static int calculatePercentageValue(double currentValue, double maxValue)
        {
            int result = 0;

            if (maxValue > 0)
            {
                result = (int)(Math.Round((decimal)(currentValue / maxValue), 2) * 100);
            }

            return result;
        }

        private static string FormatSubsysText(ShipTargetedData targetData)
        {

            string displayText = "";
            if (targetData != null)
            {

                if (targetData.TargetLocked == true)
                {
                    
                    var targetTemplate = $"<table>" +
                        $"<tr><td>{targetData.SubSystem}</td></tr>" +                        
                        $"</table>";
                    displayText = targetTemplate;
                }
                else
                {
                    displayText = "<table><tr><td>No Sub-System selected</td></tr></table>";
                }
            }
            return displayText;
        }

        private static string FormatTargetText(ShipTargetedData targetData) {

            string displayText = "";
            if (targetData != null) {

                if (targetData.TargetLocked == true) {
                    string bountyText = targetData.Bounty == 0 ? "" : targetData.Bounty.ToString();
                    string shield = ((int)Math.Round((decimal)targetData.ShieldHealth, 0)).ToString();
                    string hull = ((int)Math.Round((decimal)targetData.HullHealth, 0)).ToString();
                    string subsys = ((int)Math.Round((decimal)targetData.SubSystemHealth, 0)).ToString();
                    var targetTemplate = $"<table>" +
                        $"<tr><td>{targetData.Ship} (Shield: {shield}%. Hull: {hull}%. Subsys: {targetData.SubSystem} {subsys}%)</td></tr>" +
                        $"<tr><td>{targetData.PilotName}&nbsp;</td></tr>" +
                        $"<tr><td>{targetData.PilotRank}&nbsp;</td></tr>" +
                        $"<tr><td>{targetData.Faction}&nbsp;</td></tr>" +
                        $"<tr><td>{targetData.LegalStatus}&nbsp;</td></tr>" +
                        $"<tr><td>{bountyText}&nbsp;</td></tr>" +
                        $"</table>";
                    displayText = targetTemplate;
                } else {
                    displayText = "<table><tr><td>No target selected</td></tr></table>";
                }
            }
            return displayText;
        }

        private static string FormatTargetLabel(ShipTargetedData targetData) {

            string displayText = "";
            if (targetData != null) {
                if (targetData.TargetLocked == true) {
                    var targetTemplate = $"<table>" +
                        $"<tr><td>Target:</td></tr>" +
                        $"<tr><td>Pilot:</td></tr>" +
                        $"<tr><td>Rank:</td></tr>" +
                        $"<tr><td>Faction:</td></tr>" +
                        $"<tr><td>Status:</td></tr>" +
                        $"<tr><td>Bounty:</td></tr>" +
                        $"</table>";
                    displayText = targetTemplate;
                } else {

                    displayText = "<table><tr><td>Target:</td></tr></table>";
                }
            }
            return displayText;
        }

        private static string FormatInfoText(StatusData statusData)
        {

            string displayText = "";
            string balance = statusData.Balance.ToString("N0");
            if (statusData != null)
            {                
                var targetTemplate = $"<table>" +
                $"<tr><td>{statusData.Cargo} tons</td></tr>" +
                $"<tr><td>{balance} Cr</td></tr>" +
                $"</table>";
                displayText = targetTemplate;                
            }
            return displayText;
        }

        private static string FormatInfoLabel(StatusData statusData)
        {

            string displayText = "";
            if (statusData != null)
            {                
                var targetTemplate = $"<table>" +                    
                    $"<tr><td>Cargo:</td></tr>" +
                    $"<tr><td>Balance:</td></tr>" +
                    $"</table>";
                displayText = targetTemplate;                
            }
            return displayText;
        }

        private static string FormatPercentValueText(double value)
        {

            string displayText = "";

            if (value >= 0 && value <= 100)
            { 
                string strValue = ((int)Math.Round((decimal)value, 0)).ToString();
                var targetTemplate = $"<table>" +
                    $"<tr><td><B>{strValue}%</B></td></tr>" +
                    $"</table>";
                displayText = targetTemplate;
            }
            else
            {
                displayText = "<table><tr><td>&nbsp;</td></tr></table>";
            }

            return displayText;
        }

        private static string FormatNumberValueText(double value)
        {

            string displayText = "";

            if (value >= 0)
            {
                string strValue = ((int)Math.Round((decimal)value, 0)).ToString();
                var targetTemplate = $"<table>" +
                    $"<tr><td><B>{strValue}</B></td></tr>" +
                    $"</table>";
                displayText = targetTemplate;
            }
            else
            {
                displayText = "<table><tr><td>&nbsp;</td></tr></table>";
            }

            return displayText;
        }

        private static string FormatLandingPadText(DockingGrantedData landingData)
        {

            string displayText = "";
            if (landingData != null)
            {

                if (landingData.LandingPad > 0)
                {
                    string landingPad = landingData.LandingPad.ToString();
                    if (landingData.LandingPad < 10)
                        landingPad = "0" + landingPad;
                    var targetTemplate = $"<table>" +                        
                        $"<tr><td><B>{landingPad}</B></td></tr>" +
                        $"</table>";
                    displayText = targetTemplate;
                }
                else
                {
                    displayText = "<table><tr><td>&nbsp;</td></tr></table>";
                }
            }            

            return displayText;
        }

        private static string FormatLandingText(DockingGrantedData landingData, DockingDeniedData landingDeniedData, DockingTimeoutData landingTimeoutData, DockingCancelledData landingCancelledData, LocationData landingLocationData)
        {

            string displayText = "";
            if (landingData != null)
            {

                if (landingData.LandingPad > 0)
                {
                    string landingPad = landingData.LandingPad.ToString();
                    if (landingData.LandingPad < 10)
                        landingPad = "0" + landingPad;
                    var targetTemplate = $"<table>" +
                        $"<tr><td>{landingData.StationName}&nbsp;</td></tr>" +
                        $"<tr><td><B>{landingPad}</B></td></tr>" +
                        $"</table>";
                    displayText = targetTemplate;
                }
                else
                {
                    var targetTemplate = $"<table>" +
                       $"<tr><td>{landingData.StationName}&nbsp;</td></tr>" +
                       $"<tr><td>&nbsp;</td></tr>" +
                       $"</table>";
                    displayText = targetTemplate;
                }
            }
            else
                if (landingDeniedData != null)
                {

                    if (landingDeniedData.Reason != "")
                    {
                        var targetTemplate = $"<table>" +
                            $"<tr><td>{landingDeniedData.StationName}&nbsp;</td></tr>" +
                            $"<tr><td>{landingDeniedData.Reason}&nbsp;</td></tr>" +
                            $"</table>";
                        displayText = targetTemplate;
                    }
                    else
                    {
                        var targetTemplate = $"<table>" +
                            $"<tr><td>{landingDeniedData.StationName}&nbsp;</td></tr>" +
                            $"<tr><td>&nbsp;</td></tr>" +
                            $"</table>";
                        displayText = targetTemplate;
                    }
                }
                else
                    if (landingTimeoutData != null)
                    {
                        var targetTemplate = $"<table>" +
                            $"<tr><td>{landingTimeoutData.StationName}&nbsp;</td></tr>" +
                            $"<tr><td>Timeout</td></tr>" +
                            $"</table>";
                        displayText = targetTemplate;
                    }
                    else
                        if (landingCancelledData != null)
                        {
                            var targetTemplate = $"<table>" +
                                $"<tr><td>{landingCancelledData.StationName}</td></tr>" +
                                $"<tr><td>Cancelled</td></tr>" +
                                $"</table>";
                            displayText = targetTemplate;
                        }
                        else
                            if (landingLocationData != null)
                            {
                                string distance = Math.Round((decimal)landingLocationData.DistanceFromStarInLightSeconds, 2).ToString("N2");
                                var targetTemplate = $"<table>" +
                                     $"<tr><td>{landingLocationData.StationName}&nbsp;</td></tr>" +
                                    $"<tr><td>{landingLocationData.SystemName}&nbsp;</td></tr>" +
                                    $"<tr><td>{landingLocationData.BodyName}&nbsp;</td></tr>" +
                                    $"<tr><td>{distance} ls</td></tr>" +
                                    $"<tr><td>{landingLocationData.StationEconomy}&nbsp;</td></tr>" +                                    
                                    $"</table>";
                                displayText = targetTemplate;
                            }

            return displayText;
        }

        private static string FormatLandingLabel(DockingGrantedData landingData, DockingDeniedData landingDeniedData, DockingTimeoutData landingTimeoutData, DockingCancelledData landingCancelledData, LocationData landingLocationData)
        {

            string displayText = "";
            if (landingData != null)
            {
                if (landingData.LandingPad > 0)
                {
                    var targetTemplate = $"<table>" +
                        $"<tr><td>Station Name:</td></tr>" +
                        $"<tr><td><B>Landing Pad:</B></td></tr>" +                        
                        $"</table>";
                    displayText = targetTemplate;
                }
                else
                {

                    var targetTemplate = $"<table>" +
                        $"<tr><td>Station Name:</td></tr>" +
                        $"<tr><td>&nbsp;</td></tr>" +
                        $"</table>";
                    displayText = targetTemplate;
                }
            }
            else
                if (landingDeniedData != null)
                {

                    if (landingDeniedData.Reason != "")
                    {
                        var targetTemplate = $"<table>" +
                            $"<tr><td>Station Name:</td></tr>" +
                            $"<tr><td>Deny Reason:</td></tr>" +
                            $"</table>";
                        displayText = targetTemplate;
                    }
                    else
                    {
                        var targetTemplate = $"<table>" +
                            $"<tr><td>Station Name:</td></tr>" +
                            $"<tr><td>&nbsp;</td></tr>" +
                            $"</table>";
                        displayText = targetTemplate;
                    }
                }
                else
                    if (landingTimeoutData != null || landingCancelledData != null)
                    {
                        var targetTemplate = $"<table>" +
                            $"<tr><td>Station Name:</td></tr>" +
                            $"<tr><td>Deny Reason:</td></tr>" +
                            $"</table>";
                        displayText = targetTemplate;
                    }
                    else
                        if (landingLocationData != null)
                        {
                            var targetTemplate = $"<table>" +
                                $"<tr><td>Station Name:</td></tr>" +
                                $"<tr><td>System Name:</td></tr>" +
                                $"<tr><td>Body Name:</td></tr>" +
                                $"<tr><td>Distance Main Star:</td></tr>" +
                                $"<tr><td>Economy:</td></tr>" +
                                $"</table>";
                            displayText = targetTemplate;
                        }

            return displayText;
        }

        private static string FormatStatusText(StatusData statusData, double maxFuel, double maxResFuel) {

            string displayText = "";
            if (statusData != null) {
                string balance = statusData.Balance.ToString("N0");
                var statusTemplate = $"<table>";

                if (string.IsNullOrEmpty(statusData.BodyName) == true) {
                    statusTemplate = statusTemplate + $"<tr><td>Not available</td></tr>";
                } else {
                    statusTemplate = statusTemplate + $"<tr><td>{statusData.BodyName}</td></tr>";
                }
                if (string.IsNullOrEmpty(statusData.LegalState) == true) {
                    statusTemplate = statusTemplate + $"<tr><td>Unknown</td></tr>";
                } else {
                    statusTemplate = statusTemplate + $"<tr><td>{statusData.LegalState}</td></tr>";
                }
                statusTemplate = statusTemplate + $"<tr><td>{statusData.DestinationName}&nbsp;</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>{Math.Round((decimal)statusData.FuelMain, 1).ToString()} tons (Max: {Math.Round((decimal)maxFuel, 0).ToString()} tons, {calculatePercentageValue(statusData.FuelMain, maxFuel)}%)</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>{Math.Round((decimal)statusData.FuelReservoir, 2).ToString()} tons (Max: {Math.Round((decimal)maxResFuel, 2).ToString()} tons, {calculatePercentageValue(statusData.FuelReservoir, maxResFuel)}%)</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>&nbsp;</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>{statusData.Cargo.ToString()} tons</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>{balance} Cr</td></tr>";                
                statusTemplate = statusTemplate + $"</table>";
                displayText = statusTemplate;
            }
            return displayText;
        }

        private static string FormatStatusLabel(StatusData statusData) {

            string displayText = "";
            if (statusData != null) {
                var statusTemplate = $"<table>";

                statusTemplate = statusTemplate + $"<tr><td>Closest body:</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>Legal state:</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>Destination Name:</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>Current Main fuel:</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>Current Reservoir fuel:</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>&nbsp;</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>Cargo:</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>Balance:</td></tr>";                
                statusTemplate = statusTemplate + $"</table>";
                displayText = statusTemplate;
            }
            return displayText;
        }

        private static void updateLastEventUpdateTimeStamp(DateTime timestamp)
        {
            if (timestamp != null)
            {
                if (lastEventUpdate < timestamp)
                    lastEventUpdate = timestamp;
                else
                    if (lastEventUpdate > timestamp && DateTime.UtcNow > lastEventUpdate)
                        lastEventUpdate = DateTime.UtcNow;
            }
            else
                lastEventUpdate = DateTime.UtcNow;
        }
    }
}
