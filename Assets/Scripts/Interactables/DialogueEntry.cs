using System;

using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

[Serializable]
public struct DialogueEntry
{
    [Serializable]
    public sealed class ResponseData
    {
        public string expectedInput;
    }

    public string line;

    public bool usesAudio;
    public AudioClip sound;

    public bool hasResponse;
    public ResponseData responseData;
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(DialogueEntry))]
public sealed class DialogueEntryDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement element = new();

        SerializedProperty lineProperty = property.FindPropertyRelative(nameof(DialogueEntry.line));
        PropertyField lineField = new(lineProperty);
        lineField.BindProperty(lineProperty);
        element.Add(lineField);

        SerializedProperty soundClipProperty = property.FindPropertyRelative(nameof(DialogueEntry.sound));
        PropertyField soundClipField         = new(soundClipProperty);

        SerializedProperty useAudioProperty = property.FindPropertyRelative(nameof(DialogueEntry.usesAudio));
        soundClipField.BindProperty(soundClipProperty);
        soundClipField.RegisterCallback(
            (ChangeEvent<UnityEngine.Object> e) =>
            {
                useAudioProperty.boolValue = e.newValue != null;
                useAudioProperty.serializedObject.ApplyModifiedProperties();
            }
        );

        element.Add(soundClipField);

        SerializedProperty responseDataProperty = property.FindPropertyRelative(nameof(DialogueEntry.responseData));
        PropertyField responseDataField = new(responseDataProperty);
        responseDataField.BindProperty(responseDataProperty);

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Encoding/UI/StyleSheet.uss");
        Debug.Assert(styleSheet != null);
        responseDataField.AddToClassList("TranslatedLabel");

        SerializedProperty hasResponseProperty = property.FindPropertyRelative(nameof(DialogueEntry.hasResponse));
        Toggle hasResponseToggle = new("Has Response");
        hasResponseToggle.BindProperty(hasResponseProperty);
        hasResponseToggle.RegisterCallback(
            (ChangeEvent<bool> e) =>
            {
                Visibility visiblityState          = e.newValue ? Visibility.Visible : Visibility.Hidden;
                responseDataField.style.visibility = visiblityState;
            }
        );

        responseDataField.style.visibility = hasResponseProperty.boolValue ? Visibility.Visible : Visibility.Hidden;
        element.Add(hasResponseToggle);
        element.Add(responseDataField);


        return element;
    }
}
#endif