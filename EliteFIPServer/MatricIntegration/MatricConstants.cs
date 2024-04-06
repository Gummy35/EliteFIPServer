namespace EliteFIPServer {
    class MatricConstants {

        // Status Data
        public static string DOCKED = "Docked";
        public static string LANDED = "Landed";
        public static string LANDINGGEAR = "LandingGear";
        public static string SHIELDS = "Shields";
        public static string SUPERCRUISE = "Supercruise";
        public static string FLIGHTASSIST = "FlightAssist";
        public static string HARDPOINTS = "Hardpoints";
        public static string INWING = "InWing";
        public static string LIGHTS = "Lights";
        public static string CARGOSCOOP = "CargoScoop";
        public static string SILENTRUNNING = "SilentRunning";
        public static string SCOOPINGFUEL = "ScoopingFuel";
        public static string SRVHANDBRAKE = "SrvHandbrake";
        public static string SRVTURRET = "SrvTurret";
        public static string SRVUNDERSHIP = "SrvUnderShip";
        public static string SRVDRIVEASSIST = "SrvDriveAssist";
        public static string FSDMASSLOCK = "FSDMassLock";
        public static string FSDCHARGE = "FSDCharging";
        public static string FSDCOOLDOWN = "FSDCooldown";
        public static string LOWFUEL = "LowFuel";
        public static string OVERHEAT = "Overheat";
        public static string INDANGER = "InDanger";
        public static string INTERDICTION = "Interdiction";
        public static string INMAINSHIP = "InMainShip";
        public static string INFIGHTER = "InFighter";
        public static string INSRV = "InSRV";
        public static string HUDMODE = "HudMode";
        public static string NIGHTVISION = "NightVision";
        public static string FSDJUMP = "FsdJump";
        public static string SRVHIGHBEAM = "SrvHighBeam";       

        // Extended Status data from Odyssey
        public static string ONFOOT = "OnFoot";
        public static string INTAXI = "InTaxi";
        public static string INMULTICREW = "InMulticrew";
        public static string ONFOOTINSTATION = "OnFootInStation";
        public static string ONFOOTONPLANET = "OnFootOnPlanet";
        public static string AIMDOWNSIGHT = "AimDownSight";
        public static string LOWOXYGEN = "LowOxygen";
        public static string LOWHEALTH = "LowHealth";
        public static string COLD = "Cold";
        public static string HOT = "Hot";
        public static string VERYCOLD = "VeryCold";
        public static string VERYHOT = "VeryHot";

        // Text data and Sliders
        public static string FUELMAIN = "FuelMain";
        public static string FUELRESERVOIR = "FuelReservoir";

        public static string FUELMAIN2 = "FuelMain2";
        public static string FUELRESERVOIR2 = "FuelReservoir2";


        // Status Text Displays
        public static string STATUS = "Status";
        public static string STATUS_LABEL = "StatusLabel";
        public static string STATUS2 = "Status2";
        public static string STATUS_LABEL2 = "StatusLabel2";
        public static string STATUS3 = "Status3";
        public static string STATUS_LABEL3 = "StatusLabel3";

        // Target Text Displays
        public static string TARGET = "Target";
        public static string TARGET_LABEL = "TargetLabel";
        public static string TARGET2 = "Target2";
        public static string TARGET_LABEL2 = "TargetLabel2";
        public static string TARGET3 = "Target3";
        public static string TARGET_LABEL3 = "TargetLabel3";

        // Landing Text Displays
        public static string LANDING = "Landing";
        public static string LANDING_LABEL = "LandingLabel";
        public static string LANDING2 = "Landing2";
        public static string LANDING_LABEL2 = "LandingLabel2";

        // Info Text Displays
        public static string INFO = "Info";
        public static string INFO_LABEL = "InfoLabel";
        public static string INFO2 = "Info2";
        public static string INFO_LABEL2 = "InfoLabel2";
        public static string INFO3 = "Info3";
        public static string INFO_LABEL3= "InfoLabel3";
        public static string GAMEINFO = "GameInfo";

        // Custom Button
        public static string LANDINGPAD = "Landingpad";
        public static string TARGETSHIELDVALUE = "TargetShieldValue";
        public static string TARGETHULLVALUE = "TargetHullValue";        
        public static string TARGETSUBSYSVALUE = "TargetSubsysValue";

        public static string TARGETSUBSYSNAME = "TargetSubsysName";        

        // Button prefixes
        public static string BTN = "btn"; // Standard On/Off Button, where button text can change according to state
        public static string IND = "ind"; // Standard On/Off button, where button text is not changed by state 
        public static string WRN = "wrn"; // Standard On/Off button, where button text is not changed by state, but when 'on', button flashes 
        public static string SWT = "swt"; // Multi-position switch, button text is not changed by position
        public static string SLD = "sld"; // Sider where value can be set along a scale
        public static string TXT = "txt"; // Button containing only text for information, no state
        public static string PNL = "pnl"; // iFrame Panel where content is provided via HTTP request
    }
}
