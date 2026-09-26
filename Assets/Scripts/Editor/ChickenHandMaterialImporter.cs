using System;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

// These FBXs store Maya Standard Surface properties, rather than the
// DiffuseColor/Opacity fields read by URP's generic FBX material importer.
public sealed class ChickenHandMaterialImporter : AssetPostprocessor
{
    public override uint GetVersion() => 2;
    public override int GetPostprocessOrder() => 0; // After URP's -980/-960 importers.

    private void OnPreprocessMaterialDescription(
        MaterialDescription description, Material material, AnimationClip[] clips)
    {
        bool chickenHand = assetPath.StartsWith("Assets/Graphics/Characters/Chicken/", StringComparison.Ordinal)
            && material.name == "hand1";
        bool ankylo = assetPath == "Assets/Graphics/ankyloRock.fbx"
            || assetPath == "Assets/Graphics/ankyloPaper.fbx"
            || assetPath == "Assets/Graphics/ankyloScissor.fbx";
        if ((!chickenHand && !ankylo)
            || !string.Equals(Path.GetExtension(assetPath), ".fbx", StringComparison.OrdinalIgnoreCase))
            return;

        if (!ReadVector(description, "baseColor", out var sourceColor)
            || !ReadVector(description, "opacity", out var sourceOpacity)
            || !ReadFloat(description, "base", out var baseWeight)
            || !ReadFloat(description, "metalness", out var metalness)
            || !ReadFloat(description, "specularRoughness", out var roughness)
            || !ReadFloat(description, "emission", out var emission))
            return;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return;
        material.shader = shader;

        // Match URP's own FBX/Lit conversion of source linear material colors.
        float opacity = Mathf.Min(sourceOpacity.x, Mathf.Min(sourceOpacity.y, sourceOpacity.z));
        var color = new Color(sourceColor.x * baseWeight, sourceColor.y * baseWeight,
            sourceColor.z * baseWeight, opacity);
        if (PlayerSettings.colorSpace == ColorSpace.Linear) color = color.gamma;
        color.a = opacity;
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        ApplyTexture(description, "baseColor", material, "_BaseMap");
        material.SetTexture("_MainTex", material.GetTexture("_BaseMap"));

        material.SetFloat("_WorkflowMode", 1f);
        material.SetFloat("_Metallic", metalness);
        material.SetFloat("_Smoothness", 1f - roughness);
        material.SetTexture("_MetallicGlossMap", null);
        material.DisableKeyword("_METALLICSPECGLOSSMAP");
        material.DisableKeyword("_SPECULAR_SETUP");

        ApplyTexture(description, "normalCamera", material, "_BumpMap");
        if (material.GetTexture("_BumpMap") != null) material.EnableKeyword("_NORMALMAP");
        else material.DisableKeyword("_NORMALMAP");

        Color emissionColor = Color.black;
        if (emission > 0f && ReadVector(description, "emissionColor", out var sourceEmission))
            emissionColor = new Color(sourceEmission.x, sourceEmission.y, sourceEmission.z) * emission;
        material.SetColor("_EmissionColor", emissionColor);
        ApplyTexture(description, "emissionColor", material, "_EmissionMap");
        if (emission > 0f) material.EnableKeyword("_EMISSION");
        else material.DisableKeyword("_EMISSION");

        // A missing generic Opacity property made URP's fallback transparent;
        // use the FBX's actual opacity instead.
        bool transparent = opacity < 1f;
        material.SetFloat("_Surface", transparent ? 1f : 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_SrcBlend", (float)(transparent ? BlendMode.SrcAlpha : BlendMode.One));
        material.SetFloat("_DstBlend", (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
        material.SetFloat("_ZWrite", transparent ? 0f : 1f);
        material.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
        material.renderQueue = transparent ? (int)RenderQueue.Transparent : -1;
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        if (transparent) material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        else material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }

    private static bool ReadFloat(MaterialDescription description, string name, out float value)
        => description.TryGetProperty(name, out value)
            || description.TryGetProperty("Maya|" + name, out value);

    private static bool ReadVector(MaterialDescription description, string name, out Vector4 value)
        => description.TryGetProperty(name, out value)
            || description.TryGetProperty("Maya|" + name, out value);

    private static void ApplyTexture(MaterialDescription description, string source,
        Material material, string target)
    {
        if (description.TryGetProperty(source, out TexturePropertyDescription texture)
            || description.TryGetProperty("Maya|" + source, out texture))
        {
            material.SetTexture(target, texture.texture);
            material.SetTextureScale(target, texture.scale);
            material.SetTextureOffset(target, texture.offset);
        }
        else material.SetTexture(target, null);
    }
}
