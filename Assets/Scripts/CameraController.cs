using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float minYAngle = -20f;
    [SerializeField] private float maxYAngle = 80f;
    [SerializeField] private LayerMask collisionMask;

    private float yaw;
    private float pitch;
    private Vector3 desiredPosition;

    // LateUpdate
    void LateUpdate()
    {
        if (target == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        yaw += mouseDelta.x * rotationSpeed * Time.deltaTime;
        pitch -= mouseDelta.y * rotationSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minYAngle, maxYAngle);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetOffset = rotation * new Vector3(0f, 0f, -distance);
        desiredPosition = target.position + targetOffset;

        if (Physics.Raycast(target.position, targetOffset.normalized, out RaycastHit hit, distance, collisionMask))
        {
            desiredPosition = hit.point - targetOffset.normalized * 0.2f;
        }

        transform.position = desiredPosition;
        transform.LookAt(target);
    }
}
