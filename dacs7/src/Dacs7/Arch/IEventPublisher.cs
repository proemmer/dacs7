namespace Dacs7.Arch
{
    public delegate void PublisherEventHandlerDelegate(IEventPublisher source, Event evt);
    public interface IEventPublisher
    {
        event PublisherEventHandlerDelegate PublisherEvent;

        int GetSubscriberCount();
        bool Subscribe(IEventSubscriber subscriber);
        bool Unsubscribe(IEventSubscriber subscriber);
        void NotifySubscribers(IEventPublisher source, Event evt);
    }
}
