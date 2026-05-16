using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Fish")]
    [SerializeField] private RectTransform fishRect;
    [SerializeField] private Animator fishAnimator;
    [SerializeField] private float moveSpeed = 300f;

    [Header("Fish World Rendering")]
    [SerializeField] private Transform fishWorldTransform;
    [SerializeField] private Camera fishCamera;
    [SerializeField] private RawImage fishRenderImage;

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

    [Header("Player Level")]
    [SerializeField] private float baseScale = 0.5f;
    [SerializeField] private float scalePerLevel = 0.2f;
    [SerializeField] private int baseXPMultiplier = 5;
    [SerializeField] private float playerEatDistance = 50f;
    [SerializeField] private float enemyKillDistance = 15f;
    [SerializeField] private float respawnTimeMin = 5f;
    [SerializeField] private float respawnTimeMax = 7f;
    [SerializeField] private float cameraSizePerLevel = 0.5f;

    [Header("Enemy Fish")]
    [SerializeField] private List<EnemySpawnConfig> enemySpawnConfigs = new List<EnemySpawnConfig>();

    [Header("Portal")]
    [SerializeField] private RectTransform portalSpawnPoint;
    [SerializeField] private RectTransform portalNextPoint;
    [SerializeField] private float portalEnterDistance = 100f;

    [Header("Health")]
    [SerializeField] private int maxHP = 3;
    [SerializeField] private float healthSliderShowTime = 2f;
    [SerializeField] private float shakeAmount = 10f;
    [SerializeField] private float shakeDuration = 0.2f;

    [Header("Death Screen")]
    [SerializeField] private GameObject firstDeathScreen;

    [System.Serializable]
    public class EnemySpawnConfig
    {
        public GameObject prefab;
        public int level = 1;
        public int xp = 1;
        public int count = 5;
        public float speed = 150f;
        public float detectionRadius = 400f;
        public float chaseTime = 2.5f;
    }

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private Vector2 joystickDirection;
    private float joystickRadius;
    private Camera canvasCamera;
    private RectTransform canvasRect;

    private Vector2 bgStartPos;
    private Vector2 worldPosition;

    private List<RectTransform> activeFoods = new List<RectTransform>();
    private bool isEating;

    private int playerLevel = 1;
    private int currentXP;

    private List<EnemyFishAI> enemies = new List<EnemyFishAI>();
    private Transform enemyContainer;
    private RenderTexture fishRT;
    private float canvasToWorld;
    private Vector3 origWorldFishScale;
    private float origCameraSize;

    private Vector2 enemyBoundsMin;
    private Vector2 enemyBoundsMax;
    private TextMeshProUGUI playerLvText;
    private Transform playerLvTextCanvas;
    private float origPlayerLvTextLocalX;

    // Health
    private int currentHP;
    private bool isInvincible;
    private bool isDead;
    private bool usedSecondChance;
    private int entryLevel;
    private Image healthFillImage;
    private GameObject healthSliderObj;
    private Coroutine healthSliderHideCoroutine;

    // Flash shader
    private List<Renderer> fishRenderers = new List<Renderer>();
    private List<Material> originalMaterials = new List<Material>();
    private List<Material> flashMaterials = new List<Material>();
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");

    public Vector2 GetPlayerWorldPosition() => worldPosition;
    public int GetPlayerLevel() => playerLevel;

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

        playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        entryLevel = PlayerPrefs.GetInt("EntryLevel", playerLevel);

        if (fishWorldTransform != null)
        {
            origWorldFishScale = fishWorldTransform.localScale;
            FindPlayerLvText();
            FindHealthSlider();
            CacheRenderers();
        }

        currentHP = maxHP;
        if (healthSliderObj != null) healthSliderObj.SetActive(false);
        if (firstDeathScreen != null) firstDeathScreen.SetActive(false);

        SetupJoystick();
        SpawnFoods();
        SetupFishRendering();
        CalculateEnemyBounds();
        SpawnEnemies();
        UpdatePlayerScale();
        UpdatePlayerLevelText();
        MoveToSpawnPoint();

        PlayerPrefs.DeleteKey("DeathLevel");
        PlayerPrefs.SetInt("EntryLevel", playerLevel);
        PlayerPrefs.Save();
    }

    private void FindPlayerLvText()
    {
        playerLvTextCanvas = fishWorldTransform.Find("LvTextCanvas");
        if (playerLvTextCanvas == null) return;
        SetLayerRecursive(playerLvTextCanvas.gameObject, fishWorldTransform.gameObject.layer);
        origPlayerLvTextLocalX = playerLvTextCanvas.localPosition.x;
        var lvTextObj = playerLvTextCanvas.Find("LvText");
        if (lvTextObj != null)
            playerLvText = lvTextObj.GetComponent<TextMeshProUGUI>();
    }

    private void FindHealthSlider()
    {
        if (playerLvTextCanvas == null) return;
        var sliderT = playerLvTextCanvas.Find("HealthSlider");
        if (sliderT == null) return;
        healthSliderObj = sliderT.gameObject;
        var fillT = sliderT.Find("HealthSliderFill");
        if (fillT != null)
            healthFillImage = fillT.GetComponent<Image>();
    }

    private void CacheRenderers()
    {
        fishRenderers.Clear();
        originalMaterials.Clear();
        flashMaterials.Clear();
        var renderers = fishWorldTransform.GetComponentsInChildren<Renderer>(true);
        var flashShader = Shader.Find("Custom/SpriteWhiteFlash");

        foreach (var r in renderers)
        {
            fishRenderers.Add(r);
            originalMaterials.Add(r.material);

            if (flashShader != null)
            {
                var fm = new Material(flashShader);
                fm.mainTexture = r.material.mainTexture;
                fm.SetColor("_FlashColor", new Color(1f, 1f, 1f, 0.9f));
                fm.SetFloat(FlashAmountID, 0.85f);
                flashMaterials.Add(fm);
            }
        }
    }

    private void UpdatePlayerLevelText()
    {
        if (playerLvText == null) return;
        playerLvText.text = currentXP > 0
            ? $"Lv{playerLevel}.{currentXP}"
            : $"Lv{playerLevel}";
    }

    private void KeepTextUpright(Transform textCanvas, Transform parent, float origLocalX)
    {
        if (textCanvas == null) return;
        Vector3 s = textCanvas.localScale;
        float absX = Mathf.Abs(s.x);
        s.x = parent.localScale.x >= 0 ? absX : -absX;
        textCanvas.localScale = s;

        Vector3 p = textCanvas.localPosition;
        p.x = parent.localScale.x >= 0 ? origLocalX : -origLocalX;
        textCanvas.localPosition = p;
    }

    #region Fish World Rendering

    private void SetupFishRendering()
    {
        if (fishCamera == null) return;

        origCameraSize = fishCamera.orthographicSize;

        fishRT = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        fishRT.Create();
        fishCamera.targetTexture = fishRT;
        fishCamera.clearFlags = CameraClearFlags.SolidColor;
        fishCamera.backgroundColor = new Color(0, 0, 0, 0);

        if (fishRenderImage != null)
        {
            fishRenderImage.texture = fishRT;
            fishRenderImage.raycastTarget = false;
        }

        canvasToWorld = fishCamera.orthographicSize * 2f / Screen.height;
    }

    private void SyncEnemyWorldPositions()
    {
        if (fishWorldTransform == null || bgRect == null) return;

        Vector2 effectiveCamPos = bgStartPos - bgRect.anchoredPosition;
        Vector3 anchor = fishWorldTransform.position;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e == null) { enemies.RemoveAt(i); continue; }
            if (!e.gameObject.activeSelf) continue;

            Vector2 offset = e.canvasPosition - effectiveCamPos;
            e.transform.position = new Vector3(
                anchor.x + offset.x * canvasToWorld,
                anchor.y + offset.y * canvasToWorld,
                anchor.z);
        }
    }

    #endregion

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
        if (fishRect == null || bgRect == null || isDead) return;

        UpdateWorldPosition();
        FlipFish();
        UpdateAnimation();
        UpdateCamera();
        CheckFoodEat();
        SyncEnemyWorldPositions();
        CheckEnemyInteractions();
        CheckPortal();
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

        if (fishWorldTransform != null)
        {
            Vector3 ws = fishWorldTransform.localScale;
            float absWX = Mathf.Abs(ws.x);
            ws.x = joystickDirection.x > 0 ? -absWX : absWX;
            fishWorldTransform.localScale = ws;

            KeepTextUpright(playerLvTextCanvas, fishWorldTransform, origPlayerLvTextLocalX);
        }
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

    #region Enemy System

    private void CalculateEnemyBounds()
    {
        if (bgRect == null) return;
        float hw = bgRect.sizeDelta.x * 0.5f;
        float hh = bgRect.sizeDelta.y * 0.5f;
        enemyBoundsMin = new Vector2(-hw, -hh);
        enemyBoundsMax = new Vector2(hw, hh);
    }

    private void SpawnEnemies()
    {
        if (enemySpawnConfigs == null || enemySpawnConfigs.Count == 0) return;

        if (enemyContainer == null)
        {
            enemyContainer = new GameObject("EnemyFishContainer").transform;
            enemyContainer.localScale = Vector3.one * 0.1f;
        }

        foreach (var cfg in enemySpawnConfigs)
        {
            if (cfg.prefab == null) continue;

            for (int i = 0; i < cfg.count; i++)
            {
                Vector2 pos = RandomSpawnPosition();
                GameObject go = Instantiate(cfg.prefab, enemyContainer);
                SetLayerRecursive(go, 6);

                var ai = go.AddComponent<EnemyFishAI>();
                ai.level = cfg.level;
                ai.xp = cfg.xp;
                ai.moveSpeed = cfg.speed;
                ai.detectionRadius = cfg.detectionRadius;
                ai.chaseTime = cfg.chaseTime;
                ai.Init(this, pos, enemyBoundsMin, enemyBoundsMax);

                enemies.Add(ai);
            }
        }
    }

    private Vector2 RandomSpawnPosition()
    {
        float margin = 500f;
        Vector2 pos;
        int attempts = 0;
        do
        {
            pos = new Vector2(
                Random.Range(enemyBoundsMin.x + margin, enemyBoundsMax.x - margin),
                Random.Range(enemyBoundsMin.y + margin, enemyBoundsMax.y - margin));
            attempts++;
        } while (pos.magnitude < 800f && attempts < 30);

        return pos;
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform c in obj.transform)
            SetLayerRecursive(c.gameObject, layer);
    }

    private void CheckEnemyInteractions()
    {
        if (isEating || isInvincible) return;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e == null || !e.gameObject.activeSelf) continue;

            float d = Vector2.Distance(worldPosition, e.canvasPosition);

            if (e.level <= playerLevel && d <= playerEatDistance)
            {
                StartCoroutine(EatEnemy(e, i));
                return;
            }

            if (e.currentState == EnemyFishAI.FishState.Chasing && e.level > playerLevel && d <= enemyKillDistance)
            {
                e.PlayFood();
                StartCoroutine(ResumeEnemyAfterBite(e));
                TakeDamage();
                return;
            }
        }
    }

    private IEnumerator EatEnemy(EnemyFishAI enemy, int index)
    {
        isEating = true;

        currentXP += enemy.xp;
        UpdatePlayerLevelText();

        enemy.gameObject.SetActive(false);
        StartCoroutine(RespawnEnemy(enemy));

        if (fishAnimator != null)
            fishAnimator.CrossFade("FishFoodAnim", 0.1f, 0, 0f);

        yield return new WaitForSeconds(eatAnimDuration);

        isEating = false;

        if (fishAnimator != null)
            fishAnimator.CrossFade("Locomotion", 0.15f);

        int requiredXP = baseXPMultiplier * playerLevel;
        if (currentXP >= requiredXP)
        {
            currentXP -= requiredXP;
            playerLevel++;
            UpdatePlayerScale();
            UpdatePlayerLevelText();
        }
    }

    private IEnumerator RespawnEnemy(EnemyFishAI enemy)
    {
        yield return new WaitForSeconds(Random.Range(respawnTimeMin, respawnTimeMax));
        if (enemy == null) yield break;

        Vector2 newPos = RandomSpawnPosition();
        enemy.canvasPosition = newPos;

        if (fishWorldTransform != null && bgRect != null)
        {
            Vector2 effectiveCamPos = bgStartPos - bgRect.anchoredPosition;
            Vector2 offset = newPos - effectiveCamPos;
            Vector3 anchor = fishWorldTransform.position;
            enemy.transform.position = new Vector3(
                anchor.x + offset.x * canvasToWorld,
                anchor.y + offset.y * canvasToWorld,
                anchor.z);
        }

        enemy.gameObject.SetActive(true);
        enemy.Respawn(newPos, enemyBoundsMin, enemyBoundsMax);
    }

    private void UpdatePlayerScale()
    {
        float s = baseScale + (playerLevel - 1) * scalePerLevel;

        if (fishRect != null)
        {
            Vector3 ls = fishRect.localScale;
            float sign = ls.x >= 0 ? 1f : -1f;
            fishRect.localScale = new Vector3(sign * s, s, s);
        }

        if (fishWorldTransform != null && origWorldFishScale.sqrMagnitude > 0f)
        {
            Vector3 ws = fishWorldTransform.localScale;
            float wsign = ws.x >= 0 ? 1f : -1f;
            float absX = Mathf.Abs(origWorldFishScale.x);
            float absY = Mathf.Abs(origWorldFishScale.y);
            float absZ = Mathf.Abs(origWorldFishScale.z);
            fishWorldTransform.localScale = new Vector3(
                wsign * absX * s,
                absY * s,
                absZ * s);
        }

        if (fishCamera != null)
        {
            fishCamera.orthographicSize = origCameraSize + (playerLevel - 1) * cameraSizePerLevel;
            canvasToWorld = fishCamera.orthographicSize * 2f / Screen.height;
        }
    }

    #endregion

    private IEnumerator ResumeEnemyAfterBite(EnemyFishAI enemy)
    {
        yield return new WaitForSeconds(1f);
        if (enemy != null && enemy.gameObject.activeSelf)
            enemy.ResumeAfterBite();
    }

    #region Health & Damage

    private void TakeDamage()
    {
        if (isInvincible || isDead) return;

        currentHP--;
        UpdateHealthSlider();
        ShowHealthSlider();
        StartCoroutine(ShakeFish());

        if (currentHP <= 0)
        {
            StartCoroutine(HandleDeath());
        }
        else
        {
            StartCoroutine(DamageCooldown());
        }
    }

    private IEnumerator DamageCooldown()
    {
        isInvincible = true;
        yield return new WaitForSeconds(1.5f);
        isInvincible = false;
    }

    private void UpdateHealthSlider()
    {
        if (healthFillImage == null) return;
        healthFillImage.fillAmount = Mathf.Max(0f, currentHP * (1f / maxHP));
    }

    private void ShowHealthSlider()
    {
        if (healthSliderObj == null) return;
        healthSliderObj.SetActive(true);

        if (healthSliderHideCoroutine != null)
            StopCoroutine(healthSliderHideCoroutine);
        healthSliderHideCoroutine = StartCoroutine(HideHealthSliderAfterDelay());
    }

    private IEnumerator HideHealthSliderAfterDelay()
    {
        yield return new WaitForSeconds(healthSliderShowTime);
        if (healthSliderObj != null && currentHP > 0)
            healthSliderObj.SetActive(false);
    }

    private IEnumerator ShakeFish()
    {
        if (fishRect == null) yield break;

        Vector2 origPos = fishRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float offsetX = Random.Range(-shakeAmount, shakeAmount);
            float offsetY = Random.Range(-shakeAmount, shakeAmount);
            fishRect.anchoredPosition = origPos + new Vector2(offsetX, offsetY);
            elapsed += Time.deltaTime;
            yield return null;
        }

        fishRect.anchoredPosition = origPos;
    }

    private IEnumerator HandleDeath()
    {
        isDead = true;
        isEating = true;

        yield return new WaitForSeconds(0.5f);

        if (!usedSecondChance)
        {
            usedSecondChance = true;
            yield return StartCoroutine(FirstDeathSequence());
        }
        else
        {
            FinalDeath();
        }
    }

    private IEnumerator FirstDeathSequence()
    {
        if (firstDeathScreen != null)
        {
            firstDeathScreen.SetActive(true);

            var screenAnim = firstDeathScreen.GetComponentInChildren<Animator>();
            if (screenAnim != null)
            {
                screenAnim.Play(0);
                yield return null; // wait a frame so state info updates
                var clipInfo = screenAnim.GetCurrentAnimatorStateInfo(0);
                yield return new WaitForSeconds(clipInfo.length > 0.1f ? clipInfo.length : 2f);
            }
            else
            {
                yield return new WaitForSeconds(2f);
            }

            firstDeathScreen.SetActive(false);
        }

        currentHP = 1;
        UpdateHealthSlider();
        if (healthSliderObj != null) healthSliderObj.SetActive(false);

        isDead = false;
        isEating = false;

        yield return StartCoroutine(InvincibilityFlash());
    }

    private IEnumerator InvincibilityFlash()
    {
        isInvincible = true;

        SetFlash(true);
        yield return new WaitForSeconds(0.3f);
        SetFlash(false);
        yield return new WaitForSeconds(0.3f);
        SetFlash(true);
        yield return new WaitForSeconds(0.3f);
        SetFlash(false);

        isInvincible = false;
    }

    private void SetFlash(bool white)
    {
        for (int i = 0; i < fishRenderers.Count; i++)
        {
            if (fishRenderers[i] == null) continue;
            if (i >= flashMaterials.Count) continue;
            fishRenderers[i].material = white ? flashMaterials[i] : originalMaterials[i];
        }
    }

    private void FinalDeath()
    {
        int deathLevel = playerLevel;
        playerLevel = entryLevel;
        currentXP = 0;
        SavePlayerLevel();

        PlayerPrefs.SetInt("DeathLevel", deathLevel);
        PlayerPrefs.Save();

        SceneManager.LoadScene("MainmenuScene");
    }

    #endregion

    #region Portal & Save

    private void MoveToSpawnPoint()
    {
        if (portalNextPoint == null || bgRect == null) return;

        Vector3 localInBG = bgRect.InverseTransformPoint(portalNextPoint.position);
        worldPosition = new Vector2(localInBG.x, localInBG.y);
        ClampWorldPosition();
        bgRect.anchoredPosition = bgStartPos - worldPosition;
    }

    private void CheckPortal()
    {
        if (portalSpawnPoint == null || isEating) return;
        float dist = Vector2.Distance(fishRect.position, portalSpawnPoint.position);
        if (dist < portalEnterDistance)
        {
            SavePlayerLevel();
            SceneManager.LoadScene("MainmenuScene");
        }
    }

    private void SavePlayerLevel()
    {
        PlayerPrefs.SetInt("PlayerLevel", playerLevel);
        float s = baseScale + (playerLevel - 1) * scalePerLevel;
        PlayerPrefs.SetFloat("PlayerScale", s);
        PlayerPrefs.Save();
    }

    #endregion

    private void OnDestroy()
    {
        if (fishRT != null)
        {
            fishRT.Release();
            Destroy(fishRT);
        }
    }
}
