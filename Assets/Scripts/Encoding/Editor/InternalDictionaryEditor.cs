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

public sealed class InternalDictElement : VisualElement
{
    private readonly List<DictEntryElement> entries = new();

    private readonly ListView listUI;

    public InternalDictElement()
    {
        listUI = new ListView()
        {
            name            = "EntryList",
            itemsSource     = entries,
            fixedItemHeight = 100.0f
        };
        listUI.allowAdd = true;
        listUI.showAddRemoveFooter = true;
        Add(listUI);
    }

    public void SetValues(VisualTreeAsset entryUIAsset, LigatureSub ligatureSub, List<DictEntry> rawEntries, SerializedProperty arrayProp)
    {
        Debug.Assert(entryUIAsset != null);
        Debug.Assert(entries      != null);

        listUI.makeItem = () =>
        {
            DictEntryElement value = new(entryUIAsset);
            value.AssignCallback(ligatureSub);
            return value;
        };
        listUI.bindItem = (e, i) => (e as DictEntryElement).SetValue(rawEntries[i].rawString);
        listUI.onAdd = (l) =>
        {
            l.itemsSource.Add(new DictEntryElement(entryUIAsset));
            rawEntries.Add(new DictEntry());

            SerializedProperty elemProp  = arrayProp.GetArrayElementAtIndex(rawEntries.Count);
            PropertyField propertyField  = new(elemProp);
            propertyField.BindProperty(elemProp);

            listUI.RefreshItems();
        };
        listUI.onRemove = (l) =>
        {
            rawEntries.RemoveAt(l.itemsSource.Count    - 1);
            l.itemsSource.RemoveAt(l.itemsSource.Count - 1);

            SerializedProperty elemProp = arrayProp.GetArrayElementAtIndex(rawEntries.Count);
            PropertyField propertyField = new(elemProp);
            propertyField.BindProperty(elemProp);

            listUI.RefreshItems();
        };

        for (int i = 0; i < rawEntries.Count; i++)
        {
            DictEntryElement child = new(entryUIAsset);
            child.SetValue(rawEntries[i].rawString);
            entries.Add(child);
        }

        listUI.RefreshItems();
    }
}

[CustomEditor(typeof(InternalDictionary))]
internal sealed class InternalDictEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        InternalDictElement element = new();

        InternalDictionary dict = target as InternalDictionary;
        serializedObject.Update();

        SerializedProperty uiTreeAssetProp = serializedObject.FindProperty(nameof(InternalDictionary.entryUIAsset));
        PropertyField uiTreeAssetField     = new(uiTreeAssetProp);
        uiTreeAssetField.BindProperty(uiTreeAssetProp);
        element.Add(uiTreeAssetField);

        SerializedProperty ligatureSubProp = serializedObject.FindProperty(nameof(InternalDictionary.ligatureSub));
        PropertyField ligatureSubField     = new(ligatureSubProp);
        ligatureSubField.BindProperty(ligatureSubProp);
        element.Add(ligatureSubField);

        element.SetValues(dict.entryUIAsset, dict.ligatureSub, dict.entries, serializedObject.FindProperty(nameof(InternalDictionary.entries)));

        EditorUtility.SetDirty(dict);
        serializedObject.ApplyModifiedProperties();

        return element;
    }
}