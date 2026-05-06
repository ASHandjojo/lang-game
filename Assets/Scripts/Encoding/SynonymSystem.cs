using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SynonymEntry {
    [Tooltip("The phonetic representation of the word.")]
    public string phoneticWord;
    [Tooltip("List of strong synonyms (exact synonyms).")]
    public string[] strong;
    [Tooltip("List of weak synonyms (related words).")]
    public string[] weak;
}

[Serializable]
public struct Synonyms {
    public WordType  wordType;
    public SynonymEntry[] entries;
}

// how to use:
// call internal dict, check if word exists, then check synonym list
[CreateAssetMenu(menuName = "Linguistics/Synonym List")]
public sealed class SynonymSystem : ScriptableObject
{
    public Synonyms[] synonymLists;

    // what else?
}
