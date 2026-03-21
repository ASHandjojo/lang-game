using System;

using Unity.Collections;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.UIElements;

#nullable enable
public sealed class EditorUI : EditorWindow
{
    private const string KeyboardImportDir = "Assets/UI/Keyboard";
    private const string KeyboardTreeDir   = KeyboardImportDir + "/Keyboard.uxml";

    private const string EncodingImportDir = "Assets/Scripts/Encoding";
    // Ligature sub table also references standard table, kind of a shortcut :)
    private const string LigatureSubDir    = EncodingImportDir + "/Loader/Ligature Sub Table.asset";
    private const string WordEncoderDir    = EncodingImportDir + "/Loader/Internal Dictionary.asset";

    private const string EditorName = "Text Editor";

    private PhoneticProcessor processor;
    private WordEncoder       wordEncoder;

    private KeyboardUI keyboardUI;
    private Label      label;

    private SerializedProperty? phoneticsProp, unicodeProp;

    private EncodingEntry? responseData = null;

    /// <summary>
    /// Base function, does not enable editing towards a specific dialogue though.
    /// </summary>
    [MenuItem("Conlang/Text Editor")]
    public static void ShowWindow()
    {
        EditorUI baseWindow = GetWindow<EditorUI>(EditorName, true);
    }

    /// <summary>
    /// Meant to be called via DialogueEntry drawers.
    /// </summary>
    /// <param name="dialogueEntry"></param>
    [MenuItem("Conlang/Text Editor")]
    public static void ShowWindow(in EncodingEntry dialogueEntry, SerializedProperty phoneticsProp, SerializedProperty unicodeProp)
    {
        Debug.Assert(dialogueEntry != null);

        EditorUI baseWindow     = GetWindow<EditorUI>(EditorName, true);
        baseWindow.responseData = dialogueEntry;
        baseWindow.label!.text  = baseWindow.responseData!.line;

        baseWindow.phoneticsProp = phoneticsProp;
        baseWindow.unicodeProp   = unicodeProp;
        baseWindow.keyboardUI.PhoneticsString = baseWindow.responseData.phoneticsStr;
    }

    private void WriteToWindow(string input)
    {
        if (responseData == null)
        {
            return;
        }
        phoneticsProp!.stringValue = input;
        unicodeProp!.stringValue   = processor.Translate(input);

        Undo.RecordObject(phoneticsProp!.serializedObject.targetObject, "TextEdit");
        Undo.RecordObject(unicodeProp!.serializedObject.targetObject,   "TextEdit");

        phoneticsProp.serializedObject.ApplyModifiedProperties();
        unicodeProp.serializedObject.ApplyModifiedProperties();

        label.text = unicodeProp!.stringValue;
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        VisualTreeAsset treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(KeyboardTreeDir);
        Debug.Assert(treeAsset != null);

        LigatureSub ligatureSub = AssetDatabase.LoadAssetAtPath<LigatureSub>(LigatureSubDir);
        Debug.Assert(ligatureSub != null);
        InternalDictionary internalDict = AssetDatabase.LoadAssetAtPath<InternalDictionary>(WordEncoderDir);
        Debug.Assert(internalDict != null);
        if (!processor.IsValid)
        {
            processor   = new PhoneticProcessor(ligatureSub!.standardSignTable.entries, ligatureSub.entries, Allocator.Persistent);
            wordEncoder = WordEncoder.Create(internalDict!.entries.Convert(Allocator.Temp), Allocator.Persistent);
        }
        if (responseData != null)
        {
            keyboardUI = new KeyboardUI(treeAsset, processor, WriteToWindow, responseData.phoneticsStr);
        }
        else
        {
            keyboardUI = new KeyboardUI(treeAsset, processor, WriteToWindow);
        }
        root.Add(keyboardUI);

        label = root.Q<Label>("Input");
        Debug.Assert(label != null);
    }

    public void OnDestroy()
    {
        if (processor.IsValid)
        {
            processor.Dispose();
            wordEncoder.Dispose();
        }
    }
}
#nullable disable