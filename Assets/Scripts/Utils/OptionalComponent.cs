using System;

using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

/// <summary>
/// Avoids expensive null checks for Unity objects. Only checks on assignment.
/// </summary>
/// <typeparam name="T"></typeparam>
[Serializable]
public sealed class OptionalComponent<T> where T : UnityEngine.Object
{
    [SerializeField] private T obj;
    [SerializeField, HideInInspector] private bool isInitialized = false;

    private OptionalComponent() { }

    public OptionalComponent(T obj) => Set(obj);

    public bool HasComponent() => isInitialized;

    public void Set(T objIn)
    {
        obj = objIn;
        isInitialized = objIn != null;
    }

    /// <summary>
    /// SetObject but elides all checks. Is for when you know that the input is certain to be initialized. Can be dangerous.
    /// </summary>
    /// <param name="objIn"></param>
    public void SetNonNull(T objIn)
    {
        obj = objIn;
        isInitialized = true;
    }

    public bool TryGet(out T objectOut)
    {
        objectOut = obj;
        return isInitialized;
    }

    public void Unset()
    {
        obj = null;
        isInitialized = false;
    }

    public static implicit operator OptionalComponent<T>(T obj)
    {
        return new OptionalComponent<T>(obj);
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(OptionalComponent<>), useForChildren: true)]
public sealed class OptionalComponentDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement element = new();

        SerializedProperty objProperty = property.FindPropertyRelative("obj");
        PropertyField objField         = new(objProperty, property.displayName);

        SerializedProperty isInitProp = property.FindPropertyRelative("isInitialized");
        objField.RegisterValueChangeCallback(
            (SerializedPropertyChangeEvent e) =>
            {
                isInitProp.boolValue = e.changedProperty.boxedValue != null;
                isInitProp.serializedObject.ApplyModifiedProperties();
            }
        );

        objField.BindProperty(objProperty);
        element.Add(objField);

        return element;
    }
}
#endif