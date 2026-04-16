using System;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;

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
    private Label      unicodeLabel, englishLabel, wordTypeLabel;

    private TextField phoneticField;

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
    public static unsafe void ShowWindow(in EncodingEntry dialogueEntry, SerializedProperty phoneticsProp, SerializedProperty unicodeProp)
    {
        Debug.Assert(dialogueEntry != null);
        // Linking dialogue entry being edited.
        EditorUI baseWindow     = GetWindow<EditorUI>(EditorName, true);
        baseWindow.responseData = dialogueEntry;
        // Load from stored to UI (Unicode)

        baseWindow.phoneticsProp              = phoneticsProp;
        baseWindow.unicodeProp                = unicodeProp;
        baseWindow.keyboardUI.PhoneticsString = dialogueEntry!.phoneticsStr;

        var mixedRes = baseWindow.wordEncoder.ParseMixed(baseWindow.keyboardUI.PhoneticsString.AsSpan().ConvertU16(), baseWindow.processor, Allocator.Temp);

        ReadOnlySpan<char> displayRes  = new(mixedRes.displayOutput.GetUnsafeReadOnlyPtr(), mixedRes.displayOutput.Length);
        baseWindow.unicodeLabel!.text  = new string(displayRes);
        baseWindow.englishLabel!.text  = baseWindow.GetEnglishString(mixedRes.words);
        baseWindow.wordTypeLabel!.text = baseWindow.GetWordTypeString(mixedRes.words);

        baseWindow.phoneticField!.SetValueWithoutNotify(phoneticsProp!.stringValue);
    }

    private string GetEnglishString(in NativeArray<WordNode> words)
    {
        string englishOutput = string.Empty; // Accumulate Output
        foreach (WordNode word in words)
        {
            string result = wordEncoder.TryGetEnglish(word, out var englishStr) ?
                englishStr.ConvertChar().ToString() :
                WordType.Unknown.ToString();
            englishOutput += $" {result.Trim(' ', '\r', '\n')}";
        }
        return englishOutput;
    }

    private string GetWordTypeString(in NativeArray<WordNode> words)
    {
        string typeOutput = string.Empty; // Accumulate Output
        foreach (WordNode word in words)
        {
            typeOutput += $" {word.WordType.ToString()}";
        }
        return typeOutput;
    }

    private unsafe void MetaUpdate(string input)
    {
        phoneticsProp!.stringValue = input;

        var mixedRes = wordEncoder.ParseMixed(input.AsSpan().ConvertU16(), processor, Allocator.Temp);

        ReadOnlySpan<char> unicodeRes = new(mixedRes.unicodeOutput.GetUnsafeReadOnlyPtr(), mixedRes.unicodeOutput.Length);
        unicodeProp!.stringValue      = new string(unicodeRes);

        Undo.RecordObject(phoneticsProp!.serializedObject.targetObject, "TextEdit");
        Undo.RecordObject(unicodeProp!.serializedObject.targetObject,   "TextEdit");

        phoneticsProp.serializedObject.ApplyModifiedProperties();
        unicodeProp.serializedObject.ApplyModifiedProperties();

        ReadOnlySpan<char> displayRes = new(mixedRes.displayOutput.GetUnsafeReadOnlyPtr(), mixedRes.displayOutput.Length);
        unicodeLabel!.text  = new string(displayRes);
        englishLabel!.text  = GetEnglishString(mixedRes.words);
        wordTypeLabel!.text = GetWordTypeString(mixedRes.words);
    }

    /// <summary>
    /// A callback that takes in a phonetic string, writes to disk the new results, and then populates the appropriate Editor UI.
    /// </summary>
    /// <param name="input"></param>
    private void WriteToWindow(string input)
    {
        if (responseData == null)
        {
            return;
        }
        MetaUpdate(input);
        phoneticField!.SetValueWithoutNotify(phoneticsProp!.stringValue);
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
            processor   = PhoneticProcessor.Create(ligatureSub!.standardSignTable.entries, ligatureSub.entries, Allocator.Persistent);
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

        unicodeLabel = root.Q<Label>("Input");
        Debug.Assert(unicodeLabel != null);

        var keyboardBox = keyboardUI.Q<VisualElement>("KeyboardParent");

        phoneticField = new("Phonetics")
        {
            name = "PhoneticInput"
        };
        phoneticField.AddToClassList("StandardFont");
        phoneticField.style.whiteSpace = WhiteSpace.PreWrap;
        keyboardBox.Add(phoneticField);

        phoneticField.RegisterCallback(
            (ChangeEvent<string> e) =>
            {
                keyboardUI.PhoneticsString = responseData!.phoneticsStr;
                MetaUpdate(e.newValue);
            }
        );

        englishLabel = new()
        {
            name = "EnglishTrans"
        };
        englishLabel.AddToClassList("StandardFont");
        englishLabel.style.whiteSpace = WhiteSpace.PreWrap;
        keyboardBox.Add(englishLabel);

        wordTypeLabel = new()
        {
            name = "WordTypes"
        };
        wordTypeLabel.AddToClassList("StandardFont");
        wordTypeLabel.style.whiteSpace = WhiteSpace.PreWrap;
        keyboardBox.Add(wordTypeLabel);
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