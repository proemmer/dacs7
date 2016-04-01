using System;
using System.Collections.Generic;
using System.Linq;

namespace InacS7Core.Arch
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
            if (PublisherEvent != null)
                PublisherEvent(source, evt);
        }
    }
}
