using System;
using System.Buffers;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

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

    public void SetValue(string rawStr)
    {
        rawStringField.value = rawStr;
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

        element.AssignCallback(ligatureSub);
        
        return element;
    }
}

[CustomEditor(typeof(InternalDictionary))]
internal sealed class InternalDictEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement element = new();

        InternalDictionary dict = target as InternalDictionary;
        serializedObject.Update();

        SerializedProperty uiTreeAssetProp = serializedObject.FindProperty(nameof(InternalDictionary.entryUIAsset));
        PropertyField uiTreeAssetField     = new(uiTreeAssetProp);
        element.Add(uiTreeAssetField);

        SerializedProperty ligatureSubProp = serializedObject.FindProperty(nameof(InternalDictionary.ligatureSub));
        PropertyField ligatureSubField     = new(ligatureSubProp);
        element.Add(ligatureSubField);

        SerializedProperty arrayProp = serializedObject.FindProperty(nameof(InternalDictionary.entries));
        PropertyField arrayField = new(arrayProp);
        element.Add(arrayField);

        return element;
    }
}