using EliteAPI;
using EliteAPI.Abstractions;
using EliteAPI.Abstractions.Events;
using EliteFIPProtocol;
using EliteFIPServer.Logging;
using System.Collections.ObjectModel;

namespace EliteFIPServer {
    public class EliteAPIIntegration {

        private CoreServer CoreServer;
        public ComponentState CurrentState { get; private set; }

        public event EventHandler<RunState> onStateChange;

        // Game State Provider
        private IEliteDangerousApi EliteAPI;

        // Current Game State Information
        private StatusData currentStatus = new StatusData();
        private ShipTargetedData currentTarget = new ShipTargetedData();
        private LocationData currentLocation = new LocationData();
        private NavigationData currentNavRoute = new NavigationData();
        private NavigationData previousNavRoute = new NavigationData();
        private JumpData currentJump = new JumpData();
        private DockingGrantedData currentDockingGranted = new DockingGrantedData();
        private DockingDeniedData currentDockingDenied = new DockingDeniedData();
        private DockingTimeoutData currentDockingTimeout = new DockingTimeoutData();
        private DockingCancelledData currentDockingCancelled = new DockingCancelledData();
        private LoadGameData currentLoadGame = new LoadGameData();
        private LoadoutData currentLoadout = new LoadoutData();
        private RefuelAllData currentRefuelAll = new RefuelAllData();
        private RefuelPartialData currentRefuelPartial = new RefuelPartialData();
        private ReservoirReplenishedData currentReservoirReplenished = new ReservoirReplenishedData();
        private FuelScoopData currentFuelScoop = new FuelScoopData();
        private ShipyardBuyData currentShipyardBuy = new ShipyardBuyData();
        private ShipyardNewData currentShipyardNew = new ShipyardNewData();
        private ShipyardSwapData currentShipyardSwap = new ShipyardSwapData();

        public EliteAPIIntegration(CoreServer coreServer) {
            CoreServer = coreServer;
            CurrentState = new ComponentState();

            EliteAPI = EliteDangerousApi.Create();

            // Add events to watch list            
            EliteAPI.Events.On<EliteAPI.Status.Ship.StatusEvent>(HandleStatusEvent);
            EliteAPI.Events.On<EliteAPI.Events.ShipTargetedEvent>(HandleShipTargetedEvent);
            EliteAPI.Events.On<EliteAPI.Events.LocationEvent>(HandleLocationEvent);
            EliteAPI.Events.On<EliteAPI.Events.StartJumpEvent>(HandleStartJumpEvent);
            EliteAPI.Events.On<EliteAPI.Events.FsdJumpEvent>(HandleFsdJumpEvent);
            EliteAPI.Events.On<EliteAPI.Status.NavRoute.NavRouteEvent>(HandleNavRouteEvent);
            EliteAPI.Events.On<EliteAPI.Events.NavRouteClearEvent>(HandleNavRouteClearEvent);
            EliteAPI.Events.On<EliteAPI.Events.ApproachBodyEvent>(HandleApproachBodyEvent);
            EliteAPI.Events.On<EliteAPI.Events.LeaveBodyEvent>(HandleLeaveBodyEvent);
            EliteAPI.Events.On<EliteAPI.Events.DockedEvent>(HandleDockedEvent);
            EliteAPI.Events.On<EliteAPI.Events.UndockedEvent>(HandleUndockedEvent);
            EliteAPI.Events.On<EliteAPI.Events.DockingGrantedEvent>(HandleDockingGrantedEvent);
            EliteAPI.Events.On<EliteAPI.Events.DockingDeniedEvent>(HandleDockingDeniedEvent);
            EliteAPI.Events.On<EliteAPI.Events.DockingTimeoutEvent>(HandleDockingTimeoutEvent);
            EliteAPI.Events.On<EliteAPI.Events.DockingCancelledEvent>(HandleDockingCancelledEvent);
            EliteAPI.Events.On<EliteAPI.Events.LoadGameEvent>(HandleLoadGameEvent);
            EliteAPI.Events.On<EliteAPI.Events.LoadoutEvent>(HandleLoadoutEvent);
            EliteAPI.Events.On<EliteAPI.Events.RefuelAllEvent>(HandleRefuelAllEvent);
            EliteAPI.Events.On<EliteAPI.Events.RefuelPartialEvent>(HandleRefuelPartialEvent);
            EliteAPI.Events.On<EliteAPI.Events.ReservoirReplenishedEvent>(HandleReservoirReplenishedEvent);
            EliteAPI.Events.On<EliteAPI.Events.FuelScoopEvent>(HandleFuelScoopEvent);
            EliteAPI.Events.On<EliteAPI.Events.ShipyardBuyEvent>(HandleShipyardBuyEvent);
            EliteAPI.Events.On<EliteAPI.Events.ShipyardNewEvent>(HandleShipyardNewEvent);
            EliteAPI.Events.On<EliteAPI.Events.ShipyardSwapEvent>(HandleShipyardSwapEvent);
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
            if (currentStatus != null) { CoreServer.GameDataEvent(GameEventType.Status, currentStatus); }
            if (currentTarget != null) { CoreServer.GameDataEvent(GameEventType.Target, currentTarget); }
            if (currentLocation != null) { CoreServer.GameDataEvent(GameEventType.Location, currentLocation); }
            if (currentNavRoute != null && currentNavRoute.NavRouteActive) { CoreServer.GameDataEvent(GameEventType.Navigation, currentNavRoute); }
            if (previousNavRoute != null) { CoreServer.GameDataEvent(GameEventType.PreviousNavRoute, previousNavRoute); }
            if (currentJump != null) { CoreServer.GameDataEvent(GameEventType.Jump, currentJump); }
            if (currentDockingGranted != null) { CoreServer.GameDataEvent(GameEventType.DockingGranted, currentDockingGranted); }
            if (currentDockingDenied != null) { CoreServer.GameDataEvent(GameEventType.DockingDenied, currentDockingDenied); }
            if (currentDockingTimeout != null) { CoreServer.GameDataEvent(GameEventType.DockingTimeout, currentDockingTimeout); }
            if (currentDockingCancelled != null) { CoreServer.GameDataEvent(GameEventType.DockingCancelled, currentDockingCancelled); }
            if (currentLoadGame != null) { CoreServer.GameDataEvent(GameEventType.LoadGame, currentLoadGame); }
            if (currentLoadout != null) { CoreServer.GameDataEvent(GameEventType.Loadout, currentLoadout); }
            if (currentRefuelAll != null) { CoreServer.GameDataEvent(GameEventType.RefuelAll, currentRefuelAll); }
            if (currentRefuelPartial != null) { CoreServer.GameDataEvent(GameEventType.RefuelPartial, currentRefuelPartial); }            
            if (currentReservoirReplenished != null) { CoreServer.GameDataEvent(GameEventType.ReservoirReplenished, currentReservoirReplenished); }
            if (currentFuelScoop != null) { CoreServer.GameDataEvent(GameEventType.FuelScoop, currentFuelScoop); }
            if (currentShipyardBuy != null) { CoreServer.GameDataEvent(GameEventType.ShipyardBuy, currentShipyardBuy); }            
            if (currentShipyardNew != null) { CoreServer.GameDataEvent(GameEventType.ShipyardNew, currentShipyardNew); }
            if (currentShipyardSwap != null) { CoreServer.GameDataEvent(GameEventType.ShipyardSwap, currentShipyardSwap); }
        }

        public void HandleStatusEvent(EliteAPI.Status.Ship.StatusEvent currentStatusData, EventContext context) {

            Log.Instance.Info("Handling Status Event");

            currentStatus.LastUpdate = currentStatusData.Timestamp;

            // Original Status Flags
            if (currentStatusData.Available) {
                currentStatus.Docked = currentStatusData.Docked;
                currentStatus.Landed = currentStatusData.Landed;
                currentStatus.LandingGearDown = currentStatusData.Gear;
                currentStatus.ShieldsUp = currentStatusData.Shields;
                currentStatus.Supercruise = currentStatusData.Supercruise;
                currentStatus.FlightAssistOff = !currentStatusData.FlightAssist;
                currentStatus.HardpointsDeployed = currentStatusData.Hardpoints;
                currentStatus.InWing = currentStatusData.Winging;
                currentStatus.LightsOn = currentStatusData.Lights;
                currentStatus.CargoScoopDeployed = currentStatusData.CargoScoop;
                currentStatus.SilentRunning = currentStatusData.SilentRunning;
                currentStatus.ScoopingFuel = currentStatusData.Scooping;
                currentStatus.SrvHandbrake = currentStatusData.SrvHandbrake;
                currentStatus.SrvTurret = currentStatusData.SrvTurret;
                currentStatus.SrvUnderShip = currentStatusData.SrvNearShip;
                currentStatus.SrvDriveAssist = currentStatusData.SrvDriveAssist;
                currentStatus.FsdMassLocked = currentStatusData.MassLocked;
                currentStatus.FsdCharging = currentStatusData.FsdCharging;
                currentStatus.FsdCooldown = currentStatusData.FsdCooldown;
                currentStatus.LowFuel = currentStatusData.LowFuel;
                currentStatus.Overheating = currentStatusData.Overheating;
                if (currentStatusData.HasLatLong) {
                    currentStatus.HasLatLong = true;
                    currentStatus.Latitude = currentStatusData.Latitude;
                    currentStatus.Longitude = currentStatusData.Longitude;
                } else {
                    currentStatus.HasLatLong = false;
                    currentStatus.Latitude = 0;
                    currentStatus.Longitude = 0;
                }
                currentStatus.InDanger = currentStatusData.InDanger;
                currentStatus.BeingInterdicted = currentStatusData.InInterdiction;
                currentStatus.InMainShip = currentStatusData.InMothership;
                currentStatus.InFighter = currentStatusData.InFighter;
                currentStatus.InSRV = currentStatusData.InSrv;
                currentStatus.HudAnalysisMode = currentStatusData.AnalysisMode;
                currentStatus.NightVision = currentStatusData.NightVision;
                currentStatus.FsdJump = currentStatusData.FsdJump;
                currentStatus.AltitudeFromAverageRadius = currentStatusData.AltitudeFromAverageRadius;
                currentStatus.SrvHighBeam = currentStatusData.SrvHighBeam;

                currentStatus.OnFoot = currentStatusData.OnFoot;
                currentStatus.InTaxi = currentStatusData.InTaxi;
                currentStatus.InMulticrew = currentStatusData.InMultiCrew;
                currentStatus.OnFootInStation = currentStatusData.OnFootInStation;
                currentStatus.OnFootOnPlanet = currentStatusData.OnFootOnPlanet;
                currentStatus.AimDownSight = currentStatusData.AimDownSight;
                currentStatus.LowOxygen = currentStatusData.LowOxygen;
                currentStatus.LowHealth = currentStatusData.LowHealth;
                currentStatus.Cold = currentStatusData.Cold;
                currentStatus.Hot = currentStatusData.Hot;
                currentStatus.VeryCold = currentStatusData.VeryCold;
                currentStatus.VeryHot = currentStatusData.VeryHot;

                currentStatus.SystemPips = (int)currentStatusData.Pips.System;
                currentStatus.EnginePips = (int)currentStatusData.Pips.Engines;
                currentStatus.WeaponPips = (int)currentStatusData.Pips.Weapons;
                currentStatus.FireGroup = (int)currentStatusData.FireGroup;
                currentStatus.GuiFocus = currentStatusData.GuiFocus.ToString();
                currentStatus.FuelMain = currentStatusData.Fuel.FuelMain;
                currentStatus.FuelReservoir = currentStatusData.Fuel.FuelReservoir;
                currentStatus.Cargo = currentStatusData.Cargo;
                currentStatus.LegalState = currentStatusData.LegalState.ToString();
                currentStatus.Altitude = currentStatusData.Altitude;
                currentStatus.Heading = currentStatusData.Heading;
                currentStatus.BodyName = currentStatusData.Body;
                currentStatus.PlanetRadius = currentStatusData.BodyRadius;
                currentStatus.Balance = currentStatusData.Balance;
                currentStatus.DestinationSystem = currentStatusData.Destination.SystemId;
                currentStatus.DestinationBody = currentStatusData.Destination.BodyId;
                currentStatus.DestinationName = currentStatusData.Destination.Name;
                currentStatus.Oxygen = currentStatusData.Oxygen;
                currentStatus.Health = currentStatusData.Health;
                currentStatus.Temperature = currentStatusData.Temperature;
                currentStatus.SelectedWeapon = currentStatusData.SelectedWeapon.ToString();
                currentStatus.Gravity = currentStatusData.Gravity;
            }
            CoreServer.GameDataEvent(GameEventType.Status, currentStatus);
        }

        public void HandleShipTargetedEvent(EliteAPI.Events.ShipTargetedEvent currentTargetData, EventContext context) {

            Log.Instance.Info("Handling ShipTargetedEvent");
            Log.Instance.Info("Targetlock: {istargetlocked}, Scanstage: {scanstage}", currentTargetData.IsTargetLocked.ToString(), currentTargetData.ScanStage.ToString());
            ShipTargetedData newTargetData = new ShipTargetedData();

            TimeSpan dataAge = DateTime.UtcNow.Subtract(currentTargetData.Timestamp);
            Log.Instance.Info("Target data age: {targetage}", dataAge.ToString());
            if (dataAge.TotalMinutes < 5) {
                newTargetData.LastUpdate = currentTargetData.Timestamp;
                newTargetData.TargetLocked = currentTargetData.IsTargetLocked;
                if (newTargetData.TargetLocked == true) {

                    // Ensure ship name starts with a Uppercase letter to look nice for nonlocalised ships
                    newTargetData.Ship = char.ToUpper(currentTargetData.Ship.ToString()[0]) + currentTargetData.Ship.ToString().Substring(1);

                    // In ELiteAPI Scanstage is a long, in ELiteFIPProtocl it's an int.
                    newTargetData.ScanStage = (int)currentTargetData.ScanStage;
                    if (newTargetData.ScanStage >= 1) {
                        if (String.IsNullOrEmpty(currentTargetData.PilotName.ToString())) {
                            newTargetData.PilotName = currentTargetData.PilotName.Symbol;
                        } else {
                            newTargetData.PilotName = currentTargetData.PilotName.ToString();
                        }
                        newTargetData.PilotRank = currentTargetData.PilotRank;
                    }
                    if (newTargetData.ScanStage >= 2) {
                        newTargetData.ShieldHealth = currentTargetData.ShieldHealth;
                        newTargetData.HullHealth = currentTargetData.HullHealth;
                    }
                    if (newTargetData.ScanStage >= 3) {
                        newTargetData.Faction = currentTargetData.Faction;
                        newTargetData.LegalStatus = currentTargetData.LegalStatus;
                        newTargetData.SubSystemHealth = currentTargetData.SubsystemHealth;
                        newTargetData.Bounty = currentTargetData.Bounty;
                        if (String.IsNullOrEmpty(currentTargetData.Subsystem.ToString())) {
                            newTargetData.SubSystem = currentTargetData.Subsystem.Symbol;
                        } else {
                            newTargetData.SubSystem = currentTargetData.Subsystem.ToString();
                        }
                    }
                }
                currentTarget = newTargetData;

                CoreServer.GameDataEvent(GameEventType.Target, currentTarget);
            }
        }

        public void HandleShipyardBuyEvent(EliteAPI.Events.ShipyardBuyEvent currentShipyardBuyData, EventContext context)
        {

            Log.Instance.Info("Handling ShipyardBuy Event");

            currentShipyardBuy.Timestamp = currentShipyardBuyData.Timestamp;
            currentShipyardBuy.Event = currentShipyardBuyData.Event;
            currentShipyardBuy.ShipType = currentShipyardBuyData.ShipType.Local;
            currentShipyardBuy.ShipPrice = currentShipyardBuyData.ShipPrice;
            currentShipyardBuy.StoreOldShip = currentShipyardBuyData.StoreOldShip;
            currentShipyardBuy.MarketId = currentShipyardBuyData.MarketId;
            currentShipyardBuy.StoreShipId = currentShipyardBuyData.StoreShipId;
            currentShipyardBuy.MarketId = currentShipyardBuyData.MarketId;
            currentShipyardBuy.SellOldShip = currentShipyardBuyData.SellOldShip;
            currentShipyardBuy.SellShipID = currentShipyardBuyData.SellShipID;
            currentShipyardBuy.SellPrice = currentShipyardBuyData.SellPrice;            

            CoreServer.GameDataEvent(GameEventType.ShipyardBuy, currentShipyardBuy);

        }

        public void HandleShipyardNewEvent(EliteAPI.Events.ShipyardNewEvent currentShipyardNewData, EventContext context)
        {

            Log.Instance.Info("Handling ShipyardNew Event");

            currentShipyardNew.Timestamp = currentShipyardNewData.Timestamp;
            currentShipyardNew.Event = currentShipyardNewData.Event;
            currentShipyardNew.ShipType = currentShipyardNewData.ShipType.Local;
            currentShipyardNew.NewShipId = currentShipyardNewData.NewShipId;

            CoreServer.GameDataEvent(GameEventType.ShipyardNew, currentShipyardNew);

        }

        public void HandleShipyardSwapEvent(EliteAPI.Events.ShipyardSwapEvent currentShipyardSwapData, EventContext context)
        {

            Log.Instance.Info("Handling ShipyardSwap Event");

            currentShipyardSwap.Timestamp = currentShipyardSwapData.Timestamp;
            currentShipyardSwap.Event = currentShipyardSwapData.Event;
            currentShipyardSwap.ShipType = currentShipyardSwapData.ShipType.Local;
            currentShipyardSwap.ShipId = currentShipyardSwapData.ShipId;
            currentShipyardSwap.StoreOldShip = currentShipyardSwapData.StoreOldShip;
            currentShipyardSwap.MarketId = currentShipyardSwapData.MarketId;
            currentShipyardSwap.StoreShipId = currentShipyardSwapData.StoreShipId;
            currentShipyardSwap.MarketId = currentShipyardSwapData.MarketId;            

            CoreServer.GameDataEvent(GameEventType.ShipyardSwap, currentShipyardSwap);

        }

        public void HandleLocationEvent(EliteAPI.Events.LocationEvent currentLocationData, EventContext context) {

            Log.Instance.Info("Handling Location Event");

            currentLocation.LastUpdate = currentLocationData.Timestamp;
            currentLocation.SystemId = currentLocationData.SystemAddress;
            currentLocation.SystemName = currentLocationData.StarSystem;
            currentLocation.BodyId = currentLocationData.BodyId;
            currentLocation.BodyName =  currentLocationData.Body;
            currentLocation.MarketId = currentLocationData.MarketId;
            currentLocation.StationName = currentLocationData.StationName;
            currentLocation.StationType = currentLocationData.StationType;
            currentLocation.StationFaction = currentLocationData.StationFaction.Name;
            currentLocation.StationGovernment = currentLocationData.StationGovernment.Local;
            currentLocation.StationAllegiance = currentLocationData.StationAllegiance;
            currentLocation.StationEconomy = currentLocationData.StationEconomy.Local;
            currentLocation.DistanceFromStarInLightSeconds = currentLocationData.DistanceFromStarInLightSeconds;

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);

        }

        public void HandleLoadGameEvent(EliteAPI.Events.LoadGameEvent currentLoadGameData, EventContext context)
        {

            Log.Instance.Info("Handling LoadGame Event");

            currentLoadGame.Timestamp = currentLoadGameData.Timestamp;
            currentLoadGame.Event = currentLoadGameData.Event;
            currentLoadGame.Fid = currentLoadGameData.Fid;
            currentLoadGame.Commander = currentLoadGameData.Commander;
            currentLoadGame.HasHorizons = currentLoadGameData.HasHorizons;
            currentLoadGame.HasOdyssey = currentLoadGameData.HasOdyssey;
            currentLoadGame.Ship = currentLoadGameData.Ship.Local;
            currentLoadGame.ShipId = currentLoadGameData.ShipId;
            currentLoadGame.ShipName = currentLoadGameData.ShipName;
            currentLoadGame.ShipIdent = currentLoadGameData.ShipIdent;
            currentLoadGame.FuelLevel = currentLoadGameData.FuelLevel;
            currentLoadGame.FuelCapacity = currentLoadGameData.FuelCapacity;
            currentLoadGame.IsLanded = currentLoadGameData.IsLanded;
            currentLoadGame.GameMode = currentLoadGameData.GameMode;
            currentLoadGame.Language = currentLoadGameData.Language;
            currentLoadGame.GameVersion = currentLoadGameData.GameVersion;
            currentLoadGame.Build = currentLoadGameData.Build;
            currentLoadGame.Credits = currentLoadGameData.Credits;
            currentLoadGame.Loan = currentLoadGameData.Loan;
            currentLoadGame.Group = currentLoadGameData.Group;
            currentLoadGame.IsStartingDead = currentLoadGameData.IsStartingDead;


            CoreServer.GameDataEvent(GameEventType.LoadGame, currentLoadGame);

        }

        public void HandleLoadoutEvent(EliteAPI.Events.LoadoutEvent currentLoadoutData, EventContext context)
        {

            Log.Instance.Info("Handling Loadout Event");

            currentLoadout.Timestamp = currentLoadoutData.Timestamp;
            currentLoadout.Event = currentLoadoutData.Event;
            currentLoadout.Ship = currentLoadoutData.Ship;
            currentLoadout.ShipId = currentLoadoutData.ShipId;
            currentLoadout.ShipName = currentLoadoutData.ShipName;
            currentLoadout.ShipIdent = currentLoadoutData.ShipIdent;
            currentLoadout.HullValue = currentLoadoutData.HullValue;
            currentLoadout.ModulesValue = currentLoadoutData.ModulesValue;
            currentLoadout.HullHealth = currentLoadoutData.HullHealth;
            currentLoadout.UnladenMass = currentLoadoutData.UnladenMass;
            currentLoadout.CargoCapacity = currentLoadoutData.CargoCapacity;
            currentLoadout.MaxJumpRange = currentLoadoutData.MaxJumpRange;
            LoadoutData.FuelCapacityInfo fuel = new LoadoutData.FuelCapacityInfo();
            fuel.Main = currentLoadoutData.FuelCapacity.Main;
            fuel.Reserve = currentLoadoutData.FuelCapacity.Reserve;
            currentLoadout.FuelCapacity = fuel;
            currentLoadout.Rebuy = currentLoadoutData.Rebuy;
            currentLoadout.Modules = new Collection<LoadoutData.ModuleInfo>();

            foreach (EliteAPI.Events.LoadoutEvent.ModuleInfo moduleInfoEntry in currentLoadoutData.Modules)
            { 
                LoadoutData.ModuleInfo moduleInfo = new LoadoutData.ModuleInfo();
                moduleInfo.Slot = moduleInfoEntry.Slot;
                moduleInfo.Item = moduleInfoEntry.Item;
                moduleInfo.IsOn = moduleInfoEntry.IsOn;
                moduleInfo.Priority = moduleInfoEntry.Priority;
                moduleInfo.Value = moduleInfoEntry.Value;
                moduleInfo.Health = moduleInfoEntry.Health;
                moduleInfo.AmmoInClip = moduleInfoEntry.AmmoInClip;
                moduleInfo.AmmoInHopper = moduleInfoEntry.AmmoInHopper;

                LoadoutData.EngineeringInfo engineeringInfo = new LoadoutData.EngineeringInfo();
                engineeringInfo.Engineer = moduleInfoEntry.Engineering.Engineer;
                engineeringInfo.EngineerId = moduleInfoEntry.Engineering.EngineerId;
                engineeringInfo.BlueprintName = moduleInfoEntry.Engineering.BlueprintName;
                engineeringInfo.BlueprintId = moduleInfoEntry.Engineering.BlueprintId;
                engineeringInfo.ExperimentalEffect = moduleInfoEntry.Engineering.ExperimentalEffect.Local;
                engineeringInfo.Level = moduleInfoEntry.Engineering.Level;
                engineeringInfo.Quality = moduleInfoEntry.Engineering.Quality;

                if (moduleInfoEntry.Engineering.Modifications != null)
                {
                    engineeringInfo.Modifications = new Collection<LoadoutData.ModifierInfo>();
                    foreach (EliteAPI.Events.LoadoutEvent.ModifierInfo modifierInfoEntry in moduleInfoEntry.Engineering.Modifications)
                    {
                        LoadoutData.ModifierInfo modifierInfo = new LoadoutData.ModifierInfo();
                        modifierInfo.Label = modifierInfoEntry.Label;
                        modifierInfo.Value = modifierInfoEntry.Value;
                        modifierInfo.OriginalValue = modifierInfoEntry.OriginalValue;
                        modifierInfo.LessIsGood = modifierInfoEntry.LessIsGood;

                        engineeringInfo.Modifications.Add(modifierInfo);
                    }
                }

                moduleInfo.Engineering = engineeringInfo;
                currentLoadout.Modules.Add(moduleInfo);
            }

            currentLoadout.IsHot = currentLoadoutData.IsHot;

            CoreServer.GameDataEvent(GameEventType.Loadout, currentLoadout);
            Log.Instance.Info("Handling Loadout Event completed.");
        }

        public void HandleRefuelAllEvent(EliteAPI.Events.RefuelAllEvent currentRefuelAllData, EventContext context)
        {

            Log.Instance.Info("Handling RefuelAll Event");

            currentRefuelAll.Timestamp = currentRefuelAllData.Timestamp;
            currentRefuelAll.Event = currentRefuelAllData.Event;
            currentRefuelAll.Cost = currentRefuelAllData.Cost;
            currentRefuelAll.Amount = currentRefuelAllData.Amount;

            CoreServer.GameDataEvent(GameEventType.RefuelAll, currentRefuelAll);

        }

        public void HandleRefuelPartialEvent(EliteAPI.Events.RefuelPartialEvent currentRefuelPartialData, EventContext context)
        {

            Log.Instance.Info("Handling RefuelPartial Event");

            currentRefuelPartial.Timestamp = currentRefuelPartialData.Timestamp;
            currentRefuelPartial.Event = currentRefuelPartialData.Event;
            currentRefuelPartial.Cost = currentRefuelPartialData.Cost;
            currentRefuelPartial.Amount = currentRefuelPartialData.Amount;

            CoreServer.GameDataEvent(GameEventType.RefuelPartial, currentRefuelPartial);

        }

        public void HandleReservoirReplenishedEvent(EliteAPI.Events.ReservoirReplenishedEvent currentReservoirReplenishedData, EventContext context)
        {

            Log.Instance.Info("Handling ReservoirReplenished Event");

            currentReservoirReplenished.Timestamp = currentReservoirReplenishedData.Timestamp;
            currentReservoirReplenished.Event = currentReservoirReplenishedData.Event;
            currentReservoirReplenished.FuelMain = currentReservoirReplenishedData.FuelMain;
            currentReservoirReplenished.FuelReservoir = currentReservoirReplenishedData.FuelReservoir;

            CoreServer.GameDataEvent(GameEventType.ReservoirReplenished, currentReservoirReplenished);

        }

        public void HandleFuelScoopEvent(EliteAPI.Events.FuelScoopEvent currentFuelScoopData, EventContext context)
        {

            Log.Instance.Info("Handling FuelScoop Event");

            currentFuelScoop.Timestamp = currentFuelScoopData.Timestamp;
            currentFuelScoop.Event = currentFuelScoopData.Event;
            currentFuelScoop.Scooped = currentFuelScoopData.Scooped;
            currentFuelScoop.Total = currentFuelScoopData.Total;

            CoreServer.GameDataEvent(GameEventType.FuelScoop, currentFuelScoop);

        }

        public void HandleStartJumpEvent(EliteAPI.Events.StartJumpEvent startJumpData, EventContext context) {

            Log.Instance.Info("Handling StartJumpEvent Event");            
            if (currentJump.LastUpdate <= startJumpData.Timestamp && startJumpData.JumpType == "Hyperspace") {
                currentJump.LastUpdate = startJumpData.Timestamp;
                currentJump.JumpComplete = false;
                currentJump.OriginSystemId = currentLocation.SystemId;
                currentJump.OriginSystemName = currentLocation.SystemName;
                currentJump.DestinationSystemId = startJumpData.SystemAddress;
                currentJump.DestinationSystemName = startJumpData.StarSystem;
                currentJump.DestinationSystemClass = startJumpData.StarClass;
                currentJump.JumpDistance = 0;
                currentJump.FuelUsed = 0;

                CoreServer.GameDataEvent(GameEventType.Jump, currentJump);
            }


        }
        public void HandleFsdJumpEvent(EliteAPI.Events.FsdJumpEvent fsdJumpdataData, EventContext context) {

            Log.Instance.Info("Handling FsdJumpEvent Event");

            if (currentLocation.LastUpdate <= fsdJumpdataData.Timestamp) {
                currentLocation.LastUpdate = fsdJumpdataData.Timestamp;
                currentLocation.SystemId = fsdJumpdataData.SystemAddress;
                currentLocation.SystemName = fsdJumpdataData.StarSystem;
                currentLocation.BodyId = fsdJumpdataData.BodyId;
                currentLocation.BodyName = fsdJumpdataData.Body;

                CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
            }

            if (currentJump.LastUpdate <= fsdJumpdataData.Timestamp) {
                currentJump.LastUpdate = fsdJumpdataData.Timestamp;
                currentJump.DestinationSystemId = fsdJumpdataData.SystemAddress;
                currentJump.DestinationSystemName = fsdJumpdataData.StarSystem;
                currentJump.JumpDistance = fsdJumpdataData.JumpDist;
                currentJump.FuelUsed = fsdJumpdataData.FuelUsed;
                currentJump.JumpComplete = true;

                CoreServer.GameDataEvent(GameEventType.Jump, currentJump);
            }

            if (currentNavRoute.LastUpdate <= fsdJumpdataData.Timestamp && currentNavRoute.NavRouteActive && 
                currentNavRoute.Stops != null && currentNavRoute.Stops.Count() != 0) {

                foreach (NavigationData.NavRouteStop  navRouteStop in currentNavRoute.Stops) {
                    if (navRouteStop.SystemName == fsdJumpdataData.StarSystem) {
                        currentNavRoute.LastSystemReached = fsdJumpdataData.StarSystem;
                    }
                }
            }
        }

        public void HandleNavRouteEvent(EliteAPI.Status.NavRoute.NavRouteEvent currentNavRouteData, EventContext context) {

            Log.Instance.Info("Handling NavRoute Event");
            Log.Instance.Info("Current data from: {curDataTime}, New data from: {newDataTime}", currentNavRoute.LastUpdate.ToString(), currentNavRouteData.Timestamp.ToString());
            if ((currentNavRoute.LastUpdate <= currentNavRouteData.Timestamp) && (currentNavRouteData.Stops != null) && (currentNavRouteData.Stops.Count() != 0))  {
                if (currentNavRoute.NavRouteActive) {
                    previousNavRoute = currentNavRoute.DeepCopy();
                    previousNavRoute.NavRouteActive = false;
                    CoreServer.GameDataEvent(GameEventType.PreviousNavRoute, previousNavRoute);
                }

                currentNavRoute.LastUpdate = currentNavRouteData.Timestamp;
                
                Log.Instance.Info("New route has {jumpcount} jumps", currentNavRouteData.Stops.Count());
                currentNavRoute.NavRouteActive = true;
                currentNavRoute.Stops.Clear();
                foreach (EliteAPI.Status.NavRoute.NavRouteStop navRouteStop in currentNavRouteData.Stops) {
                    NavigationData.NavRouteStop navStop = new NavigationData.NavRouteStop();
                    navStop.SystemId = navRouteStop.Address;
                    navStop.SystemName = navRouteStop.System;
                    navStop.Class = navRouteStop.Class;
                    currentNavRoute.Stops.Add(navStop);
                }
                CoreServer.GameDataEvent(GameEventType.Navigation, currentNavRoute);                
            }

        }

        public void HandleNavRouteClearEvent(EliteAPI.Events.NavRouteClearEvent navRouteClear, EventContext context) {

            Log.Instance.Info("Handling NavRouteClear Event");
            Log.Instance.Info("Current data from: {curDataTime}, New data from: {newDataTime}", currentNavRoute.LastUpdate.ToString(), navRouteClear.Timestamp.ToString());
            if (currentNavRoute.LastUpdate <= navRouteClear.Timestamp) {
                if (currentNavRoute.NavRouteActive) {
                    previousNavRoute = currentNavRoute.DeepCopy();
                    previousNavRoute.NavRouteActive = false;
                    if (currentJump.JumpComplete == false) {
                        previousNavRoute.LastSystemReached = currentJump.DestinationSystemName;
                    }
                    CoreServer.GameDataEvent(GameEventType.PreviousNavRoute, previousNavRoute);
                }

                currentNavRoute.LastUpdate = navRouteClear.Timestamp;
                currentNavRoute.NavRouteActive = false;
                currentNavRoute.Stops.Clear();

                CoreServer.GameDataEvent(GameEventType.Navigation, currentNavRoute);                
            }

        }

        public void HandleApproachBodyEvent(EliteAPI.Events.ApproachBodyEvent approachBodyData, EventContext context) {

            Log.Instance.Info("Handling ApproachBodyEvent Event");

            currentLocation.LastUpdate = approachBodyData.Timestamp;
            currentLocation.BodyId = approachBodyData.BodyId;
            currentLocation.BodyName = approachBodyData.Body;

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
        }

        public void HandleLeaveBodyEvent(EliteAPI.Events.LeaveBodyEvent leaveBodyData, EventContext context) {

            Log.Instance.Info("Handling LeaveBodyEvent Event");

            currentLocation.LastUpdate = leaveBodyData.Timestamp;
            currentLocation.BodyId = "";
            currentLocation.BodyName = "";

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
        }

        public void HandleDockingGrantedEvent(EliteAPI.Events.DockingGrantedEvent dockingGrantedData, EventContext context) {

            Log.Instance.Info("Handling DockingGrantedEvent Event");

            currentDockingGranted.Timestamp = dockingGrantedData.Timestamp;
            currentDockingGranted.MarketId = dockingGrantedData.MarketId;
            currentDockingGranted.StationName = dockingGrantedData.StationName;
            currentDockingGranted.StationType = dockingGrantedData.StationType;
            currentDockingGranted.LandingPad = dockingGrantedData.LandingPad;

            CoreServer.GameDataEvent(GameEventType.DockingGranted, currentDockingGranted);
        }

        public void HandleDockingDeniedEvent(EliteAPI.Events.DockingDeniedEvent dockingDeniedData, EventContext context)
        {

            Log.Instance.Info("Handling DockingDeniedEvent Event");

            currentDockingDenied.Timestamp = dockingDeniedData.Timestamp;
            currentDockingDenied.MarketId = dockingDeniedData.MarketId;
            currentDockingDenied.StationName = dockingDeniedData.StationName;
            currentDockingDenied.StationType = dockingDeniedData.StationType;
            currentDockingDenied.Reason = dockingDeniedData.Reason;

            CoreServer.GameDataEvent(GameEventType.DockingDenied, currentDockingDenied);
        }

        public void HandleDockingTimeoutEvent(EliteAPI.Events.DockingTimeoutEvent dockingTimeoutData, EventContext context)
        {

            Log.Instance.Info("Handling DockingTimeoutEvent Event");

            currentDockingTimeout.Timestamp = dockingTimeoutData.Timestamp;
            currentDockingTimeout.MarketId = dockingTimeoutData.MarketID;
            currentDockingTimeout.StationName = dockingTimeoutData.StationName;
            currentDockingTimeout.StationType = dockingTimeoutData.StationType;            

            CoreServer.GameDataEvent(GameEventType.DockingTimeout, currentDockingTimeout);
        }

        public void HandleDockingCancelledEvent(EliteAPI.Events.DockingCancelledEvent dockingCancelledData, EventContext context)
        {

            Log.Instance.Info("Handling DockingCancelledEvent Event");

            currentDockingCancelled.Timestamp = dockingCancelledData.Timestamp;
            currentDockingCancelled.MarketId = dockingCancelledData.MarketID;
            currentDockingCancelled.StationName = dockingCancelledData.StationName;
            currentDockingCancelled.StationType = dockingCancelledData.StationType;

            CoreServer.GameDataEvent(GameEventType.DockingCancelled, currentDockingCancelled);
        }

        public void HandleDockedEvent(EliteAPI.Events.DockedEvent dockedData, EventContext context)
        {

            Log.Instance.Info("Handling DockedEvent Event");

            currentLocation.LastUpdate = dockedData.Timestamp;
            currentLocation.MarketId = dockedData.MarketId;
            currentLocation.StationName = dockedData.StationName;
            currentLocation.StationType = dockedData.StationType;

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
        }

        public void HandleUndockedEvent(EliteAPI.Events.UndockedEvent undockedData, EventContext context) {

            Log.Instance.Info("Handling DockedEvent Event");

            currentLocation.LastUpdate = undockedData.Timestamp;
            currentLocation.MarketId = "";
            currentLocation.StationName = "";
            currentLocation.StationType = "";

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
        }

    }
}
