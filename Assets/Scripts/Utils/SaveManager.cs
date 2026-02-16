using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using System.IO;



[Serializable]
public class GameState
{
    public Dictionary dictionary;

    public Vector3 position;

    public static void LoadPlayerData()
    {
        string savePath = Path.Combine(Application.persistentDataPath, "PlayerSave.json");
        string emptyPath = Path.Combine(Application.dataPath, "Data/PlayerSaveEmpty.json");

        string jsonString;

        // Load Player Data JSON. If save data is empty load clean save (Currently just dictionary data)
        if (File.Exists(savePath))
        {
            jsonString = jsonString = File.ReadAllText(savePath);
        }
        else 
        {
            jsonString = File.ReadAllText(emptyPath);
        }

        // Get Player object
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        PlayerController player = playerObject.GetComponent<PlayerController>();

        // Load player save
        GameState save = JsonUtility.FromJson<GameState>(jsonString);

        player.dictionary = save.dictionary;
        player.transform.position = save.position;
    }

    public static void SavePlayerData()
    {
        // Get Player object
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        PlayerController player = playerObject.GetComponent<PlayerController>();

        // Create GameState object with all save data
        GameState save = new GameState
        {
            dictionary = player.dictionary,
            position = player.transform.position
        };

        // Serialize GameState and save to player save
        string saveJson = JsonUtility.ToJson(save, true);
        string savePath = Path.Combine(Application.persistentDataPath, "PlayerSave.json");
        File.WriteAllText(savePath, saveJson);
    }
}

[Serializable]
public struct DictionaryEntry
{
    public string Word;
    string Notes;
}

[Serializable]
public struct Dictionary
{
    public DictionaryEntry[] dictionaryList;
}

