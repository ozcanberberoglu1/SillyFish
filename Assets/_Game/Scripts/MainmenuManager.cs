using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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

    [Header("Play Button")]
    [SerializeField] private Button playButton;

    [Header("Cave")]
    [SerializeField] private RectTransform cavePoint;
    [SerializeField] private float caveSwimSpeed = 300f;

    [Header("Sponge Settings")]
    [SerializeField] private Button clearButton;
    [SerializeField] private RectTransform spongeRect;
    [SerializeField] private float spongeAutoHideTime = 5f;
    [SerializeField] private float cleanSpeed = 0.5f;

    [Header("Hunger & Health")]
    [SerializeField] private Image hungerFillImage;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private RectTransform deadPointRect;
    [SerializeField] private float hungerDecreaseInterval = 60f;
    [SerializeField] private float hungerDecreaseAmount = 0.1f;
    [SerializeField] private float healthDecreaseInterval = 5f;
    [SerializeField] private float healthDecreaseAmount = 0.1f;
    [SerializeField] private float healthIncreaseInterval = 10f;
    [SerializeField] private float healthIncreaseAmount = 0.1f;
    [SerializeField] private float healthRecoveryHungerThreshold = 0.5f;
    [SerializeField] private float deathFallSpeed = 80f;

    [Header("Food")]
    [SerializeField] private Button foodButton;
    [SerializeField] private TextMeshProUGUI foodCountText;
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private RectTransform foodSpawnArea;
    [SerializeField] private RectTransform foodActiveAreaRect;
    [SerializeField] private RectTransform foodFinishAreaRect;
    [SerializeField] private int startingFoodCount = 10;
    [SerializeField] private float foodFallSpeed = 50f;
    [SerializeField] private float fishChaseSpeed = 400f;
    [SerializeField] private float hungerIncreasePerFood = 0.1f;
    [SerializeField] private float eatAnimDuration = 1f;
    [SerializeField] private float foodFadeOutDuration = 2f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    // Fish movement
    private List<RectTransform> swimPoints = new List<RectTransform>();
    private float currentSpeedMultiplier = 1f;
    private bool isBoosted;
    private bool interruptMovement;
    private Coroutine boostTimerCoroutine;

    // Dirt & Sponge
    private List<GameObject> dirtObjects = new List<GameObject>();
    private Vector2 spongeStartPos;
    private bool isDraggingSponge;
    private float lastSpongeInteractTime;
    private Canvas parentCanvas;
    private Camera canvasCamera;

    // Hunger & Health
    private float hungerValue = 1f;
    private float healthValue = 1f;
    private float lastEatTime;
    private bool isDead;

    // Food
    private int foodCount;
    private List<RectTransform> availableFoods = new List<RectTransform>();
    private RectTransform currentChaseTarget;

    // Cave
    private bool isGoingToCave;

    // Level text
    private Transform menuLvTextCanvas;
    private float origMenuLvTextLocalX;
    private TextMeshProUGUI menuLvTextTMP;

    private void Start()
    {
        CollectSwimPoints();
        SetupTouchArea();
        SetupDirtSystem();
        SetupSponge();
        SetupHungerHealth();
        SetupFoodSystem();
        SetupPlayButton();
        LoadAndDisplayLevel();

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
        if (isDead) return;

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
            if (isDead) yield break;

            RectTransform food = GetNearestAvailableFood();
            if (food != null && !IsHungerFull())
            {
                yield return StartCoroutine(ChaseFoodRoutine(food));
                continue;
            }

            interruptMovement = false;

            RectTransform target = PickRandomPoint();
            yield return StartCoroutine(MoveToPoint(target));

            if (interruptMovement) continue;

            SetAnimState("Locomotion", 0f);

            float waitMin = isBoosted ? boostMinIdleTime : minIdleTime;
            float waitMax = isBoosted ? boostMaxIdleTime : maxIdleTime;
            float waitTime = Random.Range(waitMin, waitMax);

            float waited = 0f;
            while (waited < waitTime)
            {
                if (isDead) yield break;
                if (interruptMovement) break;
                if (GetNearestAvailableFood() != null && !IsHungerFull()) break;
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
        SetAnimState("Locomotion", 1f);
        Vector2 targetPos = target.anchoredPosition;

        FlipFish(targetPos);

        while (Vector2.Distance(fishRect.anchoredPosition, targetPos) > arrivalThreshold)
        {
            if (interruptMovement || isDead) yield break;

            fishRect.anchoredPosition = Vector2.MoveTowards(
                fishRect.anchoredPosition,
                targetPos,
                moveSpeed * currentSpeedMultiplier * Time.deltaTime
            );
            yield return null;
        }

        fishRect.anchoredPosition = targetPos;
    }

    private void FlipFish(Vector2 targetWorldOrLocal)
    {
        float direction = targetWorldOrLocal.x - fishRect.anchoredPosition.x;
        if (Mathf.Abs(direction) < 0.1f) return;

        Vector3 scale = fishRect.localScale;
        scale.x = direction > 0 ? -1f : 1f;
        fishRect.localScale = scale;

        KeepLvTextUpright();
    }

    private void KeepLvTextUpright()
    {
        if (menuLvTextCanvas == null) return;

        float flipX = fishRect.localScale.x;

        Vector3 s = menuLvTextCanvas.localScale;
        float absX = Mathf.Abs(s.x);
        s.x = flipX >= 0 ? absX : -absX;
        menuLvTextCanvas.localScale = s;

        Vector3 p = menuLvTextCanvas.localPosition;
        p.x = flipX >= 0 ? origMenuLvTextLocalX : -origMenuLvTextLocalX;
        menuLvTextCanvas.localPosition = p;
    }

    private bool IsHungerFull()
    {
        return hungerValue >= 0.99f;
    }

    private void SetAnimState(string stateName, float speed = -1f)
    {
        if (fishAnimator == null) return;

        if (stateName == "Locomotion")
        {
            if (speed >= 0f)
                fishAnimator.SetFloat(SpeedHash, speed);
            fishAnimator.CrossFade("Locomotion", 0.15f);
        }
        else
        {
            fishAnimator.CrossFade(stateName, 0.1f, 0, 0f);
        }
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
                StartCoroutine(FadeInDirt(inactiveDirts[index]));
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

    #region Hunger & Health System

    private void SetupHungerHealth()
    {
        hungerValue = 1f;
        healthValue = 1f;
        lastEatTime = Time.time;
        UpdateSliders();

        StartCoroutine(HungerRoutine());
        StartCoroutine(HealthRoutine());
    }

    private void UpdateSliders()
    {
        if (hungerFillImage != null)
            hungerFillImage.fillAmount = hungerValue;
        if (healthFillImage != null)
            healthFillImage.fillAmount = healthValue;
    }

    private IEnumerator HungerRoutine()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(hungerDecreaseInterval);

            if (isDead) yield break;

            if (Time.time - lastEatTime >= hungerDecreaseInterval)
            {
                hungerValue = Mathf.Max(0f, hungerValue - hungerDecreaseAmount);
                UpdateSliders();
            }
        }
    }

    private IEnumerator HealthRoutine()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(1f);

            if (isDead) yield break;

            if (hungerValue <= 0f)
            {
                yield return HealthDamagePhase();
            }
            else if (hungerValue >= healthRecoveryHungerThreshold && healthValue < 1f)
            {
                yield return HealthRecoveryPhase();
            }
        }
    }

    private IEnumerator HealthDamagePhase()
    {
        while (hungerValue <= 0f && !isDead)
        {
            healthValue = Mathf.Max(0f, healthValue - healthDecreaseAmount);
            UpdateSliders();

            if (healthValue <= 0f)
            {
                StartCoroutine(Die());
                yield break;
            }

            yield return new WaitForSeconds(healthDecreaseInterval);
        }
    }

    private IEnumerator HealthRecoveryPhase()
    {
        while (hungerValue >= healthRecoveryHungerThreshold && healthValue < 1f && !isDead)
        {
            healthValue = Mathf.Min(1f, healthValue + healthIncreaseAmount);
            UpdateSliders();
            yield return new WaitForSeconds(healthIncreaseInterval);
        }
    }

    private IEnumerator Die()
    {
        isDead = true;
        interruptMovement = true;

        SetAnimState("FishDeadAnim");

        if (deadPointRect != null && fishRect != null)
        {
            Vector3 deadWorld = deadPointRect.position;
            RectTransform fishParent = fishRect.parent as RectTransform;
            Vector3 localDead = fishParent.InverseTransformPoint(deadWorld);
            Vector2 targetPos = new Vector2(localDead.x, localDead.y);

            while (Vector2.Distance(fishRect.anchoredPosition, targetPos) > 2f)
            {
                fishRect.anchoredPosition = Vector2.MoveTowards(
                    fishRect.anchoredPosition,
                    targetPos,
                    deathFallSpeed * Time.deltaTime
                );
                yield return null;
            }

            fishRect.anchoredPosition = targetPos;
        }
    }

    #endregion

    #region Food System

    private void SetupFoodSystem()
    {
        foodCount = startingFoodCount;
        UpdateFoodCountText();

        if (foodButton != null)
            foodButton.onClick.AddListener(OnFoodButtonClicked);
    }

    private void UpdateFoodCountText()
    {
        if (foodCountText != null)
            foodCountText.text = $"x{foodCount}";
    }

    private void OnFoodButtonClicked()
    {
        if (isDead) return;
        if (foodCount <= 0) return;
        if (foodPrefab == null || foodSpawnArea == null) return;

        foodCount--;
        UpdateFoodCountText();

        SpawnFood();
    }

    private void SpawnFood()
    {
        RectTransform spawnParent = fishRect.parent as RectTransform;
        GameObject food = Instantiate(foodPrefab, spawnParent);
        RectTransform foodRect = food.GetComponent<RectTransform>();

        Vector3 spawnWorld = foodSpawnArea.position;
        Vector3 localSpawn = spawnParent.InverseTransformPoint(spawnWorld);

        float halfWidth = foodSpawnArea.rect.width * 0.4f;
        float randomX = localSpawn.x + Random.Range(-halfWidth, halfWidth);

        foodRect.anchoredPosition = new Vector2(randomX, localSpawn.y);
        foodRect.localScale = Vector3.one;

        StartCoroutine(FoodLifecycle(foodRect));
    }

    private IEnumerator FoodLifecycle(RectTransform food)
    {
        if (food == null) yield break;

        RectTransform fishParent = fishRect.parent as RectTransform;
        bool markedAvailable = false;
        bool stopped = false;

        while (food != null && food.gameObject != null)
        {
            float activeY = GetLocalY(foodActiveAreaRect, fishParent);
            float finishY = GetLocalY(foodFinishAreaRect, fishParent);

            if (!markedAvailable && food.anchoredPosition.y <= activeY)
            {
                markedAvailable = true;
                availableFoods.Add(food);
                if (!IsHungerFull())
                    interruptMovement = true;
            }

            if (!stopped)
            {
                if (food.anchoredPosition.y <= finishY)
                {
                    stopped = true;
                    food.anchoredPosition = new Vector2(food.anchoredPosition.x, finishY);
                }
                else
                {
                    food.anchoredPosition += Vector2.down * foodFallSpeed * Time.deltaTime;
                }
            }

            if (stopped)
            {
                availableFoods.Remove(food);

                yield return new WaitForSeconds(10f);

                if (food != null)
                    yield return FadeOutAndDestroy(food);

                yield break;
            }

            yield return null;
        }
    }

    private float GetLocalY(RectTransform worldRef, RectTransform localParent)
    {
        if (worldRef == null || localParent == null) return 0f;
        Vector3 local = localParent.InverseTransformPoint(worldRef.position);
        return local.y;
    }

    private IEnumerator FadeOutAndDestroy(RectTransform food)
    {
        Image img = food.GetComponent<Image>();
        if (img == null)
        {
            Destroy(food.gameObject);
            yield break;
        }

        float elapsed = 0f;
        Color c = img.color;
        float startAlpha = c.a;

        while (elapsed < foodFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, 0f, elapsed / foodFadeOutDuration);
            img.color = c;
            yield return null;
        }

        Destroy(food.gameObject);
    }

    private RectTransform GetNearestAvailableFood()
    {
        availableFoods.RemoveAll(f => f == null || !f.gameObject.activeSelf);

        if (availableFoods.Count == 0) return null;

        RectTransform nearest = null;
        float minDist = float.MaxValue;

        foreach (var food in availableFoods)
        {
            float dist = Vector2.Distance(fishRect.position, food.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = food;
            }
        }

        return nearest;
    }

    private IEnumerator ChaseFoodRoutine(RectTransform food)
    {
        if (food == null || IsHungerFull()) yield break;

        currentChaseTarget = food;
        SetAnimState("Locomotion", 1f);

        while (food != null && food.gameObject.activeSelf)
        {
            if (isDead || IsHungerFull()) yield break;

            Vector3 localFood = ((RectTransform)fishRect.parent).InverseTransformPoint(food.position);
            Vector2 targetPos = new Vector2(localFood.x, localFood.y);

            FlipFish(targetPos);

            fishRect.anchoredPosition = Vector2.MoveTowards(
                fishRect.anchoredPosition,
                targetPos,
                fishChaseSpeed * Time.deltaTime
            );

            if (Vector2.Distance(fishRect.anchoredPosition, targetPos) < arrivalThreshold)
            {
                yield return StartCoroutine(EatFood(food));
                yield break;
            }

            yield return null;
        }

        currentChaseTarget = null;
    }

    private IEnumerator EatFood(RectTransform food)
    {
        if (food == null || IsHungerFull())
        {
            currentChaseTarget = null;
            yield break;
        }

        availableFoods.Remove(food);
        Destroy(food.gameObject);

        currentChaseTarget = null;

        SetAnimState("FishFoodAnim");
        yield return new WaitForSeconds(eatAnimDuration);

        hungerValue = Mathf.Min(1f, hungerValue + hungerIncreasePerFood);
        lastEatTime = Time.time;
        UpdateSliders();

        bool hasMoreFood = GetNearestAvailableFood() != null && !IsHungerFull();

        if (hasMoreFood)
            SetAnimState("Locomotion", 1f);
        else
            SetAnimState("Locomotion", 0f);
    }

    #endregion

    #region Play Button

    private void SetupPlayButton()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
    }

    private void OnPlayButtonClicked()
    {
        if (isDead || isGoingToCave) return;
        isGoingToCave = true;
        interruptMovement = true;
        StopAllCoroutines();
        StartCoroutine(SwimToCaveAndStart());
    }

    private IEnumerator SwimToCaveAndStart()
    {
        if (cavePoint == null || fishRect == null)
        {
            SceneManager.LoadScene("GameScene");
            yield break;
        }

        if (playButton != null) playButton.interactable = false;

        SetAnimState("Locomotion", 1f);
        RectTransform fishParent = fishRect.parent as RectTransform;

        while (true)
        {
            Vector3 localCave = fishParent.InverseTransformPoint(cavePoint.position);
            Vector2 targetPos = new Vector2(localCave.x, localCave.y);

            FlipFish(targetPos);

            fishRect.anchoredPosition = Vector2.MoveTowards(
                fishRect.anchoredPosition,
                targetPos,
                caveSwimSpeed * Time.deltaTime
            );

            if (Vector2.Distance(fishRect.anchoredPosition, targetPos) < arrivalThreshold)
            {
                SceneManager.LoadScene("GameScene");
                yield break;
            }

            yield return null;
        }
    }

    #endregion

    #region Level Display

    private void LoadAndDisplayLevel()
    {
        int savedLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        int deathLevel = PlayerPrefs.GetInt("DeathLevel", 0);

        if (fishAnimator == null) return;
        menuLvTextCanvas = fishAnimator.transform.Find("LvTextCanvas");
        if (menuLvTextCanvas == null) return;

        origMenuLvTextLocalX = menuLvTextCanvas.localPosition.x;

        var lvTextObj = menuLvTextCanvas.Find("LvText");
        if (lvTextObj == null) return;

        menuLvTextTMP = lvTextObj.GetComponent<TextMeshProUGUI>();
        if (menuLvTextTMP == null) return;

        if (deathLevel > savedLevel)
        {
            menuLvTextTMP.text = $"Lv{deathLevel}";
            StartCoroutine(AnimateMenuLevelDrop(deathLevel, savedLevel));
        }
        else
        {
            menuLvTextTMP.text = $"Lv{savedLevel}";
            PlayerPrefs.DeleteKey("DeathLevel");
        }
    }

    private IEnumerator AnimateMenuLevelDrop(int from, int to)
    {
        yield return new WaitForSeconds(1.5f);

        int current = from;
        while (current > to)
        {
            yield return new WaitForSeconds(0.5f);
            current--;
            if (menuLvTextTMP != null)
                menuLvTextTMP.text = $"Lv{current}";
        }

        PlayerPrefs.DeleteKey("DeathLevel");
        PlayerPrefs.Save();
    }

    #endregion
}
