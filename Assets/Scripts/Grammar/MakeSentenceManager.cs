using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MakeSentenceManager : MonoBehaviour, IExerciseController
{
    [Header("UI References")]
    public Transform wordBank;
    public Transform answerZone;
    public TextMeshProUGUI taskLabel;
    public TextMeshProUGUI hintLabel;
    public Button checkButton;
    public Button resetButton;
    public TextMeshProUGUI feedbackText;

    [Header("Prefabs")]
    public GameObject wordChipPrefab;

    private readonly List<DraggableWord> spawnedChips = new List<DraggableWord>();
    private MakeSentenceData currentData;
    private int currentIndex;

    private void Start()
    {
        checkButton?.onClick.AddListener(CheckAnswer);
        resetButton?.onClick.AddListener(ResetExercise);
    }

    public void LoadExercise(MakeSentenceData data)
    {
        currentData = data;
        currentIndex = 0;
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        if (currentData == null || currentData.questions == null || currentIndex >= currentData.questions.Count)
            return;

        MakeSentenceData.Question question = currentData.questions[currentIndex];
        if (question == null)
            return;

        ProjectUtilities.SetText(feedbackText, string.Empty);

        ClearAll();

        foreach (string word in question.GetShuffled())
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

        draggableWord.Init(word, wordBank, answerZone);
        spawnedChips.Add(draggableWord);
    }

    private void ClearAll()
    {
        ProjectUtilities.DestroyComponents(spawnedChips);
        ProjectUtilities.DestroyChildren(answerZone);
    }

    public void CheckAnswer()
    {
        if (currentData == null || currentData.questions == null || currentIndex >= currentData.questions.Count)
            return;

        string playerSentence = string.Join(" ", GetPlayerWords()).Trim();
        bool correct = IsCorrectAnswer(playerSentence, currentData.questions[currentIndex]);

        ProjectUtilities.SetText(feedbackText, correct ? "没错!" : "再试一遍");

        if (correct)
            StartCoroutine(NextAfterDelay());
    }

    private List<string> GetPlayerWords()
    {
        List<string> playerWords = new List<string>();

        if (answerZone == null)
            return playerWords;

        foreach (Transform child in answerZone)
        {
            DraggableWord draggableWord = child.GetComponent<DraggableWord>();
            if (draggableWord != null)
                playerWords.Add(draggableWord.Word);
        }

        return playerWords;
    }

    private static bool IsCorrectAnswer(string playerSentence, MakeSentenceData.Question question)
    {
        if (question.correctSentences == null)
            return false;

        foreach (string sentence in question.correctSentences)
        {
            if (ProjectUtilities.SameAnswer(sentence, playerSentence))
                return true;
        }

        return false;
    }

    private IEnumerator NextAfterDelay()
    {
        float delay = ProgressManager.Instance != null ? ProgressManager.Instance.nextExerciseDelay : 0f;
        yield return new WaitForSeconds(delay);

        currentIndex++;

        if (currentData != null && currentData.questions != null && currentIndex < currentData.questions.Count)
            ShowQuestion();
        else
            ProgressManager.Instance?.ShowNextButton();
    }

    public void ResetExercise()
    {
        ProjectUtilities.SetText(feedbackText, string.Empty);

        foreach (DraggableWord chip in spawnedChips)
        {
            if (chip != null)
                chip.ReturnToBank();
        }
    }

    public void OnExerciseLeave()
    {
        ClearAll();
        ProjectUtilities.SetText(feedbackText, string.Empty);
    }
}
