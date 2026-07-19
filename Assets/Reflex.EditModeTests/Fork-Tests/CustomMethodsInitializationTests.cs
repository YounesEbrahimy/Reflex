using Reflex.Generics.Interfaces;
using Reflex.Attributes;
using FluentAssertions;
using NUnit.Framework;
using Reflex.Enums;
using Reflex.Core;

namespace Reflex.EditModeTests
{
    internal class CustomMethodsInitializationTests
    {
        private interface I1
        {
        }

        private interface I2
        {
        }

        private interface I3
        {
        }

        private interface I4
        {
        }

        private interface I5
        {
        }

        private interface I6
        {
        }

        private interface I7
        {
        }

        private class Dependency
        {
        }

        private class TestClass : I1, I2, I3, I4, I5, I6, I7, IInitializable
        {
            [Inject] public Dependency DependencyField { get; private set; }
            public bool IsInitialized { get; private set; } = false;
            public bool DependencyWasNotNullDuringInitialization { get; private set; } = false;

            public void Initialize()
            {
                IsInitialized = true;
                DependencyWasNotNullDuringInitialization = DependencyField != null;
            }
        }

        private ContainerBuilder SetupBuilder()
        {
            var builder = new ContainerBuilder();
            builder.Bind<Dependency>();
            return builder;
        }

        private void AssertInitializedProperly(TestClass instance)
        {
            instance.Should().NotBeNull();
            instance.IsInitialized.Should().BeTrue("Initialize() should be called.");
            instance.DependencyWasNotNullDuringInitialization.Should()
                .BeTrue("Dependencies should be injected before Initialize() is called.");
            instance.DependencyField.Should().NotBeNull();
        }

        [Test]
        public void Bind_0Contracts_ShouldInitializeProperly()
        {
            var builder = SetupBuilder();
            builder.Bind<TestClass>();
            var container = builder.Build();

            var instance = container.Single<TestClass>();
            AssertInitializedProperly(instance);
        }

        [Test]
        public void Bind_1Contract_ShouldInitializeProperly()
        {
            var builder = SetupBuilder();
            builder.Bind<I1, TestClass>();
            var container = builder.Build();

            var instance = (TestClass)container.Single<I1>();
            AssertInitializedProperly(instance);
        }

        [Test]
        public void Bind_2Contracts_ShouldInitializeProperly()
        {
            var builder = SetupBuilder();
            builder.Bind<I1, I2, TestClass>();
            var container = builder.Build();

            var instance1 = (TestClass)container.Single<I1>();
            var instance2 = (TestClass)container.Single<I2>();

            AssertInitializedProperly(instance1);
            instance1.Should().BeSameAs(instance2);
        }

        [Test]
        public void Bind_3Contracts_ShouldInitializeProperly()
        {
            var builder = SetupBuilder();
            builder.Bind<I1, I2, I3, TestClass>();
            var container = builder.Build();

            var instance = (TestClass)container.Single<I3>();
            AssertInitializedProperly(instance);
        }

        [Test]
        public void Bind_4Contracts_ShouldInitializeProperly()
        {
            var builder = SetupBuilder();
            builder.Bind<I1, I2, I3, I4, TestClass>();
            var container = builder.Build();

            var instance = (TestClass)container.Single<I4>();
            AssertInitializedProperly(instance);
        }

        [Test]
        public void Bind_5Contracts_ShouldInitializeProperly()
        {
            var builder = SetupBuilder();
            builder.Bind<I1, I2, I3, I4, I5, TestClass>();
            var container = builder.Build();

            var instance = (TestClass)container.Single<I5>();
            AssertInitializedProperly(instance);
        }

        [Test]
        public void Bind_6Contracts_ShouldInitializeProperly()
        {
            var builder = SetupBuilder();
            builder.Bind<I1, I2, I3, I4, I5, I6, TestClass>();
            var container = builder.Build();

            var instance = (TestClass)container.Single<I6>();
            AssertInitializedProperly(instance);
        }

        [Test]
        public void Bind_7Contracts_ShouldInitializeProperly()
        {
            var builder = SetupBuilder();
            builder.Bind<I1, I2, I3, I4, I5, I6, I7, TestClass>();
            var container = builder.Build();

            var instance = (TestClass)container.Single<I7>();
            AssertInitializedProperly(instance);
        }

        [Test]
        public void BindInstance_ShouldInitializeProperly()
        {
            var builder = SetupBuilder();
            var instance = new TestClass();
            builder.BindInstance(instance);

            var container = builder.Build();
            var resolved = container.Single<TestClass>();

            resolved.IsInitialized.Should().BeFalse("Initialize() should not be called on BindInstance.");
            resolved.DependencyWasNotNullDuringInitialization.Should()
                .BeFalse("Dependencies should not be injected when using BindInstance.");
        }

        [Test]
        public void BindInstanceTo_ShouldInitializeProperly()
        {
            var builder = SetupBuilder();
            var instance = new TestClass();
            builder.BindInstanceTo<I1>(instance);
            var container = builder.Build();

            var resolved = (TestClass)container.Single<I1>();
            resolved.IsInitialized.Should().BeFalse("Initialize() should not be called on BindInstanceTo.");
            resolved.DependencyWasNotNullDuringInitialization.Should()
                .BeFalse("Dependencies should not be injected when using BindInstanceTo.");
        }

        [Test]
        public void BindInterFaces_ShouldInitializeProperly()
        {
            var builder = SetupBuilder();
            builder.BindInterFaces<TestClass>();
            var container = builder.Build();

            var instance = (TestClass)container.Single<I1>();
            AssertInitializedProperly(instance);
        }

        [Test]
        public void BindInterFacesAndSelf_ShouldInitializeProperly()
        {
            var builder = SetupBuilder();
            builder.BindInterFacesAndSelf<TestClass>();
            var container = builder.Build();

            var instance1 = container.Single<TestClass>();
            var instance2 = (TestClass)container.Single<I7>();

            AssertInitializedProperly(instance1);
            instance1.Should().BeSameAs(instance2);
        }

        private class CombinationTestClass : IInitializable
        {
            [Inject] public System.Collections.Generic.List<string> Log { get; private set; }
            [Inject] public Dependency DependencyField { get; private set; }
            public bool IsInitialized { get; private set; }
            public bool DependencyWasNotNullDuringInitialization { get; private set; }

            public void Initialize()
            {
                IsInitialized = true;
                DependencyWasNotNullDuringInitialization = DependencyField != null;
                Log?.Add("Initialized");
            }
        }

        [Test]
        public void Bind_Transient_Eager_ShouldThrowException()
        {
            var builder = SetupBuilder();
            System.Action act = () => builder.Bind<CombinationTestClass>(Lifetime.Transient, Resolution.Eager);
            act.Should().Throw<System.Exception>();
        }

        [Test]
        public void Bind_Singleton_Lazy_ShouldInitializeOnceOnFirstResolve()
        {
            var log = new System.Collections.Generic.List<string>();
            var builder = SetupBuilder();
            builder.BindInstance(log);
            builder.Bind<CombinationTestClass>(Lifetime.Singleton, Resolution.Lazy);

            var container = builder.Build();
            log.Should().BeEmpty("Should not eagerly initialize.");

            var instance1 = container.Single<CombinationTestClass>();
            log.Should().HaveCount(1, "Should initialize exactly once on first resolve.");
            instance1.IsInitialized.Should().BeTrue();
            instance1.DependencyWasNotNullDuringInitialization.Should().BeTrue();

            var instance2 = container.Single<CombinationTestClass>();
            log.Should().HaveCount(1, "Should not initialize again on subsequent resolves.");
            instance2.Should().BeSameAs(instance1);
        }

        [Test]
        public void Bind_Singleton_Eager_ShouldInitializeOnBuild()
        {
            var log = new System.Collections.Generic.List<string>();
            var builder = SetupBuilder();
            builder.BindInstance(log);
            builder.Bind<CombinationTestClass>(Lifetime.Singleton, Resolution.Eager);

            var container = builder.Build();
            log.Should().HaveCount(1, "Should initialize immediately during Build().");

            var instance1 = container.Single<CombinationTestClass>();
            log.Should().HaveCount(1, "Should not initialize again when resolved.");
            instance1.IsInitialized.Should().BeTrue();
            instance1.DependencyWasNotNullDuringInitialization.Should().BeTrue();

            var instance2 = container.Single<CombinationTestClass>();
            log.Should().HaveCount(1);
            instance2.Should().BeSameAs(instance1);
        }

        [Test]
        public void Bind_Scoped_Lazy_ShouldInitializeOncePerScopeOnFirstResolve()
        {
            var log = new System.Collections.Generic.List<string>();
            var builder = SetupBuilder();
            builder.BindInstance(log);
            builder.Bind<CombinationTestClass>(Lifetime.Scoped, Resolution.Lazy);

            var parentContainer = builder.Build();
            log.Should().BeEmpty("Should not eagerly initialize in parent.");

            var parentInstance1 = parentContainer.Single<CombinationTestClass>();
            log.Should().HaveCount(1, "Should initialize on first resolve in parent.");
            parentInstance1.IsInitialized.Should().BeTrue();
            parentInstance1.DependencyWasNotNullDuringInitialization.Should().BeTrue();

            var parentInstance2 = parentContainer.Single<CombinationTestClass>();
            log.Should().HaveCount(1);
            parentInstance2.Should().BeSameAs(parentInstance1);

            var childContainer = parentContainer.Scope();
            log.Should().HaveCount(1, "Should not eagerly initialize in child container either.");

            var childInstance1 = childContainer.Single<CombinationTestClass>();
            log.Should().HaveCount(2, "Should initialize on first resolve in child container.");
            childInstance1.IsInitialized.Should().BeTrue();
            childInstance1.DependencyWasNotNullDuringInitialization.Should().BeTrue();
            childInstance1.Should().NotBeSameAs(parentInstance1);

            var childInstance2 = childContainer.Single<CombinationTestClass>();
            log.Should().HaveCount(2);
            childInstance2.Should().BeSameAs(childInstance1);
        }

        [Test]
        public void Bind_Scoped_Eager_ShouldInitializeOnBuildPerScope()
        {
            var log = new System.Collections.Generic.List<string>();
            var builder = SetupBuilder();
            builder.BindInstance(log);
            builder.Bind<CombinationTestClass>(Lifetime.Scoped, Resolution.Eager);

            var parentContainer = builder.Build();
            log.Should().HaveCount(1, "Should initialize immediately during parent Build().");

            var parentInstance = parentContainer.Single<CombinationTestClass>();
            log.Should().HaveCount(1);
            parentInstance.IsInitialized.Should().BeTrue();
            parentInstance.DependencyWasNotNullDuringInitialization.Should().BeTrue();

            var childContainer = parentContainer.Scope();
            log.Should().HaveCount(2, "Should initialize immediately during child Scope() creation.");

            var childInstance = childContainer.Single<CombinationTestClass>();
            log.Should().HaveCount(2);
            childInstance.IsInitialized.Should().BeTrue();
            childInstance.DependencyWasNotNullDuringInitialization.Should().BeTrue();
            childInstance.Should().NotBeSameAs(parentInstance);
        }

        [Test]
        public void Bind_Transient_Lazy_ShouldInitializeOnEveryResolve()
        {
            var log = new System.Collections.Generic.List<string>();
            var builder = SetupBuilder();
            builder.BindInstance(log);
            builder.Bind<CombinationTestClass>(Lifetime.Transient, Resolution.Lazy);

            var container = builder.Build();
            log.Should().BeEmpty();

            var instance1 = container.Single<CombinationTestClass>();
            log.Should().HaveCount(1, "Should initialize on first resolve.");
            instance1.IsInitialized.Should().BeTrue();
            instance1.DependencyWasNotNullDuringInitialization.Should().BeTrue();

            var instance2 = container.Single<CombinationTestClass>();
            log.Should().HaveCount(2, "Should initialize again on second resolve.");
            instance2.IsInitialized.Should().BeTrue();
            instance2.DependencyWasNotNullDuringInitialization.Should().BeTrue();

            instance2.Should().NotBeSameAs(instance1);
        }
    }
}