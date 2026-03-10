using System;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.UIElements;

using Serialization;

public sealed class CompoundSignElement : VisualElement
{
    private readonly List<StandardSignElement> children = new();

    public CompoundSignElement(VisualTreeAsset compoundUI, VisualTreeAsset standardUI)
    {
        Debug.Assert(standardUI != null);
        Debug.Assert(compoundUI != null);

        compoundUI.CloneTree(this);

        // The actual character input
        var characterList = this.Q<ListView>("CharacterList");
        // The mapping between the raw phonetics and the representative unicode character (for Manual)
        var rawMapping = this.Q<IntegerField>("UnicodeChar");
        // The visual preview of the unicode character (custom alphabet)
        var resultField = this.Q<TextField>("Result");

        rawMapping.isReadOnly  = true;
        resultField.isReadOnly = true;

        characterList.itemsSource     = children;
        characterList.fixedItemHeight = 150.0f;
    }

    public void SetValue(in CompoundSign compoundSign, StandardSign[] standardSigns, VisualTreeAsset standardUI)
    {
        var characterList = this.Q<ListView>("CharacterList");
        var rawMapping    = this.Q<IntegerField>("UnicodeChar");
        var resultField   = this.Q<TextField>("Result");

        rawMapping.value  = compoundSign.mappedChar;
        resultField.value = ((char) compoundSign.mappedChar).ToString();

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

                var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                LigatureSubGroup[] ligatures = JsonSerializer.Deserialize<LigatureSubGroup[]>(stream, options);
                Debug.Assert(ligatures != null);

                int flatLength = ligatures.Select(arr => arr.Ligatures.Length).Sum();
                CompoundSign[] compoundSigns = new CompoundSign[flatLength];
                int flatIdx = 0;

                StandardSign[] singleEntries = standardTable.entries.Where(x => x.phonetics.Length == 1).ToArray();
                foreach (LigatureSubGroup ligatureCategory in ligatures)
                {
                    ushort first = ligatureCategory.First;
                    foreach (LigatureSubEntry ligature in ligatureCategory.Ligatures)
                    {
                        // Compute Compound Sign
                        CompoundSign compoundSign = new()
                        {
                            mappedChar = ligature.Glyph
                        };

                        List<StandardSign> standardSigns = ligature.Components
                            .Select(unicode => Array.FindIndex(singleEntries, 0, sign => sign.mappedChar == unicode))
                            .Where(index    => index != -1)
                            .Select(index   => singleEntries[index])
                        .ToList();

                        compoundSign.combinedString = standardSigns.Select(sign => sign.phonetics).Aggregate(string.Empty, (x, y) => x + y);
                        compoundSign.mappedChars    = standardSigns.Select(sign => sign.mappedChar).ToArray();
                        compoundSigns[flatIdx++]    = compoundSign;
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