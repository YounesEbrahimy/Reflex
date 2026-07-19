using Reflex.DataTypes.Interfaces;
using Reflex.Generics.Interfaces;
using Reflex.Pools.Interfaces;
using Reflex.Pools.Plain;
using FluentAssertions;
using NUnit.Framework;
using Reflex.Core;
using System;

namespace Reflex.EditModeTests
{
    internal class PoolTests
    {
        private class SimpleItem
        {
        }

        private class PoolableItem : IPoolable, IInitializable, IDisposable
        {
            public int SpawnCount { get; private set; }
            public int DespawnCount { get; private set; }
            public int InitCount { get; private set; }
            public int DisposeCount { get; private set; }

            public void Initialize() => InitCount++;
            public void OnSpawn() => SpawnCount++;
            public void OnDespawn() => DespawnCount++;
            public void Dispose() => DisposeCount++;
        }

        private class DataItem : IData<string>, IPoolable
        {
            public string Data { get; set; }
            public int SpawnCount { get; private set; }
            public int DespawnCount { get; private set; }
            public void OnSpawn() => SpawnCount++;
            public void OnDespawn() => DespawnCount++;
        }

        [Test]
        public void InvalidSizes_ShouldThrow()
        {
            Action minNegative = () =>
            {
                var builder = new ContainerBuilder();
                builder.BindPool<SimpleItem, Pool<SimpleItem>>(minSize: -1);
                builder.Build().Single<Pool<SimpleItem>>();
            };
            minNegative.Should().Throw<ArgumentOutOfRangeException>();

            Action maxLess = () =>
            {
                var builder = new ContainerBuilder();
                builder.BindPool<SimpleItem, Pool<SimpleItem>>(minSize: 5, maxSize: 4);
                builder.Build().Single<Pool<SimpleItem>>();
            };
            maxLess.Should().Throw<ArgumentOutOfRangeException>();

            Action prewarmOut = () =>
            {
                var builder = new ContainerBuilder();
                builder.BindPool<SimpleItem, Pool<SimpleItem>>(minSize: 0, maxSize: 5, preWarmSize: 6);
                builder.Build().Single<Pool<SimpleItem>>();
            };
            prewarmOut.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void PreWarm_ShouldCreateExactAmount()
        {
            var builder = new ContainerBuilder();
            builder.BindPool<PoolableItem, Pool<PoolableItem>>(minSize: 0, maxSize: 10, preWarmSize: 3);
            var container = builder.Build();
            var pool = container.Single<Pool<PoolableItem>>();

            var item1 = pool.Take();
            var item2 = pool.Take();
            var item3 = pool.Take();

            // These were prewarmed
            item1.InitCount.Should().Be(1);
            item1.DespawnCount.Should().Be(0); // OnDespawn is not called during prewarm, only on explicit Return
            item1.SpawnCount.Should().Be(1); // OnSpawn is called on Take

            item2.InitCount.Should().Be(1);
            item3.InitCount.Should().Be(1);

            // This one was not prewarmed, it will be instantiated on demand
            var item4 = pool.Take();
            item4.InitCount.Should().Be(1);
            item4.DespawnCount.Should().Be(0);
            item4.SpawnCount.Should().Be(1);
        }

        [Test]
        public void TakeAndReturn_ShouldTriggerInterfaces()
        {
            var builder = new ContainerBuilder();
            builder.BindPool<PoolableItem, Pool<PoolableItem>>();
            var container = builder.Build();
            var pool = container.Single<Pool<PoolableItem>>();

            var item = pool.Take();
            item.InitCount.Should().Be(1);
            item.SpawnCount.Should().Be(1);
            item.DespawnCount.Should().Be(0);

            pool.Return(item);
            item.DespawnCount.Should().Be(1);
            item.DisposeCount.Should().Be(0);

            // Take it again to see if it is cached
            var cachedItem = pool.Take();
            cachedItem.Should().BeSameAs(item);
            cachedItem.InitCount.Should().Be(1); // Does not initialize again
            cachedItem.SpawnCount.Should().Be(2); // Spawned again
        }

        [Test]
        public void ExceedingMaxSize_ShouldDisposeInsteadOfCache()
        {
            var builder = new ContainerBuilder();
            builder.BindPool<PoolableItem, Pool<PoolableItem>>(maxSize: 1);
            var container = builder.Build();
            var pool = container.Single<Pool<PoolableItem>>();

            var item1 = pool.Take();
            var item2 = pool.Take();

            pool.Return(item1);
            item1.DespawnCount.Should().Be(1);
            item1.DisposeCount.Should().Be(0); // Cached successfully since active count inside inactive stack is < 1

            pool.Return(item2);
            item2.DespawnCount.Should().Be(1);
            item2.DisposeCount.Should().Be(1); // Cache is full, should be disposed
        }

        [Test]
        public void DisposePool_ShouldDisposeAllCachedItems()
        {
            var builder = new ContainerBuilder();
            builder.BindPool<PoolableItem, Pool<PoolableItem>>();
            var container = builder.Build();
            var pool = container.Single<Pool<PoolableItem>>();

            var item1 = pool.Take();
            var item2 = pool.Take();

            pool.Return(item1);
            pool.Return(item2);

            item1.DisposeCount.Should().Be(0);
            item2.DisposeCount.Should().Be(0);

            pool.Dispose();

            item1.DisposeCount.Should().Be(1);
            item2.DisposeCount.Should().Be(1);
        }

        [Test]
        public void PoolWithData_ShouldPassDataProperly()
        {
            var builder = new ContainerBuilder();
            builder.BindPool<DataItem, Pool<string, DataItem>>();
            var container = builder.Build();
            var pool = container.Single<Pool<string, DataItem>>();

            var item = pool.Take("Hello Pool");

            item.Data.Should().Be("Hello Pool");
            item.SpawnCount.Should().Be(1);
            item.DespawnCount.Should().Be(0);

            pool.Return(item);
            item.DespawnCount.Should().Be(1);

            var item2 = pool.Take("Goodbye Pool");
            item2.Should().BeSameAs(item);
            item2.Data.Should().Be("Goodbye Pool");
            item2.SpawnCount.Should().Be(2);
        }

        [Test]
        public void ContainerDispose_ShouldDisposePool()
        {
            var builder = new ContainerBuilder();
            builder.BindPool<PoolableItem, Pool<PoolableItem>>();
            var container = builder.Build();
            var pool = container.Single<Pool<PoolableItem>>();

            var item1 = pool.Take();
            var item2 = pool.Take();

            pool.Return(item1);
            pool.Return(item2);

            item1.DisposeCount.Should().Be(0);
            item2.DisposeCount.Should().Be(0);

            // Disposing the container should dispose the pool (because it was bound as Singleton by default)
            container.Dispose();

            item1.DisposeCount.Should().Be(1);
            item2.DisposeCount.Should().Be(1);
        }

        [Test]
        public void InjectedDependencies_ShouldBeResolved()
        {
            var builder = new ContainerBuilder();
            builder.BindInstance("InjectedString");
            builder.BindPool<InjectedItem, Pool<InjectedItem>>();
            
            var container = builder.Build();
            var pool = container.Single<Pool<InjectedItem>>();

            var item = pool.Take();
            item.InjectedString.Should().Be("InjectedString");
        }

        private class InjectedItem
        {
            [Reflex.Attributes.Inject] public string InjectedString { get; private set; }
        }
    }
}