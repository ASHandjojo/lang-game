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
        AlignPlayerDictWithInternal(internalDictionary);

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

        int entryCount = internalDictionary.entries.Select(x => x.entries.Count()).Sum();
        DictionaryEntry[] entries = new DictionaryEntry[entryCount];

        for (int i = 0, linearIdx = 0; i < internalDictionary.entries.Count(); i++)
        {
            DictEntryColumn column = internalDictionary.entries[i];
            for (int j = 0; j < column.entries.Count; j++, linearIdx++)
            {
                entries[linearIdx].Word  = column.entries[j].unicodeString;
                entries[linearIdx].Notes = string.Empty;
            }
        }

        Dictionary dict = new()
        {
            dictionaryList = entries,
            journalPages = playerPages
        };
        player.dictionary = dict;
    }

    // Aligns the player's dictionary with the internal dictionary
    // If new words have been added to the internal dictionary the player's saves will
    // update to reflect this
    public static void AlignPlayerDictWithInternal(InternalDictionary internalDictionary)
    {
        PlayerController player = PlayerController.Instance;

        Dictionary<string, string> playerMap = new();

        foreach (DictionaryEntry entry in player.dictionary.dictionaryList)
        {
            playerMap.Add(entry.Word, entry.Notes);
        }

        InitializeEmptyDictionary(internalDictionary);
        for (int i = 0; i < player.dictionary.dictionaryList.Length; i++)
        {
            playerMap.TryGetValue(player.dictionary.dictionaryList[i].Word, out PlayerController.Instance.dictionary.dictionaryList[i].Notes);
        }
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
    public JournalPage[] journalPages;
}

