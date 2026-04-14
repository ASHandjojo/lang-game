using System;
using System.Linq;
using System.Text;

using Unity.Collections;
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
    public static void ShowWindow(in EncodingEntry dialogueEntry, SerializedProperty phoneticsProp, SerializedProperty unicodeProp)
    {
        Debug.Assert(dialogueEntry != null);
        // Linking dialogue entry being edited.
        EditorUI baseWindow     = GetWindow<EditorUI>(EditorName, true);
        baseWindow.responseData = dialogueEntry;
        // Load from stored to UI (Unicode)
        baseWindow.unicodeLabel!.text = baseWindow.responseData!.line;

        baseWindow.phoneticsProp = phoneticsProp;
        baseWindow.unicodeProp   = unicodeProp;
        baseWindow.keyboardUI.PhoneticsString = baseWindow.responseData.phoneticsStr;

        (var words, var outputStr) = baseWindow.ParseMixed(baseWindow.keyboardUI.PhoneticsString, Allocator.Temp);

        baseWindow.englishLabel!.text  = baseWindow.GetEnglishString(words);
        baseWindow.wordTypeLabel!.text = baseWindow.GetWordTypeString(words);

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

    private (NativeArray<WordNode>, string) ParseMixed(string input, Allocator allocator = Allocator.Temp)
    {
        const ushort Separator = '|';
        if (input.Length == 0)
        {
            return (default, string.Empty);
        }
        int wordCount = input.AsSpan().ConvertU16().WordCount(' ');
        int charCount = input.AsSpan().ConvertU16().CharCount(Separator);

        NativeArray<WordNode> nodes = new(math.max(wordCount - charCount, 0), allocator);
        SplitIterator wordIter      = SplitIterator.Create(input, ' ');

        StringBuilder builder = new();
        int wordIdx = 0;
        while (wordIter.MoveNext())
        {
            ReadOnlySpan<ushort> word = wordIter.Current;
            if (word.IsEmpty)
            {
                continue;
            }
            if (word[0] != Separator)
            {
                string wordConv  = processor.Translate(word.ConvertChar());
                nodes[wordIdx++] = wordEncoder.ParseSingle(wordConv.AsSpan().ConvertU16());
                builder.Append($" {wordConv}");
            }
            else
            {
                builder.Append($" <font=\"Harmony SDF\">{word[1..].ConvertChar().ToString()}</font>");
            }
        }
        return (nodes, builder.ToString().TrimStart());
    }

    private void MetaUpdate(string input)
    {
        phoneticsProp!.stringValue = input;

        (var words, var outputStr) = ParseMixed(input, Allocator.Temp);

        unicodeProp!.stringValue = outputStr;

        Undo.RecordObject(phoneticsProp!.serializedObject.targetObject, "TextEdit");
        Undo.RecordObject(unicodeProp!.serializedObject.targetObject,   "TextEdit");

        phoneticsProp.serializedObject.ApplyModifiedProperties();
        unicodeProp.serializedObject.ApplyModifiedProperties();

        unicodeLabel!.text  = unicodeProp!.stringValue;
        englishLabel!.text  = GetEnglishString(words);
        wordTypeLabel!.text = GetWordTypeString(words);
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