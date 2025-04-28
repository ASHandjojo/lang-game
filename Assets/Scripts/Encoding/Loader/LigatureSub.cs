using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

[Serializable]
public struct CompoundSign
{
    public string combinedString;
    public int mappedChar;

    // Standard Characters
    public int[] mappedChars;
}

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
#if UNITY_EDITOR
    [SerializeField] private string fileDir;
    public string FileDir => fileDir;

    public VisualTreeAsset compoundUI;
    public VisualTreeAsset compoundChildUI;

    public StandardSignTable standardSignTable;
#endif
    [SerializeField, HideInInspector] public CompoundSign[] entries;
}

#if UNITY_EDITOR
public sealed class CompoundSignElement : VisualElement
{
    private readonly List<StandardSignElement> children = new();

    public CompoundSignElement(VisualTreeAsset compoundUI, VisualTreeAsset standardUI)
    {
        Debug.Assert(compoundUI != null);

        compoundUI.CloneTree(this);

        // The actual character input
        var characterList = this.Q<ListView>("CharacterList");
        // The mapping between the raw phonetics and the representative unicode character (for Manual)
        var rawMapping = this.Q<IntegerField>("UnicodeChar");
        // The visual preview of the unicode character (custom alphabet)
        var resultField = this.Q<TextField>("Result");

        rawMapping.isReadOnly     = true;
        resultField.isReadOnly    = true;

        characterList.itemsSource     = children;
        characterList.fixedItemHeight = 122.5f;
    }

    public void SetValue(in CompoundSign compoundSign, StandardSign[] standardSigns, VisualTreeAsset standardUI)
    {
        var characterList  = this.Q<ListView>("CharacterList");
        var rawMapping     = this.Q<IntegerField>("UnicodeChar");
        var resultField    = this.Q<TextField>("Result");

        rawMapping.value  = compoundSign.mappedChar;
        resultField.value = $"{(char) compoundSign.mappedChar}";

        characterList.makeItem = ()     => new StandardSignElement(standardUI);
        characterList.bindItem = (e, i) => (e as StandardSignElement).SetValue(standardSigns[i]);

        for (int i = 0; i < standardSigns.Length; i++)
        {
            StandardSignElement child = new(standardUI);
            child.SetValue(standardSigns[i]);

            children.Add(child);
        }

        characterList.RefreshItems();
    }
}

[CustomEditor(typeof(LigatureSub))]
public sealed class LigatureSubEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement element = new();

        LigatureSub table = target as LigatureSub;

        InspectorElement.FillDefaultInspector(element, serializedObject, this);
        serializedObject.Update();

        StandardSignTable standardTable = table.standardSignTable; // For conversion help

        Button submitButton = new(
            () =>
            {
                using FileStream stream = new(table.FileDir, FileMode.Open);

                LigatureSubJSON[][]? ligatureNested = JsonSerializer.Deserialize<LigatureSubJSON[][]>(stream);
                Debug.Assert(ligatureNested != null);

                int flatLength = ligatureNested.Select(arr => arr.Length).Sum();
                var ligatures  = ligatureNested.Select(x => new LigatureSubData(x)).ToArray();
                CompoundSign[] compoundSigns = new CompoundSign[flatLength];
                int flatIdx = 0;

                foreach (LigatureSubData ligatureCategory in ligatures)
                {
                    foreach (LigatureSubEntry ligature in ligatureCategory.entries)
                    {
                        // Compute Compound Sign
                        CompoundSign compoundSign = new();
                        compoundSign.mappedChar   = ligature.Glyph;

                        StandardSign[] standardSigns = ligature.Components
                            .Select(unicode => Array.FindIndex(standardTable.entries, 0, sign => sign.mappedChar == unicode))
                            .Where(index    => index != -1)
                            .Select(index   => standardTable.entries[index])
                            .ToArray();

                        compoundSign.combinedString = standardSigns.Select(sign => sign.phonetics).Aggregate(string.Empty, (x, y) => x + y);
                        compoundSign.mappedChars    = standardSigns.Select(sign => sign.mappedChar).ToArray();
                        compoundSigns[flatIdx++] = compoundSign;
                    }
                }

                table.entries = compoundSigns;

                EditorUtility.SetDirty(table);
                serializedObject.ApplyModifiedProperties();
            }
        );

        submitButton.AddToClassList("StandardFont");
        submitButton.text = "Update";
        element.Add(submitButton);

        Label label = new($"Length: {table.entries.Length}");
        label.AddToClassList("StandardFont");
        element.Add(label);

        for (int i = 0; i < table.entries.Length; i++)
        {
            // Add Compound Sign Vis
            CompoundSignElement compoundElement = new(table.compoundUI, table.compoundChildUI);
            StandardSign[] standardSigns = table.entries[i].mappedChars
                .Select(unicode => Array.FindIndex(standardTable.entries, 0, sign => sign.mappedChar == unicode))
                .Where(index    => index != -1)
                .Select(index   => standardTable.entries[index])
                .ToArray();

            compoundElement.SetValue(table.entries[i], standardSigns, table.compoundChildUI);
            element.Add(compoundElement);
        }

        return element;
    }
}
#endif