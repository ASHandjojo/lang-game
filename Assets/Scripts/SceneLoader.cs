using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneLoader : MonoBehaviour
{

    [SerializeField] private Animator animator;
    private float transitionTime = 1.2f;

    public void LoadNextLevel()
    {
        StartCoroutine(LevelTransition());
    }

    IEnumerator LevelTransition()
    {
        // Play transition animation
        animator.SetTrigger("NextScene");

        // Wait for animation to finish
        yield return new WaitForSeconds(transitionTime);

        // Load next Scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

}