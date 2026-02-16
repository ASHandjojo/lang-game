using System;
using System.Collections.Generic;

using Unity.Collections;

using UnityEngine;
using UnityEngine.UIElements;

public enum WordType : int
{
    Subject = 1,
    Verb    = 2,
    Object  = 3
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