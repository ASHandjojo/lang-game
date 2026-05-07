using UnityEngine;

[DisallowMultipleComponent]
public sealed class SceneOptions : MonoBehaviour
{
    [SerializeField] private MovementType movementType = MovementType.SideScroll;
    [SerializeField] private Vector3 position;

    public MovementType MovementType => movementType;

    void Awake()
    {
        
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(position, new Vector3(5.0f, 5.0f, 5.0f));
    }

    void Start()
    {
        PlayerController.Instance.MovementType       = movementType;
        PlayerController.Instance.transform.position = position;
    }
}