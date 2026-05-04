using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

// note for cameras: orthographic for 3d map to look 2d
// follow character vs overlook entire map?

[DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
public sealed class BuildingInteract : NpcDialogue
{
    [Header("Interaction Settings")]
    [SerializeField] private SceneLoader    sceneLoader;
    [SerializeField] private SceneReference nextScene;
    [SerializeField] private Animator       animator;

    protected override IEnumerator OnLast()
    {
        sceneLoader.LoadNextLevel(nextScene.Name);
        yield break;
    }
}