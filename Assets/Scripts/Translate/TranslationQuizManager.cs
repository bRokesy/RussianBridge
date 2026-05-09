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
    [SerializeField] private Button audioButton; // кнопка воспроизведения

    private AudioSource audioSource;
    private float delayBeforeNext = 1.5f;
    private List<OptionButtonUI> spawned = new();
    private int index = 0;
    private bool answered = false;
    private TranslateData currentData;

    void Awake()
    {
        // AudioSource на том же GameObject
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (audioButton != null)
            audioButton.onClick.AddListener(PlayAudio);
    }

    void Start()
    {
        if (currentData != null)
            LoadExercise(currentData);

        if (ProgressManager.Instance != null)
            delayBeforeNext = ProgressManager.Instance.nextExerciseDelay;
    }

    public void LoadExercise(TranslateData data)
    {
        currentData = data;
        index = 0;
        answered = false;
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        if (optionButtonPrefab == null) { Debug.LogError("optionButtonPrefab не назначен!"); return; }
        if (optionsParent == null)      { Debug.LogError("optionsParent не назначен!");      return; }

        answered = false;

        if (currentData == null || index >= currentData.questions.Count)
        {
            Finish();
            return;
        }

        var q = currentData.questions[index];
        wordText.text = q.foreignWord;

        if (progressText)
            progressText.text = $"{index + 1}/{currentData.questions.Count}";

        // Картинка
        if (questionImage != null)
        {
            bool hasImage = q.image != null;
            questionImage.sprite = hasImage ? q.image : null;
            questionImage.preserveAspect = true;

            if (imageContainer != null)
                imageContainer.SetActive(hasImage);
            else
                questionImage.gameObject.SetActive(hasImage);
        }

        // Аудио — показать кнопку если есть клип, скрыть если нет
        if (audioButton != null)
        {
            bool hasAudio = q.audio != null;
            audioButton.gameObject.SetActive(hasAudio);

            // Автоматически воспроизвести при показе вопроса
            if (hasAudio) PlayAudio();
        }

        foreach (var opt in q.options)
        {
            var btn = Instantiate(optionButtonPrefab);
            var btnUI = btn.GetComponent<OptionButtonUI>();
            btn.transform.SetParent(optionsParent, false);
            btnUI.Setup(opt, OnOptionClicked);
            spawned.Add(btnUI);
        }
    }

    public void PlayAudio()
    {
        if (currentData == null || index >= currentData.questions.Count) return;
        var clip = currentData.questions[index].audio;
        if (clip == null || audioSource == null) return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void OnOptionClicked(OptionButtonUI clicked)
    {
        if (answered) return;
        answered = true;

        var q = currentData.questions[index];
        bool isCorrect = clicked.Value.Trim().ToLower() == q.correctTranslation.Trim().ToLower();

        if (isCorrect) clicked.SetCorrect();
        else           clicked.SetWrong();

        foreach (var btn in spawned)
        {
            btn.SetInteractable(false);
            if (btn.Value.Trim().ToLower() == q.correctTranslation.Trim().ToLower())
                btn.SetCorrect();
        }

        Finish();
        // StartCoroutine(NextAfterDelay());
    }

    private IEnumerator NextAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeNext);
        ClearOptions();
        index++;
        ShowQuestion();
    }
    
    private void Finish()
    {
        if (progressText && currentData != null)
            progressText.text = $"{currentData.questions.Count}/{currentData.questions.Count}";
        
        ProgressManager.Instance.ShowNextButton();
    }

    private void ClearOptions()
    {
        foreach (var btn in spawned)
            if (btn != null) Destroy(btn.gameObject);
        spawned.Clear();
    }

    public void OnExerciseLeave() => ClearOptions();
}