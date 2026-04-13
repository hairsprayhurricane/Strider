using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
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

    [Header("First Person")]
    public Vector3 fpCameraOffset = new Vector3(0f, 1.7f, 0f);
    public float fpTransitionDuration = 0.8f;
    private const KeyCode fpToggleKey = KeyCode.V;
    private const float fpHoldDuration = 3f;

    public static bool isFirstPersonOn = false;
    private float fpYaw = 0f;
    private float fpPitch = 0f;
    private bool isFPTransitioning = false;
    private Coroutine fpHoldCoroutine;
    public static bool isFirstPersonModeAllowed = true;

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

    public Transform cameraHolder;

    private LineRenderer rayLine;

    public PlayerHealth playerHealth;

    private StealthShadowDetector shadowDetector;

    private static PlayerController _instance;
    public static PlayerController Instance { get { return _instance; } }

    public void Awake()
    {
        if (_instance != null && _instance != this)
            Destroy(this.gameObject);
        else
            _instance = this;
        shadowDetector = GetComponent<StealthShadowDetector>();
    }

    public void Start()
    {
        Time.timeScale = 1f;
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        Transform camTarget = Camera.main.transform;
        camTarget.position = transform.position + cameraOffset;
        camTarget.rotation = Quaternion.Euler(cameraTiltX, 0f, 0f);
    }

    void Update()
    {
        //Debug.Log(shadowDetector.darknessLevel);

        if (playerHealth.isDead)
        {
            UpdatePeekOffset();
            return;
        }

        if (Input.GetKeyDown(fpToggleKey) && isFirstPersonModeAllowed)
            fpHoldCoroutine = StartCoroutine(FPHoldCoroutine());
        if (Input.GetKeyUp(fpToggleKey) && fpHoldCoroutine != null)
        {
            StopCoroutine(fpHoldCoroutine);
            fpHoldCoroutine = null;
        }

        if (canRun && Input.GetKey(KeyCode.LeftShift))
            isRunning = true;
        else if (!Input.GetKey(KeyCode.LeftShift))
            isRunning = false;

        if (isFirstPersonOn)
        {
            FirstPersonMove();
            return;
        }

        Move();
        isPeeking = Input.GetMouseButton(1);
        UpdatePeekOffset();

        DrawRay(transform.position, transform.forward * GetDistanceToCursor());
    }

    void LateUpdate()
    {
        if (isFirstPersonOn)
        {
            if (!isFPTransitioning)
            {
                Transform cam = Camera.main.transform;
                cam.position = transform.position + fpCameraOffset;
                cam.rotation = Quaternion.Euler(fpPitch, fpYaw, 0f);
            }
            return;
        }

        Vector3 baseTarget = transform.position + cameraOffset;

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

        Vector3 targetPos = baseTarget + peekOffset;
        float speed = isPeeking ? peekFollowSpeed : peekReturnSpeed;

        Transform camTarget = Camera.main.transform;

        camTarget.position = Vector3.Lerp(camTarget.position, targetPos, speed * Time.deltaTime);
        camTarget.rotation = Quaternion.Euler(cameraTiltX, 0f, 0f);
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
        Transform camTransform = cameraHolder;

        float elapsed = 0f;
        float noiseX = Random.Range(0f, 100f);
        float noiseY = Random.Range(0f, 100f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - (elapsed / duration);

            float x = (Mathf.PerlinNoise(noiseX, elapsed * 10f) - 0.5f) * magnitude * fade;
            float y = (Mathf.PerlinNoise(noiseY, elapsed * 10f) - 0.5f) * magnitude * fade;

            camTransform.localPosition = new Vector3(x, y, 0f);
            yield return null;
        }

        camTransform.localPosition = Vector3.zero;
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
        ControllerOST.Instance.Stop();
        ShakeCamera(magnitude:10);
    }

    public void DrawRay(Vector3 start, Vector3 direction)
    {
        if (isFirstPersonOn) return;

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

    private IEnumerator FPHoldCoroutine()
    {
        yield return new WaitForSeconds(fpHoldDuration);
        fpHoldCoroutine = null;
        if (isFirstPersonOn)
            FirstPersonOff();
        else
            FirstPersonOn();
    }

    public static void FirstPersonOn()
    {
        if (Instance == null || isFirstPersonOn) return;
        isFirstPersonOn = true;
        Instance.fpYaw = Instance.transform.eulerAngles.y;
        Instance.fpPitch = 0f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Instance.StartCoroutine(Instance.FirstPersonTransition(true));

        Destroy(Instance.rayLine);
    }

    public static void FirstPersonOff()
    {
        if (Instance == null || !isFirstPersonOn) return;
        isFirstPersonOn = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Instance.StartCoroutine(Instance.FirstPersonTransition(false));
    }

    private IEnumerator FirstPersonTransition(bool entering)
    {
        isFPTransitioning = true;
        Transform cam = Camera.main.transform;
        cam.GetPositionAndRotation(out Vector3 startPos, out Quaternion startRot);

        Vector3 targetPos = entering
            ? transform.position + fpCameraOffset
            : transform.position + cameraOffset;
        Quaternion targetRot = entering
            ? Quaternion.Euler(0f, fpYaw, 0f)
            : Quaternion.Euler(cameraTiltX, 0f, 0f);

        float elapsed = 0f;
        while (elapsed < fpTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fpTransitionDuration);
            cam.SetPositionAndRotation(
                Vector3.Lerp(startPos, targetPos, t),
                Quaternion.Slerp(startRot, targetRot, t));
            yield return null;
        }

        cam.SetPositionAndRotation(targetPos, targetRot);
        isFPTransitioning = false;
    }

    private void FirstPersonMove()
    {
        if (!isPlayerMovable) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        fpYaw += mouseX;
        fpPitch -= mouseY;
        fpPitch = Mathf.Clamp(fpPitch, -maxCamY, maxCamY);

        transform.rotation = Quaternion.Euler(0f, fpYaw, 0f);

        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0f)
            velocity.y = 0f;

        bool moveForward  = Input.GetKey(KeyCode.W);
        bool moveBackward = Input.GetKey(KeyCode.S);
        bool moveLeft     = Input.GetKey(KeyCode.A);
        bool moveRight    = Input.GetKey(KeyCode.D);

        Vector3 dir = Vector3.zero;
        if (moveForward)  dir += transform.forward;
        if (moveBackward) dir -= transform.forward;
        if (moveLeft)     dir -= transform.right;
        if (moveRight)    dir += transform.right;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0f) dir.Normalize();

        float speed = isRunning
            ? (moveSpeed - Mathf.Max(0, moveSpeedDeBuff)) * 1.5f
            : (moveSpeed - Mathf.Max(0, moveSpeedDeBuff));

        if (dir.sqrMagnitude > 0f)
        {
            characterController.Move(Time.deltaTime * speed * dir);
            isWalking = true;
            walkCor ??= StartCoroutine(WalkCoroutine());
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

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    public StealthShadowDetector GetStealthShadowDetector() => shadowDetector;
}