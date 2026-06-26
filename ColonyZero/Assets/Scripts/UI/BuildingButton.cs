using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingButton : MonoBehaviour
{
    [Header("UI Slots")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text costText;
    public TMP_Text ownedText;
    public TMP_Text rateText;
    public Button buyButton;

    private BuildingData _data;
    private GameManager _gm;

    public void Initialise(BuildingData data, GameManager gm)
    {
        _data = data;
        _gm = gm;

        if (iconImage != null && data.icon != null)
            iconImage.sprite = data.icon;

        nameText.text = data.buildingName;
        BuildRateString();

        buyButton.onClick.AddListener(OnBuyClicked);
        Refresh();
    }

    public void Refresh()
    {
        int owned = _gm.GetOwned(_data);
        int cost = _data.GetCurrentCost(owned);

        costText.text = $"{cost} [M]";
        ownedText.text = $"x{owned}";
        buyButton.interactable = _gm.Minerals >= cost;
    }

    void OnBuyClicked()
    {
        int owned = _gm.GetOwned(_data);
        int cost = _data.GetCurrentCost(owned);

        if (_gm.Minerals < cost) return;

        _gm.Minerals -= cost;
        _gm.RegisterPurchase(_data);
        Refresh();

        PurchaseConfirmationPopup.Instance?.Show(_data.buildingName);
        AudioManager.Instance?.PlayPurchaseSound();
    }

    void BuildRateString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (_data.mineralsPerTick != 0) parts.Add($"{_data.mineralsPerTick:+0.#} [M]/s");
        if (_data.energyPerTick != 0) parts.Add($"{_data.energyPerTick:+0.#} [E]/s");
        if (_data.oxygenPerTick != 0) parts.Add($"{_data.oxygenPerTick:+0.#} [O]/s");
        if (_data.energyDrain != 0) parts.Add($"{-_data.energyDrain:0.#} [E] drain");
        rateText.text = string.Join("  ", parts);
    }
}