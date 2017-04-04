namespace Dacs7
{
    public class Event
    {
        public enum EventCode
        {
            Warning, Full, Empty, DataReceived, SendFinished, ConnectionChanged, Shutdown
        }

        public EventCode Code { get; private set; }
        public object Origin { get; private set; }
        public object Data { get; private set; }

        public Event(EventCode code, object origin, object data)
        {
            Code = code;
            Origin = origin;
            Data = data;
        }
    }
}
