using EliteAPI.Abstractions.Events;
using EliteAPI.Events;
using EliteAPI.Status.NavRoute;
using EliteFIPServer.Logging;


namespace EliteFIPServer
{
    public class NavigationData : IEvent
    {
        public NavigationData()
        {
        }
        public NavigationData(NavRouteEvent currentNavRouteData)
        {
            Timestamp = currentNavRouteData.Timestamp;
            Log.Instance.Info($"New route has {currentNavRouteData.Stops.Count()} jumps");
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
}
