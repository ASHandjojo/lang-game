using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable]
public sealed class GameState
{
    public Dictionary dictionary;
    public Vector3 position;

    private const string RootImportDir = "Assets/Scripts/Encoding";
    private const string InternalDictionaryDir = RootImportDir + "/Loader/Internal Dictionary.asset";

    public static void LoadPlayerData(int slot)
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
        AlignPlayerDictWithInternal();

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
    public static void InitializeEmptyDictionary() 
    {
        PlayerController player = PlayerController.Instance;

        InternalDictionary internalDictionary = AssetDatabase.LoadAssetAtPath<InternalDictionary>(InternalDictionaryDir);

        DictionaryEntry[] entries = new DictionaryEntry[internalDictionary.entries.Count()];

        for (int i = 0; i < internalDictionary.entries.Count(); i++)
        {
            entries[i].Word = internalDictionary.entries[i].rawString;
            entries[i].Notes = "";
        }

        Dictionary dict = new Dictionary();
        dict.dictionaryList = entries;
        player.dictionary = dict;
    }

    // Aligns the player's dictionary with the internal dictionary
    // If new words have been added to the internal dictionary the player's saves will
    // update to reflect this
    public static void AlignPlayerDictWithInternal()
    {
        InternalDictionary internalDictionary = AssetDatabase.LoadAssetAtPath<InternalDictionary>(InternalDictionaryDir);

        Dictionary<string, string> playerMap = new Dictionary<string, string>();

        foreach (DictionaryEntry entry in PlayerController.Instance.dictionary.dictionaryList)
        {
            playerMap.Add(entry.Word, entry.Notes);
        }

        InitializeEmptyDictionary();

        for (int i = 0; i < PlayerController.Instance.dictionary.dictionaryList.Length; i++)
        {
            playerMap.TryGetValue(PlayerController.Instance.dictionary.dictionaryList[i].Word, out PlayerController.Instance.dictionary.dictionaryList[i].Notes);
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
public struct Dictionary
{
    public DictionaryEntry[] dictionaryList;
}

