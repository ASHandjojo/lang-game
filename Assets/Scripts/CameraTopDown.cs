using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public sealed class CameraTopDown : MonoBehaviour
{
    [SerializeField] private Vector3 offset;

    private Transform playerTrans;

    void Awake()
    {
        
    }

    void Start()
    {
        playerTrans = PlayerController.Instance.transform;
        playerTrans.rotation = transform.rotation;
    }

    void Update()
    {
        transform.position = playerTrans.position + offset;
    }
}