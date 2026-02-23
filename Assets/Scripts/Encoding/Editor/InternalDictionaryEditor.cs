using System;

using System.Collections.Generic;

using Unity.Collections;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.UIElements;

public sealed class DictEntryElement : VisualElement
{
    private TextField rawStringField, unicodeStrField;

    public DictEntryElement(VisualTreeAsset entryUI)
    {
        Debug.Assert(entryUI != null);
        entryUI.CloneTree(this);

        rawStringField  = this.Q<TextField>("RawString");
        unicodeStrField = this.Q<TextField>("TransString");

        Debug.Assert(rawStringField  != null);
        Debug.Assert(unicodeStrField != null);
        unicodeStrField.isReadOnly = true;
    }

    public void AssignCallback(LigatureSub ligatureSub)
    {
        rawStringField.RegisterCallback(
            (ChangeEvent<string> e) =>
            {
                Processor processor   = new(ligatureSub.standardSignTable.entries, ligatureSub.entries, Allocator.Temp);
                unicodeStrField.value = processor.Translate(e.newValue);
            }
        );
    }
}

[CustomPropertyDrawer(typeof(DictEntry))]
internal sealed class DictEntryDrawer : PropertyDrawer
{
    private const string RootImportDir  = "Assets/Scripts/Encoding";
    private const string DictEntryUIDir = RootImportDir + "/UI/WordUI.uxml";

    private const string LigatureSubDir = RootImportDir + "/Loader/Ligature Sub Table.asset";

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualTreeAsset treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DictEntryUIDir);
        Debug.Assert(treeAsset != null);
        DictEntryElement element = new(treeAsset);

        LigatureSub ligatureSub = AssetDatabase.LoadAssetAtPath<LigatureSub>(LigatureSubDir);
        Debug.Assert(ligatureSub != null);

        SerializedProperty rawStrProp = property.FindPropertyRelative(nameof(DictEntry.rawString));
        element.Q<TextField>("RawString").BindProperty(rawStrProp);

        SerializedProperty convStrProp = property.FindPropertyRelative(nameof(DictEntry.unicodeString));
        element.Q<TextField>("TransString").BindProperty(convStrProp);

        SerializedProperty englishStrProp = property.FindPropertyRelative(nameof(DictEntry.englishTranslation));
        element.Q<TextField>("EnglishString").BindProperty(englishStrProp);

        SerializedProperty wordTypeProp = property.FindPropertyRelative(nameof(DictEntry.wordType));
        element.Q<EnumField>("WordType").BindProperty(wordTypeProp);

        element.AssignCallback(ligatureSub);
        
        return element;
    }
}

[CustomEditor(typeof(InternalDictionary))]
internal sealed class InternalDictEditor : Editor
{
    private static readonly Dictionary<string, WordType> WordTypeDict = new()
    {
        { nameof(WordType.Subject), WordType.Subject },
        { nameof(WordType.Object),  WordType.Object  },
        { nameof(WordType.Verb),    WordType.Verb    },
    };

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement element = new();

        InternalDictionary dict = target as InternalDictionary;
        serializedObject.Update();

        SerializedProperty arrayProp = serializedObject.FindProperty(nameof(InternalDictionary.entries));
        PropertyField arrayField     = new(arrayProp);
        element.Add(arrayField);

        // CSV Importing
        TextField importField = new("Import CSV")
        {
            name      = "ImportField",
            multiline = true
        };
        element.Add(importField);

        Button buttonImport = new()
        {
            name = "ImportButton",
            text = "Import"
        };
        buttonImport.clicked += () =>
        {
            // Current Structure: [Phonetics, English Translation, Word Type {as str}]
            const int ExpectedArgCount = 3;

            string importText = importField.text;
            string[] lines    = importText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0)
            {
                foreach (string line in lines)
                {
                    string[] args = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    Debug.Assert(args.Length == ExpectedArgCount, $"Invalid number of arguments (expected: {ExpectedArgCount})! String: {line}, Arg Count: {args.Length}");
                    DictEntry entry = new()
                    {
                        rawString          = args[0],
                        englishTranslation = args[1],
                        wordType           = WordTypeDict[args[2]]
                    };
                    dict.entries.Add(entry);
                }
                EditorUtility.SetDirty(dict);
                serializedObject.ApplyModifiedProperties();
            }
        };
        element.Add(buttonImport);

        return element;
    }
}