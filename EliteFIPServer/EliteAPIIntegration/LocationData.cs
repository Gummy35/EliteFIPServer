using EliteAPI.Abstractions.Events;
using EliteAPI.Events;
using LiteDB;


namespace EliteFIPServer
{
    public class LocationData : IEvent
    {
        public LocationData()
        {
            StarPos = (0, 0, 0);
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
            SetStarPos(currentLocationData.StarPos);
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
        public ValueTuple<double, double, double> StarPos { get; set; }

        public string Event => "Location";

        public void SetStarPos(IReadOnlyCollection<double> starPos)
        {
            var a = starPos.ToArray();
            StarPos = (a[0], a[1], a[2]);
        }

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
            SetStarPos(fsdJumpdataData.StarPos);
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
}
