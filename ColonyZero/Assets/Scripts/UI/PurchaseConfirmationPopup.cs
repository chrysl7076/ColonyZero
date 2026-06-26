using System.Collections;
using UnityEngine;
using TMPro;

public class PurchaseConfirmationPopup : MonoBehaviour
{
    public static PurchaseConfirmationPopup Instance { get; private set; }

    public TMP_Text messageText;

    private Coroutine _hideCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
    }

    public void Show(string buildingName)
    {
        messageText.text = $"{buildingName} Purchased!";
        gameObject.SetActive(true);

        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(HideAfterDelay(2f));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}
