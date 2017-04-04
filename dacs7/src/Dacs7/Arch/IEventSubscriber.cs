namespace Dacs7
{
    public interface IEventSubscriber
    {
        void OnEvent(IEventPublisher source, Event evt);
    }
}
