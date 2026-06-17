using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class BattleArenaManager : MonoBehaviour
{
    [Header("Panel & Pages")]
    [SerializeField] private GameObject battleArenaPanel;
    [SerializeField] private GameObject firstPage;
    [SerializeField] private GameObject secondPage;
    [SerializeField] private Button battleArenaButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI playButtonText;

    [Header("Leaderboard")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject arenaListPrefab;
    [SerializeField] private int fakePlayerCount = 20;

    [Header("Me Score")]
    [SerializeField] private TextMeshProUGUI mePlayerText;
    [SerializeField] private TextMeshProUGUI meCountText;

    [Header("Settings")]
    [SerializeField] private int requiredLevel = 10;
    [SerializeField] private string playerName = "Silly Fish";

    private static readonly string[] FakeNames =
    {
        "Aquahunter", "DeepBite", "CoralKing", "WaveRider", "SharkFin",
        "OceanGhost", "TideMaster", "ReefSlayer", "AbyssWalker", "SeaWolf",
        "Kraken Jr", "BlueFang", "StormFish", "NightSwim", "SaltBoss",
        "DarkTide", "IronScale", "GoldFin", "SilverTail", "ThunderFin",
        "FrostBite", "EmberFin", "VenomTail", "ShadowFin", "CrimsonWave",
        "JadeScale", "NeonDrift", "PearlDiver", "RubyReef", "TopazTide"
    };

    private const string PREF_FIRST_PAGE_SEEN = "BattleArena_FirstPageSeen";
    private const string PREF_MY_SCORE = "BattleArena_MyScore";
    private const string PREF_WEEK_ID = "BattleArena_WeekId";
    private const string PREF_FAKE_DATA = "BattleArena_FakeData";
    private const string PREF_LAST_UPDATE = "BattleArena_LastUpdate";

    private List<FakePlayer> fakePlayers = new List<FakePlayer>();
    private List<GameObject> listItems = new List<GameObject>();
    private int myScore;
    private int myRank;

    [Serializable]
    private class FakePlayer
    {
        public string name;
        public int score;
    }

    [Serializable]
    private class FakeDataWrapper
    {
        public List<FakePlayer> players = new List<FakePlayer>();
    }

    private void Start()
    {
        if (battleArenaPanel != null) battleArenaPanel.SetActive(false);

        if (battleArenaButton != null)
            battleArenaButton.onClick.AddListener(OnBattleArenaClicked);
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(() => { if (battleArenaPanel != null) battleArenaPanel.SetActive(false); });

        myScore = PlayerPrefs.GetInt(PREF_MY_SCORE, 0);

        CheckWeeklyReset();
        LoadOrCreateFakePlayers();
        SimulateMissedTime();
        SortAndRefreshUI();
        UpdatePlayButton();
        StartCoroutine(LiveUpdateRoutine());
    }

    private void OnBattleArenaClicked()
    {
        if (battleArenaPanel == null) return;
        battleArenaPanel.SetActive(true);

        bool seen = PlayerPrefs.GetInt(PREF_FIRST_PAGE_SEEN, 0) == 1;

        if (firstPage != null) firstPage.SetActive(!seen);
        if (secondPage != null) secondPage.SetActive(seen);
    }

    private void OnNextClicked()
    {
        PlayerPrefs.SetInt(PREF_FIRST_PAGE_SEEN, 1);
        PlayerPrefs.Save();

        if (firstPage != null) firstPage.SetActive(false);
        if (secondPage != null) secondPage.SetActive(true);
    }

    private void OnPlayClicked()
    {
        int playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        if (playerLevel < requiredLevel) return;

        SceneManager.LoadScene("BattleGameScene");
    }

    private void UpdatePlayButton()
    {
        if (playButtonText == null) return;
        int playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        playButtonText.text = playerLevel >= requiredLevel ? "OYNA" : $"Lock Lv.{requiredLevel}";
    }

    #region Weekly Reset

    private int GetCurrentWeekId()
    {
        DateTime now = DateTime.UtcNow;
        int day = now.DayOfYear + now.Year * 400;
        return day / 7;
    }

    private void CheckWeeklyReset()
    {
        int currentWeek = GetCurrentWeekId();
        int savedWeek = PlayerPrefs.GetInt(PREF_WEEK_ID, -1);

        if (savedWeek != currentWeek)
        {
            PlayerPrefs.SetInt(PREF_WEEK_ID, currentWeek);
            PlayerPrefs.SetInt(PREF_MY_SCORE, 0);
            PlayerPrefs.DeleteKey(PREF_FAKE_DATA);
            PlayerPrefs.DeleteKey(PREF_LAST_UPDATE);
            PlayerPrefs.Save();
            myScore = 0;
        }
    }

    #endregion

    #region Fake Players

    private void LoadOrCreateFakePlayers()
    {
        string json = PlayerPrefs.GetString(PREF_FAKE_DATA, "");

        if (!string.IsNullOrEmpty(json))
        {
            var wrapper = JsonUtility.FromJson<FakeDataWrapper>(json);
            if (wrapper != null && wrapper.players.Count > 0)
            {
                fakePlayers = wrapper.players;
                return;
            }
        }

        CreateFakePlayers();
        SaveFakeData();
    }

    private void CreateFakePlayers()
    {
        fakePlayers.Clear();
        var usedNames = new HashSet<string>();

        for (int i = 0; i < fakePlayerCount; i++)
        {
            string name;
            do { name = FakeNames[UnityEngine.Random.Range(0, FakeNames.Length)]; }
            while (usedNames.Contains(name) && usedNames.Count < FakeNames.Length);
            usedNames.Add(name);

            fakePlayers.Add(new FakePlayer
            {
                name = name,
                score = 0
            });
        }
    }

    private void SimulateMissedTime()
    {
        string lastStr = PlayerPrefs.GetString(PREF_LAST_UPDATE, "");
        DateTime lastUpdate;

        if (string.IsNullOrEmpty(lastStr) || !DateTime.TryParse(lastStr, out lastUpdate))
            lastUpdate = DateTime.UtcNow;

        double minutesPassed = (DateTime.UtcNow - lastUpdate).TotalMinutes;
        int intervals = Mathf.FloorToInt((float)(minutesPassed / 3.0));

        for (int r = 0; r < intervals; r++)
        {
            int playersToUpdate = UnityEngine.Random.Range(3, 10);
            for (int j = 0; j < playersToUpdate && j < fakePlayers.Count; j++)
            {
                int idx = UnityEngine.Random.Range(0, fakePlayers.Count);
                fakePlayers[idx].score += UnityEngine.Random.Range(15, 300);
            }
        }

        PlayerPrefs.SetString(PREF_LAST_UPDATE, DateTime.UtcNow.ToString("o"));
        SaveFakeData();
    }

    private void SaveFakeData()
    {
        var wrapper = new FakeDataWrapper { players = fakePlayers };
        PlayerPrefs.SetString(PREF_FAKE_DATA, JsonUtility.ToJson(wrapper));
        PlayerPrefs.SetString(PREF_LAST_UPDATE, DateTime.UtcNow.ToString("o"));
        PlayerPrefs.Save();
    }

    private IEnumerator LiveUpdateRoutine()
    {
        while (true)
        {
            float waitTime = UnityEngine.Random.Range(30f, 180f);
            yield return new WaitForSeconds(waitTime);

            int playersToUpdate = UnityEngine.Random.Range(3, 10);
            for (int j = 0; j < playersToUpdate && j < fakePlayers.Count; j++)
            {
                int idx = UnityEngine.Random.Range(0, fakePlayers.Count);
                fakePlayers[idx].score += UnityEngine.Random.Range(15, 300);
            }

            SaveFakeData();
            SortAndRefreshUI();
        }
    }

    #endregion

    #region UI

    private void SortAndRefreshUI()
    {
        fakePlayers.Sort((a, b) => b.score.CompareTo(a.score));

        myRank = 1;
        foreach (var fp in fakePlayers)
        {
            if (fp.score > myScore)
                myRank++;
        }

        foreach (var item in listItems)
        {
            if (item != null) Destroy(item);
        }
        listItems.Clear();

        if (contentParent == null || arenaListPrefab == null) return;

        bool meInserted = false;
        int displayRank = 0;

        for (int i = 0; i < fakePlayers.Count; i++)
        {
            displayRank++;

            if (!meInserted && myScore >= fakePlayers[i].score)
            {
                CreateListItem(displayRank, playerName, myScore, true);
                meInserted = true;
                displayRank++;
            }

            CreateListItem(displayRank, fakePlayers[i].name, fakePlayers[i].score, false);
        }

        if (!meInserted)
        {
            displayRank++;
            CreateListItem(displayRank, playerName, myScore, true);
        }

        UpdateMeScore();
    }

    private void CreateListItem(int rank, string name, int score, bool isMe)
    {
        GameObject item = Instantiate(arenaListPrefab, contentParent);
        item.SetActive(true);
        listItems.Add(item);

        var playerText = item.transform.Find("PlayerText")?.GetComponent<TextMeshProUGUI>();
        var countText = item.transform.Find("CountText")?.GetComponent<TextMeshProUGUI>();

        if (playerText != null)
            playerText.text = $"{rank}. {name}";
        if (countText != null)
            countText.text = score.ToString();

        if (isMe)
        {
            var img = item.GetComponent<Image>();
            if (img != null)
            {
                img.enabled = true;
                img.color = new Color(1f, 0.85f, 0f, 0.25f);
            }
        }
    }

    private void UpdateMeScore()
    {
        if (mePlayerText != null)
            mePlayerText.text = $"{myRank}. {playerName}";
        if (meCountText != null)
            meCountText.text = myScore.ToString();
    }

    #endregion

    public static void AddArenaScore(int points)
    {
        int score = PlayerPrefs.GetInt(PREF_MY_SCORE, 0);
        score += points;
        PlayerPrefs.SetInt(PREF_MY_SCORE, score);
        PlayerPrefs.Save();
    }
}
