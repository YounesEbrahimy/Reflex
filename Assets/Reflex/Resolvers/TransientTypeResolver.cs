using System;
using Reflex.Core;
using Reflex.Enums;
using Reflex.Generics.Interfaces;

namespace Reflex.Resolvers
{
    internal sealed class TransientTypeResolver : IResolver
    {
        private readonly Type _concreteType;
        public Lifetime Lifetime => Lifetime.Transient;
        public Container DeclaringContainer { get; set; }
        public Resolution Resolution => Resolution.Lazy;

        public TransientTypeResolver(Type concreteType)
        {
            Diagnosis.RegisterCallSite(this);
            _concreteType = concreteType;
        }

        public object Resolve(Container resolvingContainer)
        {
            Diagnosis.IncrementResolutions(this);
            var instance = DeclaringContainer.Construct(_concreteType);
            resolvingContainer.Disposables.TryAdd(instance);
            if (instance is IInitializable initializable) initializable.Initialize();
            Diagnosis.RegisterInstance(this, instance);
            return instance;
        }

        public void Dispose()
        {
        }
    }
}