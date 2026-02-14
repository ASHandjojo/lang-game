using System;
using Unity.Collections;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.UIElements;

public sealed class DictEntryElement : VisualElement
{
    public DictEntryElement(VisualTreeAsset standardUI)
    {
        Debug.Assert(standardUI != null);

        standardUI.CloneTree(this);

        var rawStringField  = this.Q<TextField>("RawString");
        var unicodeStrField = this.Q<TextField>("TransString");

        Debug.Assert(rawStringField  != null);
        Debug.Assert(unicodeStrField != null);

        unicodeStrField.isReadOnly = true;
    }

    public void AssignCallback()
    {
        this.processor = processor;

        var rawStringField  = this.Q<TextField>("RawString");
        var unicodeStrField = this.Q<TextField>("TransString");

        rawStringField.RegisterCallback(
            (ChangeEvent<string> e) =>
            {
                unicodeStrField.value = this.processor.Translate(e.newValue);
            }
        );
    }
}

[CustomPropertyDrawer(typeof(DictEntry))]
internal sealed class DictEntryDrawer : PropertyDrawer
{
    private const string RootImportDir  = "Assets/Scripts/Encoding";
    private const string DictEntryUIDir = RootImportDir + "/UI/WordUI.uxml";

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualTreeAsset treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DictEntryUIDir);
        Debug.Assert(treeAsset != null);
        DictEntryElement element = new(treeAsset);

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

        InspectorElement.FillDefaultInspector(element, serializedObject, this);
        serializedObject.Update();

        for (int i = 0; i < dict.entries.Length; i++)
        {
            dict.entries[i].unicodeString = "";
        }

        return element;
    }
}