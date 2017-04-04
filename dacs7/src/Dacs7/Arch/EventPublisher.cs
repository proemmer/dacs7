using System;
using System.Collections.Generic;
using System.Linq;

namespace Dacs7
{
    public class EventPublisher : IEventPublisher
    {
        private object thisLock = new object();
        private List<IEventSubscriber> subscribers = new List<IEventSubscriber>();

        public event PublisherEventHandlerDelegate PublisherEvent;

        public EventPublisher()
        {
        }

        public bool Subscribe(IEventSubscriber subscriber)
        {
            PublisherEvent += subscriber.OnEvent;
            return true;
        }

        public bool Unsubscribe(IEventSubscriber subscriber)
        {
            PublisherEvent -= subscriber.OnEvent;
            return true;
        }

        public int GetSubscriberCount()
        {
            return PublisherEvent.GetInvocationList().Count();
        }

        public void NotifySubscribers(IEventPublisher source, Event evt)
        {
            PublisherEvent?.Invoke(source, evt);
        }
    }
}
