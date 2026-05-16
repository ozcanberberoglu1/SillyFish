using UnityEngine;
using TMPro;

public class EnemyFishAI : MonoBehaviour
{
    public enum EnemyBehavior { Default, Wild, Mysterious }

    [Header("Stats")]
    public int level = 1;
    public int xp = 1;
    public float moveSpeed = 150f;
    public float detectionRadius = 400f;
    public float chaseTime = 2.5f;

    [Header("Wander")]
    public float minPause = 0.5f;
    public float maxPause = 2.5f;

    public enum FishState { Wandering, Chasing, Fleeing, Paused, Returning }
    [HideInInspector] public FishState currentState = FishState.Paused;
    [HideInInspector] public Vector2 canvasPosition;
    [HideInInspector] public EnemyBehavior behavior = EnemyBehavior.Default;

    private Animator anim;
    private Vector2 wanderTarget;
    private float stateTimer;
    private float pauseTimer;
    private Vector2 boundsMin, boundsMax;
    private GameManager gm;
    private bool ready;
    private bool isEatingPlayer;
    private Transform lvTextCanvas;
    private float origLvTextLocalX;

    // Wild behavior
    private bool wildIsMoving;
    private float wildCycleTimer;

    // Mysterious behavior
    private bool hasHomeArea;
    private Vector2 homeCenter;
    private Vector2 homeHalfSize;

    public void Init(GameManager manager, Vector2 startPos, Vector2 bMin, Vector2 bMax)
    {
        gm = manager;
        canvasPosition = startPos;
        boundsMin = bMin;
        boundsMax = bMax;
        anim = GetComponentInChildren<Animator>();
        SetupLevelText();
        PickWanderTarget();
        currentState = FishState.Wandering;
        ready = true;
        PlayIdle();

        if (behavior == EnemyBehavior.Wild)
        {
            wildIsMoving = false;
            wildCycleTimer = Random.Range(3f, 4f);
        }
    }

    public void SetHomeArea(Vector2 center, Vector2 halfSize)
    {
        hasHomeArea = true;
        homeCenter = center;
        homeHalfSize = halfSize;

        canvasPosition = center;
        PickWanderTargetInHome();
    }

    private void SetupLevelText()
    {
        lvTextCanvas = transform.Find("LvTextCanvas");
        if (lvTextCanvas == null) return;
        origLvTextLocalX = lvTextCanvas.localPosition.x;
        var lvTextObj = lvTextCanvas.Find("LvText");
        if (lvTextObj == null) return;
        var tmp = lvTextObj.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = $"Lv{level}";
    }

    private void Update()
    {
        if (!ready || isEatingPlayer) return;

        Vector2 playerPos = gm.GetPlayerWorldPosition();
        int playerLevel = gm.GetPlayerLevel();
        float dist = Vector2.Distance(canvasPosition, playerPos);

        switch (currentState)
        {
            case FishState.Paused:
                UpdatePaused(dist, playerLevel);
                break;
            case FishState.Wandering:
                UpdateWandering(dist, playerLevel);
                break;
            case FishState.Chasing:
                UpdateChasing(playerPos, dist);
                break;
            case FishState.Fleeing:
                UpdateFleeing(playerPos, dist);
                break;
            case FishState.Returning:
                UpdateReturning(dist, playerLevel);
                break;
        }

        ClampToBounds();
    }

    #region Default & Shared

    private void UpdatePaused(float dist, int playerLvl)
    {
        pauseTimer -= Time.deltaTime;
        if (CheckPlayerInZone(dist, playerLvl)) return;

        if (behavior == EnemyBehavior.Wild)
        {
            UpdateWildCycle();
            return;
        }

        if (pauseTimer <= 0f)
        {
            PickAppropriateTarget();
            currentState = FishState.Wandering;
        }
    }

    private void UpdateWandering(float dist, int playerLvl)
    {
        if (CheckPlayerInZone(dist, playerLvl)) return;

        if (behavior == EnemyBehavior.Wild)
            UpdateWildCycle();

        Vector2 dir = wanderTarget - canvasPosition;
        if (dir.magnitude < 30f)
        {
            if (behavior == EnemyBehavior.Wild && wildIsMoving)
            {
                PickAppropriateTarget();
                return;
            }
            currentState = FishState.Paused;
            pauseTimer = Random.Range(minPause, maxPause);
            return;
        }

        float speed = moveSpeed;
        if (behavior == EnemyBehavior.Wild && wildIsMoving)
            speed *= 2.5f;

        dir.Normalize();
        canvasPosition += dir * speed * Time.deltaTime;
        Flip(dir.x);
    }

    private void UpdateChasing(Vector2 playerPos, float dist)
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f || dist > detectionRadius * 1.5f)
        {
            if (behavior == EnemyBehavior.Mysterious && hasHomeArea)
            {
                currentState = FishState.Returning;
                PlayIdle();
            }
            else
            {
                currentState = FishState.Paused;
                pauseTimer = Random.Range(minPause, maxPause);
                PlayIdle();
            }
            return;
        }

        Vector2 dir = (playerPos - canvasPosition).normalized;
        canvasPosition += dir * moveSpeed * 1.4f * Time.deltaTime;
        Flip(dir.x);
    }

    private void UpdateFleeing(Vector2 playerPos, float dist)
    {
        if (dist > detectionRadius * 1.3f)
        {
            if (behavior == EnemyBehavior.Mysterious && hasHomeArea)
            {
                currentState = FishState.Returning;
            }
            else
            {
                currentState = FishState.Paused;
                pauseTimer = Random.Range(minPause, maxPause);
            }
            return;
        }

        Vector2 dir = (canvasPosition - playerPos).normalized;
        canvasPosition += dir * moveSpeed * 1.2f * Time.deltaTime;
        Flip(dir.x);
    }

    private bool CheckPlayerInZone(float dist, int playerLvl)
    {
        if (dist > detectionRadius) return false;

        if (level > playerLvl)
        {
            currentState = FishState.Chasing;
            stateTimer = chaseTime;
            return true;
        }

        currentState = FishState.Fleeing;
        return true;
    }

    #endregion

    #region Wild Behavior

    private void UpdateWildCycle()
    {
        wildCycleTimer -= Time.deltaTime;
        if (wildCycleTimer <= 0f)
        {
            wildIsMoving = !wildIsMoving;
            wildCycleTimer = Random.Range(3f, 4f);

            if (wildIsMoving)
            {
                PickAppropriateTarget();
                currentState = FishState.Wandering;
            }
            else
            {
                currentState = FishState.Paused;
                pauseTimer = wildCycleTimer;
            }
        }
    }

    #endregion

    #region Mysterious Behavior

    private void UpdateReturning(float dist, int playerLvl)
    {
        if (CheckPlayerInZone(dist, playerLvl)) return;

        Vector2 dir = homeCenter - canvasPosition;
        if (dir.magnitude < 50f)
        {
            PickWanderTargetInHome();
            currentState = FishState.Wandering;
            return;
        }

        dir.Normalize();
        canvasPosition += dir * moveSpeed * Time.deltaTime;
        Flip(dir.x);
    }

    private void PickWanderTargetInHome()
    {
        float margin = 30f;
        wanderTarget = new Vector2(
            Random.Range(homeCenter.x - homeHalfSize.x + margin, homeCenter.x + homeHalfSize.x - margin),
            Random.Range(homeCenter.y - homeHalfSize.y + margin, homeCenter.y + homeHalfSize.y - margin));
    }

    #endregion

    #region Shared Helpers

    private void PickAppropriateTarget()
    {
        if (behavior == EnemyBehavior.Mysterious && hasHomeArea)
            PickWanderTargetInHome();
        else
            PickWanderTarget();
    }

    private void PickWanderTarget()
    {
        float margin = 300f;
        wanderTarget = new Vector2(
            Random.Range(boundsMin.x + margin, boundsMax.x - margin),
            Random.Range(boundsMin.y + margin, boundsMax.y - margin));
    }

    private void ClampToBounds()
    {
        canvasPosition.x = Mathf.Clamp(canvasPosition.x, boundsMin.x, boundsMax.x);
        canvasPosition.y = Mathf.Clamp(canvasPosition.y, boundsMin.y, boundsMax.y);
    }

    private void Flip(float dx)
    {
        if (Mathf.Abs(dx) < 0.01f) return;
        Vector3 s = transform.localScale;
        float a = Mathf.Abs(s.x);
        s.x = dx > 0 ? -a : a;
        transform.localScale = s;

        if (lvTextCanvas != null)
        {
            Vector3 ts = lvTextCanvas.localScale;
            float absT = Mathf.Abs(ts.x);
            ts.x = s.x >= 0 ? absT : -absT;
            lvTextCanvas.localScale = ts;

            Vector3 tp = lvTextCanvas.localPosition;
            tp.x = s.x >= 0 ? origLvTextLocalX : -origLvTextLocalX;
            lvTextCanvas.localPosition = tp;
        }
    }

    public void Respawn(Vector2 newPos, Vector2 bMin, Vector2 bMax)
    {
        canvasPosition = newPos;
        boundsMin = bMin;
        boundsMax = bMax;
        isEatingPlayer = false;
        PickAppropriateTarget();
        currentState = FishState.Wandering;
        PlayIdle();

        if (behavior == EnemyBehavior.Wild)
        {
            wildIsMoving = false;
            wildCycleTimer = Random.Range(3f, 4f);
        }
    }

    public void PlayIdle()
    {
        if (anim != null) anim.Play("Idle", 0, 0f);
    }

    public void PlayFood()
    {
        isEatingPlayer = true;
        if (anim != null) anim.Play("Food", 0, 0f);
    }

    public void ResumeAfterBite()
    {
        isEatingPlayer = false;
        PickAppropriateTarget();
        currentState = FishState.Wandering;
        PlayIdle();
    }

    #endregion
}
