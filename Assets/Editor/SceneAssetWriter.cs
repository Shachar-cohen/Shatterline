using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneAssetWriter
{
    private const string ArtRoot = "Assets/Art/Generated";
    public static void PersistAssets(Scene scene)
    {
        Directory.CreateDirectory("Assets/Art/Models");
        Directory.CreateDirectory(ArtRoot + "/Materials"); Directory.CreateDirectory(ArtRoot + "/Sprites");
        AssetDatabase.Refresh();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                filter.sharedMesh = SaveMesh(filter.sharedMesh);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = SaveMaterial(materials[i]);
                renderer.sharedMaterials = materials;
            }
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.sprite = SaveSprite(renderer.sprite);
            // Private serialized Runner sprites are not all on a SpriteRenderer while idle.
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) throw new InvalidOperationException("Missing script in authored scene.");
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (property.objectReferenceValue is Sprite)
                        property.objectReferenceValue = SaveSprite((Sprite)property.objectReferenceValue);
                    else if (property.objectReferenceValue is Material)
                        property.objectReferenceValue = SaveMaterial((Material)property.objectReferenceValue);
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        AssetDatabase.SaveAssets();
    }
    private static Mesh SaveMesh(Mesh value)
    {
        if (value == null) return null;
        string source = AssetDatabase.GetAssetPath(value);
        if (source.StartsWith("Assets/")) return value;
        string path = "Assets/Art/Models/M_" + value.name.Replace(" ", "_") + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved != null) return saved;
        saved = UnityEngine.Object.Instantiate(value); saved.name = value.name;
        AssetDatabase.CreateAsset(saved, path); return saved;
    }
    public static void SaveVisualVariants(Transform core, string name)
    {
        string directory = "Assets/Prefabs/Models/"; Directory.CreateDirectory(directory);
        var model = UnityEngine.Object.Instantiate(core.gameObject);
        model.name = name; model.transform.SetParent(null); model.transform.position = Vector3.zero;
        foreach (var sprite in model.GetComponentsInChildren<SpriteRenderer>(true))
            UnityEngine.Object.DestroyImmediate(sprite.gameObject);
        foreach (var mesh in model.GetComponentsInChildren<MeshRenderer>(true)) mesh.enabled = true;
        AssetDatabase.Refresh();
        var prefab = PrefabUtility.SaveAsPrefabAsset(model, directory + "PB_" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(model);
        var variant = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        variant.name = name + " / blue armor";
        foreach (var mesh in variant.GetComponentsInChildren<MeshRenderer>(true))
        {
            var material = new Material(mesh.sharedMaterial);
            material.color = Color.Lerp(material.color, new Color(.15f, .45f, .9f), .45f);
            mesh.sharedMaterial = SaveMaterial(material);
        }
        PrefabUtility.SaveAsPrefabAsset(variant, directory + "PB_" + name + "_Blue.prefab");
        UnityEngine.Object.DestroyImmediate(variant);
    }
    private static string Hash(string text)
    {
        using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").Substring(0, 16);
    }
    private static Material SaveMaterial(Material value)
    {
        if (value == null || AssetDatabase.Contains(value)) return value;
        string signature = value.shader.name + value.color.ToString("F4") + AssetDatabase.GetAssetPath(value.mainTexture) +
            value.renderQueue + string.Join(",", value.shaderKeywords);
        if (value.HasProperty("_Glossiness")) signature += value.GetFloat("_Glossiness").ToString("F4");
        if (value.HasProperty("_Metallic")) signature += value.GetFloat("_Metallic").ToString("F4");
        if (value.HasProperty("_EmissionColor")) signature += value.GetColor("_EmissionColor").ToString("F4");
        string path = ArtRoot + "/Materials/M_" + Hash(signature) + ".mat";
        var saved = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (saved != null) return saved;
        value.name = Path.GetFileNameWithoutExtension(path); AssetDatabase.CreateAsset(value, path); return value;
    }
    private static Sprite SaveSprite(Sprite value)
    {
        if (value == null || AssetDatabase.Contains(value)) return value;
        string signature = AssetDatabase.GetAssetPath(value.texture) + value.rect + value.pivot + value.pixelsPerUnit;
        string path = ArtRoot + "/Sprites/SPR_" + Hash(signature) + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (saved != null) return saved;
        value.name = Path.GetFileNameWithoutExtension(path); AssetDatabase.CreateAsset(value, path); return value;
    }
    public static GameObject SavePrefab(GameObject instance, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)); AssetDatabase.Refresh();
        var restore = new List<Action>();
        foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            var serialized = new SerializedObject(behaviour);
            var property = serialized.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.propertyPath.StartsWith("m_")) continue;
                var component = property.objectReferenceValue as Component;
                var gameObject = property.objectReferenceValue as GameObject;
                Transform target = component != null ? component.transform : gameObject != null ? gameObject.transform : null;
                if (target == null || target == instance.transform || target.IsChildOf(instance.transform)) continue;
                UnityEngine.Object reference = property.objectReferenceValue;
                string propertyPath = property.propertyPath;
                var owner = behaviour;
                restore.Add(() => {
                    var saved = new SerializedObject(owner);
                    saved.FindProperty(propertyPath).objectReferenceValue = reference;
                    saved.ApplyModifiedPropertiesWithoutUndo();
                });
                property.objectReferenceValue = null;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(instance, path, InteractionMode.AutomatedAction);
        foreach (var action in restore) action();
        return prefab;
    }
}
