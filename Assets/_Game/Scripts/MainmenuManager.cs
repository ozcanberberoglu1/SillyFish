using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainmenuManager : MonoBehaviour
{
    [Header("Fish")]
    [SerializeField] private RectTransform fishRect;
    [SerializeField] private Animator fishAnimator;

    [Header("Swim Points")]
    [SerializeField] private RectTransform swimPointsParent;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 150f;
    [SerializeField] private float minIdleTime = 1f;
    [SerializeField] private float maxIdleTime = 3f;
    [SerializeField] private float arrivalThreshold = 5f;

    [Header("Boost Settings")]
    [SerializeField] private GameObject touchArea;
    [SerializeField] private float boostMultiplier = 3f;
    [SerializeField] private float boostDuration = 5f;
    [SerializeField] private float boostMinIdleTime = 0f;
    [SerializeField] private float boostMaxIdleTime = 2f;

    [Header("Dirt Settings")]
    [SerializeField] private RectTransform kirlerParent;
    [SerializeField] private float dirtSpawnInterval = 60f;
    [SerializeField] private float dirtMaxAlpha = 0.5f;
    [SerializeField] private float dirtFadeInDuration = 2f;

    [Header("Sponge Settings")]
    [SerializeField] private Button clearButton;
    [SerializeField] private RectTransform spongeRect;
    [SerializeField] private float spongeAutoHideTime = 5f;
    [SerializeField] private float cleanSpeed = 0.5f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private List<RectTransform> swimPoints = new List<RectTransform>();
    private float currentSpeedMultiplier = 1f;
    private bool isBoosted;
    private bool interruptMovement;
    private Coroutine boostTimerCoroutine;

    private List<GameObject> dirtObjects = new List<GameObject>();
    private Vector2 spongeStartPos;
    private bool isDraggingSponge;
    private float lastSpongeInteractTime;
    private Canvas parentCanvas;
    private Camera canvasCamera;

    private void Start()
    {
        CollectSwimPoints();
        SetupTouchArea();
        SetupDirtSystem();
        SetupSponge();

        if (swimPoints.Count == 0 || fishRect == null)
            return;

        StartCoroutine(FishBehaviour());
    }

    #region Fish Movement

    private void SetupTouchArea()
    {
        if (touchArea == null) return;

        var trigger = touchArea.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = touchArea.AddComponent<EventTrigger>();

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener(_ => OnTouchAreaClicked());
        trigger.triggers.Add(entry);
    }

    private void OnTouchAreaClicked()
    {
        currentSpeedMultiplier = boostMultiplier;
        isBoosted = true;
        interruptMovement = true;

        if (boostTimerCoroutine != null)
            StopCoroutine(boostTimerCoroutine);
        boostTimerCoroutine = StartCoroutine(BoostTimer());
    }

    private IEnumerator BoostTimer()
    {
        yield return new WaitForSeconds(boostDuration);
        currentSpeedMultiplier = 1f;
        isBoosted = false;
        boostTimerCoroutine = null;
    }

    private void CollectSwimPoints()
    {
        if (swimPointsParent == null) return;

        for (int i = 0; i < swimPointsParent.childCount; i++)
            swimPoints.Add(swimPointsParent.GetChild(i) as RectTransform);
    }

    private IEnumerator FishBehaviour()
    {
        while (true)
        {
            interruptMovement = false;

            RectTransform target = PickRandomPoint();
            yield return StartCoroutine(MoveToPoint(target));

            if (interruptMovement) continue;

            SetSwimming(false);

            float waitMin = isBoosted ? boostMinIdleTime : minIdleTime;
            float waitMax = isBoosted ? boostMaxIdleTime : maxIdleTime;
            float waitTime = Random.Range(waitMin, waitMax);

            float waited = 0f;
            while (waited < waitTime)
            {
                if (interruptMovement) break;
                waited += Time.deltaTime;
                yield return null;
            }
        }
    }

    private RectTransform PickRandomPoint()
    {
        int index = Random.Range(0, swimPoints.Count);
        return swimPoints[index];
    }

    private IEnumerator MoveToPoint(RectTransform target)
    {
        SetSwimming(true);
        Vector2 targetPos = target.anchoredPosition;

        FlipFish(targetPos);

        while (Vector2.Distance(fishRect.anchoredPosition, targetPos) > arrivalThreshold)
        {
            if (interruptMovement) yield break;

            fishRect.anchoredPosition = Vector2.MoveTowards(
                fishRect.anchoredPosition,
                targetPos,
                moveSpeed * currentSpeedMultiplier * Time.deltaTime
            );
            yield return null;
        }

        fishRect.anchoredPosition = targetPos;
    }

    private void FlipFish(Vector2 targetPos)
    {
        float direction = targetPos.x - fishRect.anchoredPosition.x;
        if (Mathf.Abs(direction) < 0.1f) return;

        Vector3 scale = fishRect.localScale;
        scale.x = direction > 0 ? -1f : 1f;
        fishRect.localScale = scale;
    }

    private void SetSwimming(bool isSwimming)
    {
        if (fishAnimator != null)
            fishAnimator.SetFloat(SpeedHash, isSwimming ? 1f : 0f);
    }

    #endregion

    #region Dirt System

    private void SetupDirtSystem()
    {
        if (kirlerParent == null) return;

        for (int i = 0; i < kirlerParent.childCount; i++)
        {
            var dirt = kirlerParent.GetChild(i).gameObject;
            dirt.SetActive(false);
            dirtObjects.Add(dirt);
        }

        StartCoroutine(DirtSpawnRoutine());
    }

    private IEnumerator DirtSpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(dirtSpawnInterval);

            var inactiveDirts = dirtObjects.FindAll(d => !d.activeSelf);
            if (inactiveDirts.Count > 0)
            {
                int index = Random.Range(0, inactiveDirts.Count);
                var dirt = inactiveDirts[index];
                StartCoroutine(FadeInDirt(dirt));
            }
        }
    }

    private IEnumerator FadeInDirt(GameObject dirt)
    {
        var image = dirt.GetComponent<Image>();
        if (image == null) yield break;

        var c = image.color;
        c.a = 0f;
        image.color = c;
        dirt.SetActive(true);

        float elapsed = 0f;
        while (elapsed < dirtFadeInDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, dirtMaxAlpha, elapsed / dirtFadeInDuration);
            image.color = c;
            yield return null;
        }

        c.a = dirtMaxAlpha;
        image.color = c;
    }

    #endregion

    #region Sponge System

    private void SetupSponge()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
            parentCanvas = FindObjectOfType<Canvas>();

        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvasCamera = parentCanvas.worldCamera;

        if (clearButton != null)
            clearButton.onClick.AddListener(OnClearButtonClicked);

        if (spongeRect != null)
        {
            spongeStartPos = spongeRect.anchoredPosition;
            SetupSpongeEvents();
        }
    }

    private void SetupSpongeEvents()
    {
        var trigger = spongeRect.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = spongeRect.gameObject.AddComponent<EventTrigger>();

        var beginDrag = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        beginDrag.callback.AddListener(data => OnSpongeBeginDrag((PointerEventData)data));
        trigger.triggers.Add(beginDrag);

        var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        drag.callback.AddListener(data => OnSpongeDrag((PointerEventData)data));
        trigger.triggers.Add(drag);

        var endDrag = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        endDrag.callback.AddListener(data => OnSpongeEndDrag());
        trigger.triggers.Add(endDrag);
    }

    private void OnClearButtonClicked()
    {
        if (spongeRect == null) return;

        spongeRect.gameObject.SetActive(true);
        spongeRect.anchoredPosition = spongeStartPos;
        lastSpongeInteractTime = Time.time;
        StartCoroutine(SpongeAutoHideRoutine());
    }

    private void OnSpongeBeginDrag(PointerEventData eventData)
    {
        isDraggingSponge = true;
        lastSpongeInteractTime = Time.time;
    }

    private void OnSpongeDrag(PointerEventData eventData)
    {
        lastSpongeInteractTime = Time.time;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spongeRect.parent as RectTransform,
            eventData.position,
            canvasCamera,
            out Vector2 localPos
        );
        spongeRect.anchoredPosition = localPos;

        TryCleanDirts();
    }

    private void OnSpongeEndDrag()
    {
        isDraggingSponge = false;
        lastSpongeInteractTime = Time.time;
        spongeRect.anchoredPosition = spongeStartPos;
    }

    private void TryCleanDirts()
    {
        for (int i = dirtObjects.Count - 1; i >= 0; i--)
        {
            var dirt = dirtObjects[i];
            if (!dirt.activeSelf) continue;

            var dirtRect = dirt.GetComponent<RectTransform>();
            if (!RectsOverlap(spongeRect, dirtRect)) continue;

            var image = dirt.GetComponent<Image>();
            if (image == null) continue;

            var c = image.color;
            c.a -= cleanSpeed * Time.deltaTime;
            image.color = c;

            if (c.a <= 0f)
                dirt.SetActive(false);
        }
    }

    private bool RectsOverlap(RectTransform a, RectTransform b)
    {
        Vector3[] cornersA = new Vector3[4];
        Vector3[] cornersB = new Vector3[4];
        a.GetWorldCorners(cornersA);
        b.GetWorldCorners(cornersB);

        Rect rectA = new Rect(cornersA[0].x, cornersA[0].y,
            cornersA[2].x - cornersA[0].x, cornersA[2].y - cornersA[0].y);
        Rect rectB = new Rect(cornersB[0].x, cornersB[0].y,
            cornersB[2].x - cornersB[0].x, cornersB[2].y - cornersB[0].y);

        return rectA.Overlaps(rectB);
    }

    private IEnumerator SpongeAutoHideRoutine()
    {
        while (spongeRect != null && spongeRect.gameObject.activeSelf)
        {
            if (!isDraggingSponge && Time.time - lastSpongeInteractTime >= spongeAutoHideTime)
            {
                spongeRect.gameObject.SetActive(false);
                yield break;
            }
            yield return null;
        }
    }

    #endregion
}
