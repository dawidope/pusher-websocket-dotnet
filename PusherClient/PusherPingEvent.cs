namespace PusherClient
{
    internal class PusherPingEvent : PusherSystemEvent
    {
        public PusherPingEvent()
            : base(Constants.PING, null)
        {
        }
    }
}
