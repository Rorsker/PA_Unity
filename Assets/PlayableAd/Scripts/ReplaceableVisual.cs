using System.Collections.Generic;
using UnityEngine;

namespace PlayableAd
{
    public sealed class ReplaceableVisual : MonoBehaviour
    {
        [SerializeField, InspectorName("Visual Root（视觉根节点）")] private Transform visualRoot;
        [SerializeField, InspectorName("Animator（动画控制器）")] private Animator animator;

        public Transform VisualRoot => visualRoot;
        public Animator Animator => animator;

        public void Build(
            GameObject prefab,
            RuntimeAnimatorController animatorController,
            PrimitiveType fallbackPrimitive,
            Color fallbackColor,
            Vector3 fallbackScale)
        {
            if (visualRoot == null)
            {
                GameObject root = new GameObject("VisualRoot");
                visualRoot = root.transform;
                visualRoot.SetParent(transform, false);
            }

            for (int i = visualRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(visualRoot.GetChild(i).gameObject);
            }

            GameObject visual;
            if (prefab != null)
            {
                visual = Instantiate(prefab, visualRoot);
                visual.name = prefab.name;
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
            }
            else
            {
                visual = GameObject.CreatePrimitive(fallbackPrimitive);
                visual.name = "PlaceholderVisual";
                visual.transform.SetParent(visualRoot, false);
                visual.transform.localScale = fallbackScale;

                Collider placeholderCollider = visual.GetComponent<Collider>();
                if (placeholderCollider != null)
                {
                    Destroy(placeholderCollider);
                }

                Renderer renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = RuntimeStyle.CreateMaterial(fallbackColor, 0.15f, 0.65f);
                }
            }

            animator = visual.GetComponentInChildren<Animator>();
            if (animator == null && animatorController != null)
            {
                animator = visual.AddComponent<Animator>();
            }

            if (animator != null && animatorController != null)
            {
                animator.runtimeAnimatorController = animatorController;
            }
        }

        public void Trigger(string triggerName)
        {
            if (animator != null && !string.IsNullOrEmpty(triggerName))
            {
                animator.SetTrigger(triggerName);
            }
        }
    }

    internal static class RuntimeStyle
    {
        private static readonly Dictionary<int, Material> SharedMaterials = new Dictionary<int, Material>();

        public static Material CreateMaterial(Color color, float metallic = 0f, float smoothness = 0.45f)
        {
            Color32 packed = color;
            int key = packed.r | (packed.g << 8) | (packed.b << 16) | (packed.a << 24);
            key = (key * 397) ^ Mathf.RoundToInt(metallic * 1000f);
            key = (key * 397) ^ Mathf.RoundToInt(smoothness * 1000f);
            if (SharedMaterials.TryGetValue(key, out Material shared) && shared != null)
            {
                return shared;
            }

            Shader shader = Shader.Find("Standard");
            Material material = new Material(shader);
            material.name = "RuntimeShared_" + key;
            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            SharedMaterials[key] = material;
            return material;
        }
    }
}
