namespace Dacs7.Protocols
{
    internal struct CallbackHandler<T>
    {
        public ushort Id { get; }
        public AsyncAutoResetEvent<T> Event { get; }

        public CallbackHandler(ushort id)
        {
            Id = id;
            Event = new AsyncAutoResetEvent<T>();
        }

    }
}
