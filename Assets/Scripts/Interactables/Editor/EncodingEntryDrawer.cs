using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.UIElements;

[CustomPropertyDrawer(typeof(EncodingEntry))]
public sealed class EncodingEntryDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement element = new();

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Encoding/UI/StyleSheet.uss");
        Debug.Assert(styleSheet != null);
        element.styleSheets.Add(styleSheet);

        var lineProperty      = property.FindPropertyRelative(nameof(EncodingEntry.line));
        var phoneticsProperty = property.FindPropertyRelative(nameof(EncodingEntry.phoneticsStr));
        Label lineField  = new("Line");
        lineField.BindProperty(lineProperty);
        lineField.AddToClassList("TranslatedLabel");
        element.Add(lineField);

        // Text Editor Window Button
        Button openWindowButton = new()
        {
            text = "Open Text Editor"
        };

        openWindowButton.RegisterCallback(
            (ClickEvent e) =>
            {
                var lineProperty      = property.FindPropertyRelative(nameof(EncodingEntry.line));
                var phoneticsProperty = property.FindPropertyRelative(nameof(EncodingEntry.phoneticsStr));

                EncodingEntry entry = (EncodingEntry) property.boxedValue;
                EditorUI.ShowWindow(entry, phoneticsProperty, lineProperty);
            }
        );

        element.Add(openWindowButton);

        return element;
    }
}
