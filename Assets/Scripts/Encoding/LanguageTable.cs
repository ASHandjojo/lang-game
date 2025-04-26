using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

[Serializable]
public struct StandardSign : IEquatable<StandardSign>
{
    public string phonetics;
    public int mappedChar;

    public readonly bool Equals(StandardSign other) => phonetics.Equals(other.phonetics);
}

[Serializable]
public struct CompoundSign
{
    public string phonetics, rawCharInput, combinedString;
    public int combinationType, mappedChar;

    // Standard Characters
    public int[] mappedChars;
}

[DisallowMultipleComponent]
public sealed class LanguageTable : MonoBehaviour
{
#if UNITY_EDITOR
    public VisualTreeAsset standardUI;
    public VisualTreeAsset compoundUI;
    public VisualTreeAsset compoundChildUI;
#endif

    public StandardSign[] standardSigns;
    public CompoundSign[] compoundSigns;
}

#if UNITY_EDITOR

public sealed class StandardSignElement : VisualElement
{
    public StandardSignElement(VisualTreeAsset standardUI)
    {
        Debug.Assert(standardUI != null);

        standardUI.CloneTree(this);

        var phoneticsField = this.Q<TextField>("Phonetics");
        var resultField    = this.Q<TextField>("Result");

        phoneticsField.isReadOnly = true;
        resultField.isReadOnly    = true;
    }

    public void SetValue(in StandardSign standardSign)
    {
        var phoneticsField = this.Q<TextField>("Phonetics");
        var resultField    = this.Q<TextField>("Result");

        phoneticsField.value = standardSign.phonetics;
        resultField.value    = $"{(char) standardSign.mappedChar}";
    }
}

[CustomPropertyDrawer(typeof(StandardSign))]
public sealed class StandardSignDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement visualElement = new();

        var phoneticsProp = property.FindPropertyRelative(nameof(StandardSign.phonetics));
        var mappedProp    = property.FindPropertyRelative(nameof(StandardSign.mappedChar));

        LanguageTable table       = property.serializedObject.targetObject as LanguageTable;
        VisualTreeAsset treeAsset = table.standardUI;
        property.serializedObject.Update(); 

        treeAsset.CloneTree(visualElement);

        var phoneticsField = visualElement.Q<TextField>("Phonetics");
        var rawMapping     = visualElement.Q<IntegerField>("UnicodeChar");
        var resultField    = visualElement.Q<TextField>("Result");

        rawMapping.RegisterCallback<ChangeEvent<int>>(
            (e) =>
            {
                resultField.value = $"{(char) mappedProp.intValue}";
            }
        );

        phoneticsField.BindProperty(phoneticsProp);
        rawMapping.BindProperty(mappedProp);

        return visualElement;
    }
}

public enum CombiningOptions
{
    Automatic,
    Manual
}

[CustomPropertyDrawer(typeof(CompoundSign))]
public sealed class CompoundSignDrawer : PropertyDrawer
{
    private List<StandardSignElement> children = new();
    private List<StandardSign> signs = new();

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement visualElement = new();

        var phoneticsProp = property.FindPropertyRelative(nameof(CompoundSign.phonetics));
        var rawCharProp   = property.FindPropertyRelative(nameof(CompoundSign.rawCharInput));

        var mappedProp    = property.FindPropertyRelative(nameof(CompoundSign.mappedChar));

        var combiningProp = property.FindPropertyRelative(nameof(CompoundSign.combinationType));

        // Specifically not for the editor but rather as a way to easily access this information for later
        var combinedProp = property.FindPropertyRelative(nameof(CompoundSign.combinedString));

        var mappedCharsProp = property.FindPropertyRelative(nameof(CompoundSign.mappedChars));

        LanguageTable table       = property.serializedObject.targetObject as LanguageTable;
        VisualTreeAsset treeAsset = table.compoundUI;

        treeAsset.CloneTree(visualElement);

        // Phonetic pronounciation
        var phoneticsField = visualElement.Q<TextField>("Phonetics");
        // The actual character input
        var charListField  = visualElement.Q<TextField>("Characters");
        // The listed conversion of character input
        var characterList  = visualElement.Q<ListView>("CharacterList");
        // Options for character mapping
        var compoundOpts   = visualElement.Q<RadioButtonGroup>("CombinationOpts");
        // The mapping between the raw phonetics and the representative unicode character (for Manual)
        var rawMapping     = visualElement.Q<IntegerField>("UnicodeChar");
        // The visual preview of the unicode character (custom alphabet)
        var resultField    = visualElement.Q<TextField>("Result");

        // Initializing Character List
        characterList.itemsSource = children;
        characterList.makeItem    = ()     => new StandardSignElement(table.compoundChildUI);
        characterList.bindItem    = (e, i) => (e as StandardSignElement).SetValue(signs[i]);
        characterList.fixedItemHeight = 82.5f;

        compoundOpts.choices = new string[] { nameof(CombiningOptions.Automatic), nameof(CombiningOptions.Manual) };

        rawMapping.RegisterCallback<ChangeEvent<int>>(
            (e) =>
            {
                resultField.value = $"{(char) mappedProp.intValue}";
            }
        );

        phoneticsField.BindProperty(phoneticsProp);
        rawMapping.BindProperty(mappedProp);
        charListField.BindProperty(rawCharProp);
        compoundOpts.BindProperty(combiningProp);

        compoundOpts.RegisterCallback<ChangeEvent<int>>(
            (e) =>
            {
                if (e.newValue == (int) CombiningOptions.Automatic)
                {
                    rawMapping.isReadOnly = true;
                    int combination = 0;
                    for (int i = 0; i < signs.Count; i++)
                    {
                        combination += signs[i].mappedChar;
                    }

                    rawMapping.value  = combination;
                    resultField.value = $"{(char) combination}";
                }
                else
                {
                    rawMapping.isReadOnly = false;
                }
            }
        );

        charListField.RegisterCallback<ChangeEvent<string>>(
            (e) =>
            {
                // Parses comma separated list
                string[] chars = e.newValue.Split('_', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToArray();

                StandardSign[] standardSigns = chars
                    .Select(phonetic => Array.FindIndex(table.standardSigns, 0, sign => sign.phonetics.Equals(phonetic)))
                    .Where(index => index != -1)
                    .Select(index => table.standardSigns[index])
                    .ToArray();

                signs.Clear();
                signs.AddRange(standardSigns);

                children.Clear();

                string combinedString     = "";
                mappedCharsProp.arraySize = signs.Count;
                for (int i = 0; i < signs.Count; i++)
                {
                    StandardSign sign = signs[i];
                    children.Add(new StandardSignElement(table.compoundChildUI));
                    children[^1].SetValue(sign);

                    SerializedProperty property = mappedCharsProp.GetArrayElementAtIndex(i);
                    property.intValue = sign.mappedChar;

                    combinedString += sign.phonetics;
                }

                combinedProp.stringValue  = combinedString;


                characterList.RefreshItems();
                // Reset
                if (compoundOpts.value == (int) CombiningOptions.Automatic)
                {
                    int combination = 0;
                    for (int i = 0; i < signs.Count; i++)
                    {
                        combination += signs[i].mappedChar;
                    }

                    rawMapping.value  = combination;
                    resultField.value = $"{(char) combination}";
                }

            }
        );

        return visualElement;
    }
}
#endif