using Reflex.Core;
using UnityEngine;

namespace Reflex.Factories.Mono
{
    [DisallowMultipleComponent]
    public abstract class FactoryScope : MonoBehaviour
    {
        internal Container scopeContainer;
        public abstract void InstallBindings(ContainerBuilder containerBuilder);

        private void OnDestroy()
        {
            // Container is not null only if Factory has set up a scope for the clone,
            // So it must be disposed when the clone is destroyed! 
            scopeContainer?.Dispose();
        }
    }
}