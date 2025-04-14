using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public SceneLoader sl;

    private void Awake()
    {
        sl = FindObjectOfType<SceneLoader>();
    }
    public void PlayGame()
    {
        // Currently: load the first level
        sl.LoadNextLevel();
    }

    public void ExitGame()
    {
        Debug.Log("Exit game.");
        Application.Quit();
    }
}
