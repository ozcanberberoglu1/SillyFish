using UnityEngine;
using TMPro;

public class EnemyFishAI : MonoBehaviour
{
    [Header("Stats")]
    public int level = 1;
    public int xp = 1;
    public float moveSpeed = 150f;
    public float detectionRadius = 400f;
    public float chaseTime = 2.5f;

    [Header("Wander")]
    public float minPause = 0.5f;
    public float maxPause = 2.5f;

    public enum FishState { Wandering, Chasing, Fleeing, Paused }
    [HideInInspector] public FishState currentState = FishState.Paused;
    [HideInInspector] public Vector2 canvasPosition;

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
        }

        ClampToBounds();
    }

    private void UpdatePaused(float dist, int playerLvl)
    {
        pauseTimer -= Time.deltaTime;
        if (CheckPlayerInZone(dist, playerLvl)) return;
        if (pauseTimer <= 0f)
        {
            PickWanderTarget();
            currentState = FishState.Wandering;
        }
    }

    private void UpdateWandering(float dist, int playerLvl)
    {
        if (CheckPlayerInZone(dist, playerLvl)) return;

        Vector2 dir = wanderTarget - canvasPosition;
        if (dir.magnitude < 30f)
        {
            currentState = FishState.Paused;
            pauseTimer = Random.Range(minPause, maxPause);
            return;
        }

        dir.Normalize();
        canvasPosition += dir * moveSpeed * Time.deltaTime;
        Flip(dir.x);
    }

    private void UpdateChasing(Vector2 playerPos, float dist)
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f || dist > detectionRadius * 1.5f)
        {
            currentState = FishState.Paused;
            pauseTimer = Random.Range(minPause, maxPause);
            PlayIdle();
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
            currentState = FishState.Paused;
            pauseTimer = Random.Range(minPause, maxPause);
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

        // Same or lower level: flee (only the player can eat same-level fish)
        currentState = FishState.Fleeing;
        return true;
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
        PickWanderTarget();
        currentState = FishState.Wandering;
        PlayIdle();
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
        PickWanderTarget();
        currentState = FishState.Wandering;
        PlayIdle();
    }
}
