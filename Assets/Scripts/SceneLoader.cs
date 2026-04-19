using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private float transitionTime = 1.2f;

    public void LoadNextLevel(in Scene scene)
    {
        StartCoroutine(LevelTransition(scene));
    }

    public void LoadNextLevel(string sceneName)
    {
        StartCoroutine(LevelTransition(sceneName));
    }

    private IEnumerator LevelTransition(Scene scene)
    {
        // Play transition animation
        animator.SetTrigger("NextScene");

        // Wait for animation to finish
        yield return new WaitForSeconds(transitionTime);
        
        Debug.Log($"Scene Name: {scene.name}");
        // Load next Scene
        SceneManager.LoadScene(scene.buildIndex);
    }

    private IEnumerator LevelTransition(string sceneName)
    {
        // Play transition animation
        animator.SetTrigger("NextScene");

        // Wait for animation to finish
        yield return new WaitForSeconds(transitionTime);

        // Load next Scene
        SceneManager.LoadScene(sceneName);
    }
}