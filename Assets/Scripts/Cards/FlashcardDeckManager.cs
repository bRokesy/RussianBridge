using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FlashcardDeckManager : MonoBehaviour
{
    [Header("Navigation")]
    public Button previousCardButton;
    public Button nextCardButton;

    [Header("UI (опционально)")]
    public TextMeshProUGUI counterLabel;

    private UIFlashcardSpawner spawner;
    private int currentIndex;

    private void Awake()
    {
        spawner = GetComponent<UIFlashcardSpawner>();
        if (spawner == null)
            spawner = FindFirstObjectByType<UIFlashcardSpawner>();

        previousCardButton?.onClick.AddListener(PrevCard);
        nextCardButton?.onClick.AddListener(NextCard);
    }

    private void OnDestroy()
    {
        previousCardButton?.onClick.RemoveListener(PrevCard);
        nextCardButton?.onClick.RemoveListener(NextCard);
    }

    public void OnDeckLoaded()
    {
        currentIndex = 0;
        ShowCard(currentIndex);
    }

    public void NextCard()
    {
        int count = CardCount();
        if (count == 0)
            return;

        currentIndex = Mathf.Min(currentIndex + 1, count - 1);
        ShowCard(currentIndex);
    }

    public void PrevCard()
    {
        int count = CardCount();
        if (count == 0)
            return;

        currentIndex = Mathf.Max(currentIndex - 1, 0);
        ShowCard(currentIndex);
    }

    private void ShowCard(int index)
    {
        Transform content = spawner != null ? spawner.ContentParent : null;
        if (content == null || content.childCount == 0)
            return;

        int count = content.childCount;
        currentIndex = Mathf.Clamp(index, 0, count - 1);

        for (int i = 0; i < count; i++)
            content.GetChild(i).gameObject.SetActive(i == currentIndex);

        content.GetChild(currentIndex).GetComponent<UIFlashcardFlip>()?.ResetToFront();
        UpdateUI(currentIndex, count);
    }

    private void UpdateUI(int index, int count)
    {
        if (counterLabel != null)
            counterLabel.text = $"{index + 1} / {count}";
    }

    private int CardCount()
    {
        return spawner != null && spawner.ContentParent != null
            ? spawner.ContentParent.childCount
            : 0;
    }
}
