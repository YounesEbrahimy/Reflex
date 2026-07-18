using Reflex.DataTypes.Interfaces;

namespace Reflex.Factories.Plain
{
    public abstract class Factory<T> : BaseFactory<T>
    {
        public T Create()
        {
            var instance = CreateInstance();
            return ProcessInstance(instance, container);
        }
    }

    public abstract class Factory<TData, T> : BaseFactory<T> where T : IData<TData>
    {
        public T Create(TData data)
        {
            var instance = CreateInstance();
            instance.Data = data;
            return ProcessInstance(instance, container);
        }
    }
}