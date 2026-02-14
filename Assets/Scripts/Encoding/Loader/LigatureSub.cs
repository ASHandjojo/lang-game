using System;

using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public struct CompoundSign
{
    public string combinedString;
    public int mappedChar;

    // Standard Characters
    public int[] mappedChars;
}

namespace Serialization
{
    [Serializable]
    public sealed class LigatureSubEntry
    {
        public ushort Glyph { get; set; }
        public ushort[] Components { get; set; }
    }

    [Serializable]
    public sealed class LigatureSubGroup
    {
        public ushort First { get; set; }
        public LigatureSubEntry[] Ligatures { get; set; }
    }
}

[CreateAssetMenu(menuName = "Linguistics/Ligature Table")]
public sealed class LigatureSub : ScriptableObject
{
#if UNITY_EDITOR
    [SerializeField] private string fileDir;
    public string FileDir => fileDir;

    public VisualTreeAsset compoundUI;
    public VisualTreeAsset compoundChildUI;

    public StandardSignTable standardSignTable;
#endif
    [SerializeField, HideInInspector] public CompoundSign[] entries;
}