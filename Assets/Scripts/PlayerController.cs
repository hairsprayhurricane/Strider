using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float heavyGravity = -100f;
    private const byte maxCamY = 50;
    public float moveSpeed = 5f;
    public float moveSpeedDeBuff = 0;
    public float mouseSensitivity = 500f;
    public bool isWalking;
    public bool canRun = true;
    public bool isRunning;
    private CharacterController characterController;
    public float xRecoilOffset = 0f;
    public float yRecoilOffset = 0f;
    private Vector3 velocity;
    private bool isGrounded;

    private Coroutine walkCor;
    private const float timeBetweenSteps = 0.65f;
    public AudioClip stepSoundClip;
    private AudioSource audioSource;

    public static bool isCameraMovable = true;
    public static bool isPlayerMovable = true;

    public static bool isNoClipOn = false;
    private const string NoClipLayerName = "NoClip";
    private static int noClipLayer = -1;
    private static int prevLayer = -1;
    private float noClipSpeedMultiplier = 10f;

    [Header("Top-Down Camera")]
    public Vector3 cameraOffset = new Vector3(0f, 20f, -8f);
    public float cameraFollowSpeed = 8f;
    public float cameraTiltX = 55f;

    [Header("RMB Camera Peek")]
    public float peekFollowSpeed = 5f;
    public float peekReturnSpeed = 8f;
    public float maxPeekDistance = 30f;

    public bool isPeeking = false;
    private Vector3 peekOffset = Vector3.zero;

    private LineRenderer rayLine;

    public PlayerHealth playerHealth;

    private static PlayerController _instance;
    public static PlayerController Instance { get { return _instance; } }

    public void Awake()
    {
        if (_instance != null && _instance != this)
            Destroy(this.gameObject);
        else
            _instance = this;
    }

    public void Start()
    {
        Time.timeScale = 1f;
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        Camera.main.transform.position = transform.position + cameraOffset;
        Camera.main.transform.rotation = Quaternion.Euler(cameraTiltX, 0f, 0f);
    }

    void Update()
    {
        if (playerHealth.isDead) 
        {
            UpdatePeekOffset();
            return;
        }

        if (canRun && Input.GetKey(KeyCode.LeftShift))
            isRunning = true;
        else if (!Input.GetKey(KeyCode.LeftShift))
            isRunning = false;

        Move();
        isPeeking = Input.GetMouseButton(1);
        UpdatePeekOffset();
        
        DrawRay(transform.position, transform.forward * GetDistanceToCursor());
    }

    void LateUpdate()
    {
        Vector3 baseTarget = transform.position + cameraOffset;
        Vector3 targetPos = baseTarget + peekOffset;

        float speed = isPeeking ? peekFollowSpeed : peekReturnSpeed;

        Camera.main.transform.position = Vector3.Lerp(
            Camera.main.transform.position,
            targetPos,
            speed * Time.deltaTime
        );

        Camera.main.transform.rotation = Quaternion.Euler(cameraTiltX, 0f, 0f);
    }

    void UpdatePeekOffset()
    {

        if (isPeeking)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, transform.position);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldCursor = ray.GetPoint(distance);
                Vector3 towardCursor = worldCursor - transform.position;
                towardCursor.y = 0f;

                if (towardCursor.magnitude > maxPeekDistance)
                    towardCursor = towardCursor.normalized * maxPeekDistance;

                peekOffset = new Vector3(towardCursor.x, 0f, towardCursor.z);
            }
        }
        else
        {
            peekOffset = Vector3.Lerp(peekOffset, Vector3.zero, peekReturnSpeed * Time.deltaTime);

            if (peekOffset.magnitude < 0.01f)
                peekOffset = Vector3.zero;
        }
    }

    void Move()
    {
        isGrounded = characterController.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = 0f;

        if (isCameraMovable)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, transform.position);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 lookPoint = ray.GetPoint(distance);
                lookPoint.y = transform.position.y;
                transform.LookAt(lookPoint);
            }
        }

        if (isNoClipOn)
        {
            NoClipMove();
            isWalking = false;
            return;
        }

        if (isPlayerMovable)
        {
            bool moveForward  = Input.GetKey(KeyCode.W);
            bool moveBackward = Input.GetKey(KeyCode.S);
            bool moveLeft     = Input.GetKey(KeyCode.A);
            bool moveRight    = Input.GetKey(KeyCode.D);

            Vector3 moveDirection = Vector3.zero;

            if (moveForward)  moveDirection += Vector3.forward;
            if (moveBackward) moveDirection -= Vector3.forward;
            if (moveLeft)     moveDirection -= Vector3.right;
            if (moveRight)    moveDirection += Vector3.right;

            if (moveDirection != Vector3.zero)
                moveDirection = moveDirection.normalized;

            if (moveDirection != Vector3.zero)
            {
                float speed = isRunning
                    ? (moveSpeed - Mathf.Max(0, moveSpeedDeBuff)) * 1.5f
                    : (moveSpeed - Mathf.Max(0, moveSpeedDeBuff));

                characterController.Move(moveDirection * speed * Time.deltaTime);
                isWalking = true;

                if (walkCor == null)
                    walkCor = StartCoroutine(WalkCoroutine());
            }
            else
            {
                isWalking = false;
                isRunning = false;
            }

            if (!isGrounded)
            {
                velocity.y += heavyGravity * Time.deltaTime;
                characterController.Move(velocity * Time.deltaTime);
            }
        }
    }

    private IEnumerator WalkCoroutine()
    {
        while (isWalking)
        {
            if (audioSource && stepSoundClip)
                audioSource.PlayOneShot(stepSoundClip);

            yield return new WaitForSeconds(isRunning ? timeBetweenSteps / 1.5f : timeBetweenSteps);
        }
        walkCor = null;
    }

    public void ApplyRecoil(float pitchRecoil, float yawRecoil, float duration)
    {
        StopCoroutine("SmoothRecoil");
        StartCoroutine(SmoothRecoil(pitchRecoil, yawRecoil, duration));
    }

    private IEnumerator SmoothRecoil(float pitchRecoil, float yawRecoil, float duration)
    {
        float elapsed = 0f;
        float startX = xRecoilOffset, startY = yRecoilOffset;
        float targetX = startX - pitchRecoil, targetY = startY + yawRecoil;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            xRecoilOffset = Mathf.Lerp(startX, targetX, t);
            yRecoilOffset = Mathf.Lerp(startY, targetY, t);
            yield return null;
        }

        xRecoilOffset = targetX;
        yRecoilOffset = targetY;
    }

    public void ShakeCamera(float duration = 0.5f, float magnitude = 3f)
    {
        Instance.StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        Vector3 originalPosition = Camera.main.transform.localPosition;
        float elapsed = 0.0f;

        float noiseX = Random.Range(0f, 100f);
        float noiseY = Random.Range(0f, 100f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = (Mathf.PerlinNoise(noiseX, elapsed * 10f) - 0.5f) * magnitude;
            float y = (Mathf.PerlinNoise(noiseY, elapsed * 10f) - 0.5f) * magnitude;

            Camera.main.transform.localPosition = originalPosition + new Vector3(x, y, 0);
            yield return null;
        }

        Camera.main.transform.localPosition = originalPosition;
    }

    public void ChangePosition(Vector3 position)
    {
        transform.position = position;
    }

    public float GetDistanceToPlayer(Vector3 otherPosition)
    {
        return Vector3.Distance(otherPosition, Instance.transform.position);
    }

    public Quaternion GetCameraRotation()
    {
        return Camera.main != null ? Camera.main.transform.localRotation : Quaternion.identity;
    }

    public void OnDeath()
    {
        Destroy(rayLine.gameObject);
        isPeeking = false;
        UpdatePeekOffset();
    }

    public void DrawRay(Vector3 start, Vector3 direction)
    {
        if (!rayLine)
        {
            var rayObject = new GameObject("PlayerRay");
            rayObject.transform.SetParent(transform);
            rayLine = rayObject.AddComponent<LineRenderer>();

            rayLine.positionCount = 2;

            rayLine.startWidth = 0.05f;
            rayLine.endWidth = 0.05f;
            rayLine.material = new Material(Shader.Find("Sprites/Default"));

            var color = Color.blue;
            rayLine.startColor = color;
            color.a = 0f;
            rayLine.endColor = color;

            //FigureOutlineController.trackedObjects.Add(rayObject);
        }

        rayLine.SetPosition(0, start);
        rayLine.SetPosition(1, start + direction);
    }

    public float GetDistanceToCursor()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldCursor = ray.GetPoint(distance);
            return Vector3.Distance(transform.position, worldCursor);
        }

        return 0f;
    }


    public static void NoClipOn()
    {
        if (Instance == null) return;
        if (noClipLayer < 0) noClipLayer = LayerMask.NameToLayer(NoClipLayerName);
        if (isNoClipOn) return;

        isNoClipOn = true;
        prevLayer = Instance.gameObject.layer;
        SetLayerRecursively(Instance.gameObject, noClipLayer);

        for (int layer = 0; layer < 32; layer++)
        {
            if (layer == noClipLayer) continue;
            Physics.IgnoreLayerCollision(noClipLayer, layer, true);
        }

        Instance.heavyGravity = 0;
        Instance.velocity.y = 0f;
        Debug.Log("Noclip ON: Z - up; X - down.");
    }

    public static void NoClipOff()
    {
        if (Instance == null || !isNoClipOn) return;
        isNoClipOn = false;

        if (noClipLayer < 0) noClipLayer = LayerMask.NameToLayer(NoClipLayerName);

        if (noClipLayer >= 0)
        {
            for (int layer = 0; layer < 32; layer++)
            {
                if (layer == noClipLayer) continue;
                Physics.IgnoreLayerCollision(noClipLayer, layer, false);
            }
        }

        Instance.heavyGravity = -100;
        if (prevLayer >= 0) SetLayerRecursively(Instance.gameObject, prevLayer);
        Debug.Log("Noclip OFF.");
    }

    private void NoClipMove()
    {
        if (!isPlayerMovable) return;

        bool moveForward  = Input.GetKey(KeyCode.W);
        bool moveBackward = Input.GetKey(KeyCode.S);
        bool moveLeft     = Input.GetKey(KeyCode.A);
        bool moveRight    = Input.GetKey(KeyCode.D);
        bool moveUp       = Input.GetKey(KeyCode.Z);
        bool moveDown     = Input.GetKey(KeyCode.X);

        Vector3 dir = Vector3.zero;
        if (moveForward)  dir += Vector3.forward;
        if (moveBackward) dir -= Vector3.forward;
        if (moveRight)    dir += Vector3.right;
        if (moveLeft)     dir -= Vector3.right;
        if (moveUp)       dir += Vector3.up;
        if (moveDown)     dir -= Vector3.up;

        if (dir.sqrMagnitude > 0f) dir.Normalize();

        bool run = canRun && Input.GetKey(KeyCode.LeftShift);
        float speed = (moveSpeed - Mathf.Max(0, moveSpeedDeBuff)) * noClipSpeedMultiplier * (run ? 1.5f : 1f);

        characterController.Move(dir * speed * Time.deltaTime);
        velocity.y = 0f;
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}