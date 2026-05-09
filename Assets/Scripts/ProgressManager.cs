using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance { get; private set; }
    public static string CurrentLessonTitle;

    [Header("Уроки по порядку")]
    public List<LessonData> lessons;

    [Header("Timing")]
    public float nextExerciseDelay = 1.5f;

    [Header("Navigation UI (опционально)")]
    public TextMeshProUGUI progressLabel;
    public Button nextButton;
    public Button prevButton;
    public Slider progressBar;

    private int currentLesson   = 0;
    private int currentExercise = 0;
    private SceneUIPanels scenePanels;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        BindButtons();
        StartCoroutine(LoadAfterFrame());
    }

    IEnumerator LoadAfterFrame()
    {
        yield return null;
        LoadCurrent();
    }

    void BindButtons()
    {
        nextButton?.onClick.RemoveAllListeners();
        prevButton?.onClick.RemoveAllListeners();
        nextButton?.onClick.AddListener(NextExerciseNoDelay);
        prevButton?.onClick.AddListener(PrevExercise);
    }

    // ─── Navigation ───────────────────────────────────────────────────────────

    public void NextExercise()
    {
        StartCoroutine(NextExerciseDelayed(nextExerciseDelay));
    }

    public void NextExerciseNoDelay()
    {
        StartCoroutine(NextExerciseDelayed(0f));
    }

    /// <summary>
    /// Вызывается менеджерами когда упражнение завершено.
    /// Показывает NextButton вместо автоперехода.
    /// </summary>
    public void ShowNextButton()
    {
        nextButton.gameObject.SetActive(true);
        nextButton.interactable = true;
    }

    IEnumerator NextExerciseDelayed(float delay)
    {
        if (nextButton) nextButton.gameObject.SetActive(false);
        yield return new WaitForSeconds(delay);

        var lesson = lessons[currentLesson];
        if (currentExercise < lesson.Count - 1)
            currentExercise++;
        else if (currentLesson < lessons.Count - 1)
        {
            currentLesson++;
            currentExercise = 0;
        }
        else { OnAllComplete(); yield break; }

        SaveProgress();
        LoadCurrent();
    }

    public void PrevExercise()
    {
        if (currentExercise > 0)
            currentExercise--;
        else if (currentLesson > 0)
        {
            currentLesson--;
            currentExercise = Mathf.Max(0, lessons[currentLesson].Count - 1);
        }

        SaveProgress();
        LoadCurrent();
    }

    public void ResetProgress()
    {
        currentLesson   = 0;
        currentExercise = 0;
        SaveProgress();
        LoadCurrent();
    }

    // ─── Panels ───────────────────────────────────────────────────────────────

    public void RegisterPanels(SceneUIPanels panels)
    {
        scenePanels = panels;
        if (lessons != null && lessons.Count > 0)
        {
            var entry = lessons[currentLesson].GetExercise(currentExercise);
            if (entry != null) scenePanels.ShowOnly(entry.type);
        }
    }

    // ─── Load ─────────────────────────────────────────────────────────────────
    public void LoadCurrent()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu") return;

        if (lessons == null || lessons.Count == 0)
        {
            Debug.LogWarning("ProgressManager: список lessons пустой!");
            return;
        }

        NotifyLeave();

        var lesson = lessons[currentLesson];
        currentExercise = Mathf.Clamp(currentExercise, 0, Mathf.Max(0, lesson.Count - 1));

        var entry = lesson.GetExercise(currentExercise);
        if (entry == null)
        {
            Debug.LogWarning($"ProgressManager: упражнение {currentExercise} не найдено в {lesson.lessonName}");
            return;
        }

        if (!entry.IsValid())
        {
            Debug.LogWarning($"ProgressManager: поле данных не заполнено для типа {entry.type} в {lesson.lessonName}[{currentExercise}]");
            return;
        }

        if (!progressBar) progressBar  = GameObject.Find("ProgressBar")?.GetComponent<Slider>();
        if (!progressLabel) progressLabel = GameObject.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
        if (!nextButton) {
            nextButton = GameObject.Find("NextButton")?.GetComponent<Button>();
            nextButton.gameObject.GetComponent<Image>().enabled = true;
        }

        CurrentLessonTitle = lesson.lessonName;
        scenePanels?.ShowOnly(entry.type);

        Debug.Log($"ProgressManager: {lesson.lessonName} [{currentExercise + 1}/{lesson.Count}] тип: {entry.type}");

        switch (entry.type)
        {
            case LessonData.ExerciseType.FillBlank:
                FindAndLoad<FillBlankManager>(m => m.LoadExercise(entry.fillBlank));
                break;
            case LessonData.ExerciseType.MakeSentence:
                FindAndLoad<MakeSentenceManager>(m => m.LoadExercise(entry.makeSentence));
                break;
            case LessonData.ExerciseType.Translate:
                FindAndLoad<TranslationQuizManager>(m => m.LoadExercise(entry.translate));
                break;
            case LessonData.ExerciseType.Writing:
                FindAndLoad<WordQuizController>(m => m.LoadExercise(entry.writing));
                break;
            case LessonData.ExerciseType.Flashcards:
                FindAndLoad<UIFlashcardSpawner>(m => m.LoadDeck(entry.flashcards));
                break;
        }

        UpdateUI(lesson, entry.type);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    void NotifyLeave()
    {
        foreach (var mono in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mono is IExerciseController ec)
                ec.OnExerciseLeave();
        }
    }

    void FindAndLoad<T>(System.Action<T> action) where T : Object
    {
        var manager = FindFirstObjectByType<T>();
        if (manager != null)
            action(manager);
        else
            Debug.LogWarning($"ProgressManager: {typeof(T).Name} не найден в сцене.");
    }

    void UpdateUI(LessonData lesson, LessonData.ExerciseType type)
    {
        if (progressLabel)
            progressLabel.text = $"{lesson.lessonName}  •  {currentExercise + 1} / {lesson.Count}";
        if (progressBar)
        {
            progressBar.maxValue = lesson.Count;
            progressBar.value    = currentExercise + 1;
        }

        if (prevButton) prevButton.interactable = !(currentLesson == 0 && currentExercise == 0);

        // NextButton: для Flashcards всегда видна, для остальных скрыта до завершения
        if (nextButton != null)
        {
            bool isFlashcards = type == LessonData.ExerciseType.Flashcards;
            nextButton.gameObject.SetActive(isFlashcards);

            print(lesson.exerciseType);
            print(isFlashcards);
        }
    }

    void SaveProgress()
    {
        PlayerPrefs.Save();
    }

    void OnAllComplete()
    {
        if (progressLabel) progressLabel.text = "所有课程已完成!";
        if (nextButton)    nextButton.interactable = false;

        Destroy(gameObject);
        SceneManager.LoadScene("MainMenu");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindButtons();
        StartCoroutine(LoadAfterFrame());
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}