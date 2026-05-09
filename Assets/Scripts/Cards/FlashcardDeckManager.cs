using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class FlashcardDeckManager : MonoBehaviour
{
    [Header("Navigation")]
    public Button previousCardButton;
    public Button nextCardButton;

    [Header("UI (опционально)")]
    public TextMeshProUGUI counterLabel;

    private UIFlashcardSpawner spawner;
    private int currentIndex = 0;

    void Awake()
    {
        spawner = GetComponent<UIFlashcardSpawner>();
        if (spawner == null)
            spawner = FindFirstObjectByType<UIFlashcardSpawner>();

        previousCardButton?.onClick.AddListener(PrevCard);
        nextCardButton?.onClick.AddListener(NextCard);
    }

    // Вызывается из UIFlashcardSpawner.SpawnAll() — после того как карточки готовы
    public void OnDeckLoaded()
    {
        currentIndex = 0;
        ShowCard(0);
    }

    public void NextCard()
    {
        int count = CardCount();
        if (count == 0) return;
        if (currentIndex + 1 >= count)
        {
            // ProgressManager.Instance.NextExerciseNoDelay();
        }

        currentIndex = Mathf.Min(currentIndex + 1, count - 1);

        ShowCard(currentIndex);
    }

    public void PrevCard()
    {
        int count = CardCount();
        if (count == 0) return;
        currentIndex = Mathf.Max(currentIndex - 1, 0);

        if (currentIndex == 0)
        {
            // ProgressManager.Instance.PrevExercise();
        }

        ShowCard(currentIndex);
    }

    void ShowCard(int index)
    {
        if (spawner?.ContentParent == null) return;

        Transform content = spawner.ContentParent;
        int count = content.childCount;
        if (count == 0) return;

        index = Mathf.Clamp(index, 0, count - 1);

        for (int i = 0; i < count; i++)
            content.GetChild(i).gameObject.SetActive(i == index);

        content.GetChild(index).GetComponent<UIFlashcardFlip>()?.ResetToFront();

        UpdateUI(index, count);
    }

    void UpdateUI(int index, int count)
    {
        if (counterLabel)
            counterLabel.text = $"{index + 1} / {count}";

        // if (previousCardButton) previousCardButton.interactable = index > 0;
        // if (nextCardButton) nextCardButton.interactable     = index < count - 1; 
    }

    int CardCount() => spawner?.ContentParent != null ? spawner.ContentParent.childCount : 0;
}