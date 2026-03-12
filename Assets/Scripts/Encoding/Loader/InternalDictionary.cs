using System;
using System.Collections.Generic;

using Unity.Collections;

using UnityEngine;
using UnityEngine.UIElements;

public enum WordType : ushort
{
    Noun      = 1,
    Adjective = 2,

    Verb    = 4,
    Adverb  = 8,
    Object  = 16,

    TypeCount = 5,

    Unknown   = ushort.MaxValue
}

[Serializable]
public struct DictEntry
{
    [Tooltip("The raw phonetics representation.")]
    public string rawString;
    [Tooltip("The processed unicode representation.")]
    public string unicodeString;
    [Tooltip("The English equivalent translation.")]
    public string englishTranslation;

    public WordType wordType;
}

[CreateAssetMenu(menuName = "Linguistics/Internal Dictionary")]
public sealed class InternalDictionary : ScriptableObject
{
    public List<DictEntry> entries;
}