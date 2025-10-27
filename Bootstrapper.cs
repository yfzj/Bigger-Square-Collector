using UnityEngine;

namespace 更大的方块收集器
{
    public static class Bootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Boot()
        {
            var go = new GameObject("更大的方块收集器_Bootstrap");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<ModBehaviour>();
            Debug.Log("[更大的方块收集器] Bootstrap created");
        }
    }
}
