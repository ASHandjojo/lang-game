using Unity.Collections;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(DialogueEntry))]
public sealed class DialogueEntryDrawer : PropertyDrawer
{
    private const string EncodingImportDir = "Assets/Scripts/Encoding";
    // Ligature sub table also references standard table, kind of a shortcut :)
    private const string LigatureSubDir    = EncodingImportDir + "/Loader/Ligature Sub Table.asset";

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement element = new();

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Encoding/UI/StyleSheet.uss");
        Debug.Assert(styleSheet != null);
        element.styleSheets.Add(styleSheet);

        LigatureSub ligatureSub = AssetDatabase.LoadAssetAtPath<LigatureSub>(LigatureSubDir);
        Debug.Assert(ligatureSub != null);

        // Input Line
        SerializedProperty lineProperty = property.FindPropertyRelative(nameof(DialogueEntry.line));
        PropertyField lineField         = new(lineProperty);
        element.Add(lineField);

        // Sound
        SerializedProperty soundClipProperty = property.FindPropertyRelative(nameof(DialogueEntry.sound));
        PropertyField soundClipField         = new(soundClipProperty);
        element.Add(soundClipField);

        return element;
    }
}
#endif