using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class FillBlankManager : MonoBehaviour, IExerciseController
{
    [Header("UI References")]
    public Transform sentenceContainer;
    public Transform wordBank;
    public TextMeshProUGUI taskLabel;
    public TextMeshProUGUI hintLabel;
    public Button checkButton;
    public Image checkButtonImage;
    public TextMeshProUGUI checkButtonText;
    public Button resetButton;
    public TextMeshProUGUI feedbackText;

    [Header("Prefabs")]
    public GameObject wordChipPrefab;
    public GameObject textChunkPrefab;
    public GameObject slotPrefab;

    private List<DraggableWord> spawnedChips = new List<DraggableWord>();
    private List<BlankSlot> spawnedSlots     = new List<BlankSlot>();
    private FillBlankData currentData;
    private int currentIndex = 0;

    void Start()
    {
        checkButton.onClick.AddListener(CheckAnswer);
        resetButton.onClick.AddListener(ResetExercise);
    }

    public void LoadExercise(FillBlankData data)
    {
        currentData  = data;
        currentIndex = 0;
        ShowQuestion();
    }

    void ShowQuestion()
    {
        if (currentData == null || currentIndex >= currentData.questions.Count) return;

        var q = currentData.questions[currentIndex];

        feedbackText.text = "";
        taskLabel.text    = q.taskTitle;
        hintLabel.text    = q.hint;

        ClearAll();
        BuildSentence(q);
        SpawnWordBank(q);
        StartCoroutine(ForceLayout());
    }

    IEnumerator ForceLayout()
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            sentenceContainer.GetComponent<RectTransform>());
    }

    void BuildSentence(FillBlankData.Question q)
    {
        string[] parts = q.sentenceWithBlanks.Split(new string[] { "___" }, System.StringSplitOptions.None);
        int blankIndex = 0;

        for (int i = 0; i < parts.Length; i++)
        {
            if (!string.IsNullOrEmpty(parts[i]))
            {
                GameObject textGO = Instantiate(textChunkPrefab, sentenceContainer);
                var tmp = textGO.GetComponent<TextMeshProUGUI>();
                if (tmp) tmp.text = parts[i];
            }

            if (i < parts.Length - 1 && blankIndex < q.correctAnswers.Count)
            {
                GameObject slotGO = Instantiate(slotPrefab, sentenceContainer);
                BlankSlot slot = slotGO.GetComponent<BlankSlot>();
                slot.correctAnswer = q.correctAnswers[blankIndex];
                spawnedSlots.Add(slot);
                blankIndex++;
            }
        }
    }

    void SpawnWordBank(FillBlankData.Question q)
    {
        var words = new List<string>(q.wordBankWords);
        for (int i = words.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (words[i], words[j]) = (words[j], words[i]);
        }

        foreach (string word in words)
        {
            GameObject chip = Instantiate(wordChipPrefab, wordBank);
            DraggableWord dw = chip.GetComponent<DraggableWord>();
            dw.Init(word, wordBank);
            spawnedChips.Add(dw);
        }
    }

    public void CheckAnswer()
    {
        int correct = 0;
        foreach (var slot in spawnedSlots)
            if (slot.IsCorrect()) correct++;

        bool allCorrect = correct == spawnedSlots.Count;

        if (allCorrect)
        {
            // checkButton.interactable = false;
            ProgressManager.Instance.ShowNextButton();
        } else
        {
            ResetExercise();
        }

        feedbackText.text  = allCorrect ? "没错!" : $"答对了 {correct} 题（共 {spawnedSlots.Count} 题）";
    }

    IEnumerator NextAfterDelay()
    {
        yield return new WaitForSeconds(ProgressManager.Instance.nextExerciseDelay);

        currentIndex++;
        if (currentIndex < currentData.questions.Count)
            ShowQuestion();
        else
            ProgressManager.Instance.ShowNextButton();
    }

    public void ResetExercise()
    {
        feedbackText.text = "";
        foreach (var chip in spawnedChips)
            if (chip != null) chip.ReturnToBank();
        foreach (var slot in spawnedSlots)
            slot.ClearSlot();
    }

    void ClearAll()
    {
        foreach (var chip in spawnedChips)
            if (chip != null) Destroy(chip.gameObject);
        spawnedChips.Clear();

        foreach (var slot in spawnedSlots)
            if (slot != null) Destroy(slot.gameObject);
        spawnedSlots.Clear();

        foreach (Transform child in sentenceContainer)
            Destroy(child.gameObject);
    }

    public void OnExerciseLeave()
    {
        ClearAll();
        feedbackText.text = "";
    }
}