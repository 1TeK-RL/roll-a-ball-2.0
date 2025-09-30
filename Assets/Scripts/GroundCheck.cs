using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    [SerializeField] private float groundLength = 0.75f;
    [SerializeField] private LayerMask groundLayer;

    public bool IsGrounded { get; private set; }

    // Update
    private void FixedUpdate()
    {
        IsGrounded = Physics.Raycast(transform.position, Vector3.down, groundLength, groundLayer);
    }

    // OnDrawGizmos
    private void OnDrawGizmos()
    {
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundLength);
    }
}
