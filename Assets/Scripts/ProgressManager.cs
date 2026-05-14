using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase.Firestore;

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
    private int lastSyncedLesson = -1;
    private FirebaseFirestore database;
    private SceneUIPanels scenePanels;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        database = FirebaseFirestore.DefaultInstance;

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
        if (nextButton == null)
            nextButton = GameObject.Find("NextButton")?.GetComponent<Button>();

        if (nextButton == null)
        {
            Debug.LogWarning("ProgressManager: NextButton не найден в сцене.");
            return;
        }

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
            yield return StartCoroutine(SendLessonCompleteToFirebase(currentLesson));
            currentLesson++;
            currentExercise = 0;
        }
        else
        {
            yield return StartCoroutine(SendLessonCompleteToFirebase(currentLesson));
            OnAllComplete();
            yield break;
        }

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
        if (!nextButton)
        {
            nextButton = GameObject.Find("NextButton")?.GetComponent<Button>();
            Image nextButtonImage = nextButton?.gameObject.GetComponent<Image>();
            if (nextButtonImage != null)
                nextButtonImage.enabled = true;
        }

        CurrentLessonTitle = lesson.lessonName;
        SyncCurrentLessonToFirebase(lesson);
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

    void SyncCurrentLessonToFirebase(LessonData lesson)
    {
        if (lesson == null || currentLesson == lastSyncedLesson) return;
        if (string.IsNullOrEmpty(References.userId)) return;

        lastSyncedLesson = currentLesson;
        References.currentLesson = lesson.lessonName;
        StartCoroutine(SendCurrentLessonToFirebase(lesson));
    }

    IEnumerator SendCurrentLessonToFirebase(LessonData lesson)
    {
        string uid = References.userId;

        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("ProgressManager: UID is empty, current lesson was not sent to Firebase.");
            yield break;
        }

        if (database == null) database = FirebaseFirestore.DefaultInstance;

        DocumentReference userRef = database.Collection("users").Document(uid);
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "CurrentLesson", lesson.lessonName },
            { "CurrentLessonIndex", currentLesson },
            { "CurrentExerciseIndex", currentExercise },
            { "CurrentLessonUpdatedAt", Timestamp.GetCurrentTimestamp() }
        };

        var updateTask = userRef.SetAsync(updates, SetOptions.MergeAll);
        yield return new WaitUntil(() => updateTask.IsCompleted);

        if (updateTask.Exception != null)
            Debug.LogWarning("ProgressManager: failed to send current lesson to Firebase: " + updateTask.Exception);
    }

    IEnumerator SendLessonCompleteToFirebase(int lessonIndex)
    {
        string uid = References.userId;

        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("ProgressManager: UID is empty, lesson completion was not sent to Firebase.");
            yield break;
        }

        if (database == null) database = FirebaseFirestore.DefaultInstance;

        LessonData lesson = lessons[lessonIndex];
        string lessonKey = GetLessonKey(lesson, lessonIndex);
        DocumentReference userRef = database.Collection("users").Document(uid);
        DocumentReference lessonRef = userRef.Collection("lessonProgress").Document(lessonKey);

        var getTask = lessonRef.GetSnapshotAsync();
        yield return new WaitUntil(() => getTask.IsCompleted);

        if (getTask.Exception != null)
        {
            Debug.LogWarning("ProgressManager: failed to read lesson progress from Firebase: " + getTask.Exception);
            yield break;
        }

        int previousScore = 0;
        bool wasCompleted = false;

        DocumentSnapshot snapshot = getTask.Result;
        if (snapshot.Exists)
        {
            snapshot.TryGetValue("Score", out previousScore);
            snapshot.TryGetValue("Completed", out wasCompleted);
        }

        int score = 100;
        int awardedScore = Mathf.Max(0, score - previousScore);

        Dictionary<string, object> lessonUpdates = new Dictionary<string, object>
        {
            { "LessonIndex", lessonIndex },
            { "LessonName", lesson.lessonName },
            { "TotalExercises", lesson.Count },
            { "Score", score },
            { "Completed", true },
            { "CompletedAt", Timestamp.GetCurrentTimestamp() },
            { "UpdatedAt", Timestamp.GetCurrentTimestamp() }
        };

        var setLessonTask = lessonRef.SetAsync(lessonUpdates, SetOptions.MergeAll);
        yield return new WaitUntil(() => setLessonTask.IsCompleted);

        if (setLessonTask.Exception != null)
        {
            Debug.LogWarning("ProgressManager: failed to send lesson progress to Firebase: " + setLessonTask.Exception);
            yield break;
        }

        Dictionary<string, object> userUpdates = new Dictionary<string, object>
        {
            { "CurrentLesson", lesson.lessonName },
            { "CurrentLessonIndex", lessonIndex },
            { "CurrentExerciseIndex", lesson.Count - 1 },
            { "Experience", References.experience + awardedScore },
            { "CompletedLessons", References.completedLessons + (wasCompleted ? 0 : 1) },
            { "LastCompletedLesson", lesson.lessonName },
            { "LastCompletedLessonIndex", lessonIndex },
            { "LastProgressUpdate", Timestamp.GetCurrentTimestamp() }
        };

        var updateUserTask = userRef.SetAsync(userUpdates, SetOptions.MergeAll);
        yield return new WaitUntil(() => updateUserTask.IsCompleted);

        if (updateUserTask.Exception != null)
        {
            Debug.LogWarning("ProgressManager: failed to update user lesson stats in Firebase: " + updateUserTask.Exception);
            yield break;
        }

        References.experience += awardedScore;
        if (!wasCompleted) References.completedLessons++;
        References.currentLesson = lesson.lessonName;

        Debug.Log($"ProgressManager: lesson '{lesson.lessonName}' completed, awarded {awardedScore}/100 points.");
    }

    string GetLessonKey(LessonData lesson, int lessonIndex)
    {
        string source = string.IsNullOrEmpty(lesson.lessonName)
            ? $"lesson_{lessonIndex + 1}"
            : lesson.lessonName.ToLowerInvariant();

        foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
            source = source.Replace(invalidChar, '_');

        return source.Replace(' ', '_').Replace('.', '_');
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
