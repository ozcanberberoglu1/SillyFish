using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    [Header("Fish")]
    [SerializeField] private RectTransform fishRect;
    [SerializeField] private Animator fishAnimator;
    [SerializeField] private float moveSpeed = 300f;

    [Header("Joystick")]
    [SerializeField] private RectTransform joystickBase;
    [SerializeField] private RectTransform joystickHandle;

    [Header("Background & Camera")]
    [SerializeField] private RectTransform bgRect;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private float cameraSmoothSpeed = 12f;

    [Header("Food")]
    [SerializeField] private List<GameObject> foodPrefabs;
    [SerializeField] private RectTransform foodsPanel;
    [SerializeField] private int minFoodCount = 10;
    [SerializeField] private int maxFoodCount = 13;
    [SerializeField] private float foodScale = 1f;
    [SerializeField] private float eatDistance = 80f;
    [SerializeField] private float eatAnimDuration = 0.8f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private Vector2 joystickDirection;
    private float joystickRadius;
    private Camera canvasCamera;
    private RectTransform canvasRect;

    private Vector2 bgStartPos;
    private Vector2 worldPosition;

    private List<RectTransform> activeFoods = new List<RectTransform>();
    private bool isEating;

    private void Start()
    {
        if (joystickBase != null)
            joystickRadius = joystickBase.sizeDelta.x * 0.5f;

        if (bgRect != null)
            bgStartPos = bgRect.anchoredPosition;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                canvasCamera = canvas.worldCamera;
        }

        SetupJoystick();
        SpawnFoods();
    }

    #region Joystick

    private void SetupJoystick()
    {
        if (joystickBase == null) return;

        var trigger = joystickBase.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = joystickBase.gameObject.AddComponent<EventTrigger>();

        var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDown.callback.AddListener(data => OnJoystickDown((PointerEventData)data));
        trigger.triggers.Add(pointerDown);

        var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        drag.callback.AddListener(data => OnJoystickDrag((PointerEventData)data));
        trigger.triggers.Add(drag);

        var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUp.callback.AddListener(_ => OnJoystickUp());
        trigger.triggers.Add(pointerUp);
    }

    private void OnJoystickDown(PointerEventData eventData)
    {
        OnJoystickDrag(eventData);
    }

    private void OnJoystickDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBase, eventData.position, canvasCamera, out Vector2 localPos);

        Vector2 clamped = Vector2.ClampMagnitude(localPos, joystickRadius);
        joystickHandle.anchoredPosition = clamped;
        joystickDirection = clamped / joystickRadius;
    }

    private void OnJoystickUp()
    {
        joystickHandle.anchoredPosition = Vector2.zero;
        joystickDirection = Vector2.zero;
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        if (fishRect == null || bgRect == null) return;

        UpdateWorldPosition();
        FlipFish();
        UpdateAnimation();
        UpdateCamera();
        CheckFoodEat();
    }

    private void UpdateWorldPosition()
    {
        if (joystickDirection.sqrMagnitude < 0.01f) return;

        worldPosition += joystickDirection * moveSpeed * Time.deltaTime;
        ClampWorldPosition();
    }

    private void ClampWorldPosition()
    {
        if (canvasRect == null) return;

        float halfScreenW = canvasRect.rect.width * 0.5f;
        float halfScreenH = canvasRect.rect.height * 0.5f;
        float halfBGW = bgRect.sizeDelta.x * 0.5f;
        float halfBGH = bgRect.sizeDelta.y * 0.5f;

        float maxX = halfBGW - halfScreenW;
        float maxY = halfBGH - halfScreenH;

        worldPosition.x = Mathf.Clamp(worldPosition.x, -maxX, maxX);
        worldPosition.y = Mathf.Clamp(worldPosition.y, -maxY, maxY);
    }

    private void FlipFish()
    {
        if (Mathf.Abs(joystickDirection.x) < 0.1f) return;

        Vector3 scale = fishRect.localScale;
        float absX = Mathf.Abs(scale.x);
        scale.x = joystickDirection.x > 0 ? -absX : absX;
        fishRect.localScale = scale;
    }

    private void UpdateAnimation()
    {
        if (fishAnimator == null || isEating) return;

        float speed = joystickDirection.sqrMagnitude > 0.01f ? 1f : 0f;
        fishAnimator.SetFloat(SpeedHash, speed);
    }

    private void UpdateCamera()
    {
        Vector2 targetBGPos = bgStartPos - worldPosition;
        bgRect.anchoredPosition = Vector2.Lerp(bgRect.anchoredPosition, targetBGPos, cameraSmoothSpeed * Time.deltaTime);
    }

    #endregion

    #region Food System

    private void SpawnFoods()
    {
        if (foodPrefabs == null || foodPrefabs.Count == 0 || foodsPanel == null) return;

        int count = Random.Range(minFoodCount, maxFoodCount + 1);
        Rect panelRect = foodsPanel.rect;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = foodPrefabs[Random.Range(0, foodPrefabs.Count)];
            GameObject food = Instantiate(prefab, foodsPanel);
            RectTransform foodRect = food.GetComponent<RectTransform>();

            float x = Random.Range(panelRect.xMin * 0.9f, panelRect.xMax * 0.9f);
            float y = Random.Range(panelRect.yMin * 0.9f, panelRect.yMax * 0.9f);
            foodRect.anchoredPosition = new Vector2(x, y);
            foodRect.localScale = Vector3.one * foodScale;

            activeFoods.Add(foodRect);
        }
    }

    private void CheckFoodEat()
    {
        for (int i = activeFoods.Count - 1; i >= 0; i--)
        {
            if (activeFoods[i] == null)
            {
                activeFoods.RemoveAt(i);
                continue;
            }

            float dist = Vector2.Distance(fishRect.position, activeFoods[i].position);
            if (dist < eatDistance)
            {
                StartCoroutine(EatFood(activeFoods[i]));
                activeFoods.RemoveAt(i);
                break;
            }
        }
    }

    private IEnumerator EatFood(RectTransform food)
    {
        isEating = true;

        if (food != null)
            Destroy(food.gameObject);

        if (fishAnimator != null)
            fishAnimator.CrossFade("FishFoodAnim", 0.1f, 0, 0f);

        yield return new WaitForSeconds(eatAnimDuration);

        isEating = false;

        if (fishAnimator != null)
            fishAnimator.CrossFade("Locomotion", 0.15f);
    }

    #endregion
}
