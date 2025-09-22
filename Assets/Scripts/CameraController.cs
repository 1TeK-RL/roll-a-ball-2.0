using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;
    private Vector3 offset;

    // Start
    void Start()
    {
        offset = transform.position - player.transform.position;
    }

    // LateUpdate
    void LateUpdate()
    {
        transform.position = player.transform.position + offset;
    }
}
