using System;
using System.Text.Json;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

[Serializable]
public sealed class SignJSON
{
    public ushort Unicode { get; set; }
    public string Characters { get; set; }
}

[CreateAssetMenu(menuName = "Linguistics/Sign Importer")]
public sealed class SignImporter : ScriptableObject
{
#if UNITY_EDITOR
    [SerializeField] private string fileDir;
    public string FileDir => fileDir;

    public VisualTreeAsset standardUI;
#endif

    [SerializeField, HideInInspector] public StandardSign[] entries;
}

#if UNITY_EDITOR
[CustomEditor(typeof(SignImporter))]
public sealed class SignImporterEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement element = new();
        SignImporter table = target as SignImporter;

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
                        phonetics = sign.Characters
                    }
                ).ToArray();

                table.entries = standardSigns;

                for ( int i = 0; i < standardSigns.Length; i++)
                {
                    StandardSignElement child = new(table.standardUI);
                    child.SetValue(standardSigns[i]);

                    element.Add(child);
                }

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

        return element;
    }
}
#endif