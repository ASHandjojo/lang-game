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

        // Response Data
        SerializedProperty responseDataProperty = property.FindPropertyRelative(nameof(DialogueEntry.responseData));
        PropertyField responseDataField = new(responseDataProperty);
        responseDataField.AddToClassList("Translate");

        // Has Response
        SerializedProperty hasResponseProperty = property.FindPropertyRelative(nameof(DialogueEntry.hasResponse));
        Toggle hasResponseToggle = new("Has Response");
        hasResponseToggle.BindProperty(hasResponseProperty);

        responseDataField.RegisterCallback(
            (ChangeEvent<string> e) =>
            {
                PhoneticProcessor processor = PhoneticProcessor.Create(ligatureSub.standardSignTable.entries, ligatureSub.entries, Allocator.Temp);
                property.serializedObject.ApplyModifiedProperties();
            }
        );

        // Adding Callback for Response Toggle
        hasResponseToggle.RegisterCallback(
            (ChangeEvent<bool> e) =>
            {
                Visibility visiblityState = e.newValue ? Visibility.Visible : Visibility.Hidden;
                DisplayStyle displayStyle = e.newValue ? DisplayStyle.Flex  : DisplayStyle.None;

                responseDataField.style.visibility = visiblityState;
                responseDataField.style.display    = displayStyle;
            }
        );
        responseDataField.style.visibility = hasResponseProperty.boolValue ? Visibility.Visible : Visibility.Hidden;

        element.Add(hasResponseToggle);
        element.Add(responseDataField);

        return element;
    }
}
#endif