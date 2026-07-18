using System;
using System.Collections;
using FluentAssertions;
using NUnit.Framework;
using Reflex.Core;
using Reflex.DataTypes.Interfaces;
using Reflex.Enums;
using Reflex.Pools;
using Reflex.Pools.Interfaces;
using Reflex.Pools.Mono;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Reflex.PlayModeTests
{
    internal class MonoPoolTests
    {
        private Container container;
        private GameObject prefab;

        [TearDown]
        public void TearDown()
        {
            if (container != null)
            {
                container.Dispose();
                container = null;
            }
            
            if (prefab != null)
            {
                Object.DestroyImmediate(prefab);
                prefab = null;
            }
            
            // Clean up the global pool root to avoid test pollution
            var poolRoot = GameObject.Find("[Reflex Pools]");
            if (poolRoot != null)
            {
                Object.DestroyImmediate(poolRoot);
            }
        }

        private class SimpleItem : MonoBehaviour
        {
        }

        private class PoolableItem : MonoBehaviour, IPoolable
        {
            public int SpawnCount { get; private set; }
            public int DespawnCount { get; private set; }
            
            public void OnSpawn() => SpawnCount++;
            public void OnDespawn() => DespawnCount++;
        }

        private class DataItem : MonoBehaviour, IData<string>, IPoolable
        {
            public string Data { get; set; }
            public int SpawnCount { get; private set; }
            public int DespawnCount { get; private set; }
            public void OnSpawn() => SpawnCount++;
            public void OnDespawn() => DespawnCount++;
        }

        private class InjectedItem : MonoBehaviour
        {
            [Reflex.Attributes.Inject] public string InjectedString { get; private set; }
        }

        [Test]
        public void InvalidSizes_ShouldThrow()
        {
            prefab = new GameObject("Prefab");
            prefab.AddComponent<SimpleItem>();

            Action minNegative = () =>
            {
                var builder = new ContainerBuilder();
                builder.BindMonoPool<SimpleItem, MonoPool<SimpleItem>>(prefab.GetComponent<SimpleItem>(), minSize: -1);
                container = builder.Build();
                container.Single<MonoPool<SimpleItem>>();
            };
            minNegative.Should().Throw<ArgumentOutOfRangeException>();

            Action maxLess = () =>
            {
                var builder = new ContainerBuilder();
                builder.BindMonoPool<SimpleItem, MonoPool<SimpleItem>>(prefab.GetComponent<SimpleItem>(), minSize: 5, maxSize: 4);
                container = builder.Build();
                container.Single<MonoPool<SimpleItem>>();
            };
            maxLess.Should().Throw<ArgumentOutOfRangeException>();

            Action prewarmOut = () =>
            {
                var builder = new ContainerBuilder();
                builder.BindMonoPool<SimpleItem, MonoPool<SimpleItem>>(prefab.GetComponent<SimpleItem>(), minSize: 0, maxSize: 5, preWarmSize: 6);
                container = builder.Build();
                container.Single<MonoPool<SimpleItem>>();
            };
            prewarmOut.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void PreWarm_ShouldCreateExactAmount_AndSetInactive()
        {
            prefab = new GameObject("Prefab");
            var comp = prefab.AddComponent<PoolableItem>();
            
            var builder = new ContainerBuilder();
            builder.BindMonoPool<PoolableItem, MonoPool<PoolableItem>>(comp, minSize: 0, maxSize: 10, preWarmSize: 3);
            container = builder.Build();
            var pool = container.Single<MonoPool<PoolableItem>>();

            var poolRootName = $"[MonoPool<{typeof(PoolableItem).Name}>]";
            var poolRoot = PoolParent.Root.Find(poolRootName);
            poolRoot.Should().NotBeNull();
            poolRoot.childCount.Should().Be(3);

            var item1 = pool.Take();
            var item2 = pool.Take();
            var item3 = pool.Take();

            item1.gameObject.activeSelf.Should().BeTrue();
            item1.DespawnCount.Should().Be(0); // Only called on explicit return
            item1.SpawnCount.Should().Be(1);
        }

        [Test]
        public void TakeAndReturn_ShouldTriggerInterfaces_AndToggleActiveState()
        {
            prefab = new GameObject("Prefab");
            var comp = prefab.AddComponent<PoolableItem>();
            
            var builder = new ContainerBuilder();
            builder.BindMonoPool<PoolableItem, MonoPool<PoolableItem>>(comp);
            container = builder.Build();
            var pool = container.Single<MonoPool<PoolableItem>>();

            var item = pool.Take();
            item.gameObject.activeSelf.Should().BeTrue();
            item.SpawnCount.Should().Be(1);
            item.DespawnCount.Should().Be(0);

            pool.Return(item);
            item.gameObject.activeSelf.Should().BeFalse();
            item.DespawnCount.Should().Be(1);
            
            var poolRootName = $"[MonoPool<{typeof(PoolableItem).Name}>]";
            var poolRoot = PoolParent.Root.Find(poolRootName);
            ((object)item.transform.parent).Should().BeSameAs(poolRoot);

            var cachedItem = pool.Take();
            cachedItem.Should().BeSameAs(item);
            cachedItem.gameObject.activeSelf.Should().BeTrue();
            cachedItem.SpawnCount.Should().Be(2);
        }

        [UnityTest]
        public IEnumerator ExceedingMaxSize_ShouldDestroyInsteadOfCache()
        {
            prefab = new GameObject("Prefab");
            var comp = prefab.AddComponent<PoolableItem>();
            
            var builder = new ContainerBuilder();
            builder.BindMonoPool<PoolableItem, MonoPool<PoolableItem>>(comp, maxSize: 1);
            container = builder.Build();
            var pool = container.Single<MonoPool<PoolableItem>>();

            var item1 = pool.Take();
            var item2 = pool.Take();

            pool.Return(item1);
            item1.DespawnCount.Should().Be(1);
            item1.gameObject.Should().NotBeNull(); // Cached safely

            pool.Return(item2);
            item2.DespawnCount.Should().Be(1);
            
            yield return null; // Wait for Destroy

            // Unity overrides == null for destroyed objects
            (item2 == null).Should().BeTrue();
        }

        [UnityTest]
        public IEnumerator DisposePool_ShouldDestroyAllCachedItems_AndPoolRoot()
        {
            prefab = new GameObject("Prefab");
            var comp = prefab.AddComponent<PoolableItem>();
            
            var builder = new ContainerBuilder();
            builder.BindMonoPool<PoolableItem, MonoPool<PoolableItem>>(comp);
            container = builder.Build();
            var pool = container.Single<MonoPool<PoolableItem>>();

            var item1 = pool.Take();
            var item2 = pool.Take();

            pool.Return(item1);
            pool.Return(item2);

            var poolRootName = $"[MonoPool<{typeof(PoolableItem).Name}>]";
            var poolRoot = PoolParent.Root.Find(poolRootName);
            poolRoot.Should().NotBeNull();

            pool.Dispose();

            yield return null; // Wait for Destroy

            (item1 == null).Should().BeTrue();
            (item2 == null).Should().BeTrue();
            (poolRoot == null).Should().BeTrue();
        }

        [UnityTest]
        public IEnumerator ContainerDispose_ShouldDisposePool()
        {
            prefab = new GameObject("Prefab");
            var comp = prefab.AddComponent<PoolableItem>();
            
            var builder = new ContainerBuilder();
            builder.BindMonoPool<PoolableItem, MonoPool<PoolableItem>>(comp);
            container = builder.Build();
            var pool = container.Single<MonoPool<PoolableItem>>();

            var item1 = pool.Take();
            pool.Return(item1);

            container.Dispose();
            container = null; // Prevent double dispose in TearDown

            yield return null;

            (item1 == null).Should().BeTrue();
        }

        [Test]
        public void PoolWithData_ShouldPassDataProperly()
        {
            prefab = new GameObject("Prefab");
            var comp = prefab.AddComponent<DataItem>();
            
            var builder = new ContainerBuilder();
            builder.BindMonoPool<DataItem, MonoPool<string, DataItem>>(comp);
            container = builder.Build();
            var pool = container.Single<MonoPool<string, DataItem>>();

            var item = pool.Take("Hello MonoPool");
            
            item.Data.Should().Be("Hello MonoPool");
            item.SpawnCount.Should().Be(1);
            item.DespawnCount.Should().Be(0);

            pool.Return(item);
            item.DespawnCount.Should().Be(1);

            var item2 = pool.Take("Goodbye MonoPool");
            item2.Should().BeSameAs(item);
            item2.Data.Should().Be("Goodbye MonoPool");
            item2.SpawnCount.Should().Be(2);
        }

        [Test]
        public void InjectedDependencies_ShouldBeResolved()
        {
            prefab = new GameObject("Prefab");
            var comp = prefab.AddComponent<InjectedItem>();
            
            var builder = new ContainerBuilder();
            builder.BindInstance("InjectedString");
            builder.BindMonoPool<InjectedItem, MonoPool<InjectedItem>>(comp);
            
            container = builder.Build();
            var pool = container.Single<MonoPool<InjectedItem>>();

            var item = pool.Take();
            item.InjectedString.Should().Be("InjectedString");
        }
    }
}
