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
    // [SerializeField] private SceneLoader    sceneLoader;
    [SerializeField] private SceneReference nextScene;
    [SerializeField] private Animator       animator;
    [SerializeField] private GameObject      enteringSpawnPoint;
    private float transitionTime = 1.2f;
    private Vector3 spawnPoint;
    private Vector3 previousSceneSpawnPoint = new Vector3(0.0f, 0.0f, 0.0f);

    protected override IEnumerator OnLast()
    {
        //sceneLoader.LoadNextLevel(nextScene.name);
        // Play transition animation
        animator.SetTrigger("NextScene");

        // Wait for animation to finish
        yield return new WaitForSeconds(transitionTime);

        // Load next Scene
        SceneManager.LoadScene(nextScene.Name);
        
        // // Set player spawn point in next scene
        // PlayerController.Instance.transform.position = spawnPoint;
    }

    // If player collides with building, enter the building using the coroutine above. 
    // If player collides with building from inside, exit the building and return to the previous scene.
    private void OnTriggerEnter(Collider collision)
    {
        Debug.Log("Player collided with building");
        if (collision.gameObject.CompareTag("Player"))
        {
            spawnPoint = enteringSpawnPoint.transform.position;
            GameObject player = collision.gameObject;
            PlayerController controller = player.GetComponent<PlayerController>();
            controller.BuildingSpawnPoint = spawnPoint;
            StartCoroutine(OnLast());
        }
    }
}