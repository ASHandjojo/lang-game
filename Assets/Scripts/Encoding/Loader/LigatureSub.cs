using System;
using System.IO;
using System.Linq;
using System.Text.Json;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public sealed class LigatureSubJSON
{
    public ushort Glyph { get; set; }
    public ushort[] Components { get; set; }
}

[Serializable]
public sealed class LigatureSubEntry
{
    public ushort Glyph;
    public ushort[] Components;

    public LigatureSubEntry(LigatureSubJSON json)
    {
        Glyph      = json.Glyph;
        Components = json.Components;
    }
    public override string ToString()
    {
        string output = $"Glyph: {Glyph} ";
        foreach (ushort component in Components)
        {
            output += $"{component},";
        }
        return output;
    }
}

[Serializable]
public sealed class LigatureSubData
{
    public LigatureSubEntry[] entries;

    public LigatureSubData(LigatureSubJSON[] json)
    {
        entries = json.Select(y => new LigatureSubEntry(y)).ToArray();
    }

    public override string ToString()
    {
        string output = "";
        foreach (LigatureSubEntry entry in entries)
        {
            output += $"{entry}\n";
        }
        return output;
    }
}

[CreateAssetMenu(menuName = "Linguistics/Ligature Table")]
public sealed class LigatureSub : ScriptableObject
{
    [SerializeField] private string fileDir;
    [SerializeField, HideInInspector] public LigatureSubData[] entries;

    public string FileDir => fileDir;
}

#if UNITY_EDITOR
[CustomEditor(typeof(LigatureSub))]
public sealed class LigatureSubEditor : Editor
{
    public override void OnInspectorGUI()
    {
        LigatureSub table = target as LigatureSub;

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        serializedObject.Update();
        bool hasChanges = EditorGUI.EndChangeCheck();

        if (hasChanges)
        {
            using FileStream stream = new(table.FileDir, FileMode.Open);

            LigatureSubJSON[][]? ligatureNested = JsonSerializer.Deserialize<LigatureSubJSON[][]>(stream);
            Debug.Assert(ligatureNested != null);

            var ligatures = ligatureNested.Select(x => new LigatureSubData(x)).ToArray();
            table.entries = ligatures;

            EditorUtility.SetDirty(table);
            serializedObject.ApplyModifiedProperties();
        }

        GUILayout.Label($"Length: {table.entries.Length}", EditorStyles.boldLabel);
    }
}
#endif