using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.UIElements;

[CustomPropertyDrawer(typeof(SceneReference))]
internal sealed class SceneReferenceDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement element = new();

        var nameValue = property.FindPropertyRelative("name");
        var pathValue = property.FindPropertyRelative("path");

        ObjectField sceneField = new()
        {
            allowSceneObjects = true,
            objectType        = typeof(SceneAsset)
        };

        if (nameValue.stringValue.Length > 0)
        {
            SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(pathValue.stringValue);
            if (asset != null)
            {
                sceneField.value = asset;
            }
        }

        sceneField.RegisterCallback(
            (ChangeEvent<Object> e) =>
            {
                SceneAsset sceneAsset = e.newValue as SceneAsset;
                if (sceneAsset != null)
                {
                    nameValue.stringValue = sceneAsset.name;
                    pathValue.stringValue = AssetDatabase.GetAssetPath(sceneAsset);
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        );

        element.Add(sceneField);

        return element;
    }
}