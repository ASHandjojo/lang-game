using System;
using System.Collections.Generic;
using UnityEngine;

// synonyms only really need to be checked after the word is submitted as an answer from the player?

[CreateAssetMenu(menuName = "Linguistics/Synonym System")]
public class SynonymSystem : ScriptableObject
{
    [Serializable]
    public struct SynonymEntry {
        public string word; // word. how to format? unicode?
        public List<string> strongSynonyms;
        public List<string> weakSynonyms;
    }

    public List<SynonymEntry> synonymEntries;

    private Dictionary<string, (List<string> strong, List<string> weak)> synonymMap;

    void OnEnable() {
        BuildMap();
    }

    private void BuildMap() {
        synonymMap = new Dictionary<string, (List<string> strong, List<string> weak)>();
        foreach (var e in synonymEntries){
            synonymMap[e.word] = (e.strongSynonyms, e.weakSynonyms);
        }
    }

    // Add this back as a static method
    public static bool TryGetSynonyms(string input, out List<string> strongSynonyms, out List<string> weakSynonyms) {
        return //InternalDictionary.TryGetSynonyms(input, out strongSynonyms, out weakSynonyms);
    }

    // gets synonyms for a word if its valid
    // input- string (unicode representation)
    // returns- list of lists: [strongSynonyms, weakSynonyms] if valid word, null otherwise
    public static List<List<string>> GetSynonyms(string input) {
        if (TryGetSynonyms(input, out var strong, out var weak)) {
            return new List<List<string>> { strong, weak };
        }
        return null;
    }

    // checks if the given string is a valid word and returns synonyms if it is
    // true if the input is valid, false if not.
    public bool TryGetSynonyms(string input, out List<string> strongSynonyms, out List<string> weakSynonyms) {
        strongSynonyms = null;
        weakSynonyms = null;

        bool isValidWord = false;
        // if the string is seen as a valid word (in a word list?) { isValidWord = true; }

        if (!isValidWord) {
            return false;
        }

        // get synonyms
        if (synonymMap.TryGetValue(input, out var synonyms)) {
            strongSynonyms = new List<string>(synonyms.strong);
            weakSynonyms = new List<string>(synonyms.weak);
            return true;
        }

        // no synonyms, return empty lists
        strongSynonyms = new List<string>();
        weakSynonyms = new List<string>();
        return true;
    }
}