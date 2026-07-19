using Reflex.DataTypes.Interfaces;
using Reflex.Pools.Interfaces;
using UnityEngine;

namespace Reflex.Pools.Mono
{
    public class MonoPool<T> : BaseMonoPool<T> where T : MonoBehaviour
    {
    }

    public class MonoPool<TData, T> : BaseMonoPool<T> where T : MonoBehaviour, IData<TData>
    {
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