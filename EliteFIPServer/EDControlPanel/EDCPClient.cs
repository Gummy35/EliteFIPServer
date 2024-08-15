//using EliteFIPProtocol;
using EliteAPI.Abstractions.Events;
using EliteAPI.Events;
using EliteAPI.Status.Ship;
using EliteAPI.Status.Ship.Events;
using EliteFIPServer.Logging;
using Microsoft.Win32;
using RJCP.IO.Ports;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace EliteFIPServer
{
    public class EDCPClient
    {

        public ComponentState CurrentState { get; private set; } = new ComponentState();

        private static DateTime lastEventUpdate = DateTime.UtcNow;
        private WinSerialPortStream ardPort;
        private string portName = null;
        private string info = "";

        // Flash Worker
        private CancellationTokenSource WorkerCTS;
        private Task WorkerTask;

        // Data variables
        private double fuelCapacity = 0;
        private double fuelCurrent = 0;
        private double fuelReserve = 0;
        private double fuelReserveMax = 0;
        private string commander = "";
        private string currentShipId = "";

        private bool resetMaxFuel = false;
        private bool resetMaxReservoirFuel = false;
        private RegistryKey rkCurrentUser;
        private NavigationData currentNavData = null;
        private LocationData currentLocation = null;
        private string commanderName = "";
        private string shipName = "";

        public EDCPClient()
        {
        }

        public void SavePort(string portName)
        {
            try
            {
                this.rkCurrentUser = Registry.CurrentUser;
                RegistryKey registryKey = this.rkCurrentUser.OpenSubKey("Software\\EDControlPanel", true);
                if (registryKey == null)
                    registryKey = this.rkCurrentUser.CreateSubKey("Software\\EDControlPanel");

                if (portName == null)
                    registryKey.DeleteValue("LastKnownPort");
                else
                    registryKey.SetValue("LastKnownPort", (object)portName);

                registryKey.Close();
                this.rkCurrentUser.Close();
            }
            catch (Exception ex)
            {
            }
        }

        public string GetLastKnownPort()
        {
            string result = null;
            try
            {
                this.rkCurrentUser = Registry.CurrentUser;
                RegistryKey registryKey = this.rkCurrentUser.OpenSubKey("Software\\EDControlPanel", true);
                if (registryKey == null)
                {
                    RegistryKey subKey = this.rkCurrentUser.CreateSubKey("Software\\EDControlPanel");
                    subKey.SetValue("LastKnownPort", (object)"");
                    subKey.Close();
                }
                else
                {
                    result = (string)registryKey.GetValue("LastKnownPort");
                }
                this.rkCurrentUser.Close();
            }
            catch (Exception ex)
            {
            }

            return result;
        }

        public bool OpenPort(string portName)
        {
            this.ardPort = new WinSerialPortStream(portName);
            this.ardPort.BaudRate = 115200;
            this.ardPort.DataBits = 8;
            this.ardPort.ReceivedBytesThreshold = 2;
            this.ardPort.DtrEnable = true;
            this.ardPort.DataReceived += ArdPort_DataReceived;
            this.ardPort.ReadTimeout = 100;
            this.ardPort.WriteTimeout = 200;
            try
            {
                this.ardPort.Open();
                CurrentState.Set(RunState.Starting);
                Thread.Sleep(100);
                try
                {
                    this.ardPort.Write("I");
                    var s = this.ardPort.ReadLine();
                    if (!s.Contains("EDControlPanel"))
                    {
                        ClosePort();
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    return false;
                }
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                Log.Instance.Error(ex.Message);
                return false;
            }
            return true;
        }

        public void ClosePort()
        {
            this.info = "";
            if (this.ardPort != null)
            {
                if (this.ardPort.IsOpen)
                {
                    this.ardPort.DiscardInBuffer();
                    this.ardPort.DiscardOutBuffer();
                    try
                    {
                        Thread.Sleep(200);
                        this.ardPort.Close();
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                    }
                }
                try
                {
                    if (this.ardPort != null)
                    {
                        this.ardPort.Dispose();
                        this.ardPort = null;
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }

        public void Start()
        {
            Log.Instance.Info("Starting EDCP Integration");
            CurrentState.Set(RunState.Starting);

            var sp = new SerialPortStream();
            string lastPort = GetLastKnownPort();
            var portNames = sp.GetPortNames();
            if (!string.IsNullOrEmpty(lastPort) && portNames.Contains(lastPort))
            {
                if (OpenPort(lastPort))
                {
                    CurrentState.Set(RunState.Started);
                    return;
                }
            }
            if (string.IsNullOrEmpty(this.info))
            {
                SavePort("");

                foreach (var portName in portNames)
                {
                    if (OpenPort(portName))
                    {
                        CurrentState.Set(RunState.Started);
                        SavePort(portName);
                        return;
                    }
                }
            }

            if (string.IsNullOrEmpty(this.info)) CurrentState.Set(RunState.Stopped);
        }

        private void CompleteStart()
        {

            // Start Matric Flash Thread
            Log.Instance.Info("Starting Flash Thread");
            WorkerCTS = new CancellationTokenSource();
            WorkerTask = new Task(new Action(WorkerThread), WorkerCTS.Token);
            WorkerTask.ContinueWith(WorkerThreadEnded);
            WorkerTask.Start();
            
            CurrentState.Set(RunState.Started);
        }

        public void Stop()
        {
            Log.Instance.Info("Stopping EDCP Integration");
            if (CurrentState.State == RunState.Started)
            {
                CurrentState.Set(RunState.Stopping);
                ClosePort();
                WorkerCTS?.Cancel();
                WorkerTask?.Wait();
            }
            CurrentState.Set(RunState.Stopped);
        }

        public void SendNavData()
        {
            if ((currentLocation != null) && (currentNavData != null) && currentNavData.NavRouteActive)
            {
                var idx = currentNavData.Stops.FindIndex(s => s.SystemId == currentLocation.SystemId);
                if (idx >= 0)
                {
                    List<string> stops = ["N"];
                    for (int i = idx + 1; i < Math.Min(idx + 4, currentNavData.Stops.Count); i++)
                    {
                        stops.Add(currentNavData.Stops[i].SystemName);
                    }
                    while (stops.Count < 4) stops.Add("");
                    sendStrings(stops.ToArray(), 19);
                }
            }
            else
            {
                sendStrings(["N", "", "", ""]);
            }
        }

        public void AlertMessage(string title, string l1 = "", string l2 = "", string l3 = "", uint duration = 10)
        {
            ardPort.Write("A");
            ardPort.Write(BitConverter.GetBytes(duration), 0, 4);
            sendStrings([title, l1, l2, l3]);
        }

        public void ClearAlert()
        {
            ardPort.Write("A");
            ardPort.Write(BitConverter.GetBytes(0), 0, 4);
            sendStrings(["", "", "", ""]);
        }

        public void UpdateGameState(IEvent evt)
        {
            // Only update if EDCP Integration is running
            if (CurrentState.State == RunState.Started)
            {
                if (evt is StatusEvent)
                {
                    var data = (StatusEvent)evt;
                    Log.Instance.Info(JsonSerializer.Serialize(evt));
                    UpdateStatus(data);
                    UpdateInfo(data);
                    updateLastEventUpdateTimeStamp(evt.Timestamp);
                }
                else if (evt is ShipTargetedEvent)
                {
                    var data = (ShipTargetedEvent)evt;
                    UpdateTarget(data);
                    updateLastEventUpdateTimeStamp(evt.Timestamp);
                }
                else if (evt is DockingGrantedEvent)
                {
                    var data = (DockingGrantedEvent)evt;
                    SendDockingGrantedData(data);
                    updateLastEventUpdateTimeStamp(evt.Timestamp);
                }
                else if (evt is DockingDeniedEvent)
                {
                    var data = (DockingDeniedEvent)evt;
                    SendDockingDeniedData(data);
                    updateLastEventUpdateTimeStamp(evt.Timestamp);
                }
                else if (evt is DockingTimeoutEvent)
                {
                    var data = (DockingTimeoutEvent)evt;
                    SendDockingTimeoutData(data);
                    updateLastEventUpdateTimeStamp(evt.Timestamp);
                }
                else if (evt is DockingCancelledEvent)
                {
                    var data = (DockingCancelledEvent)evt;
                    SendDockingCancelledData(data);
                    updateLastEventUpdateTimeStamp(evt.Timestamp);
                }
                else if (evt is DockedEvent)
                {
                    var data = (DockedEvent)evt;
                    SendDockedData(data);
                    updateLastEventUpdateTimeStamp(evt.Timestamp);
                }
                else if (evt is LocationData)
                {
                    currentLocation = (LocationData)evt;
                    ardSendLocation(currentLocation);
                    SendNavData();
                    updateLastEventUpdateTimeStamp(currentLocation.Timestamp);
                }
                else if (evt is LoadGameEvent)
                {
                    var data = (LoadGameEvent)evt;
                    UpdateMaxFuelData(data, null, null, null);                    
                    UpdateGameInfo(data);
                    updateLastEventUpdateTimeStamp(evt.Timestamp);
                }
                
                //else if (eventType == GameEventType.RefuelAll)
                //{
                //    RefuelAllData currentRefuelAllData = gameData as RefuelAllData;
                //    UpdateMaxFuelData(null, null, currentRefuelAllData, null);
                //    updateLastEventUpdateTimeStamp(currentRefuelAllData.Timestamp);
                //}
                //else if (eventType == GameEventType.ReservoirReplenished)
                //{
                //    ReservoirReplenishedData currentReservoirReplenishedData = gameData as ReservoirReplenishedData;
                //    UpdateMaxFuelData(null, null, null, currentReservoirReplenishedData);
                //    updateLastEventUpdateTimeStamp(currentReservoirReplenishedData.Timestamp);
                //}
                //else if (eventType == GameEventType.ShipyardNew)
                //{
                //    ShipyardNewData currentShipyardNewData = gameData as ShipyardNewData;
                //    if (currentShipyardNewData != null && currentShipyardNewData.NewShipId != null && currentShipyardNewData.NewShipId != "" && !currentShipyardNewData.NewShipId.Equals(this.currentShipId) && currentShipyardNewData.Timestamp > lastEventUpdate)
                //    {
                //        Log.Instance.Info("Found a Ship Change (old id: " + this.currentShipId + ". new id: " + currentShipyardNewData.NewShipId + "), reset Max fuel with next status update");
                //        this.currentShipId = currentShipyardNewData.NewShipId;
                //        this.resetMaxFuel = true;
                //        this.resetMaxReservoirFuel = true;
                //    }
                //    updateLastEventUpdateTimeStamp(currentShipyardNewData.Timestamp);
                //}
                //else if (eventType == GameEventType.ShipyardSwap)
                //{
                //    ShipyardSwapData currentShipyardSwapData = gameData as ShipyardSwapData;
                //    if (currentShipyardSwapData != null && currentShipyardSwapData.ShipId != null && currentShipyardSwapData.ShipId != "" && !currentShipyardSwapData.ShipId.Equals(this.currentShipId) && currentShipyardSwapData.Timestamp > lastEventUpdate)
                //    {
                //        Log.Instance.Info("Found a Ship Change (old id: " + this.currentShipId + ". new id: " + currentShipyardSwapData.ShipId + "), reset Max fuel with next status update");
                //        this.currentShipId = currentShipyardSwapData.ShipId;
                //        this.resetMaxFuel = true;
                //        this.resetMaxReservoirFuel = true;
                //    }
                //    updateLastEventUpdateTimeStamp(currentShipyardSwapData.Timestamp);
                //}
                else if (evt is LoadoutEvent)
                {
                    var data = (LoadoutEvent)evt;
                    //Log.Instance.Info($"New loadout data IsHot : {loadoutData.IsHot}");
                    //foreach (var module in loadoutData.Modules)
                    //{
                    //    Log.Instance.Info($"Module {module.Item} : ammo in clip={module.AmmoInClip}, ammo in hopper={module.AmmoInHopper}, isOn={module.IsOn}, health={module.Health}, priority={module.Priority}, slot={module.Slot}");
                    //}
                    SendLoadoutData(data);                    
                }
                else if (evt is NavigationData)
                {
                    var data = (NavigationData)evt;
//                    Log.Instance.Info($"Navigation data : {navigationData.NavRouteActive} / Last system reached : {navigationData.LastSystemReached}");
//                    foreach (var stop in navigationData.Stops)
//                    {
////                        Log.Instance.Info($"Navstop : {stop.SystemName}");
//                    }
                    currentNavData = data;
                    SendNavData();
                }
                else if (evt is ApproachBodyEvent)
                {
                    var data = (ApproachBodyEvent)evt;
                    Log.Instance.Info($"Approach Body data : {data.StarSystem} / {data.Body}");
                    SendApproachBodyData(data);
                }
                //else if (evt is PipsStatusEvent)
                //{
                //}
                else if (evt is JumpData)
                {
//                    Log.Instance.Info("Jump data : " + JsonSerializer.Serialize(evt));
                }
            }
        }

        private void SendDockedData(DockedEvent data)
        {
            AlertMessage("Docking complete", duration: 5);
        }

        private void SendDockingCancelledData(DockingCancelledEvent data)
        {
            AlertMessage("** Docking Cancelled");
        }

        private void SendDockingTimeoutData(DockingTimeoutEvent data)
        {
            AlertMessage("** Docking Timeout");
        }

        private void SendDockingDeniedData(DockingDeniedEvent data)
        {
            AlertMessage("** Docking denied",
                "",
                data.Reason);
        }

        private void SendDockingGrantedData(DockingGrantedEvent data)
        {
            AlertMessage("** Docking granted",
                "",
                $"Landing pad {data.LandingPad}", duration: 600);
        }

        private void SendLoadoutData(LoadoutEvent loadoutData)
        {
            UInt32 flag1 = 0;
            UInt32 flag2 = 0;
            foreach (var module in loadoutData.Modules)
            {
                if (module.Item.Contains("_chafflauncher_"))
                {
                    if (module.IsOn)
                        flag1 += 0x00000001;
                    if (module.AmmoInClip > 0)
                        flag1 += 0x00000002;
                    if (module.AmmoInHopper > 0)
                        flag1 += 0x00000004;
                    if (module.Health > 0)
                        flag1 += 0x00000008;
                }
                else if (module.Item.Contains("_heatsinklauncher_"))
                {
                    if (module.IsOn)
                        flag1 += 0x00000010;
                    if (module.AmmoInClip > 0)
                        flag1 += 0x00000020;
                    if (module.AmmoInHopper > 0)
                        flag1 += 0x00000040;
                    if (module.Health > 0)
                        flag1 += 0x00000080;
                }
                else if (module.Item.Contains("_shieldcellbank_"))
                {
                    if (module.IsOn)
                        flag1 += 0x00000100;
                    if (module.AmmoInClip > 0)
                        flag1 += 0x00000200;
                    if (module.AmmoInHopper > 0)
                        flag1 += 0x00000400;
                    if (module.Health > 0)
                        flag1 += 0x00000800;
                }
                else if (module.Item.Contains("_electroniccountermeasure_"))
                {
                    if (module.IsOn)
                        flag1 += 0x00001000;
                    if (module.AmmoInClip > 0)
                        flag1 += 0x00002000;
                    if (module.AmmoInHopper > 0)
                        flag1 += 0x00004000;
                    if (module.Health > 0)
                        flag1 += 0x00008000;
                }

                else if (module.Item.Contains("_hyperdrive_"))
                {
                    if (module.IsOn)
                        flag2 += 0x00000001;
                    if (module.Health > 0)
                        flag2 += 0x00000002;
                }
                else if (module.Item.Contains("_engine_"))
                {
                    if (module.IsOn)
                        flag2 += 0x00000010;
                    if (module.Health > 0)
                        flag2 += 0x00000020;
                }
                else if (module.Item.Contains("_lifesupport_"))
                {
                    if (module.IsOn)
                        flag2 += 0x00000100;
                    if (module.Health > 0)
                        flag2 += 0x00000200;
                }
                else if (module.Item.Contains("_powerdistributor_"))
                {
                    if (module.IsOn)
                        flag2 += 0x00001000;
                    if (module.Health > 0)
                        flag2 += 0x00001000;
                }
                else if (module.Item.Contains("_sensors_"))
                {
                    if (module.IsOn)
                        flag2 += 0x00010000;
                    if (module.Health > 0)
                        flag2 += 0x00020000;
                }
                else if (module.Item.Contains("_shieldgenerator_"))
                {
                    if (module.IsOn)
                        flag2 += 0x00100000;
                    if (module.Health > 0)
                        flag2 += 0x00200000;
                }
                else if (module.Item.Contains("_detailedsurfacescanner_"))
                {
                    if (module.IsOn)
                        flag2 += 0x01000000;
                    if (module.Health > 0)
                        flag2 += 0x02000000;
                }
            }
            ardPort.Write("M");
            ardPort.Write(BitConverter.GetBytes(flag1), 0, 4);
            ardPort.Write(BitConverter.GetBytes(flag2), 0, 4);
            sendString("");


            var tmpShipname = !string.IsNullOrEmpty(loadoutData.ShipName)
                                ? loadoutData.ShipName
                                : !string.IsNullOrEmpty(loadoutData.ShipIdent)
                                    ? loadoutData.ShipIdent
                                    : loadoutData.ShipId;
            if (!string.IsNullOrWhiteSpace(tmpShipname))
            {
                shipName = tmpShipname;
                sendStrings(
                    ["G",
                    commanderName,
                    shipName
                ]);
            }


        }

        private void SendApproachBodyData(ApproachBodyEvent approachBodyData)
        {
            AlertMessage("**Approaching**",
                "",
                approachBodyData.StarSystem,
                approachBodyData.Body);
        }

        private void WorkerThread()
        {
            Log.Instance.Info("Worker Thread started");

            CancellationToken token = WorkerCTS.Token;
            while (token.IsCancellationRequested == false)
            {

                Thread.Sleep(500);
            }
            Log.Instance.Info("Worker Thread ending");
        }

        private void WorkerThreadEnded(Task task)
        {
            if (task.Exception != null)
            {
                Log.Instance.Info("Worker Thread Exception: {exception}", task.Exception.ToString());
            }
            if (CurrentState.State != RunState.Stopping)
            {
                Stop();
            }
            Log.Instance.Info("Worker Thread ended");
        }


        public void UpdateStatus(StatusEvent currentStatus)
        {

            UInt32 flag1 = 0;
            if (currentStatus.Docked) flag1 += 0x00000001;
            if (currentStatus.Landed) flag1 += 0x00000002;
            if (currentStatus.Gear) flag1 += 0x00000004;


            if (currentStatus.Shields) flag1 += 0x00000008;
            if (currentStatus.Supercruise) flag1 += 0x00000010;
            if (!currentStatus.FlightAssist) flag1 += 0x00000020;
            if (currentStatus.Hardpoints) flag1 += 0x00000040;
            if (currentStatus.Winging) flag1 += 0x00000080;
            if (currentStatus.Lights) flag1 += 0x00000100;
            if (currentStatus.CargoScoop) flag1 += 0x00000200;
            if (currentStatus.SilentRunning) flag1 += 0x00000400;
            if (currentStatus.Scooping) flag1 += 0x00000800;
            if (currentStatus.SrvHandbrake) flag1 += 0x00001000;
            if (currentStatus.SrvTurret) flag1 += 0x00002000;
            if (currentStatus.SrvNearShip) flag1 += 0x00004000;
            if (currentStatus.SrvDriveAssist) flag1 += 0x00008000;
            if (currentStatus.MassLocked) flag1 += 0x00010000;
            if (currentStatus.FsdCharging) flag1 += 0x00020000;
            if (currentStatus.FsdCooldown) flag1 += 0x00040000;
            if (currentStatus.LowFuel) flag1 += 0x00080000;
            if (currentStatus.Overheating) flag1 += 0x00100000;
            if (currentStatus.HasLatLong) flag1 += 0x00200000;
            if (currentStatus.InDanger) flag1 += 0x00400000;
            if (currentStatus.InInterdiction) flag1 += 0x00800000;
            if (currentStatus.InMothership) flag1 += 0x01000000;
            if (currentStatus.InFighter) flag1 += 0x02000000;
            if (currentStatus.InSrv) flag1 += 0x04000000;
            if (currentStatus.AnalysisMode) flag1 += 0x08000000;
            if (currentStatus.NightVision) flag1 += 0x10000000;
            if (currentStatus.AltitudeFromAverageRadius) flag1 += 0x20000000;
            if (currentStatus.FsdJump) flag1 += 0x40000000;
            if (currentStatus.SrvHighBeam) flag1 += 0x80000000;

            UInt32 flag2 = 0;
            // Flags 2
            if (currentStatus.OnFoot) flag2 += 0x00000001;
            if (currentStatus.InTaxi) flag2 += 0x00000002;
            if (currentStatus.InMultiCrew) flag2 += 0x00000004;
            if (currentStatus.OnFootInStation) flag2 += 0x00000008;
            if (currentStatus.OnFootOnPlanet) flag2 += 0x00000010;
            if (currentStatus.AimDownSight) flag2 += 0x00000020;
            if (currentStatus.LowOxygen) flag2 += 0x00000040;
            if (currentStatus.LowHealth) flag2 += 0x00000080;
            if (currentStatus.Cold) flag2 += 0x00000100;
            if (currentStatus.Hot) flag2 += 0x00000200;
            if (currentStatus.VeryCold) flag2 += 0x00000400;
            if (currentStatus.VeryHot) flag2 += 0x00000800;
            if (currentStatus.Gliding) flag2 += 0x00001000;
            if (currentStatus.OnFootInHangar) flag2 += 0x00002000;
            if (currentStatus.OnFootInSocialSpace) flag2 += 0x00004000;
            if (currentStatus.OnFootInExterior) flag2 += 0x00008000;
            if (currentStatus.BreathableAtmosphere) flag2 += 0x00010000;

            ardPort.Write("F");
            ardPort.Write(BitConverter.GetBytes(flag1), 0, 4);
            ardPort.Write(BitConverter.GetBytes(flag2), 0, 4);
            ardPort.Write([
                (byte)currentStatus.GuiFocus,
                (byte)currentStatus.FireGroup
            ], 0, 2);
            sendString(currentStatus.LegalState.ToString() ?? "");
        }

        public void UpdateGameInfo(LoadGameEvent currentLoadGameData)
        {
            commanderName = currentLoadGameData.Commander ?? "";
            if (!string.IsNullOrWhiteSpace(currentLoadGameData.ShipName))
                shipName = currentLoadGameData.ShipName;
            else if (!string.IsNullOrWhiteSpace(currentLoadGameData.ShipIdent))
                shipName = currentLoadGameData.ShipIdent;
            sendStrings(["G", commanderName, shipName]);
        }

        public void UpdateInfo(StatusEvent? currentStatus)
        {

            if (currentStatus != null)
            {

            }
        }

        public void UpdateTarget(ShipTargetedEvent currentTarget)
        {
        }

        public void UpdateMaxFuelData(LoadGameEvent? currentLoadGameData, LoadoutEvent? currentLoadoutData, RefuelAllEvent? currentRefuelAllData, ReservoirReplenishedEvent? currentReservoirReplenishedData)
        {


            if (currentLoadGameData != null && (this.fuelCapacity == 0 || currentLoadGameData.Value.Timestamp > lastEventUpdate))
            {
                this.fuelCapacity = currentLoadGameData.Value.FuelCapacity;
                this.fuelCurrent = currentLoadGameData.Value.FuelLevel;
                this.currentShipId = currentLoadGameData.Value.ShipId;
                Log.Instance.Info("Fuel Data found after loading game. Capacity: " + this.fuelCapacity + ". Level: " + this.fuelCurrent + ". Percentage: " + calculatePercentageValue(this.fuelCurrent, this.fuelCapacity) + "%");
            }
            if (currentLoadoutData != null && currentLoadoutData.Value.Timestamp > lastEventUpdate)
            {
                this.fuelCapacity = currentLoadoutData.Value.FuelCapacity.Main;
                this.fuelReserveMax = currentLoadoutData.Value.FuelCapacity.Reserve;
                this.currentShipId = currentLoadoutData.Value.ShipId;
                Log.Instance.Info("Fuel Data found after loading Loadout. Max Main: " + this.fuelCapacity + " (current level: " + calculatePercentageValue(this.fuelCurrent, this.fuelCapacity) + "%). Max Reserve: " + this.fuelReserveMax + " (current level: " + calculatePercentageValue(this.fuelReserve, this.fuelReserveMax) + "%).");
            }
            if (currentReservoirReplenishedData != null && currentReservoirReplenishedData.Value.Timestamp > lastEventUpdate)
            {
                Log.Instance.Info("ReservoirReplenishedData Event found. Reset Max Reservoir Fuel with next Status update");
                this.resetMaxReservoirFuel = true;
            }
            if (currentRefuelAllData != null && currentRefuelAllData.Value.Timestamp > lastEventUpdate)
            {
                this.resetMaxFuel = true;
                this.resetMaxReservoirFuel = true;
                Log.Instance.Info("RefuelAll Event found. Reset Max Fuel with next Status update");
            }
        }

        private void LandingDataCollect(string field, string fieldLabel, DockingGrantedEvent currentDockingGranted, DockingDeniedEvent currentDockingDenied, DockingTimeoutEvent currentDockingTimeout, DockingCancelledEvent currentDockingCancelled, LocationData currentLocation)
        {
            // Handle Text fields
            //if (MatricButtonList.ContainsKey(fieldLabel))
            //{
            //    MatricButtonList[fieldLabel].GameState = false;
            //    MatricButtonList[fieldLabel].OffText = FormatLandingLabel(currentDockingGranted, currentDockingDenied, currentDockingTimeout, currentDockingCancelled, currentLocation);
            //}
            //if (MatricButtonList.ContainsKey(field))
            //{
            //Log.Instance.Info("Landing Event triggered.");
            //if (currentDockingDenied != null && currentDockingDenied.Reason != "" && currentDockingDenied.Timestamp > lastEventUpdate)
            //{
            //    MatricButtonList[field].ButtonState = false;
            //    MatricButtonList[field].GameState = false;
            //    MatricButtonList[field].UpdateButtonText = true;
            //    MatricButtonList[field].OffText = FormatLandingText(currentDockingGranted, currentDockingDenied, currentDockingTimeout, currentDockingCancelled, currentLocation);
            //    MatricButtonList[field].OnText = "<table><tr><td>&nbsp;</td></tr></table>";
            //    Log.Instance.Info("Docking denied Event triggered.");
            //}
            //else
            //    if (currentDockingGranted != null && currentDockingGranted.LandingPad > 0 && currentDockingGranted.Timestamp > lastEventUpdate)
            //{
            //    MatricButtonList[field].ButtonState = true;
            //    MatricButtonList[field].GameState = true;
            //    MatricButtonList[field].UpdateButtonText = true;
            //    MatricButtonList[field].OffText = "<table><tr><td>&nbsp;</td></tr></table>";
            //    MatricButtonList[field].OnText = FormatLandingText(currentDockingGranted, currentDockingDenied, currentDockingTimeout, currentDockingCancelled, currentLocation);
            //    Log.Instance.Info("Docking granted Event triggered.");
            //}
            //else
            //        if ((currentDockingTimeout != null && currentDockingTimeout.Timestamp > lastEventUpdate) || (currentDockingCancelled != null && currentDockingCancelled.Timestamp > lastEventUpdate))
            //{
            //    MatricButtonList[field].ButtonState = false;
            //    MatricButtonList[field].GameState = false;
            //    MatricButtonList[field].UpdateButtonText = true;
            //    MatricButtonList[field].OffText = FormatLandingText(currentDockingGranted, currentDockingDenied, currentDockingTimeout, currentDockingCancelled, currentLocation);
            //    MatricButtonList[field].OnText = "<table><tr><td>&nbsp;</td></tr></table>";
            //    if (currentDockingTimeout != null)
            //        Log.Instance.Info("Docking Timeout Event triggered.");
            //    if (currentDockingCancelled != null)
            //        Log.Instance.Info("Docking Cancelled Event triggered.");
            //}
            //else
            //}
        }

        static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString.EnumerateRunes())
            {
                var unicodeCategory = Rune.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        public void sendString(string s, int maxLength = 20)
        {
            s = RemoveDiacritics(s);
            if (string.IsNullOrEmpty(s))
            {
                ardPort.Write("\0");
            }
            else
            {
                ardPort.Write(s.Substring(0, Math.Min(s.Length, maxLength)) + '\0');
            }
        }

        public void sendStrings(string[] strings, int maxLength = 20)
        {
            string r = "";
            for (int i = 0; i < strings.Length; i++)
            {
                var s = RemoveDiacritics(strings[i]);
                r += s.Substring(0, Math.Min(s.Length, maxLength)).PadRight(1);
                if (i < strings.Length - 1) r += "\t";
            }
            ardPort.Write(r + "\0");
        }

        private void ardSendLocation(LocationData currentLocation)
        {
            sendStrings(
                ["l",
                currentLocation.SystemName,
                currentLocation.StationName,
                string.IsNullOrWhiteSpace(currentLocation.StationName)
                    ? currentLocation.SystemAllegiance
                    : string.IsNullOrWhiteSpace(currentLocation.StationAllegiance) 
                        ? currentLocation.SystemAllegiance 
                        : currentLocation.StationAllegiance,
                    currentLocation.SystemSecurity
                    ]);
            //sendStrings(
            //    ["l",
            //    "systemname-lmnopqrstuv",
            //    "stationname-0145678901",
            //    "localallegiance-qrstuv",
            //    "systemsecurity-5678901",
            //        ]);

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

        private void ArdPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            WinSerialPortStream serialPort = (WinSerialPortStream)sender;
            try
            {
                while (serialPort.BytesToRead > 0)
                {
                    string indata = serialPort.ReadLine();
                    this.processArduinoData(indata);
                }
            }
            catch (TimeoutException timeoutException)
            {
                // We are out of sync with controller. Send a stop, clear buffers, and restart
                this.ardPort.Write("S");
                Thread.Sleep(100);
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                this.ardPort.Write("H");
            }
            catch (Exception ex)
            {
            }
        }

        private void processArduinoData(string datain)
        {
            //            Log.Instance.Info("received " + datain);

            if (datain.Length == 0)
                return;
            char ch = datain[0];
            try
            {
                switch (ch)
                {
                    case 'H':
                        this.ardPort.DiscardOutBuffer();
                        this.ardPort.Write("V");
                        Thread.Sleep(300);
                        break;
                    case 'S':
                        break;
                    case 'V':
                        CurrentState.Set(RunState.Started);
                        break;
                
                    default:
                        string[] strArray = datain.Split('\t');
                        string strData = string.Join('\t', strArray.Skip(1));
                       
                        if (strArray[0].Equals("I"))
                        {
                            this.info = strArray[1].Substring(0, strArray[1].Length - 1);
                            if (this.info.StartsWith("EDControlPanel"))
                            {
                                CurrentState.Set(RunState.Started);
                            }
                            else
                            {
                                Log.Instance.Error("Not an ED Control panel");
                            }

                            Log.Instance.Info(datain);
                            break;
                        }
                        if (strArray[0].Equals("l"))
                        {
                            Log.Instance.Info($"EDCP Acknowledged location: {strData}");
                            break;
                        }
                        if (strArray[0].Equals("g"))
                        {
                            Log.Instance.Info($"EDCP Acknowledged gamedata: {strData}");
                            break;
                        }
                        if (strArray[0].Equals("n"))
                        {
                            Log.Instance.Info($"EDCP Acknowledged navroute: {strData}");
                            break;
                        }


                        if (strArray[0].Equals("L"))
                        {
                            Log.Instance.Info($"EDCP Log: {strData}");
                            break;
                        }

                        if (strArray[0].Equals("f"))
                        {
                            Log.Instance.Info($"EDCP Acknowledged flags / status: {strData}");
                            break;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
