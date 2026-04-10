using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
public sealed class BuildingInteract : NpcDialogue
{
    [Header("Interaction Settings")]
    [SerializeField] private SceneLoader    sceneLoader;
    [SerializeField] private SceneReference nextScene;
    [SerializeField] private Animator       animator;

    protected override IEnumerator OnLast()
    {
        sceneLoader.LoadNextLevel(nextScene.name);
        yield break;
    }
}