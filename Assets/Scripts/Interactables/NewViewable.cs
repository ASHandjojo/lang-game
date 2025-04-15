using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewViewable : Interactable
{
    private float timeElapsed;
    private bool isZoomed;
    [SerializeField] private float transitionTime = 1f;

    private void Start()
    {
        isZoomed = false;
    }

    public override void Interact(PlayerController player)
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null) {
            Debug.LogWarning("Main camera not found.");
            return;
        }

        if(isZoomed)
        {
            StartCoroutine(CamDetransition(mainCamera, player));
        }
        else
        {
            StartCoroutine(CamTransition(mainCamera, player));
        }

        isZoomed = !isZoomed;
        
    }

    IEnumerator CamTransition(Camera mainCamera, PlayerController player)
    {
        // Disable box collider to prevent further interaction & freeze position to prevent movement
        player.GetComponent<BoxCollider2D>().enabled = false;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        rb.constraints |= RigidbodyConstraints2D.FreezePositionX;
        mainCamera.GetComponent<Camera_Movement>().enabled = false;

        Vector3 startPos = mainCamera.transform.position;
        Vector3 endPos = new Vector3(transform.position.x, transform.position.y, -10f);
        timeElapsed = 0;

        // Change camera's focus from player to viewable
        while (timeElapsed < transitionTime)
        {
            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, timeElapsed / transitionTime);
            mainCamera.orthographicSize = Mathf.Lerp(5, 1, timeElapsed / transitionTime);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        mainCamera.transform.position = endPos;
        mainCamera.orthographicSize = 1;

        // Re-enable box collider to restore interaction
        player.GetComponent<BoxCollider2D>().enabled = true;
    }

    IEnumerator CamDetransition(Camera mainCamera, PlayerController player)
    {
        // Disable box collider to prevent further interaction & position to prevent movement
        player.GetComponent<BoxCollider2D>().enabled = false;

        Vector3 startPos = mainCamera.transform.position;
        Vector3 endPos = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
        timeElapsed = 0;

        // Change camera focus from player to viewable
        while (timeElapsed < transitionTime)
        {
            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, timeElapsed / transitionTime);
            mainCamera.orthographicSize = Mathf.Lerp(1, 5, timeElapsed / transitionTime);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        mainCamera.transform.position = endPos;
        mainCamera.orthographicSize = 5;

        mainCamera.GetComponent<Camera_Movement>().enabled = true;

        // Restore movement
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;

        // Re-enable box collider to restore interaction
        player.GetComponent<BoxCollider2D>().enabled = true;
    }
}
