using Reflex.DataTypes.Interfaces;
using Reflex.Generics.Interfaces;
using Reflex.Pools.Interfaces;
using Reflex.Injectors;
using System;

namespace Reflex.Pools.Plain
{
    public class Pool<T> : BasePool<T>
    {
        protected override T CreateInstance()
        {
            var instance = (T)Activator.CreateInstance(typeof(T));
            AttributeInjector.Inject(instance, container);
            if (instance is IInitializable initializable)
            {
                initializable.Initialize();
            }

            return instance;
        }

        protected override void DestroyInstance(T instance)
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        protected override void OnTake(T instance)
        {
        }

        protected override void OnReturn(T instance)
        {
        }
    }

    public class Pool<TData, T> : BasePool<T> where T : IData<TData>
    {
        protected override T CreateInstance()
        {
            var instance = (T)Activator.CreateInstance(typeof(T));
            AttributeInjector.Inject(instance, container);
            if (instance is IInitializable initializable)
            {
                initializable.Initialize();
            }

            return instance;
        }

        protected override void DestroyInstance(T instance)
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        protected override void OnTake(T instance)
        {
        }

        protected override void OnReturn(T instance)
        {
        }

        public T Take(TData data)
        {
            var instance = inactive.Count > 0 ? inactive.Pop() : CreateInstance();
            OnTake(instance);
            instance.Data = data;
            if (instance is IPoolable poolable)
            {
                poolable.OnSpawn();
            }

            return instance;
        }
    }
}