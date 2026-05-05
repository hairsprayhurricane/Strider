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

    // Счётчик коллайдеров игрока в зоне (несколько коллайдеров на персонаже)
    private int  playerInRange = 0;
    private bool picked        = false;
    private float startY;

    void Start()
    {
        startY = transform.position.y;
    }

    void Update()
    {
        float y = startY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        if (playerInRange > 0 && Input.GetKeyDown(KeyCode.E))
            TryPickup();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (playerInRange == 0)
            LogInterface.Add($"[E] {item.itemName}", Color.white);
        playerInRange++;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = Mathf.Max(0, playerInRange - 1);
    }

    void TryPickup()
    {
        if (picked) return;
        if (!PlayerInventory.Instance.AddItem(item)) return;

        picked  = true;
        enabled = false; // останавливаем Update до Destroy

        CanvasSelectorController.Instance?.OnItemPickedUp();

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Destroy(gameObject);
    }
}
