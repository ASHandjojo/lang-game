using System;
using System.IO;
using System.Linq;
using System.Text.Json;

using UnityEngine;
using UnityEngine.UIElements;

public interface ISign
{
    public int MappedChar { get; }
}

[Serializable]
public struct StandardSign : ISign, IEquatable<StandardSign>
{
    public string phonetics;
    public int mappedChar;

    public readonly int MappedChar => mappedChar;

    public readonly bool Equals(StandardSign other) => phonetics.Equals(other.phonetics);

    public override readonly string ToString() => $"Phonetics: {phonetics}, Mapped Char: {mappedChar}";
}

[Serializable]
public sealed class SignJSON
{
    public ushort Unicode { get; set; }
    public string Characters { get; set; }
}

#if UNITY_EDITOR
[Serializable]
public sealed class OverridePhonetics
{
    public string phonetics;
    public string replace;
}
#endif

[CreateAssetMenu(menuName = "Linguistics/Sign Importer")]
public sealed class StandardSignTable : ScriptableObject
{
#if UNITY_EDITOR
    [SerializeField] private string fileDir;
    public string FileDir => fileDir;

    public OverridePhonetics[] overrides;

    public VisualTreeAsset standardUI;
#endif

    [SerializeField, HideInInspector] public StandardSign[] entries;
}