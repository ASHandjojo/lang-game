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
    public string line;

    public bool usesAudio;
    public AudioClip sound;
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
        PropertyField soundClipField = new(soundClipProperty);
        soundClipField.BindProperty(soundClipProperty);

        Toggle useAudioToggle = new("Use Audio");
        useAudioToggle.RegisterCallback(
            (ChangeEvent<bool> e) =>
            {
                Visibility visiblityState = e.newValue ? Visibility.Visible : Visibility.Hidden;
                soundClipField.style.visibility = visiblityState;
            }
        );

        SerializedProperty useAudioProperty = property.FindPropertyRelative(nameof(DialogueEntry.usesAudio));
        useAudioToggle.BindProperty(useAudioProperty);

        element.Add(useAudioToggle);
        element.Add(soundClipField);

        // Initial Visibility
        soundClipField.style.visibility = useAudioProperty.boolValue ? Visibility.Visible : Visibility.Hidden;

        return element;
    }
}
#endif