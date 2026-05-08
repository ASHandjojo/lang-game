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
        playerTrans.rotation   = Quaternion.Euler(45.0f, -5.0f, 0.0f);
        playerTrans.localScale = new Vector3(0.325f, 0.325f, 0.325f);

        playerTrans.gameObject.GetComponent<Rigidbody>().useGravity = true;

    }

    void Update()
    {
        transform.position = playerTrans.position + offset;
    }
}