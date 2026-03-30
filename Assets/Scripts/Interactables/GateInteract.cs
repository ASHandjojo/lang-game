using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
public sealed class GateInteract : NpcDialogue
{
    [Header("Interaction Settings")]
    [SerializeField] private SceneLoader    sceneLoader;
    [SerializeField] private SceneReference nextScene;
    [SerializeField] private Animator       animator;

    private float transitionTime = 1.2f;

    protected override IEnumerator OnLast()
    {
        //sceneLoader.LoadNextLevel(nextScene.name);
        // Play transition animation
        animator.SetTrigger("NextScene");

        // Wait for animation to finish
        yield return new WaitForSeconds(transitionTime);

        // Load next Scene
        SceneManager.LoadScene(nextScene.Name);
    }
}