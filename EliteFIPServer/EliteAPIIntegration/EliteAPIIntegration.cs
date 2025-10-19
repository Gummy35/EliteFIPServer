using EliteAPI;
using EliteAPI.Abstractions;
using EliteAPI.Abstractions.Events;
using EliteAPI.Events;
using EliteAPI.Status.NavRoute;
using EliteAPI.Status.Ship;
using EliteAPI.Status.Ship.Events;
using EliteFIPServer.Logging;
using ExoScan.StellarStructs;
using static ExoScan.DataManager;

namespace EliteFIPServer
{

    public class EliteAPIIntegration
    {

        bool handleImplicitEvents = true;
        private CoreServer CoreServer;
        public ComponentState CurrentState { get; private set; }

        public event EventHandler<RunState> onStateChange;

        // Game State Provider
        private IEliteDangerousApi EliteAPI;
        public ApiEventDataManager currentData = new ApiEventDataManager();
        private Dictionary<Type, ApiEventHandler> handlers = new Dictionary<Type, ApiEventHandler>();

        // used for journal replay
        private bool ignoreTimeStamps = false;

        public EliteAPIIntegration(CoreServer coreServer)
        {
            CoreServer = coreServer;
            CurrentState = new ComponentState();

            EliteAPI = EliteDangerousApi.Create();
            // Add events to watch list                        

            handlers.Add(typeof(StatusEvent), HandleStatusEvent);
            handlers.Add(typeof(LocationEvent), HandleLocationEvent);
            handlers.Add(typeof(ApproachBodyEvent), HandleApproachBodyEvent);
            handlers.Add(typeof(LeaveBodyEvent), HandleLeaveBodyEvent);
            handlers.Add(typeof(StartJumpEvent), HandleStartJumpEvent);
            handlers.Add(typeof(FsdJumpEvent), HandleFsdJumpEvent);
            handlers.Add(typeof(NavRouteEvent), HandleNavRouteEvent);
            handlers.Add(typeof(NavRouteClearEvent), HandleNavRouteClearEvent);
            handlers.Add(typeof(DockedEvent), HandleDockedEvent);
            handlers.Add(typeof(UndockedEvent), HandleUndockedEvent);
            handlers.Add(typeof(GuiFocusStatusEvent), HandleGuiFocusStatusEvent);
            EliteAPI.Events.OnAny(HandleEliteApiEvent);
            EliteAPI.Events.OnAnyJson(HandleEliteApiEventJson);
            ExoData.SetEliteAPIInstance(EliteAPI);
            ExoData.ExoDataUpdateHandler += UpdateExoData;
            ExoData.OnLog += Log.Instance.Info;
        }

        private void HandleGuiFocusStatusEvent(IEvent eventData)
        {
            var data = (GuiFocusStatusEvent)eventData;
            var statusData = currentData[typeof(StatusEvent)];
            if (statusData != null)
            {
                StatusEvent statusEvent = (StatusEvent)statusData;
                statusEvent.GuiFocus = data.Value;
                statusEvent.Timestamp = data.Timestamp;
                currentData[typeof(StatusEvent)] = statusEvent;
                CoreServer.GameDataEvent(statusEvent);
            }
        }

        private void HandleEliteApiEventJson(string json, EventContext context)
        {
            //            Log.Instance.Info($"Json : " + json);
        }

        public void Start()
        {
            CurrentState.Set(RunState.Starting);
            // Start tracking game events
            EliteAPI.StartAsync();
            CurrentState.Set(RunState.Started);
        }

        public void Stop()
        {
            CurrentState.Set(RunState.Stopping);
            // Stop tracking game events
            EliteAPI.StopAsync();
            CurrentState.Set(RunState.Stopped);

        }

        public void FullClientUpdate()
        {
            CoreServer.GameDataEvent(currentData[typeof(StatusEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(ShipTargetedEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(LocationEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(ShipTargetedEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(NavRouteEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(JumpData)]);
            CoreServer.GameDataEvent(currentData[typeof(NavigationData)]);
            CoreServer.GameDataEvent(currentData[typeof(ExobiologyData)]);
            CoreServer.GameDataEvent(currentData[typeof(DockingGrantedEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(DockingDeniedEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(DockingTimeoutEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(DockingCancelledEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(LoadGameEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(LoadoutEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(RefuelAllEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(RefuelPartialEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(ReservoirReplenishedEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(FuelScoopEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(ShipyardBuyEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(ShipyardNewEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(ShipyardSwapEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(ApproachBodyEvent)]);
        }

        public void HandleEliteApiEvent(IEvent eventData, EventContext context)
        {
            Log.Instance.Info($"Handling {eventData.Event} Event");
            //            Log.Instance.Info($"Handling {eventData.Event} Event : " + JsonSerializer.Serialize(eventData));

            ExoData.HandleEliteApiEvent(eventData, context);

            if (handlers.ContainsKey(eventData.GetType()))
                handlers[eventData.GetType()](eventData);
            else if (handleImplicitEvents)
            {
                currentData[eventData.GetType()] = eventData;
                CoreServer.GameDataEvent(eventData);
            }
        }

        //private void HandleScanEvent(IEvent eventData)
        //{
        //    var data = (ScanEvent)eventData;
        //    LocationData currentLocation = (LocationData)currentData[typeof(LocationData)] ?? new LocationData();
        //    ExobiologyData exo = (ExobiologyData)currentData[typeof(ExobiologyData)] ?? new ExobiologyData();

        //    if (ignoreTimeStamps || (exo.Timestamp <= data.Timestamp))
        //    {
        //        currentData[data.GetType()] = data;
        //        CoreServer.GameDataEvent(data);
        //    }
        //}

        private void HandleUndockedEvent(IEvent eventData)
        {
            var data = (UndockedEvent)eventData;

            LocationData location = (LocationData)currentData[typeof(LocationData)] ?? new LocationData();
            currentData[typeof(LocationData)] = location.Update(data);
            CoreServer.GameDataEvent(location);

            currentData[eventData.GetType()] = eventData;
            CoreServer.GameDataEvent(eventData);
        }

        private void HandleDockedEvent(IEvent eventData)
        {
            var data = (DockedEvent)eventData;

            LocationData location = (LocationData)currentData[typeof(LocationData)] ?? new LocationData();
            currentData[typeof(LocationData)] = location.Update(data);
            CoreServer.GameDataEvent(location);

            currentData[eventData.GetType()] = eventData;
            CoreServer.GameDataEvent(eventData);
        }

        private void HandleNavRouteClearEvent(IEvent eventData)
        {
            var navRouteClear = (NavRouteClearEvent)eventData;
            var currentNavRoute = (NavigationData)currentData[typeof(NavigationData)] ?? new NavigationData();
            if (currentNavRoute.Timestamp <= navRouteClear.Timestamp)
            {
                //if (currentNavRoute.NavRouteActive)
                //{
                //    previousNavRoute = currentNavRoute;
                //    previousNavRoute.NavRouteActive = false;
                //    if (currentJump.JumpComplete == false)
                //    {
                //        previousNavRoute.LastSystemReached = currentJump.DestinationSystemName;
                //    }
                //    CoreServer.GameDataEvent(GameEventType.PreviousNavRoute, previousNavRoute);
                //}
                currentData[typeof(NavigationData)] = currentNavRoute.Update(navRouteClear);
                CoreServer.GameDataEvent(currentNavRoute);
            }
        }

        private void HandleNavRouteEvent(IEvent eventData)
        {
            var currentNavRouteData = (NavRouteEvent)eventData;
            var currentNavRoute = (NavigationData)currentData[typeof(NavigationData)] ?? new NavigationData();
            if ((currentNavRoute.Timestamp <= currentNavRouteData.Timestamp) && (currentNavRouteData.Stops != null) && (currentNavRouteData.Stops.Count() != 0))
            {
                currentNavRoute = new NavigationData(currentNavRouteData);
                currentData[typeof(NavigationData)] = currentNavRoute;
                CoreServer.GameDataEvent(currentNavRoute);
            }
        }

        private void HandleFsdJumpEvent(IEvent eventData)
        {
            var fsdJumpdataData = (FsdJumpEvent)eventData;
            LocationData currentLocation = (LocationData)currentData[typeof(LocationData)] ?? new LocationData();

            if (currentLocation.Timestamp <= fsdJumpdataData.Timestamp)
            {
                currentData[typeof(LocationData)] = currentLocation.Update(fsdJumpdataData);
                CoreServer.GameDataEvent(currentLocation);
            }

            var currentJump = (JumpData)currentData[typeof(JumpData)] ?? new JumpData();
            if (currentJump.Timestamp <= fsdJumpdataData.Timestamp)
            {
                currentData[typeof(JumpData)] = currentJump.Update(fsdJumpdataData);
                CoreServer.GameDataEvent(currentJump);
            }

            var currentNavRoute = (NavigationData)currentData[typeof(NavigationData)] ?? new NavigationData();
            if (currentNavRoute.Timestamp <= fsdJumpdataData.Timestamp && currentNavRoute.NavRouteActive &&
                currentNavRoute.Stops != null && currentNavRoute.Stops.Count() != 0)
            {
                currentData[typeof(NavigationData)] = currentNavRoute.Update(fsdJumpdataData);
            }
        }

        private void HandleStartJumpEvent(IEvent eventData)
        {
            var data = (StartJumpEvent)eventData;
            JumpData currentJump = (JumpData)currentData[typeof(JumpData)] ?? new JumpData();
            if (currentJump.Timestamp <= data.Timestamp && data.JumpType == "Hyperspace")
            {
                LocationData locationData = (LocationData)currentData[typeof(LocationData)] ?? new LocationData();
                currentData[typeof(JumpData)] = currentJump.Update(locationData, data);
                CoreServer.GameDataEvent(currentJump);
            }
        }

        private void HandleLeaveBodyEvent(IEvent eventData)
        {
            var data = (LeaveBodyEvent)eventData;

            LocationData location = (LocationData)currentData[typeof(LocationData)] ?? new LocationData();
            currentData[typeof(LocationData)] = location.Update(data);
            CoreServer.GameDataEvent(location);

            currentData[eventData.GetType()] = eventData;
            CoreServer.GameDataEvent(eventData);
        }

        private void HandleApproachBodyEvent(IEvent eventData)
        {
            var data = (ApproachBodyEvent)eventData;

            LocationData location = (LocationData)currentData[typeof(LocationData)] ?? new LocationData();
            currentData[typeof(LocationData)] = location.Update(data);
            CoreServer.GameDataEvent(location);

            currentData[eventData.GetType()] = eventData;
            CoreServer.GameDataEvent(eventData);
        }

        private void HandleLocationEvent(IEvent eventData)
        {
            var data = (LocationEvent)eventData;
            LocationData location = new LocationData(data);
            ExoData.HandleLocation(data);
            currentData[typeof(LocationData)] = location;
            CoreServer.GameDataEvent(location);
        }

        private void HandleStatusEvent(IEvent eventData)
        {
            StatusEvent statusEvent = (StatusEvent)eventData;
            if (statusEvent.Available)
            {
                currentData[eventData.GetType()] = eventData;
            }
            CoreServer.GameDataEvent(eventData);
        }

        internal void ImportAll()
        {
            ignoreTimeStamps = true;
            ExoData.ImportAllJournals();
            ignoreTimeStamps = false;
        }

        //public void HandleShipTargetedEvent(EliteAPI.Events.ShipTargetedEvent currentTargetData, EventContext context)
        //{

        //    Log.Instance.Info("Handling ShipTargetedEvent");
        //    Log.Instance.Info("Targetlock: {istargetlocked}, Scanstage: {scanstage}", currentTargetData.IsTargetLocked.ToString(), currentTargetData.ScanStage.ToString());
        //    ShipTargetedData newTargetData = new ShipTargetedData();

        //    TimeSpan dataAge = DateTime.UtcNow.Subtract(currentTargetData.Timestamp);
        //    Log.Instance.Info("Target data age: {targetage}", dataAge.ToString());
        //    if (dataAge.TotalMinutes < 5)
        //    {
        //        newTargetData.LastUpdate = currentTargetData.Timestamp;
        //        newTargetData.TargetLocked = currentTargetData.IsTargetLocked;
        //        if (newTargetData.TargetLocked == true)
        //        {

        //            // Ensure ship name starts with a Uppercase letter to look nice for nonlocalised ships
        //            newTargetData.Ship = char.ToUpper(currentTargetData.Ship.ToString()[0]) + currentTargetData.Ship.ToString().Substring(1);

        //            // In ELiteAPI Scanstage is a long, in ELiteFIPProtocl it's an int.
        //            newTargetData.ScanStage = (int)currentTargetData.ScanStage;
        //            if (newTargetData.ScanStage >= 1)
        //            {
        //                if (String.IsNullOrEmpty(currentTargetData.PilotName.ToString()))
        //                {
        //                    newTargetData.PilotName = currentTargetData.PilotName.Symbol;
        //                }
        //                else
        //                {
        //                    newTargetData.PilotName = currentTargetData.PilotName.ToString();
        //                }
        //                newTargetData.PilotRank = currentTargetData.PilotRank;
        //            }
        //            if (newTargetData.ScanStage >= 2)
        //            {
        //                newTargetData.ShieldHealth = currentTargetData.ShieldHealth;
        //                newTargetData.HullHealth = currentTargetData.HullHealth;
        //            }
        //            if (newTargetData.ScanStage >= 3)
        //            {
        //                newTargetData.Faction = currentTargetData.Faction;
        //                newTargetData.LegalStatus = currentTargetData.LegalStatus;
        //                newTargetData.SubSystemHealth = currentTargetData.SubsystemHealth;
        //                newTargetData.Bounty = currentTargetData.Bounty;
        //                if (String.IsNullOrEmpty(currentTargetData.Subsystem.ToString()))
        //                {
        //                    newTargetData.SubSystem = currentTargetData.Subsystem.Symbol;
        //                }
        //                else
        //                {
        //                    newTargetData.SubSystem = currentTargetData.Subsystem.ToString();
        //                }
        //            }
        //        }
        //        currentTarget = newTargetData;

        //        CoreServer.GameDataEvent(GameEventType.Target, currentTarget);
        //    }
        //}


        #region Exobiology handling

        /// <summary>
        /// This method is called by ExoScan when internal scan related data changed.
        /// </summary>
        /// <param name="System"></param>
        /// <param name="bodyId"></param>
        /// <param name="timestamp"></param>
        private void UpdateExoData(StarSystem System, int? bodyId, DateTime timestamp)
        {
            ExobiologyData exo = (ExobiologyData)currentData[typeof(ExobiologyData)] ?? new ExobiologyData();
            if (ignoreTimeStamps || (exo.Timestamp <= timestamp))
            {
                System.UpdateScanResults(bodyId, out var hasChanges);
                if (hasChanges)
                {
                    exo.Update(System, bodyId, timestamp);
                    currentData[typeof(ExobiologyData)] = exo;
                    CoreServer.GameDataEvent(exo);
                }
            }
        }

        public List<ExobiologyData> GetSystems(Position currentPosition = null, double range = 1000, bool retriggerEvent = false)
        {
            IEnumerable<StarSystem> systems = ExoData.GetSystemsInRange(currentPosition, range);
            var result = new List<ExobiologyData>();
            foreach (var system in systems)
            {
                ExobiologyData exo = new ExobiologyData();
                if (system.ScanResults.Count > 0)
                {
                    exo.Update(system, null, DateTime.Now);
                    result.Add(exo);
                    if (retriggerEvent)
                    {
                        currentData[typeof(ExobiologyData)] = exo;
                        CoreServer.GameDataEvent(exo);
                    }
                }
            }
            return result;
        }

        #endregion
    }
}
