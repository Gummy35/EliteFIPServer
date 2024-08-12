using EliteAPI;
using EliteAPI.Abstractions;
using EliteAPI.Abstractions.Events;
using EliteAPI.Events;
using EliteAPI.Status.Modules;
using EliteAPI.Status.NavRoute;
using EliteAPI.Status.Ship;
using EliteFIPServer.Logging;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using System.Text.Json;


namespace EliteFIPServer {

    public class LocationData: IEvent
    {
        public LocationData()
        {

        }
        public LocationData(LocationEvent currentLocationData)
        {
            Timestamp = currentLocationData.Timestamp;
            SystemId = currentLocationData.SystemAddress;
            SystemName = currentLocationData.StarSystem;
            BodyId = currentLocationData.BodyId;
            Body = currentLocationData.Body;
            MarketId = currentLocationData.MarketId;
            StationName = currentLocationData.StationName;
            StationType = currentLocationData.StationType;
            StationFaction = currentLocationData.StationFaction.Name;
            StationGovernment = currentLocationData.StationGovernment.Local;
            StationAllegiance = currentLocationData.StationAllegiance;
            StationEconomy = currentLocationData.StationEconomy.Local;
            DistanceFromStarInLightSeconds = currentLocationData.DistanceFromStarInLightSeconds;
            SystemAllegiance = currentLocationData.SystemAllegiance;
            SystemSecurity = currentLocationData.SystemSecurity.Local;
        }

        public string SystemId { get; set; }
        public string SystemName { get; set; }
        public string BodyId { get; set; }
        public string Body { get; set; }
        public string MarketId { get; set; }
        public string StationName { get; set; }
        public string StationType { get; set; }
        public double DistanceFromStarInLightSeconds { get; set; }
        public string StationAllegiance { get; set; }
        public string StationEconomy { get; set; }
        public string StationFaction { get; set; }
        public string StationGovernment { get; set; }
        public string SystemAllegiance { get; set; }
        public string SystemSecurity { get; set; }

        public DateTime Timestamp { get; set; }

        public string Event => "Location";


        public LocationData Update(ApproachBodyEvent data)
        {
            Timestamp = data.Timestamp;
            BodyId = data.BodyId;
            Body = data.Body;
            SystemName = data.StarSystem;
            SystemId = data.SystemAddress;
            return this;
        }

        public LocationData Update(FsdJumpEvent fsdJumpdataData)
        {
            Timestamp = fsdJumpdataData.Timestamp;
            SystemId = fsdJumpdataData.SystemAddress;
            SystemName = fsdJumpdataData.StarSystem;
            BodyId = fsdJumpdataData.BodyId;
            Body = fsdJumpdataData.Body;
            SystemAllegiance = fsdJumpdataData.SystemAllegiance;
            SystemSecurity = fsdJumpdataData.SystemSecurity.Local;
            return this;
        }

        public LocationData Update(LeaveBodyEvent data)
        {
            Timestamp = data.Timestamp;
            BodyId = "";
            Body = "";

            return this;
        }

        public LocationData Update(DockedEvent dockedData)
        {
            Timestamp = dockedData.Timestamp;
            MarketId = dockedData.MarketId;
            StationName = dockedData.StationName;
            StationType = dockedData.StationType;
            return this;
        }

        public LocationData Update(UndockedEvent undockedData)
        {
            Timestamp = undockedData.Timestamp;
            MarketId = "";
            StationName = "";
            StationType = "";
            return this;
        }
    }

    public class NavigationData : IEvent
    {
        public NavigationData() {
        }
        public NavigationData(NavRouteEvent currentNavRouteData)
        {
            Timestamp = currentNavRouteData.Timestamp;
            Log.Instance.Info("New route has {jumpcount} jumps", currentNavRouteData.Stops.Count());
            NavRouteActive = true;
            Stops.Clear();
            foreach (EliteAPI.Status.NavRoute.NavRouteStop navRouteStop in currentNavRouteData.Stops)
            {
                NavigationData.NavRouteStop navStop = new NavigationData.NavRouteStop();
                navStop.SystemId = navRouteStop.Address;
                navStop.SystemName = navRouteStop.System;
                navStop.Class = navRouteStop.Class;
                Stops.Add(navStop);
            }
        }

        public bool NavRouteActive { get; set; }

        public string LastSystemReached { get; set; }
        public List<NavRouteStop> Stops { get; set; } = new List<NavRouteStop>();

        public DateTime Timestamp { get; set; }

        public string Event => "Navigation";

        public class NavRouteStop
        {
            public string SystemId { get; set; }
            public string SystemName { get; set; }
            public string Class { get; set; }
        }

        public NavigationData Update(FsdJumpEvent fsdJumpdataData)
        {
            foreach (NavRouteStop navRouteStop in Stops)
            {
                if (navRouteStop.SystemName == fsdJumpdataData.StarSystem)
                {
                    LastSystemReached = fsdJumpdataData.StarSystem;
                }
            }
            return this;
        }

        public NavigationData Update(NavRouteClearEvent clearEvent)
        {
            Timestamp = clearEvent.Timestamp;
            NavRouteActive = false;
            Stops.Clear();
            return this;

        }
    }

    public class JumpData : IEvent
    {
        public bool JumpComplete { get; set; }
        public string OriginSystemId { get; set; }
        public string OriginSystemName { get; set; }
        public string DestinationSystemId { get; set; }
        public string DestinationSystemName { get; set; }
        public string DestinationSystemClass { get; set; }
        public double JumpDistance { get; set; }
        public double FuelUsed { get; set; }
        public DateTime Timestamp { get; set; }
        public string Event => "Jump";

        public JumpData Update(LocationData currentLocation, StartJumpEvent data)
        {
            Timestamp = data.Timestamp;
            JumpComplete = false;

            OriginSystemId = currentLocation.SystemId;
            OriginSystemName = currentLocation.SystemName;
            DestinationSystemId = data.SystemAddress;
            DestinationSystemName = data.StarSystem;
            DestinationSystemClass = data.StarClass;
            JumpDistance = 0;
            FuelUsed = 0;

            return this;
        }

        public JumpData Update(FsdJumpEvent fsdJumpdataData)
        {
            Timestamp = fsdJumpdataData.Timestamp;
            DestinationSystemId = fsdJumpdataData.SystemAddress;
            DestinationSystemName = fsdJumpdataData.StarSystem;
            JumpDistance = fsdJumpdataData.JumpDist;
            FuelUsed = fsdJumpdataData.FuelUsed;
            JumpComplete = true;
            return this;
        }
    }

    public class ApiEventDataManager
    {
        private Dictionary<Type, IEvent> lastData = new Dictionary<Type, IEvent>();
        
        public IEvent this[Type evtType]
        {
            get
            {
                if (lastData.TryGetValue(evtType, out IEvent value))
                {
                    return value;
                }
                return null;
            }
            set
            {
                lastData[evtType] = value;
            }
        }

    }

    public delegate void ApiEventHandler(IEvent @event);
    public class EliteAPIIntegration {

        bool handleImplicitEvents = true;
        private CoreServer CoreServer;
        public ComponentState CurrentState { get; private set; }

        public event EventHandler<RunState> onStateChange;

        // Game State Provider
        private IEliteDangerousApi EliteAPI;
        public ApiEventDataManager currentData = new ApiEventDataManager();
        private Dictionary<Type, ApiEventHandler> handlers = new Dictionary<Type, ApiEventHandler>();

        public EliteAPIIntegration(CoreServer coreServer) {
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
            EliteAPI.Events.OnAny(HandleEliteApiEvent);
            // EliteAPI.Events.OnAnyJson(HandleEliteApiEventJson);
        }

        private void HandleEliteApiEventJson(string json, EventContext context)
        {
            Log.Instance.Info($"Json : " + json);
        }

        public void Start() {
            CurrentState.Set(RunState.Starting);
            // Start tracking game events
            EliteAPI.StartAsync();
            CurrentState.Set(RunState.Started);
        }

        public void Stop() {
            CurrentState.Set(RunState.Stopping);
            // Stop tracking game events
            EliteAPI.StopAsync();
            CurrentState.Set(RunState.Stopped);

        }

        public void FullClientUpdate() {
            CoreServer.GameDataEvent(currentData[typeof(StatusEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(ShipTargetedEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(LocationEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(ShipTargetedEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(NavRouteEvent)]);
            CoreServer.GameDataEvent(currentData[typeof(JumpData)]);
            CoreServer.GameDataEvent(currentData[typeof(NavigationData)]);
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
            if (handlers.ContainsKey(eventData.GetType()))
                handlers[eventData.GetType()](eventData);
            else if (handleImplicitEvents)
            {
                currentData[eventData.GetType()] = eventData;
                CoreServer.GameDataEvent(eventData);
            }
        }

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

    }
}
