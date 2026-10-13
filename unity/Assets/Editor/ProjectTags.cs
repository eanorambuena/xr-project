using UnityEditor;
using UnityEngine;

/// <summary>
/// Asegura que existan los tags que usa el proyecto (Vehicle, Obstacle).
/// Se ejecuta automáticamente al cargar el Editor.
/// </summary>
[InitializeOnLoad]
public static class ProjectTags
{
    static readonly string[] RequiredTags = { "Vehicle", "Obstacle" };

    static ProjectTags()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        var tagManager = new SerializedObject(assets[0]);
        var tags = tagManager.FindProperty("tags");
        bool changed = false;
        foreach (var tag in RequiredTags)
        {
            bool exists = false;
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) { exists = true; break; }
            if (exists) continue;
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            changed = true;
            Debug.Log($"[ProjectTags] Tag agregado: {tag}");
        }
        if (changed) tagManager.ApplyModifiedProperties();
    }
}
