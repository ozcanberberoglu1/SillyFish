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
    [Tooltip("Balığımızın Canvas'taki RectTransform'u")]
    [SerializeField] private RectTransform fishRect;
    [Tooltip("Balığımızın Animator bileşeni")]
    [SerializeField] private Animator fishAnimator;
    [Tooltip("Balığımızın hareket hızı (piksel/saniye)")]
    [SerializeField] private float moveSpeed = 300f;

    [Header("Fish World Rendering")]
    [Tooltip("BalıkSpriteRenderer'ın Transform'u (dünya uzayında)")]
    [SerializeField] private Transform fishWorldTransform;
    [Tooltip("Balıkları renderlamak için kullanılan kamera")]
    [SerializeField] private Camera fishCamera;
    [Tooltip("Balık kamerasının render texture'ını gösteren RawImage")]
    [SerializeField] private RawImage fishRenderImage;

    [Header("Joystick")]
    [Tooltip("Joystick'in ana kapsayıcısı (tıklayınca açılır, bırakınca kapanır)")]
    [SerializeField] private RectTransform joystickLine;
    [Tooltip("Joystick'in arka plan dairesi")]
    [SerializeField] private RectTransform joystickBase;
    [Tooltip("Joystick'in sürüklenen topuzu")]
    [SerializeField] private RectTransform joystickHandle;

    [Header("Background & Camera")]
    [Tooltip("Arka plan (BG) RectTransform'u")]
    [SerializeField] private RectTransform bgRect;
    [Tooltip("Ana oyun kamerası")]
    [SerializeField] private Camera gameCamera;
    [Tooltip("Kameranın hedefe ulaşma yumuşaklığı (yüksek = hızlı takip)")]
    [SerializeField] private float cameraSmoothSpeed = 12f;

    [Header("Food")]
    [Tooltip("Yem prefab listesi (rastgele seçilir)")]
    [SerializeField] private List<GameObject> foodPrefabs;
    [Tooltip("Yemlerin spawn olacağı panel")]
    [SerializeField] private RectTransform foodsPanel;
    [Tooltip("Minimum yem sayısı")]
    [SerializeField] private int minFoodCount = 10;
    [Tooltip("Maksimum yem sayısı")]
    [SerializeField] private int maxFoodCount = 13;
    [Tooltip("Yem boyut çarpanı")]
    [SerializeField] private float foodScale = 1f;
    [Tooltip("Yemi yeme mesafesi (piksel)")]
    [SerializeField] private float eatDistance = 80f;
    [Tooltip("Yeme animasyonu süresi (saniye)")]
    [SerializeField] private float eatAnimDuration = 0.8f;

    [Header("Player Level")]
    [Tooltip("Balığımızın başlangıç boyutu (scale)")]
    [SerializeField] private float baseScale = 0.5f;
    [Tooltip("Her level'de ne kadar büyüyeceği")]
    [SerializeField] private float scalePerLevel = 0.2f;
    [Tooltip("Level atlamak için gereken XP çarpanı (gerekli XP = bu değer × mevcut level)")]
    [SerializeField] private int baseXPMultiplier = 5;
    [Tooltip("Düşman balığı yeme mesafesi (piksel)")]
    [SerializeField] private float playerEatDistance = 50f;
    [Tooltip("Düşman balığın bizi öldürme mesafesi (piksel, küçük = zor öldürür)")]
    [SerializeField] private float enemyKillDistance = 15f;
    [Tooltip("Yenilen düşmanın yeniden doğma süresi - minimum (saniye)")]
    [SerializeField] private float respawnTimeMin = 5f;
    [Tooltip("Yenilen düşmanın yeniden doğma süresi - maksimum (saniye)")]
    [SerializeField] private float respawnTimeMax = 7f;
    [Tooltip("Her level'de kameranın ne kadar uzaklaşacağı (orthographic size artışı)")]
    [SerializeField] private float cameraSizePerLevel = 0.5f;

    [Header("Enemy Fish")]
    [Tooltip("Düşman balık türlerinin ayarları")]
    [SerializeField] private List<EnemySpawnConfig> enemySpawnConfigs = new List<EnemySpawnConfig>();

    [Header("Portal")]
    [Tooltip("Portal giriş noktası (buraya yaklaşınca MainMenu'ye döner)")]
    [SerializeField] private RectTransform portalSpawnPoint;
    [Tooltip("Oyun başladığında balığımızın spawn olacağı nokta")]
    [SerializeField] private RectTransform portalNextPoint;
    [Tooltip("Portala giriş mesafesi (piksel)")]
    [SerializeField] private float portalEnterDistance = 100f;

    [Header("Health")]
    [Tooltip("Maksimum can sayısı (kaç ısırık yiyebilir)")]
    [SerializeField] private int maxHP = 3;
    [Tooltip("Can barının görünür kalma süresi (saniye)")]
    [SerializeField] private float healthSliderShowTime = 2f;
    [Tooltip("Hasar alınca titreme miktarı (piksel)")]
    [SerializeField] private float shakeAmount = 10f;
    [Tooltip("Hasar alınca titreme süresi (saniye)")]
    [SerializeField] private float shakeDuration = 0.2f;

    [Header("Death Screen")]
    [Tooltip("İlk ölümde gösterilecek ikinci şans ekranı (animasyonlu)")]
    [SerializeField] private GameObject firstDeathScreen;

    [Header("Skills")]
    [Tooltip("Skill prefablarının spawn olacağı alan (BG altında RectTransform)")]
    [SerializeField] private RectTransform skillArea;
    [Tooltip("Skill'i yeme mesafesi (piksel)")]
    [SerializeField] private float skillPickupDistance = 80f;
    [SerializeField] private List<SkillConfig> skillConfigs = new List<SkillConfig>();

    [Header("Dark Deep")]
    [Tooltip("DarkDeep kilidi açılma leveli")]
    [SerializeField] private int darkDeepUnlockLevel = 25;
    [Tooltip("DarkDeep kilit popup alanı (temas edince kilit gösterilir)")]
    [SerializeField] private RectTransform darkDeepLockPopupArea;
    [Tooltip("Balığın üzerindeki kilit görseli")]
    [SerializeField] private GameObject darkDeepLockObj;
    [Tooltip("Zincir objesi (level'e ulaşınca kapatılır)")]
    [SerializeField] private GameObject chainsObj;
    [Tooltip("DarkDeep arkaplan (level'e ulaşınca açılır)")]
    [SerializeField] private RectTransform darkDeepBGRect;

    [Header("Combo")]
    [Tooltip("ComboText prefab'ı (TextMeshProUGUI içermeli)")]
    [SerializeField] private GameObject comboTextPrefab;
    [Tooltip("ComboText'in spawn olacağı parent obje")]
    [SerializeField] private RectTransform comboTextSpawnPoint;
    [Tooltip("Combo için gereken süre (saniye)")]
    [SerializeField] private float comboTimeWindow = 3f;
    [Tooltip("Combo için gereken yeme sayısı")]
    [SerializeField] private int comboKillCount = 5;
    [Tooltip("Combo yazıları (rastgele seçilir)")]
    [SerializeField] private List<string> comboMessages = new List<string>
        { "Harika!", "Mükemmel!", "Olağanüstü!", "Canavar!", "Efsane!", "Muhteşem!" };

    [System.Serializable]
    public class EnemySpawnConfig
    {
        [Tooltip("Düşman balık prefab'ı")]
        public GameObject prefab;
        [Tooltip("Bu balığın level'i")]
        public int level = 1;
        [Tooltip("Yenildiğinde oyuncuya verdiği XP")]
        public int xp = 1;
        [Tooltip("Eski spawn sayısı (spawnCount kullan)")]
        public int count = 5;
        [Tooltip("Yüzme hızı")]
        public float speed = 150f;
        [Tooltip("Oyuncuyu algılama yarıçapı (piksel)")]
        public float detectionRadius = 400f;
        [Tooltip("Kovalama süresi (saniye, süre bitince bırakır)")]
        public float chaseTime = 2.5f;
        [Tooltip("Hareket davranışı: Default=normal, Wild=vahşi, Mysterious=gizemli")]
        public EnemyFishAI.EnemyBehavior behavior = EnemyFishAI.EnemyBehavior.Default;
        [Tooltip("Gizemli davranış için yüzme alanı (BG altına RectTransform koy)")]
        public RectTransform swimArea;
        [Tooltip("Açıksa kendinden düşük level balıkları avlar")]
        public bool canEatLowerLevel;
        [Tooltip("Bu balığın görünmesi için oyuncunun ulaşması gereken minimum level")]
        public int requiredPlayerLevel = 1;
        [Tooltip("Bu balıktan kaç tane spawn olacak")]
        public int spawnCount = 5;
    }

    public enum SkillType { Shield, Health, Magnet }

    [System.Serializable]
    public class SkillConfig
    {
        [Tooltip("Skill türü")]
        public SkillType type;
        [Tooltip("Skill prefab'ı (sahnede spawn olacak obje)")]
        public GameObject prefab;
        [Tooltip("Aynı anda kaç tane spawn olacak")]
        public int spawnCount = 1;
        [Tooltip("Yenildikten sonra tekrar spawn olma süresi (saniye)")]
        public float respawnTime = 15f;
        [Tooltip("Skill etki süresi (saniye) - Shield ve Mıknatıs için")]
        public float duration = 10f;
    }

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private Vector2 joystickDirection;
    private float joystickRadius;
    private bool joystickActive;
    private Camera canvasCamera;
    private RectTransform canvasRect;

    private Vector2 bgStartPos;
    private Vector2 worldPosition;

    private List<RectTransform> activeFoods = new List<RectTransform>();
    private bool isEating;

    private int playerLevel = 1;
    private int currentXP;

    // Combo
    private List<float> recentKillTimes = new List<float>();

    // Skills
    private List<RectTransform> activeSkills = new List<RectTransform>();
    private List<SkillType> activeSkillTypes = new List<SkillType>();
    private List<SkillConfig> activeSkillConfigs = new List<SkillConfig>();
    private bool shieldActive;
    private bool magnetActive;
    private Coroutine shieldCoroutine;
    private Coroutine magnetCoroutine;
    private float shieldTimeRemaining;
    private float magnetTimeRemaining;
    private float shieldDuration;
    private float magnetDuration;
    private GameObject shieldObj;
    private GameObject healthPlusObj;
    private GameObject magnetIconObj;
    private GameObject shieldSliderParent;
    private Image shieldSliderFill;
    private GameObject magnetSliderParent;
    private Image magnetSliderFill;

    private List<EnemyFishAI> enemies = new List<EnemyFishAI>();
    private Transform enemyContainer;
    private RenderTexture fishRT;
    private float canvasToWorld;
    private Vector3 origWorldFishScale;
    private float origCameraSize;

    private Vector2 enemyBoundsMin;
    private Vector2 enemyBoundsMax;
    private HashSet<int> spawnedConfigIndices = new HashSet<int>();
    private TextMeshProUGUI playerLvText;
    private Transform playerLvTextCanvas;
    private float origPlayerLvTextLocalX;

    // Health
    private int currentHP;
    private bool isInvincible;
    private bool isDead;
    private bool darkDeepUnlocked;
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
    public List<EnemyFishAI> GetEnemies() => enemies;
    public bool IsShieldActive() => shieldActive;
    public bool IsMagnetActive() => magnetActive;

    private void Start()
    {
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
            FindSkillObjects();
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
        SpawnSkills();
        UpdatePlayerScale();
        UpdatePlayerLevelText();
        MoveToSpawnPoint();
        SetupDarkDeep();

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
        if (joystickBase == null || joystickHandle == null) return;
        joystickRadius = joystickBase.sizeDelta.x * 0.5f;

        if (joystickLine != null)
            joystickLine.gameObject.SetActive(false);
    }

    private void HandleJoystickInput()
    {
        if (joystickLine == null || joystickBase == null || joystickHandle == null) return;

        // Touch support
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (IsOverUIButton()) return;
                    ActivateJoystickAt(touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (joystickActive) UpdateJoystickDrag(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (joystickActive) DeactivateJoystick();
                    break;
            }
            return;
        }

        // Mouse support (editor)
        if (Input.GetMouseButtonDown(0))
        {
            if (IsOverUIButton()) return;
            ActivateJoystickAt(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && joystickActive)
        {
            UpdateJoystickDrag(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0) && joystickActive)
        {
            DeactivateJoystick();
        }
    }

    private void ActivateJoystickAt(Vector2 screenPos)
    {
        joystickActive = true;

        RectTransform parent = joystickLine.parent as RectTransform;
        if (parent != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, screenPos, canvasCamera, out Vector2 localPos);
            joystickLine.anchoredPosition = localPos;
        }

        joystickLine.gameObject.SetActive(true);
        joystickHandle.anchoredPosition = Vector2.zero;
        joystickDirection = Vector2.zero;
    }

    private void UpdateJoystickDrag(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBase, screenPos, canvasCamera, out Vector2 localPos);

        Vector2 clamped = Vector2.ClampMagnitude(localPos, joystickRadius);
        joystickHandle.anchoredPosition = clamped;
        joystickDirection = clamped / joystickRadius;
    }

    private void DeactivateJoystick()
    {
        joystickActive = false;
        joystickHandle.anchoredPosition = Vector2.zero;
        joystickDirection = Vector2.zero;
        joystickLine.gameObject.SetActive(false);
    }

    private bool IsOverUIButton()
    {
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = (Vector2)Input.mousePosition
        };
        var results = new List<UnityEngine.EventSystems.RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            if (r.gameObject.GetComponent<Button>() != null)
                return true;
        }
        return false;
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        if (fishRect == null || bgRect == null) return;

        HandleJoystickInput();

        if (isDead) return;

        UpdateWorldPosition();
        FlipFish();
        UpdateAnimation();
        UpdateCamera();
        CheckFoodEat();
        CheckSkillPickup();
        SyncEnemyWorldPositions();
        CheckEnemyInteractions();
        CheckPortal();
        CheckDarkDeepLock();
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

        float bgTopY = bgStartPos.y + halfBGH;
        float bgBottomY = bgStartPos.y - halfBGH;

        if (darkDeepUnlocked && darkDeepBGRect != null)
        {
            float deepHalfH = darkDeepBGRect.sizeDelta.y * 0.5f;
            float deepBottomY = darkDeepBGRect.anchoredPosition.y - deepHalfH;
            bgBottomY = deepBottomY;
        }

        float maxX = halfBGW - halfScreenW;
        float maxUp = bgTopY - halfScreenH - bgStartPos.y;
        float maxDown = bgStartPos.y - bgBottomY - halfScreenH;

        worldPosition.x = Mathf.Clamp(worldPosition.x, -maxX, maxX);
        worldPosition.y = Mathf.Clamp(worldPosition.y, -maxDown, maxUp);
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

        for (int cfgIdx = 0; cfgIdx < enemySpawnConfigs.Count; cfgIdx++)
        {
            if (spawnedConfigIndices.Contains(cfgIdx)) continue;

            var cfg = enemySpawnConfigs[cfgIdx];
            if (cfg.prefab == null) continue;
            if (playerLevel < cfg.requiredPlayerLevel) continue;

            spawnedConfigIndices.Add(cfgIdx);

            int total = cfg.spawnCount > 0 ? cfg.spawnCount : cfg.count;
            for (int i = 0; i < total; i++)
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
                ai.behavior = cfg.behavior;
                ai.canEatLowerLevel = cfg.canEatLowerLevel;
                ai.Init(this, pos, enemyBoundsMin, enemyBoundsMax);

                if (cfg.behavior == EnemyFishAI.EnemyBehavior.Mysterious && cfg.swimArea != null)
                {
                    Vector3 localInBG = bgRect.InverseTransformPoint(cfg.swimArea.position);
                    Vector2 center = new Vector2(localInBG.x, localInBG.y);
                    Vector2 halfSize = cfg.swimArea.sizeDelta * 0.5f;
                    ai.SetHomeArea(center, halfSize);
                }

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

    public void OnEnemyEatEnemy(EnemyFishAI prey)
    {
        if (prey == null || !prey.gameObject.activeSelf) return;
        prey.gameObject.SetActive(false);
        StartCoroutine(RespawnEnemy(prey));
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

            if (!shieldActive && e.currentState == EnemyFishAI.FishState.Chasing && e.level > playerLevel && d <= enemyKillDistance)
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
        TrackCombo();

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
            SpawnEnemies();
            CheckDarkDeepUnlock();
        }
    }

    private IEnumerator DelayedScatter(float delay)
    {
        yield return new WaitForSeconds(delay);
        ScatterAllEnemies();
    }

    private void ScatterAllEnemies()
    {
        foreach (var e in enemies)
        {
            if (e == null) continue;
            Vector2 newPos = RandomSpawnPosition();
            e.canvasPosition = newPos;

            if (fishWorldTransform != null && bgRect != null)
            {
                Vector2 effectiveCamPos = bgStartPos - bgRect.anchoredPosition;
                Vector2 offset = newPos - effectiveCamPos;
                Vector3 anchor = fishWorldTransform.position;
                e.transform.position = new Vector3(
                    anchor.x + offset.x * canvasToWorld,
                    anchor.y + offset.y * canvasToWorld,
                    anchor.z);
            }

            if (e.gameObject.activeSelf)
                e.Respawn(newPos, enemyBoundsMin, enemyBoundsMax);
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
        StartCoroutine(DelayedScatter(2f));

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

    #region Combo

    private void TrackCombo()
    {
        float now = Time.time;
        recentKillTimes.Add(now);
        recentKillTimes.RemoveAll(t => now - t > comboTimeWindow);

        if (recentKillTimes.Count >= comboKillCount)
        {
            recentKillTimes.Clear();
            SpawnComboText();
        }
    }

    private void SpawnComboText()
    {
        if (comboTextPrefab == null || comboTextSpawnPoint == null) return;
        if (comboMessages == null || comboMessages.Count == 0) return;

        GameObject go = Instantiate(comboTextPrefab, comboTextSpawnPoint);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = comboMessages[Random.Range(0, comboMessages.Count)];

        StartCoroutine(AnimateComboText(go.GetComponent<RectTransform>()));
    }

    private IEnumerator AnimateComboText(RectTransform rt)
    {
        if (rt == null) yield break;

        rt.localScale = Vector3.zero;
        float t = 0f;

        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(0f, 1.3f, t / 0.15f);
            rt.localScale = Vector3.one * s;
            yield return null;
        }

        t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1.3f, 1f, t / 0.1f);
            rt.localScale = Vector3.one * s;
            yield return null;
        }

        rt.localScale = Vector3.one;
        yield return new WaitForSeconds(0.8f);

        t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1f, 0f, t / 0.2f);
            rt.localScale = Vector3.one * s;
            yield return null;
        }

        if (rt != null)
            Destroy(rt.gameObject);
    }

    #endregion

    #region Skills

    private void FindSkillObjects()
    {
        int fishLayer = fishWorldTransform.gameObject.layer;

        shieldObj = fishWorldTransform.Find("BalıkSkillShield")?.gameObject
                 ?? fishWorldTransform.Find("BalikSkillShield")?.gameObject;
        if (shieldObj != null) { SetLayerRecursive(shieldObj, fishLayer); shieldObj.SetActive(false); }

        if (playerLvTextCanvas == null) return;

        healthPlusObj = playerLvTextCanvas.Find("+health")?.gameObject;
        if (healthPlusObj != null) { SetLayerRecursive(healthPlusObj, fishLayer); healthPlusObj.SetActive(false); }

        magnetIconObj = playerLvTextCanvas.Find("+mıknatıs")?.gameObject
                     ?? playerLvTextCanvas.Find("+miknatıs")?.gameObject
                     ?? playerLvTextCanvas.Find("+miknatis")?.gameObject;
        if (magnetIconObj != null) { SetLayerRecursive(magnetIconObj, fishLayer); magnetIconObj.SetActive(false); }

        var skillSlidersT = playerLvTextCanvas.Find("SkillSliders");
        if (skillSlidersT == null) return;
        SetLayerRecursive(skillSlidersT.gameObject, fishLayer);

        var shieldT = skillSlidersT.Find("Shield");
        if (shieldT != null)
        {
            shieldSliderParent = shieldT.gameObject;
            var fill = shieldT.Find("SkillSlider/SliderFill") ?? shieldT.Find("SkillSilder/SliderFill");
            if (fill != null) shieldSliderFill = fill.GetComponent<Image>();
            shieldSliderParent.SetActive(false);
        }

        var magnetT = skillSlidersT.Find("Magnet");
        if (magnetT != null)
        {
            magnetSliderParent = magnetT.gameObject;
            var fill = magnetT.Find("SkillSlider/SliderFill") ?? magnetT.Find("SkillSilder/SliderFill");
            if (fill != null) magnetSliderFill = fill.GetComponent<Image>();
            magnetSliderParent.SetActive(false);
        }
    }

    private void SpawnSkills()
    {
        if (skillConfigs == null || skillArea == null) return;

        foreach (var cfg in skillConfigs)
        {
            if (cfg.prefab == null) continue;
            for (int i = 0; i < cfg.spawnCount; i++)
                SpawnOneSkill(cfg);
        }
    }

    private void SpawnOneSkill(SkillConfig cfg)
    {
        if (cfg.prefab == null || skillArea == null) return;

        Rect area = skillArea.rect;
        float x = Random.Range(area.xMin * 0.8f, area.xMax * 0.8f);
        float y = Random.Range(area.yMin * 0.8f, area.yMax * 0.8f);

        GameObject go = Instantiate(cfg.prefab, skillArea);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = new Vector2(x, y);
            activeSkills.Add(rt);
            activeSkillTypes.Add(cfg.type);
            activeSkillConfigs.Add(cfg);
        }
    }

    private void CheckSkillPickup()
    {
        if (isEating) return;

        for (int i = activeSkills.Count - 1; i >= 0; i--)
        {
            if (activeSkills[i] == null)
            {
                activeSkills.RemoveAt(i);
                activeSkillTypes.RemoveAt(i);
                activeSkillConfigs.RemoveAt(i);
                continue;
            }

            float dist = Vector2.Distance(fishRect.position, activeSkills[i].position);
            if (dist < skillPickupDistance)
            {
                SkillType type = activeSkillTypes[i];
                SkillConfig cfg = activeSkillConfigs[i];

                Destroy(activeSkills[i].gameObject);
                activeSkills.RemoveAt(i);
                activeSkillTypes.RemoveAt(i);
                activeSkillConfigs.RemoveAt(i);

                StartCoroutine(RespawnSkillAfterDelay(cfg));
                ActivateSkill(type, cfg);

                if (fishAnimator != null)
                    fishAnimator.CrossFade("FishFoodAnim", 0.1f, 0, 0f);
                break;
            }
        }
    }

    private IEnumerator RespawnSkillAfterDelay(SkillConfig cfg)
    {
        yield return new WaitForSeconds(cfg.respawnTime);
        SpawnOneSkill(cfg);
    }

    private void ActivateSkill(SkillType type, SkillConfig cfg)
    {
        switch (type)
        {
            case SkillType.Shield:
                if (shieldCoroutine != null)
                    StopCoroutine(shieldCoroutine);
                shieldDuration = cfg.duration;
                shieldTimeRemaining = cfg.duration;
                shieldCoroutine = StartCoroutine(ShieldRoutine());
                break;
            case SkillType.Health:
                ApplyHealthSkill();
                break;
            case SkillType.Magnet:
                if (magnetCoroutine != null)
                    StopCoroutine(magnetCoroutine);
                magnetDuration = cfg.duration;
                magnetTimeRemaining = cfg.duration;
                magnetCoroutine = StartCoroutine(MagnetRoutine());
                break;
        }
    }

    private IEnumerator ShieldRoutine()
    {
        shieldActive = true;
        if (shieldObj != null) shieldObj.SetActive(true);

        foreach (var e in enemies)
        {
            if (e != null && e.gameObject.activeSelf &&
                e.currentState == EnemyFishAI.FishState.Chasing && e.level > playerLevel)
            {
                e.ResumeAfterBite();
            }
        }

        if (shieldSliderParent != null) shieldSliderParent.SetActive(true);
        if (shieldSliderFill != null) shieldSliderFill.fillAmount = 1f;

        while (shieldTimeRemaining > 0f)
        {
            shieldTimeRemaining -= Time.deltaTime;
            if (shieldSliderFill != null)
                shieldSliderFill.fillAmount = shieldTimeRemaining / shieldDuration;
            yield return null;
        }

        shieldActive = false;
        shieldCoroutine = null;
        if (shieldObj != null) shieldObj.SetActive(false);
        if (shieldSliderParent != null) shieldSliderParent.SetActive(false);
    }

    private void ApplyHealthSkill()
    {
        if (currentHP < maxHP)
        {
            currentHP++;
            UpdateHealthSlider();
        }

        if (healthPlusObj != null)
            StartCoroutine(ShowHealthPlus());
    }

    private IEnumerator ShowHealthPlus()
    {
        healthPlusObj.SetActive(true);
        yield return new WaitForSeconds(2f);
        if (healthPlusObj != null) healthPlusObj.SetActive(false);
    }

    private IEnumerator MagnetRoutine()
    {
        magnetActive = true;
        if (magnetIconObj != null) magnetIconObj.SetActive(true);
        if (magnetSliderParent != null) magnetSliderParent.SetActive(true);
        if (magnetSliderFill != null) magnetSliderFill.fillAmount = 1f;

        while (magnetTimeRemaining > 0f)
        {
            magnetTimeRemaining -= Time.deltaTime;
            if (magnetSliderFill != null)
                magnetSliderFill.fillAmount = magnetTimeRemaining / magnetDuration;
            yield return null;
        }

        magnetActive = false;
        magnetCoroutine = null;
        if (magnetIconObj != null) magnetIconObj.SetActive(false);
        if (magnetSliderParent != null) magnetSliderParent.SetActive(false);
    }

    #endregion

    #region Dark Deep

    private void SetupDarkDeep()
    {
        if (darkDeepLockObj != null) darkDeepLockObj.SetActive(false);

        if (playerLevel >= darkDeepUnlockLevel)
        {
            UnlockDarkDeep();
        }
        else
        {
            if (darkDeepBGRect != null) darkDeepBGRect.gameObject.SetActive(false);
        }
    }

    private void CheckDarkDeepUnlock()
    {
        if (darkDeepUnlocked) return;
        if (playerLevel < darkDeepUnlockLevel) return;
        UnlockDarkDeep();
    }

    private void UnlockDarkDeep()
    {
        darkDeepUnlocked = true;
        if (chainsObj != null) chainsObj.SetActive(false);
        if (darkDeepLockPopupArea != null) darkDeepLockPopupArea.gameObject.SetActive(false);
        if (darkDeepLockObj != null) darkDeepLockObj.SetActive(false);
        if (darkDeepBGRect != null) darkDeepBGRect.gameObject.SetActive(true);
    }

    private void CheckDarkDeepLock()
    {
        if (darkDeepUnlocked || darkDeepLockPopupArea == null || darkDeepLockObj == null) return;

        Vector3 localInBG = bgRect.InverseTransformPoint(darkDeepLockPopupArea.position);
        Vector2 areaPos = new Vector2(localInBG.x, localInBG.y);
        Vector2 areaHalfSize = darkDeepLockPopupArea.sizeDelta * 0.5f;

        bool inside = worldPosition.x >= areaPos.x - areaHalfSize.x &&
                      worldPosition.x <= areaPos.x + areaHalfSize.x &&
                      worldPosition.y >= areaPos.y - areaHalfSize.y &&
                      worldPosition.y <= areaPos.y + areaHalfSize.y;

        darkDeepLockObj.SetActive(inside);
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
