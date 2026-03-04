using Unity.Collections;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;

public sealed class EditorUI : EditorWindow
{
    private const string KeyboardImportDir = "Assets/UI/Keyboard";
    private const string KeyboardTreeDir   = KeyboardImportDir + "/Keyboard.uxml";

    private const string EncodingImportDir = "Assets/Scripts/Encoding";
    private const string LigatureSubDir    = EncodingImportDir + "/Loader/Ligature Sub Table.asset";

    private Processor processor;

    [MenuItem("Conlang/Text Editor")]
    public static void ShowWindow()
    {
        EditorWindow baseWindow = GetWindow<EditorUI>("Text Editor", true);
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
            processor = new Processor(ligatureSub.standardSignTable.entries, ligatureSub.entries, Allocator.Persistent);
        }

        KeyboardUI ui = new(treeAsset, processor, (str) => { });
        root.Add(ui);
    }

    public void OnDestroy()
    {
        if (processor.IsValid)
        {
            processor.Dispose();
        }
    }
}