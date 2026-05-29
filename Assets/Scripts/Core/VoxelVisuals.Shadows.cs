using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Labyrinth.Core
{
    public static partial class VoxelVisuals
    {
        private static readonly Dictionary<int, Material> ContactShadowMaterials = new Dictionary<int, Material>();

        public static GameObject CreateContactShadow(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            float alpha = 0.38f)
        {
            var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = objectName;
            shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = localPosition;
            shadow.transform.localRotation = Quaternion.identity;
            shadow.transform.localScale = localScale;

            var renderer = shadow.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetContactShadowMaterial(alpha);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var collider = shadow.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            return shadow;
        }

        private static Material GetContactShadowMaterial(float alpha)
        {
            var normalizedAlpha = Mathf.Clamp(alpha, 0f, 0.28f);
            var key = Mathf.RoundToInt(normalizedAlpha * 255f);
            if (ContactShadowMaterials.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var color = new Color(0f, 0f, 0f, normalizedAlpha);
            var material = new Material(shader)
            {
                name = $"Voxel Contact Shadow {key}",
                color = color
            };
            ApplyMaterialProfile(material, color);
            material.renderQueue = 2990;
            ContactShadowMaterials[key] = material;
            return material;
        }

        private static void TryAddProjectedGroundShadow(GameObject target, Material material, bool figurePart)
        {
            if (target == null
                || material == null
                || figurePart
                || IsTransparent(material)
                || !ShouldAddProjectedGroundShadow(target.name))
            {
                return;
            }

            var parent = target.transform.parent;
            if (parent == null || parent.Find($"{target.name} Projected Shadow") != null)
            {
                return;
            }

            var size = target.transform.lossyScale;
            var width = Mathf.Abs(size.x);
            var height = Mathf.Abs(size.y);
            var depth = Mathf.Abs(size.z);
            if (height < 0.08f || Mathf.Max(width, depth) < 0.12f)
            {
                return;
            }

            var footprint = Mathf.Max(width, depth);
            var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = $"{target.name} Projected Shadow";
            shadow.transform.SetParent(parent, true);
            var shadowPosition = target.transform.position + new Vector3(-height * 0.34f, 0f, -height * 0.18f);
            shadowPosition.y = parent.position.y + 0.01f;
            shadow.transform.position = shadowPosition;
            shadow.transform.rotation = Quaternion.Euler(0f, 28f, 0f);
            shadow.transform.localScale = new Vector3(
                Mathf.Max(0.08f, width * 0.78f + height * 0.2f),
                0.0035f,
                Mathf.Max(0.08f, depth * 0.62f + height * 0.14f));

            var renderer = shadow.GetComponent<Renderer>();
            if (renderer != null)
            {
                var alpha = Mathf.Clamp(0.08f + footprint * 0.03f + height * 0.018f, 0.1f, 0.2f);
                renderer.sharedMaterial = GetContactShadowMaterial(alpha);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var collider = shadow.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }

        private static bool ShouldAddProjectedGroundShadow(string objectName)
        {
            return !Contains(objectName, "Voxel")
                && !Contains(objectName, "Shadow")
                && !Contains(objectName, "Light")
                && !Contains(objectName, "Glow")
                && !Contains(objectName, "Fire")
                && !Contains(objectName, "Yard")
                && !Contains(objectName, "Road")
                && !Contains(objectName, "Bridge")
                && !Contains(objectName, "Crop")
                && !Contains(objectName, "Field")
                && !Contains(objectName, "Cell")
                && !Contains(objectName, "Marker");
        }
    }
}
