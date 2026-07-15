using UnityEngine;

namespace Reflex.Factories.Mono
{
    public abstract class MonoFactory<T> : BaseMonoFactory<T> where T : MonoBehaviour
    {
        public T Create()
        {
            var clone = CreateBasePrefab(out var originalState, original);
            InjectClone(clone.gameObject, container, factoryScope);
            return SetOriginalState(clone, originalState);
        }
    }

    public abstract class MonoFactory<TData, T> : BaseMonoFactory<T> where T : MonoBehaviour, IFactoryData<TData>
    {
        public T Create(TData data)
        {
            var clone = CreateBasePrefab(out var originalState, original);
            InjectClone(clone.gameObject, container, factoryScope, data);
            clone.Data = data;
            return SetOriginalState(clone, originalState);
        }
    }
}