using Unity.Collections;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.UIElements;

using static DialogueEntry;

#nullable enable
public sealed class EditorUI : EditorWindow
{
    private const string KeyboardImportDir = "Assets/UI/Keyboard";
    private const string KeyboardTreeDir   = KeyboardImportDir + "/Keyboard.uxml";

    private const string EncodingImportDir = "Assets/Scripts/Encoding";
    // Ligature sub table also references standard table, kind of a shortcut :)
    private const string LigatureSubDir    = EncodingImportDir + "/Loader/Ligature Sub Table.asset";
    private const string InternalDictDir   = EncodingImportDir + "/Loader/Internal Dictionary.asset";

    private const string EditorName = "Text Editor";

    private PhoneticProcessor processor;
    private KeyboardUI keyboardUI;
    private Label      label;

    private PropertyField? expectedLabel;

    private ResponseData? responseData = null;

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
    public static void ShowWindow(in DialogueEntry dialogueEntry, PropertyField expectedLabel)
    {
        Debug.Assert(expectedLabel != null);
        Debug.Assert(dialogueEntry.hasResponse && dialogueEntry.responseData != null);

        EditorUI baseWindow     = GetWindow<EditorUI>(EditorName, true);
        baseWindow.responseData = dialogueEntry.responseData;
        baseWindow.label!.text  = baseWindow.responseData!.expectedInput;

        baseWindow.expectedLabel = expectedLabel;
    }

    private void WriteToWindow(string input)
    {
        label.text = input;
        if (responseData == null)
        {
            return;
        }
        expectedLabel!.label = input;
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        VisualTreeAsset treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(KeyboardTreeDir);
        Debug.Assert(treeAsset != null);

        LigatureSub ligatureSub = AssetDatabase.LoadAssetAtPath<LigatureSub>(LigatureSubDir);
        Debug.Assert(ligatureSub != null);

        if (!processor.IsValid)
        {
            processor = new PhoneticProcessor(ligatureSub!.standardSignTable.entries, ligatureSub.entries, Allocator.Persistent);
        }
        if (responseData != null)
        {
            keyboardUI = new KeyboardUI(treeAsset, processor, WriteToWindow, responseData.expectedInput);
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
        }
    }
}
#nullable disable