using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FillBlankManager : MonoBehaviour, IExerciseController
{
    private static readonly string[] BlankToken = { "___" };

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

    private readonly List<DraggableWord> spawnedChips = new List<DraggableWord>();
    private readonly List<BlankSlot> spawnedSlots = new List<BlankSlot>();
    private FillBlankData currentData;
    private int currentIndex;

    private void Start()
    {
        checkButton?.onClick.AddListener(CheckAnswer);
        resetButton?.onClick.AddListener(ResetExercise);
    }

    public void LoadExercise(FillBlankData data)
    {
        currentData = data;
        currentIndex = 0;
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        if (currentData == null || currentData.questions == null || currentIndex >= currentData.questions.Count)
            return;

        FillBlankData.Question question = currentData.questions[currentIndex];
        if (question == null)
            return;

        ProjectUtilities.SetText(feedbackText, string.Empty);
        ProjectUtilities.SetText(taskLabel, question.taskTitle);
        ProjectUtilities.SetText(hintLabel, question.hint);

        ClearAll();
        BuildSentence(question);
        SpawnWordBank(question);
        StartCoroutine(ForceLayout());
    }

    private IEnumerator ForceLayout()
    {
        yield return null;

        if (sentenceContainer is RectTransform rectTransform)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private void BuildSentence(FillBlankData.Question question)
    {
        string source = question.sentenceWithBlanks ?? string.Empty;
        string[] parts = source.Split(BlankToken, System.StringSplitOptions.None);
        int blankIndex = 0;

        for (int i = 0; i < parts.Length; i++)
        {
            if (!string.IsNullOrEmpty(parts[i]))
                SpawnTextChunk(parts[i]);

            if (question.correctAnswers != null && i < parts.Length - 1 && blankIndex < question.correctAnswers.Count)
            {
                SpawnSlot(question.correctAnswers[blankIndex]);
                blankIndex++;
            }
        }
    }

    private void SpawnTextChunk(string text)
    {
        if (textChunkPrefab == null || sentenceContainer == null)
            return;

        GameObject textObject = Instantiate(textChunkPrefab, sentenceContainer);
        TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();

        if (textComponent != null)
            textComponent.text = text;
    }

    private void SpawnSlot(string correctAnswer)
    {
        if (slotPrefab == null || sentenceContainer == null)
            return;

        GameObject slotObject = Instantiate(slotPrefab, sentenceContainer);
        BlankSlot slot = slotObject.GetComponent<BlankSlot>();

        if (slot == null)
            return;

        slot.correctAnswer = correctAnswer;
        spawnedSlots.Add(slot);
    }

    private void SpawnWordBank(FillBlankData.Question question)
    {
        foreach (string word in ProjectUtilities.ShuffledCopy(question.wordBankWords))
            SpawnWordChip(word);
    }

    private void SpawnWordChip(string word)
    {
        if (wordChipPrefab == null || wordBank == null)
            return;

        GameObject chipObject = Instantiate(wordChipPrefab, wordBank);
        DraggableWord draggableWord = chipObject.GetComponent<DraggableWord>();

        if (draggableWord == null)
            return;

        draggableWord.Init(word, wordBank);
        spawnedChips.Add(draggableWord);
    }

    public void CheckAnswer()
    {
        int correct = 0;

        foreach (BlankSlot slot in spawnedSlots)
        {
            if (slot != null && slot.IsCorrect())
                correct++;
        }

        bool allCorrect = correct == spawnedSlots.Count;

        if (allCorrect)
            ProgressManager.Instance?.ShowNextButton();
        else
            ResetExercise();

        ProjectUtilities.SetText(
            feedbackText,
            allCorrect ? "没错!" : $"答对了 {correct} 题（共 {spawnedSlots.Count} 题）");
    }

    public void ResetExercise()
    {
        ProjectUtilities.SetText(feedbackText, string.Empty);

        foreach (DraggableWord chip in spawnedChips)
        {
            if (chip != null)
                chip.ReturnToBank();
        }

        foreach (BlankSlot slot in spawnedSlots)
        {
            if (slot != null)
                slot.ClearSlot();
        }
    }

    private void ClearAll()
    {
        ProjectUtilities.DestroyComponents(spawnedChips);
        spawnedSlots.Clear();
        ProjectUtilities.DestroyChildren(sentenceContainer);
    }

    public void OnExerciseLeave()
    {
        ClearAll();
        ProjectUtilities.SetText(feedbackText, string.Empty);
    }
}
