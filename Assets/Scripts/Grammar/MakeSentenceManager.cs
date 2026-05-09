using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private List<DraggableWord> spawnedChips = new List<DraggableWord>();
    private MakeSentenceData currentData;
    private int currentIndex = 0;

    void Start()
    {
        checkButton.onClick.AddListener(CheckAnswer);
        resetButton.onClick.AddListener(ResetExercise);
    }

    public void LoadExercise(MakeSentenceData data)
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
        // taskLabel.text    = q.taskTitle;
        // hintLabel.text    = q.hint;

        ClearAll();

        foreach (string word in q.GetShuffled())
        {
            GameObject chip = Instantiate(wordChipPrefab, wordBank);
            DraggableWord dw = chip.GetComponent<DraggableWord>();
            dw.Init(word, wordBank, answerZone);
            spawnedChips.Add(dw);
        }
    }

    void ClearAll()
    {
        foreach (var chip in spawnedChips)
            if (chip != null) Destroy(chip.gameObject);
        spawnedChips.Clear();

        // Очистить answerZone
        foreach (Transform child in answerZone)
            Destroy(child.gameObject);
    }

    public void CheckAnswer()
    {
        if (currentData == null) return;

        List<string> playerWords = new List<string>();
        foreach (Transform child in answerZone)
        {
            DraggableWord dw = child.GetComponent<DraggableWord>();
            if (dw != null) playerWords.Add(dw.Word);
        }

        string playerSentence = string.Join(" ", playerWords).Trim();
        bool correct = false;

        foreach (string sentence in currentData.questions[currentIndex].correctSentences)
        {
            if (sentence.ToLower() == playerSentence) { correct = true; break; }
        }

        feedbackText.text  = correct ? "没错!" : "再试一遍";

        if (correct) StartCoroutine(NextAfterDelay());
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
    }

    public void OnExerciseLeave()
    {
        ClearAll();
        feedbackText.text = "";
    }
}