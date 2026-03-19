using System;

using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public struct SceneReference
{
    [SerializeField, HideInInspector]
    public string name;
    public readonly string Name => name;

    public readonly Scene GetScene() => SceneManager.GetSceneByName(name);
    public readonly void LoadScene() => SceneManager.LoadScene(name);
}