using Object = UnityEngine.Object;
using Reflex.Injectors;
using Reflex.Logging;
using Reflex.Core;
using UnityEngine;
using System;

namespace Reflex.Factories.Mono
{
    public abstract class BaseMonoFactory<T> : IDisposable where T : MonoBehaviour
    {
        protected Container container;
        protected T original;
        protected bool factoryScope;

        private protected BaseMonoFactory()
        {
        }

        public void Setup(Container container, T original, bool factoryScope)
        {
            this.container = container;
            this.original = original;
            this.factoryScope = factoryScope;
        }

        public void Dispose()
        {
            original = null;
            container = null;
        }

        protected static T CreateBasePrefab(out bool state, T original)
        {
            state = original.gameObject.activeSelf;
            original.gameObject.SetActive(false);
            var clone = Object.Instantiate(original.gameObject);
            original.gameObject.SetActive(state);
            return clone.GetComponent<T>();
        }

        protected static void InjectClone(GameObject clone, Container container, bool factoryScope,
            object factoryData = null)
        {
            if (factoryScope)
            {
                var scope = clone.GetComponent<FactoryScope>();
                if (!scope)
                    throw new MissingComponentException(
                        $"No component of type {nameof(FactoryScope)} was found on the object!");
                container = container.Scope(builder =>
                {
                    builder.SetName($"{typeof(T)} ({clone.GetHashCode()})");
                    scope.InstallBindings(builder);
                    if (factoryData != null) builder.BindInstance(factoryData);
                    ReflexLogger.Log($"Factory ({typeof(T)}) Bindings Installed", LogLevel.Info, clone);
                });
                scope.scopeContainer = container;
            }

            GameObjectInjector.InjectRecursive(clone, container);
        }

        protected static T SetOriginalState(T clone, bool state)
        {
            clone.gameObject.SetActive(state);
            return clone;
        }
    }
}