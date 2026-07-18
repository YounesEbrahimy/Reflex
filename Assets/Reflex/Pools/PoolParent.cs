using UnityEngine;

namespace Reflex.Pools
{
    internal static class PoolParent
    {
        private static Transform _root;

        public static Transform Root
        {
            get
            {
                if (_root == null)
                {
                    var go = new GameObject("[Reflex Pools]");
                    Object.DontDestroyOnLoad(go);
                    _root = go.transform;
                }

                return _root;
            }
        }
    }
}