using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private int currentLesson;
    private int currentExercise;
    private int lastSyncedLesson = -1;
    private int lastSyncedExercise = -1;
    private bool isChangingExercise;
    private FirebaseFirestore database;
    private SceneUIPanels scenePanels;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        database = FirebaseFirestore.DefaultInstance;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ResolveNavigationReferences();
        BindButtons();
        StartCoroutine(LoadAfterFrame());
    }

    private IEnumerator LoadAfterFrame()
    {
        yield return null;
        LoadCurrent();
    }

    private void BindButtons()
    {
        nextButton?.onClick.RemoveAllListeners();
        prevButton?.onClick.RemoveAllListeners();
        nextButton?.onClick.AddListener(NextExerciseNoDelay);
        prevButton?.onClick.AddListener(PrevExercise);
    }

    public void NextExercise()
    {
        StartNextExercise(nextExerciseDelay);
    }

    public void NextExerciseNoDelay()
    {
        StartNextExercise(0f);
    }

    private void StartNextExercise(float delay)
    {
        if (isChangingExercise)
            return;

        StartCoroutine(NextExerciseDelayed(delay));
    }

    public void ShowNextButton()
    {
        if (isChangingExercise)
            return;

        ResolveNextButton();

        if (nextButton == null)
        {
            Debug.LogWarning("ProgressManager: NextButton не найден в сцене.");
            return;
        }

        nextButton.gameObject.SetActive(true);
        nextButton.interactable = true;
    }

    private IEnumerator NextExerciseDelayed(float delay)
    {
        if (isChangingExercise)
            yield break;

        isChangingExercise = true;

        if (!TryGetCurrentLesson(out LessonData lesson))
        {
            isChangingExercise = false;
            yield break;
        }

        if (nextButton != null)
        {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(delay);

        if (currentExercise < lesson.Count - 1)
        {
            currentExercise++;
        }
        else if (currentLesson < lessons.Count - 1)
        {
            yield return StartCoroutine(SendLessonCompleteToFirebase(currentLesson));
            currentLesson++;
            currentExercise = 0;
        }
        else
        {
            yield return StartCoroutine(SendLessonCompleteToFirebase(currentLesson));
            isChangingExercise = false;
            OnAllComplete();
            yield break;
        }

        SaveProgress();
        isChangingExercise = false;
        LoadCurrent();
    }

    public void PrevExercise()
    {
        if (isChangingExercise)
            return;

        if (currentExercise > 0)
        {
            currentExercise--;
        }
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
        isChangingExercise = false;
        currentLesson = 0;
        currentExercise = 0;
        lastSyncedLesson = -1;
        lastSyncedExercise = -1;

        SaveProgress();
        LoadCurrent();
    }

    public void RegisterPanels(SceneUIPanels panels)
    {
        scenePanels = panels;

        if (!TryGetCurrentEntry(out LessonData.LessonEntry entry))
            return;

        scenePanels.ShowOnly(entry.type);
    }

    public void LoadCurrent()
    {
        if (SceneManager.GetActiveScene().name == SceneNames.MainMenu)
            return;

        if (!TryGetCurrentLesson(out LessonData lesson))
            return;

        NotifyLeave();

        if (!TryGetCurrentEntry(lesson, out LessonData.LessonEntry entry))
            return;

        ResolveNavigationReferences();

        CurrentLessonTitle = lesson.lessonName;
        SyncCurrentLessonToFirebase(lesson);
        scenePanels?.ShowOnly(entry.type);

        Debug.Log($"ProgressManager: {lesson.lessonName} [{currentExercise + 1}/{lesson.Count}] тип: {entry.type}");

        LoadExercise(entry);
        UpdateUI(lesson, entry.type);
    }

    private void LoadExercise(LessonData.LessonEntry entry)
    {
        switch (entry.type)
        {
            case LessonData.ExerciseType.FillBlank:
                FindAndLoad<FillBlankManager>(manager => manager.LoadExercise(entry.fillBlank));
                break;
            case LessonData.ExerciseType.MakeSentence:
                FindAndLoad<MakeSentenceManager>(manager => manager.LoadExercise(entry.makeSentence));
                break;
            case LessonData.ExerciseType.Translate:
                FindAndLoad<TranslationQuizManager>(manager => manager.LoadExercise(entry.translate));
                break;
            case LessonData.ExerciseType.Writing:
                FindAndLoad<WordQuizController>(manager => manager.LoadExercise(entry.writing));
                break;
            case LessonData.ExerciseType.Flashcards:
                FindAndLoad<UIFlashcardSpawner>(manager => manager.LoadDeck(entry.flashcards));
                break;
            default:
                Debug.LogWarning($"ProgressManager: неподдерживаемый тип упражнения {entry.type}.");
                break;
        }
    }

    private void NotifyLeave()
    {
        foreach (MonoBehaviour mono in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mono is IExerciseController exerciseController)
                exerciseController.OnExerciseLeave();
        }
    }

    private void FindAndLoad<T>(Action<T> action) where T : UnityEngine.Object
    {
        T manager = FindFirstObjectByType<T>();

        if (manager != null)
            action(manager);
        else
            Debug.LogWarning($"ProgressManager: {typeof(T).Name} не найден в сцене.");
    }

    private void UpdateUI(LessonData lesson, LessonData.ExerciseType type)
    {
        if (progressLabel != null)
            progressLabel.text = $"{lesson.lessonName}  •  {currentExercise + 1} / {lesson.Count}";

        if (progressBar != null)
        {
            progressBar.maxValue = lesson.Count;
            progressBar.value = currentExercise + 1;
        }

        if (prevButton != null)
            prevButton.interactable = currentLesson != 0 || currentExercise != 0;

        if (nextButton != null)
            nextButton.gameObject.SetActive(type == LessonData.ExerciseType.Flashcards);
    }

    private bool TryGetCurrentLesson(out LessonData lesson)
    {
        lesson = null;

        if (lessons == null || lessons.Count == 0)
        {
            Debug.LogWarning("ProgressManager: список lessons пустой.");
            return false;
        }

        currentLesson = Mathf.Clamp(currentLesson, 0, lessons.Count - 1);
        lesson = lessons[currentLesson];

        if (lesson == null)
        {
            Debug.LogWarning($"ProgressManager: урок {currentLesson} не назначен.");
            return false;
        }

        if (lesson.Count <= 0)
        {
            Debug.LogWarning($"ProgressManager: в уроке {lesson.lessonName} нет упражнений.");
            return false;
        }

        currentExercise = Mathf.Clamp(currentExercise, 0, lesson.Count - 1);
        return true;
    }

    private bool TryGetCurrentEntry(out LessonData.LessonEntry entry)
    {
        entry = null;
        return TryGetCurrentLesson(out LessonData lesson) && TryGetCurrentEntry(lesson, out entry);
    }

    private bool TryGetCurrentEntry(LessonData lesson, out LessonData.LessonEntry entry)
    {
        entry = lesson.GetExercise(currentExercise);

        if (entry == null)
        {
            Debug.LogWarning($"ProgressManager: упражнение {currentExercise} не найдено в {lesson.lessonName}.");
            return false;
        }

        if (!entry.IsValid())
        {
            Debug.LogWarning($"ProgressManager: данные не заполнены для типа {entry.type} в {lesson.lessonName}[{currentExercise}].");
            return false;
        }

        return true;
    }

    private void ResolveNavigationReferences()
    {
        if (progressBar == null)
            progressBar = GameObject.Find(SceneObjectNames.ProgressBar)?.GetComponent<Slider>();

        if (progressLabel == null)
            progressLabel = GameObject.Find(SceneObjectNames.ProgressText)?.GetComponent<TextMeshProUGUI>();

        ResolveNextButton();
    }

    private void ResolveNextButton()
    {
        if (nextButton != null)
            return;

        nextButton = GameObject.Find(SceneObjectNames.NextButton)?.GetComponent<Button>();

        Image nextButtonImage = nextButton?.GetComponent<Image>();
        if (nextButtonImage != null)
            nextButtonImage.enabled = true;
    }

    private void SaveProgress()
    {
        PlayerPrefs.Save();
    }

    private void SyncCurrentLessonToFirebase(LessonData lesson)
    {
        if (lesson == null)
            return;

        if (currentLesson == lastSyncedLesson && currentExercise == lastSyncedExercise)
            return;

        if (string.IsNullOrEmpty(References.userId))
            return;

        lastSyncedLesson = currentLesson;
        lastSyncedExercise = currentExercise;

        References.currentLesson = lesson.lessonName;
        StartCoroutine(SendCurrentLessonToFirebase(lesson));
    }

    private IEnumerator SendCurrentLessonToFirebase(LessonData lesson)
    {
        string uid = References.userId;

        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("ProgressManager: UID пустой, текущий урок не отправлен в Firebase.");
            yield break;
        }

        EnsureDatabase();

        Timestamp now = Timestamp.GetCurrentTimestamp();
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { FirestoreFields.CurrentLesson, lesson.lessonName },
            { FirestoreFields.CurrentLessonIndex, currentLesson },
            { FirestoreFields.CurrentExerciseIndex, currentExercise },
            { FirestoreFields.CurrentLessonUpdatedAt, now }
        };

        DocumentReference userRef = database.Collection(FirestoreCollections.Users).Document(uid);
        var updateTask = userRef.SetAsync(updates, SetOptions.MergeAll);

        yield return new WaitUntil(() => updateTask.IsCompleted);

        if (updateTask.Exception != null)
            Debug.LogWarning("ProgressManager: не удалось отправить текущий урок в Firebase: " + updateTask.Exception);
    }

    private IEnumerator SendLessonCompleteToFirebase(int lessonIndex)
    {
        string uid = References.userId;

        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("ProgressManager: UID пустой, завершение урока не отправлено в Firebase.");
            yield break;
        }

        if (lessons == null || lessonIndex < 0 || lessonIndex >= lessons.Count)
            yield break;

        EnsureDatabase();

        LessonData lesson = lessons[lessonIndex];
        string lessonKey = GetLessonKey(lesson, lessonIndex);

        DocumentReference userRef = database.Collection(FirestoreCollections.Users).Document(uid);
        DocumentReference lessonRef = userRef.Collection(FirestoreCollections.LessonProgress).Document(lessonKey);

        var getTask = lessonRef.GetSnapshotAsync();
        yield return new WaitUntil(() => getTask.IsCompleted);

        if (getTask.Exception != null)
        {
            Debug.LogWarning("ProgressManager: не удалось прочитать прогресс урока из Firebase: " + getTask.Exception);
            yield break;
        }

        int previousScore = 0;
        bool wasCompleted = false;

        DocumentSnapshot snapshot = getTask.Result;
        if (snapshot.Exists)
        {
            snapshot.TryGetValue(FirestoreFields.Score, out previousScore);
            snapshot.TryGetValue(FirestoreFields.Completed, out wasCompleted);
        }

        int score = 100;
        int awardedScore = Mathf.Max(0, score - previousScore);
        Timestamp now = Timestamp.GetCurrentTimestamp();

        Dictionary<string, object> lessonUpdates = new Dictionary<string, object>
        {
            { FirestoreFields.LessonIndex, lessonIndex },
            { FirestoreFields.LessonName, lesson.lessonName },
            { FirestoreFields.TotalExercises, lesson.Count },
            { FirestoreFields.Score, score },
            { FirestoreFields.Completed, true },
            { FirestoreFields.CompletedAt, now },
            { FirestoreFields.UpdatedAt, now }
        };

        var setLessonTask = lessonRef.SetAsync(lessonUpdates, SetOptions.MergeAll);
        yield return new WaitUntil(() => setLessonTask.IsCompleted);

        if (setLessonTask.Exception != null)
        {
            Debug.LogWarning("ProgressManager: не удалось отправить прогресс урока в Firebase: " + setLessonTask.Exception);
            yield break;
        }

        Dictionary<string, object> userUpdates = new Dictionary<string, object>
        {
            { FirestoreFields.CurrentLesson, lesson.lessonName },
            { FirestoreFields.CurrentLessonIndex, lessonIndex },
            { FirestoreFields.CurrentExerciseIndex, lesson.Count - 1 },
            { FirestoreFields.Experience, References.experience + awardedScore },
            { FirestoreFields.CompletedLessons, References.completedLessons + (wasCompleted ? 0 : 1) },
            { FirestoreFields.LastCompletedLesson, lesson.lessonName },
            { FirestoreFields.LastCompletedLessonIndex, lessonIndex },
            { FirestoreFields.LastProgressUpdate, now }
        };

        var updateUserTask = userRef.SetAsync(userUpdates, SetOptions.MergeAll);
        yield return new WaitUntil(() => updateUserTask.IsCompleted);

        if (updateUserTask.Exception != null)
        {
            Debug.LogWarning("ProgressManager: не удалось обновить статистику пользователя в Firebase: " + updateUserTask.Exception);
            yield break;
        }

        References.experience += awardedScore;
        if (!wasCompleted) References.completedLessons++;
        References.currentLesson = lesson.lessonName;

        Debug.Log($"ProgressManager: урок '{lesson.lessonName}' завершён, начислено {awardedScore}/100 очков.");
    }

    private void EnsureDatabase()
    {
        if (database == null)
            database = FirebaseFirestore.DefaultInstance;
    }

    private string GetLessonKey(LessonData lesson, int lessonIndex)
    {
        string source = string.IsNullOrEmpty(lesson.lessonName)
            ? $"lesson_{lessonIndex + 1}"
            : lesson.lessonName.ToLowerInvariant();

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            source = source.Replace(invalidChar, '_');

        return source.Replace(' ', '_').Replace('.', '_');
    }

    private void OnAllComplete()
    {
        if (progressLabel != null)
            progressLabel.text = "Все уроки завершены!";

        if (nextButton != null)
            nextButton.interactable = false;

        Destroy(gameObject);
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isChangingExercise = false;
        scenePanels = null;
        ResolveNavigationReferences();
        BindButtons();
        StartCoroutine(LoadAfterFrame());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
            Instance = null;
    }
}
