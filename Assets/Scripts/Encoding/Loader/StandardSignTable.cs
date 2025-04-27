using System;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

[Serializable]
public struct StandardSign : IEquatable<StandardSign>
{
    public string phonetics;
    public int mappedChar;

    public readonly bool Equals(StandardSign other) => phonetics.Equals(other.phonetics);
}

[CreateAssetMenu(menuName = "Linguistics/Standard Sign Table")]
public sealed class StandardSignTable : ScriptableObject
{
    public Vector2Int[] ranges;

#if UNITY_EDITOR
    public VisualTreeAsset standardUI;
    [SerializeField, HideInInspector]
    public StandardSign[] standardSigns;
#endif
}

#if UNITY_EDITOR
public sealed class StandardSignElement : VisualElement
{
    public StandardSignElement(VisualTreeAsset standardUI)
    {
        Debug.Assert(standardUI != null);

        standardUI.CloneTree(this);

        var phoneticsField = this.Q<TextField>("Phonetics");
        var resultField    = this.Q<TextField>("Result");

        phoneticsField.isReadOnly = true;
        resultField.isReadOnly    = true;
    }

    public void SetValue(in StandardSign standardSign)
    {
        var phoneticsField   = this.Q<TextField>("Phonetics");
        var unicodeCharField = this.Q<IntegerField>("UnicodeChar");
        var resultField      = this.Q<TextField>("Result");

        phoneticsField.value   = standardSign.phonetics;
        unicodeCharField.value = standardSign.mappedChar;
        resultField.value      = $"{(char) standardSign.mappedChar}";
    }
}

[CustomPropertyDrawer(typeof(StandardSign))]
public sealed class StandardSignDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement visualElement = new();

        var phoneticsProp = property.FindPropertyRelative(nameof(StandardSign.phonetics));
        var mappedProp    = property.FindPropertyRelative(nameof(StandardSign.mappedChar));

        var table     = property.serializedObject.targetObject as StandardSignTable;
        var treeAsset = table.standardUI;
        property.serializedObject.Update();

        treeAsset.CloneTree(visualElement);

        var phoneticsField = visualElement.Q<TextField>("Phonetics");
        var rawMapping     = visualElement.Q<IntegerField>("UnicodeChar");
        var resultField    = visualElement.Q<TextField>("Result");

        rawMapping.RegisterCallback<ChangeEvent<int>>(
            (e) =>
            {
                resultField.value = $"{(char) mappedProp.intValue}";
            }
        );

        phoneticsField.BindProperty(phoneticsProp);
        rawMapping.BindProperty(mappedProp);

        return visualElement;
    }
}

[CustomEditor(typeof(StandardSignTable))]
public sealed class StandardTableEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement element = new();
        InspectorElement.FillDefaultInspector(element, serializedObject, this);

        StandardSignTable table = target as StandardSignTable;

        Button submitButton = new(
            () =>
            {
                int totalChars = table.ranges.Select(range => (range.y - range.x) + 1).Sum();
                table.standardSigns = new StandardSign[totalChars];
                int index = 0;
                foreach (var range in table.ranges)
                {
                    for (int i = range.x; i <= range.y; i++, index++)
                    {
                        table.standardSigns[index] = new StandardSign()
                        {
                            phonetics  = $"{(char) i}",
                            mappedChar = (char) i,
                        };
                    }
                }

                // Create Visual Elements
                for (int i = 0; i < table.standardSigns.Length; i++)
                {
                    StandardSignElement child = new(table.standardUI);
                    child.SetValue(table.standardSigns[i]);

                    element.Add(child);
                }
            }
        );

        submitButton.AddToClassList("StandardFont");
        submitButton.text = "Update Ranges";
        element.Add(submitButton);

        return element;
    }
}

#endif