using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private List<RectTransform> swimPoints = new List<RectTransform>();

    private void Start()
    {
        CollectSwimPoints();

        if (swimPoints.Count == 0 || fishRect == null)
            return;

        StartCoroutine(FishBehaviour());
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
            RectTransform target = PickRandomPoint();
            yield return StartCoroutine(MoveToPoint(target));

            SetSwimming(false);

            float waitTime = Random.Range(minIdleTime, maxIdleTime);
            yield return new WaitForSeconds(waitTime);
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
            fishRect.anchoredPosition = Vector2.MoveTowards(
                fishRect.anchoredPosition,
                targetPos,
                moveSpeed * Time.deltaTime
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
