using Resolution = Reflex.Enums.Resolution;
using Reflex.DataTypes.Interfaces;
using Object = UnityEngine.Object;
using Reflex.Factories.Mono;
using Reflex.Attributes;
using FluentAssertions;
using NUnit.Framework;
using Reflex.Enums;
using Reflex.Core;
using UnityEngine;
using System;

namespace Reflex.PlayModeTests
{
    internal class ForkMonoFactoryTests
    {
        #region Models & Helper Classes

        private interface IDependency
        {
        }

        private class Dependency : IDependency
        {
        }

        private class TestMonoBehaviour : MonoBehaviour
        {
            [Inject] public IDependency dependency { get; set; }
        }

        private class TestMonoFactory : MonoFactory<TestMonoBehaviour>
        {
        }

        public struct TestMonoData
        {
            public string Label;
            public int ID;
        }

        private class TestParameterizedMonoBehaviour : MonoBehaviour, IData<TestMonoData>
        {
            TestMonoData IData<TestMonoData>.Data { get; set; }
            [Inject] public IDependency dependency { get; set; }
        }

        private class TestParameterizedMonoFactory : MonoFactory<TestMonoData, TestParameterizedMonoBehaviour>
        {
        }

        private interface IScopedDependency
        {
        }

        private class ScopedDependency : IScopedDependency, IDisposable
        {
            public static bool Disposed { get; set; } = false;
            public void Dispose() => Disposed = true;
        }

        private class TestScopedMonoBehaviour : MonoBehaviour
        {
            [Inject] public IScopedDependency ScopedDep { get; set; }
        }

        private class TestFactoryScope : FactoryScope
        {
            public override void InstallBindings(ContainerBuilder builder)
            {
                builder.Bind<IScopedDependency, ScopedDependency>(Lifetime.Scoped);
            }
        }

        private class TestScopedMonoFactory : MonoFactory<TestScopedMonoBehaviour>
        {
        }

        // --- 3-Level Nesting Scope Models ---

        private interface ILevel1Service
        {
        }

        private class Level1Service : ILevel1Service, IDisposable
        {
            public static bool Disposed { get; set; }
            public void Dispose() => Disposed = true;
        }

        private interface ILevel2Service
        {
        }

        private class Level2Service : ILevel2Service, IDisposable
        {
            public static bool Disposed { get; set; }
            public void Dispose() => Disposed = true;
        }

        private class Level1MonoBehaviour : MonoBehaviour, IData<Level1Data>
        {
            Level1Data IData<Level1Data>.Data { get; set; }
        }

        private class Level1Factory : MonoFactory<Level1Data, Level1MonoBehaviour>
        {
        }

        private class Level1Data
        {
        }

        private class Level2MonoBehaviour : MonoBehaviour, IData<Level2Data>
        {
            [Inject] public Level1Data level1Data;
            Level2Data IData<Level2Data>.Data { get; set; }
        }

        private class Level2Factory : MonoFactory<Level2Data, Level2MonoBehaviour>
        {
        }

        private class Level2Data
        {
        }

        private class Level3MonoBehaviour : MonoBehaviour, IData<Level3Data>
        {
            [Inject] public Level1Data level1Data;
            [Inject] public Level2Data level2Data;
            [Inject] public ILevel2Service level2Service { get; set; }
            Level3Data IData<Level3Data>.Data { get; set; }
        }

        private class Level3Factory : MonoFactory<Level3Data, Level3MonoBehaviour>
        {
        }

        private class Level3Data
        {
        }

        private class Level1Scope : FactoryScope
        {
            public static Level2MonoBehaviour Level2Prefab { get; set; }

            public override void InstallBindings(ContainerBuilder builder)
            {
                builder.BindMonoFactory<Level2MonoBehaviour, Level2Factory>(Level2Prefab, hasFactoryScope: true);
                builder.Bind<ILevel1Service, Level1Service>(Lifetime.Scoped, Resolution.Eager);
            }
        }

        private class Level2Scope : FactoryScope
        {
            public static Level3MonoBehaviour Level3Prefab { get; set; }

            public override void InstallBindings(ContainerBuilder builder)
            {
                builder.BindMonoFactory<Level3MonoBehaviour, Level3Factory>(Level3Prefab, hasFactoryScope: false);
                builder.Bind<ILevel2Service, Level2Service>(Lifetime.Scoped, Resolution.Eager);
            }
        }

        #endregion

        [Test]
        public void Create_StandardMonoFactoryWithoutScope_ShouldInstantiateAndInject()
        {
            GameObject prefabGo = null;
            TestMonoBehaviour clone = null;
            try
            {
                prefabGo = new GameObject("Prefab");
                var component = prefabGo.AddComponent<TestMonoBehaviour>();

                var builder = new ContainerBuilder();
                builder.Bind<IDependency, Dependency>(Lifetime.Singleton);
                builder.BindMonoFactory<TestMonoBehaviour, TestMonoFactory>(component, hasFactoryScope: false);
                var container = builder.Build();

                var factory = container.Single<TestMonoFactory>();
                factory.Should().NotBeNull();

                clone = factory.Create();
                clone.Should().NotBeNull();
                clone.dependency.Should().NotBeNull();
                clone.dependency.Should().BeOfType<Dependency>();
            }
            finally
            {
                if (clone != null) Object.DestroyImmediate(clone.gameObject);
                if (prefabGo != null) Object.DestroyImmediate(prefabGo);
            }
        }

        [Test]
        public void Create_ParameterizedMonoFactoryWithoutScope_ShouldInstantiateInjectAndAssignData()
        {
            GameObject prefabGo = null;
            TestParameterizedMonoBehaviour clone = null;
            try
            {
                prefabGo = new GameObject("Prefab");
                var component = prefabGo.AddComponent<TestParameterizedMonoBehaviour>();

                var builder = new ContainerBuilder();
                builder.Bind<IDependency, Dependency>(Lifetime.Singleton);
                builder.BindMonoFactory<TestParameterizedMonoBehaviour, TestParameterizedMonoFactory>(component,
                    hasFactoryScope: false);
                var container = builder.Build();

                var factory = container.Single<TestParameterizedMonoFactory>();
                factory.Should().NotBeNull();

                var testData = new TestMonoData { Label = "Level1_Enemy", ID = 42 };
                clone = factory.Create(testData);

                clone.Should().NotBeNull();
                clone.dependency.Should().NotBeNull();
                clone.dependency.Should().BeOfType<Dependency>();
                ((IData<TestMonoData>)clone).Data.Label.Should().Be("Level1_Enemy");
                ((IData<TestMonoData>)clone).Data.ID.Should().Be(42);
            }
            finally
            {
                if (clone != null) Object.DestroyImmediate(clone.gameObject);
                if (prefabGo != null) Object.DestroyImmediate(prefabGo);
            }
        }

        [Test]
        public void Create_StandardMonoFactoryWithScope_ShouldSetupScopeAndDisposeOnDestroy()
        {
            ScopedDependency.Disposed = false;
            GameObject prefabGo = null;
            TestScopedMonoBehaviour clone = null;
            try
            {
                prefabGo = new GameObject("Prefab");
                var component = prefabGo.AddComponent<TestScopedMonoBehaviour>();
                prefabGo.AddComponent<TestFactoryScope>();

                var builder = new ContainerBuilder();
                builder.BindMonoFactory<TestScopedMonoBehaviour, TestScopedMonoFactory>(component,
                    hasFactoryScope: true);
                var container = builder.Build();

                var factory = container.Single<TestScopedMonoFactory>();
                factory.Should().NotBeNull();

                clone = factory.Create();
                clone.Should().NotBeNull();
                clone.ScopedDep.Should().NotBeNull();
                clone.ScopedDep.Should().BeOfType<ScopedDependency>();

                var scope = clone.GetComponent<TestFactoryScope>();
                scope.Should().NotBeNull();
                scope.scopeContainer.Should().NotBeNull();

                Object.DestroyImmediate(clone.gameObject);
                clone = null;

                ScopedDependency.Disposed.Should().BeTrue();
            }
            finally
            {
                if (clone != null) Object.Destroy(clone.gameObject);
                if (prefabGo != null) Object.Destroy(prefabGo);
            }
        }

        [Test]
        public void Create_NestedScopes_ShouldInjectAndDisposeRecursive()
        {
            Level1Service.Disposed = false;
            Level2Service.Disposed = false;

            GameObject p1 = null;
            GameObject p2 = null;
            GameObject p3 = null;

            Level1MonoBehaviour clone1 = null;
            Level2MonoBehaviour clone2 = null;
            Level3MonoBehaviour clone3 = null;

            try
            {
                p3 = new GameObject("Prefab3");
                var comp3 = p3.AddComponent<Level3MonoBehaviour>();
                Level2Scope.Level3Prefab = comp3;

                p2 = new GameObject("Prefab2");
                var comp2 = p2.AddComponent<Level2MonoBehaviour>();
                p2.AddComponent<Level2Scope>();
                Level1Scope.Level2Prefab = comp2;

                p1 = new GameObject("Prefab1");
                var comp1 = p1.AddComponent<Level1MonoBehaviour>();
                p1.AddComponent<Level1Scope>();

                var builder = new ContainerBuilder();
                builder.BindMonoFactory<Level1MonoBehaviour, Level1Factory>(comp1, hasFactoryScope: true);
                var container = builder.Build();

                var factory1 = container.Single<Level1Factory>();
                var data1 = new Level1Data();
                clone1 = factory1.Create(data1);
                clone1.Should().NotBeNull();
                ((IData<Level1Data>)clone1).Data.Should().NotBeNull();
                ((IData<Level1Data>)clone1).Data.Should().BeSameAs(data1);
                var scope1 = clone1.GetComponent<Level1Scope>();
                scope1.Should().NotBeNull();
                var container1 = scope1.scopeContainer;
                container1.Should().NotBeNull();

                var factory2 = container1.Resolve<Level2Factory>();
                var data2 = new Level2Data();
                clone2 = factory2.Create(data2);
                clone2.Should().NotBeNull();
                ((IData<Level2Data>)clone2).Data.Should().NotBeNull();
                ((IData<Level2Data>)clone2).Data.Should().BeSameAs(data2);
                clone2.level1Data.Should().NotBeNull();
                clone2.level1Data.Should().BeSameAs(data1);
                var scope2 = clone2.GetComponent<Level2Scope>();
                scope2.Should().NotBeNull();
                var container2 = scope2.scopeContainer;
                container2.Should().NotBeNull();

                var factory3 = container2.Resolve<Level3Factory>();
                var data3 = new Level3Data();
                clone3 = factory3.Create(data3);
                clone3.Should().NotBeNull();
                ((IData<Level3Data>)clone3).Data.Should().NotBeNull();
                ((IData<Level3Data>)clone3).Data.Should().BeSameAs(data3);
                clone3.level1Data.Should().NotBeNull();
                clone3.level1Data.Should().BeSameAs(data1);
                clone3.level2Data.Should().NotBeNull();
                clone3.level2Data.Should().BeSameAs(data2);

                clone3.level2Service.Should().NotBeNull();
                clone3.level2Service.Should().BeOfType<Level2Service>();

                Object.DestroyImmediate(clone1.gameObject);
                clone1 = null;

                Level1Service.Disposed.Should().BeTrue();
                Level2Service.Disposed.Should().BeTrue();
            }
            finally
            {
                if (clone3 != null) Object.DestroyImmediate(clone3.gameObject);
                if (clone2 != null) Object.DestroyImmediate(clone2.gameObject);
                if (clone1 != null) Object.DestroyImmediate(clone1.gameObject);
                if (p3 != null) Object.DestroyImmediate(p3);
                if (p2 != null) Object.DestroyImmediate(p2);
                if (p1 != null) Object.DestroyImmediate(p1);
            }
        }
    }
}