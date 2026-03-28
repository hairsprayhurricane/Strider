using System.Collections;
using UnityEngine;

public class CanvasSelectorController : MonoBehaviour
{
    [Header("Selectors")]
    public RectTransform weaponSelector;
    public RectTransform itemSelector;
    public RectTransform ammunitionInfo;

    [Header("Settings")]
    public float slideDuration = 0.4f;

    private RectTransform canvasRect;

    void Awake()
    {
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    }

    public enum SlideDirection
    {
        FromTop,
        FromBottom,
        FromLeft,
        FromRight,
    }

    private bool isOpen = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            StopAllCoroutines();
            StartCoroutine(SlideIn(weaponSelector, SlideDirection.FromTop));
            StartCoroutine(SlideIn(itemSelector,   SlideDirection.FromBottom));
            StartCoroutine(SlideIn(ammunitionInfo, SlideDirection.FromRight));

            isOpen = true;
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            StopAllCoroutines();
            StartCoroutine(SlideOut(weaponSelector, SlideDirection.FromTop));
            StartCoroutine(SlideOut(itemSelector,   SlideDirection.FromBottom));
            StartCoroutine(SlideOut(ammunitionInfo, SlideDirection.FromRight));

            isOpen = false;
        }
    }

    private Vector2 GetOffsetByDirection(SlideDirection direction)
    {
        float w = canvasRect.rect.width;
        float h = canvasRect.rect.height;

        return direction switch
        {
            SlideDirection.FromTop    => new Vector2(0,  h),
            SlideDirection.FromBottom => new Vector2(0, -h),
            SlideDirection.FromLeft   => new Vector2(-w, 0),
            SlideDirection.FromRight  => new Vector2( w, 0),
            _ => Vector2.zero
        };
    }

    private IEnumerator SlideIn(RectTransform target, SlideDirection direction)
    {
        target.gameObject.SetActive(true);

        Vector2 offset    = GetOffsetByDirection(direction);
        Vector2 startPos  = offset;
        Vector2 targetPos = offset / 2.5f;

        target.anchoredPosition = startPos;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            target.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        target.anchoredPosition = targetPos;
    }

    private IEnumerator SlideOut(RectTransform target, SlideDirection direction)
    {
        Vector2 offset    = GetOffsetByDirection(direction);
        Vector2 startPos  = offset / 2.5f;
        Vector2 targetPos = offset;

        target.anchoredPosition = startPos;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            target.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        target.anchoredPosition = targetPos;
        target.gameObject.SetActive(false);
    }
}