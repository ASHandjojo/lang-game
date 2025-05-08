using UnityEngine;

public class UIBase : MonoBehaviour
{
    // Disable box collider to prevent further interaction & freeze position to prevent movement
    protected void DisableWorldActions()
    {
        PlayerController.Instance.GetComponent<BoxCollider2D>().enabled = false;
        PlayerController.Instance.GetComponent<Rigidbody2D>().constraints |= RigidbodyConstraints2D.FreezePositionX;
    }

    // Restore movement & Re-enable box collider
    protected void EnableWorldActions()
    {
        PlayerController.Instance.GetComponent<Rigidbody2D>().constraints &= ~RigidbodyConstraints2D.FreezePositionX;
        PlayerController.Instance.GetComponent<BoxCollider2D>().enabled = true;
    }
}
