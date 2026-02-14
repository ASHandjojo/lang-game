using System;
using System.IO;
using System.Linq;
using System.Text.Json;

using UnityEngine;
using UnityEngine.UIElements;

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