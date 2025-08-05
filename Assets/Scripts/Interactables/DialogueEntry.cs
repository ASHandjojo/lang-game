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
        element.();
        
        return element;
    }
}
#endif