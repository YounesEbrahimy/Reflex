using System;
using System.Collections.Generic;
using System.Linq;
using Reflex.Enums;
using Reflex.Factories.Mono;
using Reflex.Factories.Plain;
using Reflex.Generics;
using Reflex.Pools;
using Reflex.Pools.Mono;
using Reflex.Resolvers;
using UnityEngine;
using UnityEngine.Assertions;
using Resolution = Reflex.Enums.Resolution;

namespace Reflex.Core
{
    public sealed class ContainerBuilder
    {
        public string Name { get; private set; }
        public Container Parent { get; private set; }
        public List<Binding> Bindings { get; } = new();
        public event Action<Container> OnContainerBuilt;

        public Container Build()
        {
            var disposables = new DisposableCollection();
            var resolversByContract = new Dictionary<Type, List<IResolver>>();

            // Inherited resolvers
            if (Parent != null)
            {
                foreach (var (contract, resolvers) in Parent.ResolversByContract)
                {
                    resolversByContract[contract] = new List<IResolver>(resolvers);
                }
            }

            // Owned resolvers
            foreach (var binding in Bindings)
            {
                disposables.Add(binding.Resolver);

                foreach (var contract in binding.Contracts)
                {
                    if (!resolversByContract.TryGetValue(contract, out var resolvers))
                    {
                        resolvers = new List<IResolver>();
                        resolversByContract.Add(contract, resolvers);
                    }

                    resolvers.Add(binding.Resolver);
                }
            }

            var container = new Container(Name, Parent, resolversByContract, disposables);

            foreach (var binding in Bindings)
            {
                binding.Resolver.DeclaringContainer = container;
            }

            // Eagerly resolve inherited Scoped + Eager bindings
            if (Parent != null)
            {
                var inheritedEagerResolvers = Parent.ResolversByContract
                    .SelectMany(kvp => kvp.Value)
                    .ToHashSet()
                    .Where(r => r.Lifetime == Lifetime.Scoped && r.Resolution == Resolution.Eager);

                foreach (var resolver in inheritedEagerResolvers)
                {
                    resolver.Resolve(container);
                }
            }

            // Eagerly resolve self Singleton/Scoped + Eager bindings
            if (Bindings != null)
            {
                var selfEagerResolvers = Bindings
                    .Select(b => b.Resolver)
                    .Where(r => r.Resolution == Resolution.Eager &&
                                (r.Lifetime is Lifetime.Singleton or Lifetime.Scoped));

                foreach (var resolver in selfEagerResolvers)
                {
                    resolver.Resolve(container);
                }
            }

            Bindings.Clear();
            OnContainerBuilt?.Invoke(container);
            return container;
        }

        public ContainerBuilder SetName(string name)
        {
            Name = name;
            return this;
        }

        public ContainerBuilder SetParent(Container parent)
        {
            Parent = parent;
            return this;
        }

        public bool HasBinding(Type contract)
        {
            return Bindings.Any(binding => binding.Contracts.Contains(contract));
        }

        internal ContainerBuilder RegisterType(Type type, Lifetime lifetime, Resolution resolution)
        {
            return RegisterType(type, new[] { type }, lifetime, resolution);
        }

        internal ContainerBuilder RegisterType(Type type, Type[] contracts, Lifetime lifetime, Resolution resolution)
        {
            Assert.IsNotNull(type);
            Assert.IsTrue(contracts != null && contracts.Length > 0);
            Assert.IsFalse(lifetime == Lifetime.Transient && resolution == Resolution.Eager,
                "Type registration Lifetime.Transient + Resolution.Eager not allowed");

            IResolver resolver = lifetime switch
            {
                Lifetime.Singleton => new SingletonTypeResolver(type, resolution),
                Lifetime.Transient => new TransientTypeResolver(type),
                Lifetime.Scoped => new ScopedTypeResolver(type, resolution),
                _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime,
                    "Unhandled lifetime in ContainerBuilder.RegisterType() method.")
            };

            return Add(type, contracts, resolver);
        }

        internal ContainerBuilder RegisterValue(object value)
        {
            return RegisterValue(value, new[] { value.GetType() });
        }

        internal ContainerBuilder RegisterValue(object value, Type[] contracts)
        {
            Assert.IsTrue(contracts != null && contracts.Length > 0);
            var resolver = new SingletonValueResolver(value);
            return Add(value.GetType(), contracts, resolver);
        }

        internal ContainerBuilder RegisterFactory<T>(Func<Container, T> factory, Lifetime lifetime,
            Resolution resolution)
        {
            return RegisterFactory(factory, new[] { typeof(T) }, lifetime, resolution);
        }

        internal ContainerBuilder RegisterFactory<T>(Func<Container, T> factory, Type[] contracts, Lifetime lifetime,
            Resolution resolution)
        {
            Assert.IsNotNull(factory);
            Assert.IsTrue(contracts != null && contracts.Length > 0);
            Assert.IsFalse(lifetime == Lifetime.Transient && resolution == Resolution.Eager,
                "Factory registration Lifetime.Transient + Resolution.Eager not allowed");

            object TypelessFactory(Container container)
            {
                return factory.Invoke(container);
            }

            IResolver resolver = lifetime switch
            {
                Lifetime.Singleton => new SingletonFactoryResolver(TypelessFactory, resolution),
                Lifetime.Transient => new TransientFactoryResolver(TypelessFactory),
                Lifetime.Scoped => new ScopedFactoryResolver(TypelessFactory, resolution),
                _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime,
                    "Unhandled lifetime in ContainerBuilder.RegisterFactory() method.")
            };

            return Add(typeof(T), contracts, resolver);
        }

        internal ContainerBuilder RegisterFactory(Func<Container, object> factory, Type concreteType, Type[] contracts,
            Lifetime lifetime, Resolution resolution)
        {
            Assert.IsNotNull(factory);
            Assert.IsTrue(contracts != null && contracts.Length > 0);
            Assert.IsTrue(contracts != null && contracts.Length > 0);
            Assert.IsFalse(lifetime == Lifetime.Transient && resolution == Resolution.Eager,
                "Factory registration Lifetime.Transient + Resolution.Eager not allowed");

            IResolver resolver = lifetime switch
            {
                Lifetime.Singleton => new SingletonFactoryResolver(factory, resolution),
                Lifetime.Transient => new TransientFactoryResolver(factory),
                Lifetime.Scoped => new ScopedFactoryResolver(factory, resolution),
                _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime,
                    "Unhandled lifetime in ContainerBuilder.RegisterFactory() method.")
            };

            return Add(concreteType, contracts, resolver);
        }

        #region CustomMethods

        public void Bind<TConcrete>(Lifetime lifeTime = Lifetime.Singleton, Resolution resolution = Resolution.Lazy)
        {
            RegisterType(typeof(TConcrete), lifeTime, resolution);
        }

        public void Bind<T1, TConcrete>(Lifetime lifeTime = Lifetime.Singleton, Resolution resolution = Resolution.Lazy)
        {
            RegisterType(typeof(TConcrete), new[] { typeof(T1) }, lifeTime, resolution);
        }

        public void Bind<T1, T2, TConcrete>(Lifetime lifeTime = Lifetime.Singleton,
            Resolution resolution = Resolution.Lazy)
        {
            RegisterType(typeof(TConcrete), new[] { typeof(T1), typeof(T2) }, lifeTime, resolution);
        }

        public void Bind<T1, T2, T3, TConcrete>(Lifetime lifeTime = Lifetime.Singleton,
            Resolution resolution = Resolution.Lazy)
        {
            RegisterType(typeof(TConcrete), new[] { typeof(T1), typeof(T2), typeof(T3) }, lifeTime, resolution);
        }

        public void Bind<T1, T2, T3, T4, TConcrete>(Lifetime lifeTime = Lifetime.Singleton,
            Resolution resolution = Resolution.Lazy)
        {
            RegisterType(typeof(TConcrete), new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, lifeTime,
                resolution);
        }

        public void Bind<T1, T2, T3, T4, T5, TConcrete>(Lifetime lifeTime = Lifetime.Singleton,
            Resolution resolution = Resolution.Lazy)
        {
            RegisterType(typeof(TConcrete), new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) },
                lifeTime, resolution);
        }

        public void Bind<T1, T2, T3, T4, T5, T6, TConcrete>(Lifetime lifeTime = Lifetime.Singleton,
            Resolution resolution = Resolution.Lazy)
        {
            RegisterType(typeof(TConcrete),
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) }, lifeTime, resolution);
        }

        public void Bind<T1, T2, T3, T4, T5, T6, T7, TConcrete>(Lifetime lifeTime = Lifetime.Singleton,
            Resolution resolution = Resolution.Lazy)
        {
            RegisterType(typeof(TConcrete),
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) }, lifeTime,
                resolution);
        }

        public void BindInstance(object instance)
        {
            RegisterValue(instance);
        }

        public void BindInstanceTo<T>(object instance)
        {
            RegisterValue(instance, new[] { typeof(T) });
        }

        public void BindInterFaces<TConcrete>(Lifetime lifeTime = Lifetime.Singleton,
            Resolution resolution = Resolution.Lazy)
        {
            RegisterType(typeof(TConcrete), typeof(TConcrete).GetInterfaces(), lifeTime, resolution);
        }

        public void BindInterFacesAndSelf<TConcrete>(Lifetime lifeTime = Lifetime.Singleton,
            Resolution resolution = Resolution.Lazy)
        {
            var contracts = typeof(TConcrete).GetInterfaces().Append(typeof(TConcrete)).ToArray();
            RegisterType(typeof(TConcrete), contracts, lifeTime, resolution);
        }

        #endregion

        #region Factories

        public void BindFactory<T, TFactory>(Lifetime lifeTime = Lifetime.Singleton,
            Resolution resolution = Resolution.Lazy) where TFactory : BaseFactory<T>
        {
            if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException(
                    $"Cannot use BindFactory for MonoBehaviour types like {typeof(T).Name}. Use BindMonoFactory instead.");

            RegisterFactory<TFactory>(CreateFactory, lifeTime, resolution);
            return;

            TFactory CreateFactory(Container container)
            {
                var factory = (TFactory)Activator.CreateInstance(typeof(TFactory));
                factory.Setup(container);
                return factory;
            }
        }

        public void BindMonoFactory<T, TFactory>(T original, bool hasFactoryScope = false,
            Lifetime lifeTime = Lifetime.Singleton, Resolution resolution = Resolution.Lazy)
            where TFactory : BaseMonoFactory<T> where T : MonoBehaviour
        {
            RegisterFactory<TFactory>(CreateMonoFactory, lifeTime, resolution);
            return;

            TFactory CreateMonoFactory(Container container)
            {
                var factory = (TFactory)Activator.CreateInstance(typeof(TFactory));
                factory.Setup(container, original, hasFactoryScope);
                return factory;
            }
        }

        #endregion

        #region Pools

        public void BindPool<T, TPool>(int minSize = 0, int maxSize = int.MaxValue, int preWarmSize = 0,
            Lifetime lifeTime = Lifetime.Singleton, Resolution resolution = Resolution.Lazy) where TPool : BasePool<T>
        {
            if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException(
                    $"Cannot use BindPool for MonoBehaviour types like {typeof(T).Name}. Use BindMonoPool instead.");

            RegisterFactory<TPool>(CreatePool, lifeTime, resolution);
            return;

            TPool CreatePool(Container container)
            {
                var pool = (TPool)Activator.CreateInstance(typeof(TPool));
                pool.Setup(container, minSize, maxSize, preWarmSize);
                return pool;
            }
        }

        public void BindMonoPool<T, TPool>(T prefab, int minSize = 0, int maxSize = int.MaxValue, int preWarmSize = 0,
            Lifetime lifeTime = Lifetime.Singleton, Resolution resolution = Resolution.Lazy)
            where TPool : BaseMonoPool<T> where T : MonoBehaviour
        {
            RegisterFactory<TPool>(CreateMonoPool, lifeTime, resolution);
            return;

            TPool CreateMonoPool(Container container)
            {
                var pool = (TPool)Activator.CreateInstance(typeof(TPool));
                pool.Setup(container, prefab, minSize, maxSize, preWarmSize);
                return pool;
            }
        }

        #endregion

        private ContainerBuilder Add(Type concrete, Type[] contracts, IResolver resolver)
        {
            var binding = Binding.Validated(resolver, concrete, contracts);
            Bindings.Add(binding);
            return this;
        }
    }
}