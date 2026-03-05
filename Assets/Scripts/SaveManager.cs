using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class GameState
{
    public Dictionary dictionary;
    public Vector3 position;

    public static void LoadPlayerData(int slot)
    {
        string savePath  = Path.Combine(Application.persistentDataPath, "PlayerSave" + slot + ".json");
        string emptyPath = Path.Combine(Application.dataPath, "Data/PlayerSaveEmpty.json");

        string jsonString;

        // Load Player Data JSON. If save data is empty load clean save (Currently just dictionary data)
        if (File.Exists(savePath))
        {
            jsonString = File.ReadAllText(savePath);
        }
        else 
        {
            jsonString = File.ReadAllText(emptyPath);
        }
        // Get Player Object (Singleton)
        PlayerController player = PlayerController.Instance;
        // Load player save
        GameState save = JsonUtility.FromJson<GameState>(jsonString);

        player.dictionary = save.dictionary;
        player.transform.position = save.position;
    }

    public static void SavePlayerData(int slot)
    {
        // Get Player Object (Singleton)
        PlayerController player = PlayerController.Instance;

        GameState save;
        string savePath = Path.Combine(Application.persistentDataPath, "PlayerSave" + slot + ".json");
        if (player.dictionary.dictionaryList.Length == 0)
        {
            string emptyPath = Path.Combine(Application.dataPath, "Data/PlayerSaveEmpty.json");
            string jsonString = File.ReadAllText(emptyPath);

            save = JsonUtility.FromJson<GameState>(jsonString);
            save.position = player.transform.position;
        }
        else
        {
            // Create GameState object with all save data
            save = new()
            {
                dictionary = player.dictionary,
                position = player.transform.position
            };
        }

        // Serialize GameState and save to player save
        string saveJson = JsonUtility.ToJson(save, prettyPrint: true);
        File.WriteAllText(savePath, saveJson);
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

