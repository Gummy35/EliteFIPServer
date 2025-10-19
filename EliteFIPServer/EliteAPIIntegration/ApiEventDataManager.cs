using EliteAPI.Abstractions.Events;


namespace EliteFIPServer
{
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

}
