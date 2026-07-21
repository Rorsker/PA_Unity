using UnityEngine;

namespace PlayableAd
{
    [DisallowMultipleComponent]
    public sealed class SoldierBreakEffectPool : MonoBehaviour
    {
        private SoldierBreakEffect[] tier1Effects;
        private SoldierBreakEffect[] tier4Effects;
        private int tier1Cursor;
        private int tier4Cursor;
        private uint playSequence;
        private bool warnedTier1;
        private bool warnedTier4;

        public void Initialize(GameObject tier1Prefab, GameObject tier4Prefab, int tier1Capacity = 8, int tier4Capacity = 4)
        {
            tier1Effects = BuildPool(tier1Prefab, Mathf.Max(1, tier1Capacity), "Tier1Break");
            tier4Effects = BuildPool(tier4Prefab, Mathf.Max(1, tier4Capacity), "Tier4Break");
        }

        public bool PlayBreak(int tier, Vector3 position, Quaternion rotation, float normalizedSpeed,
            float impactForwardSpeed, int speedLevel)
        {
            SoldierBreakEffect[] pool = tier == 1 ? tier1Effects : tier == 4 ? tier4Effects : null;
            if (pool == null || pool.Length == 0)
            {
                if (tier == 1 && !warnedTier1)
                {
                    warnedTier1 = true;
                    Debug.LogWarning("Tier 1 soldier BreakVFX prefab is missing; legacy obstacle break fallback will be used.", this);
                }
                else if (tier == 4 && !warnedTier4)
                {
                    warnedTier4 = true;
                    Debug.LogWarning("Tier 4 soldier BreakVFX prefab is missing; legacy obstacle break fallback will be used.", this);
                }
                return false;
            }

            SoldierBreakEffect effect = tier == 1
                ? GetNext(pool, ref tier1Cursor)
                : GetNext(pool, ref tier4Cursor);
            effect.Play(position, rotation, normalizedSpeed, impactForwardSpeed, speedLevel, ++playSequence);
            return true;
        }

        private SoldierBreakEffect[] BuildPool(GameObject prefab, int capacity, string prefix)
        {
            if (prefab == null) return new SoldierBreakEffect[0];
            SoldierBreakEffect prefabEffect = prefab.GetComponent<SoldierBreakEffect>();
            if (prefabEffect == null || !prefabEffect.IsConfigured)
            {
                Debug.LogError(prefab.name
                    + " must have a configured SoldierBreakEffect component on its root.", prefab);
                return new SoldierBreakEffect[0];
            }
            SoldierBreakEffect[] pool = new SoldierBreakEffect[capacity];
            for (int i = 0; i < capacity; i++)
            {
                GameObject instance = Instantiate(prefab, transform, false);
                instance.name = prefix + "_Pooled_" + (i + 1);
                SoldierBreakEffect effect = instance.GetComponent<SoldierBreakEffect>();
                pool[i] = effect;
                effect.StopAndHide();
            }
            return pool;
        }

        private static SoldierBreakEffect GetNext(SoldierBreakEffect[] pool, ref int cursor)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                int index = (cursor + i) % pool.Length;
                if (!pool[index].IsPlaying)
                {
                    cursor = (index + 1) % pool.Length;
                    return pool[index];
                }
            }

            SoldierBreakEffect oldest = pool[0];
            for (int i = 1; i < pool.Length; i++)
                if (pool[i].Sequence < oldest.Sequence) oldest = pool[i];
            oldest.StopAndHide();
            return oldest;
        }
    }
}
