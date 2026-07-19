using Reflex.Generics.Interfaces;
using Reflex.Injectors;
using Reflex.Core;
using System;

namespace Reflex.Factories.Plain
{
    public abstract class BaseFactory<T> : IDisposable
    {
        protected Container container;

        private protected BaseFactory()
        {
        }

        public void Setup(Container container)
        {
            this.container = container;
        }

        public void Dispose()
        {
            container = null;
        }

        protected static T CreateInstance()
        {
            return (T)Activator.CreateInstance(typeof(T));
        }

        protected static T ProcessInstance(T instance, Container container)
        {
            AttributeInjector.Inject(instance, container);
            if (instance is IInitializable initializable) initializable.Initialize();
            return instance;
        }
    }
}