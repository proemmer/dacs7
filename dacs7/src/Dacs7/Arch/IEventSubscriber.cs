namespace Dacs7.Arch
{
    public interface IEventSubscriber
    {
        void OnEvent(IEventPublisher source, Event evt);
    }
}
