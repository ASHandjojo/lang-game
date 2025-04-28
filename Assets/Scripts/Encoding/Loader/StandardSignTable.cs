using System;
using System.IO;
using System.Linq;
using System.Text.Json;

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

[Serializable]
public sealed class SignJSON
{
    public ushort Unicode { get; set; }
    public string Characters { get; set; }
}

[CreateAssetMenu(menuName = "Linguistics/Sign Importer")]
public sealed class StandardSignTable : ScriptableObject
{
#if UNITY_EDITOR
    [SerializeField] private string fileDir;
    public string FileDir => fileDir;

    public VisualTreeAsset standardUI;
#endif

    [SerializeField, HideInInspector] public StandardSign[] entries;
}

#if UNITY_EDITOR
public sealed class StandardSignElement : VisualElement
{
    public StandardSignElement(VisualTreeAsset standardUI)
    {
        Debug.Assert(standardUI != null);

        standardUI.CloneTree(this);

        var phoneticsField   = this.Q<TextField>("Phonetics");
        var unicodeCharField = this.Q<IntegerField>("UnicodeChar");
        var resultField      = this.Q<TextField>("Result");

        phoneticsField.isReadOnly   = true;
        unicodeCharField.isReadOnly = true;
        resultField.isReadOnly      = true;
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

[CustomEditor(typeof(StandardSignTable))]
public sealed class SignImporterEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement element = new();
        StandardSignTable table = target as StandardSignTable;

        InspectorElement.FillDefaultInspector(element, serializedObject, this);
        serializedObject.Update();

        Button submitButton = new(
            () =>
            {
                using FileStream stream = new(table.FileDir, FileMode.Open);

                SignJSON[]? signs = JsonSerializer.Deserialize<SignJSON[]>(stream);
                Debug.Assert(signs != null);

                StandardSign[] standardSigns = signs.Select(
                    sign => new StandardSign()
                    {
                        mappedChar = sign.Unicode,
                        phonetics  = sign.Characters
                    }
                ).ToArray();

                table.entries = standardSigns;

                EditorUtility.SetDirty(table);
                serializedObject.ApplyModifiedProperties();
            }
        );

        submitButton.AddToClassList("StandardFont");
        submitButton.text = "Update";
        element.Add(submitButton);

        Label label = new($"Length: {table.entries.Length}");
        label.AddToClassList("StandardFont");
        element.Add(label);

        for (int i = 0; i < table.entries.Length; i++)
        {
            StandardSignElement child = new(table.standardUI);
            child.SetValue(table.entries[i]);

            element.Add(child);
        }

        return element;
    }
}
#endif