namespace InacS7Core.Arch
{
    public interface IEventSubscriber
    {
        void OnEvent(IEventPublisher source, Event evt);
    }
}
