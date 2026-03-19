using System.Xml.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(SceneReference))]
internal sealed class SceneReferenceDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement element = new();

        var nameValue = property.FindPropertyRelative("name");

        ObjectField sceneField = new()
        {
            allowSceneObjects = true,
            objectType        = typeof(SceneAsset)
        };
        sceneField.RegisterCallback(
            (ChangeEvent<Object> e) =>
            {
                SceneAsset sceneAsset = e.newValue as SceneAsset;
                if (sceneAsset != null)
                {
                    nameValue.stringValue = sceneAsset.name;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        );

        element.Add(sceneField);

        return element;
    }
}