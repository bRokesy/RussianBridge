using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TranslationQuizManager : MonoBehaviour, IExerciseController
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI wordText;
    [SerializeField] private Transform optionsParent;
    [SerializeField] private GameObject optionButtonPrefab;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Image (опционально)")]
    [SerializeField] private Image questionImage;
    [SerializeField] private GameObject imageContainer;

    [Header("Audio (опционально)")]
    [SerializeField] private Button audioButton;

    private readonly List<OptionButtonUI> spawnedOptions = new List<OptionButtonUI>();
    private AudioSource audioSource;
    private TranslateData currentData;
    private int index;
    private bool answered;
    private bool isAdvancingQuestion;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioButton?.onClick.AddListener(PlayAudio);
    }

    private void Start()
    {
        if (currentData != null)
            LoadExercise(currentData);
    }

    private void OnDestroy()
    {
        audioButton?.onClick.RemoveListener(PlayAudio);
    }

    public void LoadExercise(TranslateData data)
    {
        StopAllCoroutines();
        currentData = data;
        index = 0;
        answered = false;
        isAdvancingQuestion = false;
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        answered = false;
        isAdvancingQuestion = false;

        if (!HasRequiredReferences())
            return;

        ClearOptions();

        if (currentData == null || currentData.questions == null || index >= currentData.questions.Count)
        {
            Finish();
            return;
        }

        TranslateData.Question question = currentData.questions[index];
        if (question == null)
        {
            Finish();
            return;
        }

        ProjectUtilities.SetText(wordText, question.foreignWord);
        ProjectUtilities.SetText(progressText, $"{index + 1}/{currentData.questions.Count}");

        SetupQuestionImage(question);
        SetupAudioButton(question);
        SpawnOptions(question);
    }

    private bool HasRequiredReferences()
    {
        if (optionButtonPrefab == null)
        {
            Debug.LogError("TranslationQuizManager: optionButtonPrefab не назначен.");
            return false;
        }

        if (optionsParent == null)
        {
            Debug.LogError("TranslationQuizManager: optionsParent не назначен.");
            return false;
        }

        return true;
    }

    private void SetupQuestionImage(TranslateData.Question question)
    {
        if (questionImage == null)
            return;

        bool hasImage = question.image != null;
        questionImage.sprite = hasImage ? question.image : null;
        questionImage.preserveAspect = true;

        if (imageContainer != null)
            imageContainer.SetActive(hasImage);
        else
            questionImage.gameObject.SetActive(hasImage);
    }

    private void SetupAudioButton(TranslateData.Question question)
    {
        if (audioButton == null)
            return;

        bool hasAudio = question.audio != null;
        audioButton.gameObject.SetActive(hasAudio);

        if (hasAudio)
            PlayAudio();
    }

    private void SpawnOptions(TranslateData.Question question)
    {
        if (question.options == null)
            return;

        foreach (string option in question.options)
        {
            GameObject buttonObject = Instantiate(optionButtonPrefab, optionsParent);
            OptionButtonUI optionButton = buttonObject.GetComponent<OptionButtonUI>();

            if (optionButton == null)
                continue;

            optionButton.Setup(option, OnOptionClicked);
            spawnedOptions.Add(optionButton);
        }
    }

    public void PlayAudio()
    {
        if (currentData == null || currentData.questions == null || index >= currentData.questions.Count)
            return;

        TranslateData.Question question = currentData.questions[index];
        if (question == null)
            return;

        AudioClip clip = question.audio;
        if (clip == null || audioSource == null)
            return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void OnOptionClicked(OptionButtonUI clicked)
    {
        if (answered || isAdvancingQuestion || clicked == null || currentData == null || currentData.questions == null)
            return;

        if (index >= currentData.questions.Count)
            return;

        TranslateData.Question question = currentData.questions[index];
        if (question == null)
            return;

        answered = true;
        isAdvancingQuestion = true;

        bool isCorrect = ProjectUtilities.SameAnswer(clicked.Value, question.correctTranslation);

        if (isCorrect)
            clicked.SetCorrect();
        else
            clicked.SetWrong();

        MarkCorrectOption(question);
        StartCoroutine(NextAfterDelay());
    }

    private IEnumerator NextAfterDelay()
    {
        float delay = ProgressManager.Instance != null ? ProgressManager.Instance.nextExerciseDelay : 0f;
        yield return new WaitForSeconds(delay);

        index++;

        if (currentData != null && currentData.questions != null && index < currentData.questions.Count)
            ShowQuestion();
        else
            Finish();
    }

    private void MarkCorrectOption(TranslateData.Question question)
    {
        foreach (OptionButtonUI optionButton in spawnedOptions)
        {
            if (optionButton == null)
                continue;

            optionButton.SetInteractable(false);

            if (ProjectUtilities.SameAnswer(optionButton.Value, question.correctTranslation))
                optionButton.SetCorrect();
        }
    }

    private void Finish()
    {
        isAdvancingQuestion = false;

        if (progressText != null && currentData != null && currentData.questions != null)
            progressText.text = $"{currentData.questions.Count}/{currentData.questions.Count}";

        ProgressManager.Instance?.ShowNextButton();
    }

    private void ClearOptions()
    {
        ProjectUtilities.DestroyComponents(spawnedOptions);
    }

    public void OnExerciseLeave()
    {
        StopAllCoroutines();
        ClearOptions();
        answered = false;
        isAdvancingQuestion = false;
    }
}
