using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private GameObject winTextObject;

    private int count;

    // Awake
    private void Awake()
    {
        count = 0;
        winTextObject.SetActive(false);
    }

    // FixedUpdate
    void FixedUpdate()
    {
        SetCountText();
    }

    // SetCountText
    void SetCountText()
    {
        countText.text = "Count: " + count.ToString();
        if (count >= 16)
        {
            winTextObject.SetActive(true);
            Destroy(GameObject.FindGameObjectWithTag("Enemy"));
        }
    }

    // OnTriggerEnter
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PickUp"))
        {
            other.gameObject.SetActive(false);

            count = count + 1;
            SetCountText();
        }
    }

    // OnCollisionEnter
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Destroy the current object
            Destroy(gameObject);
            // Update the winText to display "You Lose!"
            winTextObject.gameObject.SetActive(true);
            winTextObject.GetComponent<TextMeshProUGUI>().text = "You Lose!";
        }
    }
}
