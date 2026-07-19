using System.Collections.Generic;
using Reflex.Pools.Interfaces;
using Reflex.Core;
using System;

namespace Reflex.Pools
{
    public abstract class BasePool<T> : IDisposable
    {
        protected Container container;
        protected readonly Stack<T> inactive = new Stack<T>();

        protected int minSize;
        protected int maxSize;
        protected int preWarmSize;

        private protected BasePool()
        {
        }

        public void Setup(Container container, int minSize, int maxSize, int preWarmSize)
        {
            this.container = container;
            this.minSize = minSize;
            this.maxSize = maxSize;
            this.preWarmSize = preWarmSize;

            ValidateSizes();
            PreWarm();
        }

        private void ValidateSizes()
        {
            if (minSize < 0)
                throw new ArgumentOutOfRangeException(nameof(minSize), "MinSize cannot be negative.");

            if (maxSize < minSize)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "MaxSize cannot be less than MinSize.");

            if (preWarmSize < minSize || preWarmSize > maxSize)
                throw new ArgumentOutOfRangeException(nameof(preWarmSize),
                    "PreWarmSize must be between MinSize and MaxSize.");
        }

        private void PreWarm()
        {
            for (var i = 0; i < preWarmSize; i++)
            {
                var instance = CreateInstance();
                OnReturn(instance);
                inactive.Push(instance);
            }
        }

        public T Take()
        {
            var instance = inactive.Count > 0 ? inactive.Pop() : CreateInstance();
            OnTake(instance);
            if (instance is IPoolable poolable)
            {
                poolable.OnSpawn();
            }

            return instance;
        }

        public void Return(T instance)
        {
            if (instance is IPoolable poolable)
            {
                poolable.OnDespawn();
            }

            if (inactive.Count >= maxSize)
            {
                DestroyInstance(instance);
            }
            else
            {
                OnReturn(instance);
                inactive.Push(instance);
            }
        }

        public virtual void Dispose()
        {
            while (inactive.Count > 0)
            {
                DestroyInstance(inactive.Pop());
            }

            container = null;
        }

        protected abstract T CreateInstance();
        protected abstract void DestroyInstance(T instance);
        protected abstract void OnTake(T instance);
        protected abstract void OnReturn(T instance);
    }
}