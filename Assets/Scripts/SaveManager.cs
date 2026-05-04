using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

[Serializable]
public sealed class GameState
{
    public Dictionary dictionary;
    public Vector3 position;

    public static void LoadPlayerData(int slot, InternalDictionary internalDictionary)
    {
        string savePath = Path.Combine(Application.persistentDataPath, "PlayerSave" + slot + ".json");
        string jsonString;

        // Get Player Object (Singleton)
        PlayerController player = PlayerController.Instance;
        if (!File.Exists(savePath)) 
        {
            return;
        }
        // Load Player Data JSON
        jsonString = File.ReadAllText(savePath);
        // Load player save
        GameState save = JsonUtility.FromJson<GameState>(jsonString);

        player.dictionary = save.dictionary;
        //AlignPlayerDictWithInternal(internalDictionary);

        player.transform.position = save.position;
    }

    public static void SavePlayerData(int slot)
    {
        // Get Player Object (Singleton)
        PlayerController player = PlayerController.Instance;

        GameState save;
        string savePath = Path.Combine(Application.persistentDataPath, "PlayerSave" + slot + ".json");
        
        // Create GameState object with all save data
        save = new()
        {
            dictionary = player.dictionary,
            position = player.transform.position
        };

        // Serialize GameState and save to player save
        string saveJson = JsonUtility.ToJson(save, prettyPrint: true);
        File.WriteAllText(savePath, saveJson);
    }

    // Create an empty dictionary for the player based on the internal dictionary
    public static void InitializeEmptyDictionary(InternalDictionary internalDictionary) 
    {
        PlayerController player = PlayerController.Instance;

        JournalPage[] playerPages;
        if (player.dictionary.journalPages == null)
        {
            playerPages = new JournalPage[player.playerJournalSize];
        }
        else
        {
            playerPages = player.dictionary.journalPages;
        }

        DictionaryEntry[] entries = new DictionaryEntry[0];

        Dictionary dict = new()
        {
            dictionaryList = entries,
            journalPages = playerPages
        };
        player.dictionary = dict;
    }

    //Aligns the player's dictionary with the internal dictionary
    // If new words have been added to the internal dictionary the player's saves will
    // update to reflect this
    //public static void AlignPlayerDictWithInternal(InternalDictionary internalDictionary)
    //{
    //    PlayerController player = PlayerController.Instance;

    //    Dictionary<string, string> playerMap = new();

    //    foreach (DictionaryEntry entry in player.dictionary.dictionaryList)
    //    {
    //        playerMap.Add(entry.Word, entry.Notes);
    //    }

    //    InitializeEmptyDictionary(internalDictionary);
    //    for (int i = 0; i < player.dictionary.dictionaryList.Length; i++)
    //    {
    //        playerMap.TryGetValue(player.dictionary.dictionaryList[i].Word, out PlayerController.Instance.dictionary.dictionaryList[i].Notes);
    //    }
    //}

    // Add new word to the player dictionary at a checkpoint
    public static void AddWordsToDict(string[] newWords)
    {
        PlayerController player = PlayerController.Instance;

        Dictionary<string, string> playerMap = new();

        foreach (DictionaryEntry entry in player.dictionary.dictionaryList)
        {
            playerMap.Add(entry.Word, entry.Notes);
        }

        List<DictionaryEntry> wordsToAdd = new();

        for (int i = 0; i < newWords.Count(); i++)
        {
            if (!playerMap.ContainsKey(newWords[i]))
            {
                DictionaryEntry entry = new()
                {
                    Word = newWords[i],
                    Notes = string.Empty
                };

                wordsToAdd.Add(entry);
                playerMap.Add(entry.Word, entry.Notes);
            }
        }

        Dictionary dict = new()
        {
            dictionaryList = player.dictionary.dictionaryList.Concat(wordsToAdd.ToArray()).ToArray(),
            journalPages = player.dictionary.journalPages
        };
        player.dictionary = dict;
    }
}

[Serializable]
public struct DictionaryEntry
{
    public string Word;
    public string Notes;
}

[Serializable]
public struct JournalPage
{
    public string Content;
}

[Serializable]
public struct Dictionary
{
    public DictionaryEntry[] dictionaryList;
    public JournalPage[]     journalPages;
}

