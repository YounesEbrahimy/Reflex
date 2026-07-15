using System;
using Reflex.Core;
using Reflex.Enums;
using Reflex.Injectors;
using Reflex.Generics.Interfaces;

namespace Reflex.Resolvers
{
    internal sealed class SingletonFactoryResolver : IResolver
    {
        private object _instance;
        private readonly Func<Container, object> _factory;

        public Lifetime Lifetime => Lifetime.Singleton;
        public Container DeclaringContainer { get; set; }
        public Resolution Resolution { get; }

        public SingletonFactoryResolver(Func<Container, object> factory, Resolution resolution)
        {
            Diagnosis.RegisterCallSite(this);
            _factory = factory;
            Resolution = resolution;
        }

        public object Resolve(Container resolvingContainer)
        {
            Diagnosis.IncrementResolutions(this);

            if (_instance == null)
            {
                _instance = _factory.Invoke(resolvingContainer);
                DeclaringContainer.Disposables.TryAdd(_instance);
                AttributeInjector.Inject(_instance, resolvingContainer);
                if (_instance is IInitializable initializable) initializable.Initialize();
                Diagnosis.RegisterInstance(this, _instance);
            }

            return _instance;
        }

        public void Dispose()
        {
        }
    }
}