using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace 更大的方块收集器
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private const int TargetTypeId = 1166;      // 改成你的方块收集器的 typeID
        private const float TargetValue = 30f;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(RetryPatch());
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene s, LoadSceneMode m)
        {
            StartCoroutine(RetryPatch());
        }

        // 连续尝试几次以覆盖晚加载对象
        private IEnumerator RetryPatch()
        {
            int total = 0;
            for (int i = 0; i < 8; i++)
            {
                total += PatchActiveSceneByTypeId();
                total += PatchLoadedAssetsByTypeId();
                if (total > 0) break;
                yield return new WaitForSeconds(0.6f);
            }
            Debug.Log($"[更大的方块收集器] Patched entries: {total}");
        }

        // 场景中的实例
        private int PatchActiveSceneByTypeId()
        {
            int hits = 0;
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                var comps = root.GetComponentsInChildren<Component>(true);
                foreach (var c in comps)
                {
                    if (HasTypeId(c, TargetTypeId))
                    {
                        if (PatchTwoByIndex(((Component)c).gameObject)) hits++;
                    }
                }
            }
            return hits;
        }

        // 已加载的资源和预制
        private int PatchLoadedAssetsByTypeId()
        {
            int hits = 0;
            var comps = Resources.FindObjectsOfTypeAll<Component>();
            foreach (var c in comps)
            {
                if (c == null) continue;
                if (HasTypeId(c, TargetTypeId))
                {
                    if (PatchTwoByIndex(c.gameObject)) hits++;
                }
            }
            return hits;
        }

        // 任意组件只要有 int typeID 字段且等于目标就命中
        private static bool HasTypeId(Component comp, int target)
        {
            var fi = comp.GetType().GetField("typeID",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi == null) return false;
            try
            {
                var v = fi.GetValue(comp);
                return v is int id && id == target;
            }
            catch { return false; }
        }

        // 只改 list[0] 与 list[1] 两条
        private bool PatchTwoByIndex(GameObject go)
        {
            var mdcType = FindType("ItemStatsSystem.ModifierDescriptionCollection");
            if (mdcType == null) return false;

            var mdc = go.GetComponent(mdcType);
            if (mdc == null) return false;

            // 用 IList 直接按下标写
            var list = ReadField<System.Collections.IList>(mdc, "list");
            if (list == null || list.Count < 2) return false;

            bool changed = false;
            changed |= ForceSet(list[0]); // 0 行通常是 Character MaxWeight Add
            changed |= ForceSet(list[1]); // 1 行通常是 Character InventoryCapacity Add
            return changed;
        }

        private bool ForceSet(object desc)
        {
            if (desc == null) return false;

            // value = 30
            var vf = desc.GetType().GetField("value",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (vf != null)
            {
                try { vf.SetValue(desc, TargetValue); } catch { }
            }

            // type = Add 如果存在该枚举值
            var tf = desc.GetType().GetField("type",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (tf != null && tf.FieldType.IsEnum)
            {
                foreach (var v in Enum.GetValues(tf.FieldType))
                {
                    if (string.Equals(v.ToString(), "Add", StringComparison.OrdinalIgnoreCase))
                    {
                        try { tf.SetValue(desc, v); } catch { }
                        break;
                    }
                }
            }
            return true;
        }

        // 反射帮助
        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        private static T ReadField<T>(object obj, string field)
        {
            if (obj == null) return default;
            var fi = obj.GetType().GetField(field,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi == null) return default;
            var v = fi.GetValue(obj);
            return v is T tv ? tv : default;
        }
    }
}
