using UnityEngine;

public class Rotator : MonoBehaviour
{
    // Update
    void Update()
    {
        transform.Rotate(new Vector3(15, 30, 45) * Time.deltaTime);
    }
}
