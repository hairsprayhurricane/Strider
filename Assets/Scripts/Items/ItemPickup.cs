using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class ItemPickup : MonoBehaviour
{
    public PlayerItem item;

    [Header("World Animation")]
    public float bobHeight = 0.15f;
    public float bobSpeed  = 2f;
    public float spinSpeed = 90f;

    [Header("Pickup Sound")]
    public AudioClip pickupSound;

    private bool  playerNearby = false;
    private float startY;

    void Start()
    {
        startY = transform.position.y;
    }

    void Update()
    {
        // Парящая анимация
        float y = startY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        // Подбор по нажатию E
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
            TryPickup();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNearby = true;
        LogInterface.Add($"[E] {item.itemName}", Color.white);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNearby = false;
    }

    void TryPickup()
    {
        if (!PlayerInventory.Instance.AddItem(item)) return;

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Destroy(gameObject);
    }
}
