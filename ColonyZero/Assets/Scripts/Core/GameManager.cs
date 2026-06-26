using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Resources")]
    public float Minerals = 100f;
    public float Energy = 50f;
    public float Oxygen = 50f;
    public float Colonists = 0f;

    [Header("References")]
    public BuildingPanel buildingPanel;
    public BuildingData[] allBuildings;

    [Header("Win Condition")]
    public GameObject winPanel;
    private bool _hasWon = false;

    [Header("Progression")]
    public int currentPhase = 1;

    [Header("Prestige")]
    public int PrestigeLevel = 0;
    public GameObject prestigePanel;

    [Header("Offline Earnings")]
    public OfflineEarningsPanel offlineEarningsPanel;

    public float ProductionMultiplier => 1f + PrestigeLevel * 0.10f;

    private Dictionary<BuildingData, int> _owned = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        SaveManager.Instance?.LoadGame(this);
        CalculateOfflineEarnings();
        StartCoroutine(PassiveIncome());
    }

    private void OnApplicationQuit() => SaveManager.Instance?.SaveGame(this);

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveManager.Instance?.SaveGame(this);
    }

    public int GetOwned(BuildingData data)
        => _owned.TryGetValue(data, out int n) ? n : 0;

    public void SetOwned(BuildingData data, int count)
        => _owned[data] = count;

    public void RegisterPurchase(BuildingData data)
    {
        if (!_owned.ContainsKey(data)) _owned[data] = 0;
        _owned[data]++;
        buildingPanel?.RefreshAll();
        CheckWinCondition();
    }

    public void AddResources(float m, float e, float o)
    {
        Minerals += m;
        Energy += e;
        Oxygen += o;

        Minerals = Mathf.Max(0, Minerals);
        Energy = Mathf.Max(0, Energy);
        Oxygen = Mathf.Max(0, Oxygen);
    }

    public bool SpendResources(float m, float e, float o)
    {
        if (Minerals >= m && Energy >= e && Oxygen >= o)
        {
            Minerals -= m;
            Energy -= e;
            Oxygen -= o;
            return true;
        }
        return false;
    }

    public bool IsWinConditionMet()
    {
        bool allBuildingsOwned;
        if (allBuildings != null && allBuildings.Length > 0)
        {
            allBuildingsOwned = true;
            foreach (var b in allBuildings)
            {
                if (b == null || GetOwned(b) < 1) { allBuildingsOwned = false; break; }
            }
        }
        else
        {
            // fallback if allBuildings not wired up in Inspector
            allBuildingsOwned = _owned.Count >= 6;
        }
        return allBuildingsOwned && Colonists >= 5;
    }

    public void Prestige()
    {
        PrestigeLevel++;
        Minerals = 100f;
        Energy = 50f;
        Oxygen = 50f;
        Colonists = 0f;
        _owned.Clear();
        _hasWon = false;
        prestigePanel?.SetActive(false);
        buildingPanel?.RefreshAll();
        SaveManager.Instance?.SaveGame(this);
    }

    private void CalculateOfflineEarnings()
    {
        string savedStr = PlayerPrefs.GetString("LastSaveTime", "0");
        if (!long.TryParse(savedStr, out long lastSave) || lastSave == 0) return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long elapsed = Math.Min(now - lastSave, 8L * 3600L);
        if (elapsed <= 0) return;

        float minerals = 0, energy = 0, oxygen = 0, colonists = 0;
        float mult = ProductionMultiplier;

        foreach (var kvp in _owned)
        {
            BuildingData d = kvp.Key;
            int n = kvp.Value;
            minerals  += d.mineralsPerTick    * n * mult * elapsed;
            energy    += d.energyPerTick      * n * mult * elapsed;
            energy    -= d.energyDrain        * n        * elapsed;
            oxygen    += d.oxygenPerTick      * n * mult * elapsed;
            colonists += d.populationPerTick  * n * mult * elapsed;
        }

        Minerals  = Mathf.Max(0, Minerals  + minerals);
        Energy    = Mathf.Max(0, Energy    + energy);
        Oxygen    = Mathf.Max(0, Oxygen    + oxygen);
        Colonists = Mathf.Max(0, Colonists + colonists);

        if (offlineEarningsPanel != null)
        {
            offlineEarningsPanel.Show(elapsed, minerals, energy, oxygen, colonists);
            offlineEarningsPanel.gameObject.SetActive(true);
        }
    }

    private IEnumerator PassiveIncome()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            float mult = ProductionMultiplier;
            foreach (var kvp in _owned)
            {
                BuildingData d = kvp.Key;
                int n = kvp.Value;
                Minerals  += d.mineralsPerTick   * n * mult;
                Energy    += d.energyPerTick     * n * mult;
                Oxygen    += d.oxygenPerTick     * n * mult;
                Colonists += d.populationPerTick * n * mult;
                Energy    -= d.energyDrain       * n;
            }

            Minerals  = Mathf.Max(0, Minerals);
            Energy    = Mathf.Max(0, Energy);
            Oxygen    = Mathf.Max(0, Oxygen);

            CheckWinCondition();
            buildingPanel?.RefreshAll();
        }
    }

    private void CheckWinCondition()
    {
        if (_hasWon) return;

        if (IsWinConditionMet())
        {
            _hasWon = true;
            prestigePanel?.SetActive(true);
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
