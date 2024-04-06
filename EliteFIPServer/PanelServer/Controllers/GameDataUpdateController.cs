using EliteFIPProtocol;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace EliteFIPServer
{
    class GameDataUpdateController
    {

        private readonly IHubContext<GameDataUpdateHub> _hubContext;

        public GameDataUpdateController(IHubContext<GameDataUpdateHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void SendStatusUpdate(StatusData statusData)
        {
            string statusJSON = JsonSerializer.Serialize(statusData);
            _hubContext.Clients.All.SendAsync("StatusData", statusJSON);
        }

        public void SendTargetUpdate(ShipTargetedData targetData)
        {
            string targetJSON = JsonSerializer.Serialize(targetData);
            _hubContext.Clients.All.SendAsync("TargetData", targetJSON);
        }

        public void SendLocationUpdate(LocationData locationData)
        {
            string locationJSON = JsonSerializer.Serialize(locationData);
            _hubContext.Clients.All.SendAsync("LocationData", locationJSON);
        }

        public void SendNavRouteUpdate(NavigationData navRouteData)
        {
            string navRouteJSON = JsonSerializer.Serialize(navRouteData);
            _hubContext.Clients.All.SendAsync("NavRouteData", navRouteJSON);
        }

        public void SendPreviousNavRoute(NavigationData navRouteData) {
            string navRouteJSON = JsonSerializer.Serialize(navRouteData);
            _hubContext.Clients.All.SendAsync("PreviousNavRoute", navRouteJSON);
        }

        public void SendJumpUpdate(JumpData jumpData) {
            string jumpJSON = JsonSerializer.Serialize(jumpData);
            _hubContext.Clients.All.SendAsync("JumpData", jumpJSON);
        }

        public void SendDockingGrantedUpdate(DockingGrantedData dockingGrantedData)
        {
            string jumpJSON = JsonSerializer.Serialize(dockingGrantedData);
            _hubContext.Clients.All.SendAsync("DockingGrantedData", jumpJSON);
        }

        public void SendDockingDeniedUpdate(DockingDeniedData dockingDeniedData)
        {
            string jumpJSON = JsonSerializer.Serialize(dockingDeniedData);
            _hubContext.Clients.All.SendAsync("DockingDeniedData", jumpJSON);
        }

        public void SendDockingCancelledUpdate(DockingCancelledData dockingCancelledData)
        {
            string jumpJSON = JsonSerializer.Serialize(dockingCancelledData);
            _hubContext.Clients.All.SendAsync("DockingCancelledData", jumpJSON);
        }

        public void SendDockingTimeoutUpdate(DockingTimeoutData dockingTimeoutData)
        {
            string jumpJSON = JsonSerializer.Serialize(dockingTimeoutData);
            _hubContext.Clients.All.SendAsync("DockingTimeoutData", jumpJSON);
        }

        public void SendRefuelAllUpdate(RefuelAllData refuelAllData)
        {
            string jumpJSON = JsonSerializer.Serialize(refuelAllData);
            _hubContext.Clients.All.SendAsync("RefuelAllData", jumpJSON);
        }

        public void SendRefuelPartialUpdate(RefuelPartialData refuelPartialData)
        {
            string jumpJSON = JsonSerializer.Serialize(refuelPartialData);
            _hubContext.Clients.All.SendAsync("RefuelPartialData", jumpJSON);
        }

        public void SendReservoirReplenishedUpdate(ReservoirReplenishedData reservoirReplenishedData)
        {
            string jumpJSON = JsonSerializer.Serialize(reservoirReplenishedData);
            _hubContext.Clients.All.SendAsync("ReservoirReplenishedData", jumpJSON);
        }

        public void SendLoadGameUpdate(LoadGameData loadGameData)
        {
            string jumpJSON = JsonSerializer.Serialize(loadGameData);
            _hubContext.Clients.All.SendAsync("LoadGameData", jumpJSON);
        }

        public void SendLoadoutUpdate(LoadoutData loadoutData)
        {
            string jumpJSON = JsonSerializer.Serialize(loadoutData);
            _hubContext.Clients.All.SendAsync("LoadoutData", jumpJSON);
        }

        public void SendShipyardNewUpdate(ShipyardNewData shipyardNewData)
        {
            string jumpJSON = JsonSerializer.Serialize(shipyardNewData);
            _hubContext.Clients.All.SendAsync("ShipyardNewData", jumpJSON);
        }

        public void SendShipyardSwapUpdate(ShipyardSwapData shipyardSwapData)
        {
            string jumpJSON = JsonSerializer.Serialize(shipyardSwapData);
            _hubContext.Clients.All.SendAsync("ShipyardSwapData", jumpJSON);
        }
    }
}
