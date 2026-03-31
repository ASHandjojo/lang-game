using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

public enum WordType : ushort
{
    Noun      = 0,
    Adjective = 1,

    Verb    = 2,
    Adverb  = 3,
    Object  = 4,

    Particle = 5,

    Interjection = 6,

    TypeCount = 7,

    Unknown   = ushort.MaxValue
}

[BurstCompile]
public static class WordTypeExtMethods
{
    public static FixedString32Bytes ToFixedString(this WordType wordType) => wordType switch
    {
        WordType.Noun      => nameof(WordType.Noun),
        WordType.Adjective => nameof(WordType.Adjective),
        
        WordType.Verb   => nameof(WordType.Verb),
        WordType.Adverb => nameof(WordType.Adverb),
        WordType.Object => nameof(WordType.Object),

        WordType.Particle     => nameof(WordType.Particle),
        WordType.Interjection => nameof(WordType.Interjection),

        WordType.Unknown => nameof(WordType.Unknown),

        _ => throw new NotImplementedException()
    };
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
}

[Serializable]
public struct DictEntryColumn
{
    public WordType        wordType;
    public List<DictEntry> entries;
}

[CreateAssetMenu(menuName = "Linguistics/Internal Dictionary")]
public sealed class InternalDictionary : ScriptableObject
{
    public List<DictEntryColumn> entries;
}