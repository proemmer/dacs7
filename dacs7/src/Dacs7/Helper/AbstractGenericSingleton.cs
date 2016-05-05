using System;

namespace Dacs7.Heper
{
    public abstract class AbstractGenericSingleton<T> where T : AbstractGenericSingleton<T>
    {
        private static T _instance = null;

        protected static bool Initialised
        {
            get
            {
                return (_instance != null);
            }
        }

        protected static T UniqueInstance
        {
            get
            {
                if (Initialised)
                {
                    return SingletonCreator.instance;
                }
                return null;
            }
        }

        protected AbstractGenericSingleton() { }

        protected static void Init(T newInstance)
        {
            if (newInstance == null) throw new ArgumentNullException();
            _instance = newInstance;
        }

        class SingletonCreator
        {
            static SingletonCreator() { }
            internal static readonly T instance = _instance;
        }
    }
}
