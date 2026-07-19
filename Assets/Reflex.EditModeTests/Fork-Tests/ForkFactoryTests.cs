using Reflex.DataTypes.Interfaces;
using Reflex.Generics.Interfaces;
using Reflex.Factories.Plain;
using Reflex.Attributes;
using FluentAssertions;
using NUnit.Framework;
using Reflex.Enums;
using Reflex.Core;

namespace Reflex.EditModeTests
{
    internal class ForkFactoryTests
    {
        private interface IDependency
        {
        }

        private class Dependency : IDependency
        {
        }

        private class SimplePlayer : IInitializable
        {
            [Inject] public IDependency dependency { get; set; }
            public bool Initialized { get; private set; } = false;

            public void Initialize()
            {
                Initialized = true;
            }
        }

        private class SimplePlayerFactory : Factory<SimplePlayer>
        {
        }

        private struct SimplePlayerData
        {
            public string Name;
            public int Score;
        }

        private class PlayerWithData : IData<SimplePlayerData>, IInitializable
        {
            SimplePlayerData IData<SimplePlayerData>.Data { get; set; }
            [Inject] public IDependency dependency { get; set; }
            public bool Initialized { get; private set; } = false;

            public void Initialize()
            {
                Initialized = true;
            }
        }

        private class PlayerWithDataFactory : Factory<SimplePlayerData, PlayerWithData>
        {
        }

        [Test]
        public void Create_StandardFactory_ShouldInstantiateInjectAndInitialize()
        {
            var builder = new ContainerBuilder();
            builder.Bind<IDependency, Dependency>(Lifetime.Singleton);
            builder.BindFactory<SimplePlayer, SimplePlayerFactory>(Lifetime.Singleton);
            var container = builder.Build();

            var factory = container.Single<SimplePlayerFactory>();
            factory.Should().NotBeNull();

            var player = factory.Create();
            player.Should().NotBeNull();
            player.dependency.Should().NotBeNull();
            player.dependency.Should().BeOfType<Dependency>();
            player.Initialized.Should().BeTrue();
        }

        [Test]
        public void Create_ParameterizedFactory_ShouldInstantiateInjectAssignDataAndInitialize()
        {
            var builder = new ContainerBuilder();
            builder.Bind<IDependency, Dependency>(Lifetime.Singleton);
            builder.BindFactory<PlayerWithData, PlayerWithDataFactory>(Lifetime.Singleton);
            var container = builder.Build();

            var factory = container.Single<PlayerWithDataFactory>();
            factory.Should().NotBeNull();

            var testData = new SimplePlayerData { Name = "Joseph", Score = 100 };
            var player = factory.Create(testData);

            player.Should().NotBeNull();
            player.dependency.Should().NotBeNull();
            player.dependency.Should().BeOfType<Dependency>();
            ((IData<SimplePlayerData>)player).Data.Name.Should().Be("Joseph");
            ((IData<SimplePlayerData>)player).Data.Score.Should().Be(100);
            player.Initialized.Should().BeTrue();
        }
    }
}