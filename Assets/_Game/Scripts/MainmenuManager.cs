using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private List<RectTransform> swimPoints = new List<RectTransform>();
    private float currentSpeedMultiplier = 1f;
    private bool isBoosted;
    private bool interruptMovement;
    private Coroutine boostTimerCoroutine;

    private void Start()
    {
        CollectSwimPoints();
        SetupTouchArea();

        if (swimPoints.Count == 0 || fishRect == null)
            return;

        StartCoroutine(FishBehaviour());
    }

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
}
