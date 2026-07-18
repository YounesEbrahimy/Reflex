using Reflex.Injectors;
using Reflex.Core;
using UnityEngine;

namespace Reflex.Pools.Mono
{
    public abstract class BaseMonoPool<T> : BasePool<T> where T : MonoBehaviour
    {
        private T prefab;
        private Transform poolRoot;

        public void Setup(Container container, T prefab, int minSize, int maxSize, int preWarmSize)
        {
            this.prefab = prefab;

            var poolName = $"[MonoPool<{typeof(T).Name}>]";
            var go = new GameObject(poolName);
            go.transform.SetParent(PoolParent.Root);
            poolRoot = go.transform;

            base.Setup(container, minSize, maxSize, preWarmSize);
        }

        protected override T CreateInstance()
        {
            var wasActive = prefab.gameObject.activeSelf;
            prefab.gameObject.SetActive(false);

            var instance = Object.Instantiate(prefab, poolRoot);
            prefab.gameObject.SetActive(wasActive);

            GameObjectInjector.InjectRecursive(instance.gameObject, container);

            return instance;
        }

        protected override void DestroyInstance(T instance)
        {
            if (instance != null && instance.gameObject != null)
            {
                Object.Destroy(instance.gameObject);
            }
        }

        protected override void OnTake(T instance)
        {
            instance.gameObject.SetActive(true);
        }

        protected override void OnReturn(T instance)
        {
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(poolRoot);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (poolRoot != null)
            {
                Object.Destroy(poolRoot.gameObject);
            }

            prefab = null;
        }
    }
}