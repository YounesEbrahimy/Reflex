using System;
using FluentAssertions;
using NUnit.Framework;
using Reflex.Core;
using Reflex.Enums;
using Reflex.Exceptions;

namespace Reflex.EditModeTests
{
    internal class ForkCustomMethodsTests
    {
        private interface I1 {}
        private interface I2 {}
        private interface I3 {}
        private interface I4 {}
        private interface I5 {}
        private interface I6 {}
        private interface I7 {}

        private class MultiInterfaceClass : I1, I2, I3, I4, I5, I6, I7 {}

        private class SimpleConcrete {}

        [Test]
        public void Bind_SelfSingleton_ShouldReturnSameInstance()
        {
            var builder = new ContainerBuilder();
            builder.Bind<SimpleConcrete>(Lifetime.Singleton);
            var container = builder.Build();

            var instance1 = container.Single<SimpleConcrete>();
            var instance2 = container.Single<SimpleConcrete>();

            instance1.Should().NotBeNull();
            instance2.Should().NotBeNull();
            instance1.Should().BeSameAs(instance2);
        }

        [Test]
        public void Bind_SelfTransient_ShouldReturnDifferentInstances()
        {
            var builder = new ContainerBuilder();
            builder.Bind<SimpleConcrete>(Lifetime.Transient);
            var container = builder.Build();

            var instance1 = container.Single<SimpleConcrete>();
            var instance2 = container.Single<SimpleConcrete>();

            instance1.Should().NotBeNull();
            instance2.Should().NotBeNull();
            instance1.Should().NotBeSameAs(instance2);
        }

        [Test]
        public void Bind_OneContract_ShouldResolve()
        {
            var builder = new ContainerBuilder();
            builder.Bind<I1, MultiInterfaceClass>(Lifetime.Singleton);
            var container = builder.Build();

            var resolved = container.Single<I1>();
            resolved.Should().NotBeNull();
            resolved.Should().BeOfType<MultiInterfaceClass>();
        }

        [Test]
        public void Bind_TwoContracts_ShouldResolve()
        {
            var builder = new ContainerBuilder();
            builder.Bind<I1, I2, MultiInterfaceClass>(Lifetime.Singleton);
            var container = builder.Build();

            var res1 = container.Single<I1>();
            var res2 = container.Single<I2>();
            
            res1.Should().NotBeNull();
            res2.Should().NotBeNull();
            res1.Should().BeSameAs(res2);
        }

        [Test]
        public void Bind_ThreeContracts_ShouldResolve()
        {
            var builder = new ContainerBuilder();
            builder.Bind<I1, I2, I3, MultiInterfaceClass>(Lifetime.Singleton);
            var container = builder.Build();

            var res1 = container.Single<I1>();
            var res2 = container.Single<I2>();
            var res3 = container.Single<I3>();

            res1.Should().NotBeNull();
            res2.Should().NotBeNull();
            res3.Should().NotBeNull();
            res1.Should().BeSameAs(res2);
            res2.Should().BeSameAs(res3);
        }

        [Test]
        public void Bind_FourContracts_ShouldResolve()
        {
            var builder = new ContainerBuilder();
            builder.Bind<I1, I2, I3, I4, MultiInterfaceClass>(Lifetime.Singleton);
            var container = builder.Build();

            var res1 = container.Single<I1>();
            var res2 = container.Single<I2>();
            var res3 = container.Single<I3>();
            var res4 = container.Single<I4>();

            res1.Should().NotBeNull();
            res2.Should().NotBeNull();
            res3.Should().NotBeNull();
            res4.Should().NotBeNull();
            res1.Should().BeSameAs(res2);
            res2.Should().BeSameAs(res3);
            res3.Should().BeSameAs(res4);
        }

        [Test]
        public void Bind_FiveContracts_ShouldResolve()
        {
            var builder = new ContainerBuilder();
            builder.Bind<I1, I2, I3, I4, I5, MultiInterfaceClass>(Lifetime.Singleton);
            var container = builder.Build();

            var res1 = container.Single<I1>();
            var res5 = container.Single<I5>();

            res1.Should().NotBeNull();
            res5.Should().NotBeNull();
            res1.Should().BeSameAs(res5);
        }

        [Test]
        public void Bind_SixContracts_ShouldResolve()
        {
            var builder = new ContainerBuilder();
            builder.Bind<I1, I2, I3, I4, I5, I6, MultiInterfaceClass>(Lifetime.Singleton);
            var container = builder.Build();

            var res1 = container.Single<I1>();
            var res6 = container.Single<I6>();

            res1.Should().NotBeNull();
            res6.Should().NotBeNull();
            res1.Should().BeSameAs(res6);
        }

        [Test]
        public void Bind_SevenContracts_ShouldResolve()
        {
            var builder = new ContainerBuilder();
            builder.Bind<I1, I2, I3, I4, I5, I6, I7, MultiInterfaceClass>(Lifetime.Singleton);
            var container = builder.Build();

            var res1 = container.Single<I1>();
            var res7 = container.Single<I7>();

            res1.Should().NotBeNull();
            res7.Should().NotBeNull();
            res1.Should().BeSameAs(res7);
        }

        [Test]
        public void BindInstance_ShouldResolveAsSingleton()
        {
            var instance = new SimpleConcrete();
            var builder = new ContainerBuilder();
            builder.BindInstance(instance);
            var container = builder.Build();

            var resolved = container.Single<SimpleConcrete>();
            resolved.Should().NotBeNull();
            resolved.Should().BeSameAs(instance);
        }

        [Test]
        public void BindInstanceTo_ShouldResolveAsContract()
        {
            var instance = new MultiInterfaceClass();
            var builder = new ContainerBuilder();
            builder.BindInstanceTo<I1>(instance);
            var container = builder.Build();

            var resolved = container.Single<I1>();
            resolved.Should().NotBeNull();
            resolved.Should().BeSameAs(instance);
            
            Action resolveConcrete = () => container.Single<MultiInterfaceClass>();
            resolveConcrete.Should().Throw<UnknownContractException>();
        }

        [Test]
        public void BindInterfaces_ShouldResolveAllInterfacesButNotSelf()
        {
            var builder = new ContainerBuilder();
            builder.BindInterFaces<MultiInterfaceClass>(Lifetime.Singleton);
            var container = builder.Build();

            var res1 = container.Single<I1>();
            var res2 = container.Single<I2>();
            var res3 = container.Single<I3>();
            var res4 = container.Single<I4>();
            var res5 = container.Single<I5>();
            var res6 = container.Single<I6>();
            var res7 = container.Single<I7>();

            res1.Should().NotBeNull();
            res2.Should().NotBeNull();
            res3.Should().NotBeNull();
            res4.Should().NotBeNull();
            res5.Should().NotBeNull();
            res6.Should().NotBeNull();
            res7.Should().NotBeNull();

            res1.Should().BeOfType<MultiInterfaceClass>();
            res2.Should().BeOfType<MultiInterfaceClass>();
            res3.Should().BeOfType<MultiInterfaceClass>();
            res4.Should().BeOfType<MultiInterfaceClass>();
            res5.Should().BeOfType<MultiInterfaceClass>();
            res6.Should().BeOfType<MultiInterfaceClass>();
            res7.Should().BeOfType<MultiInterfaceClass>();

            Action resolveConcrete = () => container.Single<MultiInterfaceClass>();
            resolveConcrete.Should().Throw<UnknownContractException>();
        }

        [Test]
        public void BindInterfacesAndSelf_ShouldResolveAllInterfacesAndSelf()
        {
            var builder = new ContainerBuilder();
            builder.BindInterFacesAndSelf<MultiInterfaceClass>(Lifetime.Singleton);
            var container = builder.Build();

            var res1 = container.Single<I1>();
            var concrete = container.Single<MultiInterfaceClass>();

            res1.Should().NotBeNull();
            concrete.Should().NotBeNull();
            
            res1.Should().BeOfType<MultiInterfaceClass>();
            concrete.Should().BeOfType<MultiInterfaceClass>();
            res1.Should().BeSameAs(concrete);
        }
    }
}
