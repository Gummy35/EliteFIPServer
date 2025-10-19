using EliteAPI.Abstractions.Events;
using EliteAPI.Events;


namespace EliteFIPServer
{
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
}
