using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GrammarProgressManager : MonoBehaviour
{
    [System.Serializable]
    public class LessonConfig
    {
        public string lessonName;
        public ExerciseType exerciseType;
        public List<FillBlankData> fillBlankExercises;
        public List<MakeSentenceData> makeSentenceExercises;
    }

    public enum ExerciseType { FillBlank, MakeSentence }

    [Header("Lessons (по порядку)")]
    public List<LessonConfig> lessons;

    [Header("Managers")]
    public FillBlankManager fillBlankManager;
    public MakeSentenceManager makeSentenceManager;

    [Header("UI Panels")]
    public GameObject fillBlankPanel;
    public GameObject makeSentencePanel;

    [Header("Navigation UI")]
    public TextMeshProUGUI progressLabel;
    public Button nextButton;
    public Button prevButton;
    
    private int currentLesson    = 0;
    private int currentExercise  = 0;
    
    // const string PREF_LESSON   = "grammar_lesson";
    // const string PREF_EXERCISE = "grammar_exercise";

    public static GrammarProgressManager instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // currentLesson   = PlayerPrefs.GetInt(PREF_LESSON,   0);
        // currentExercise = PlayerPrefs.GetInt(PREF_EXERCISE, 0);
        
        currentLesson   = Mathf.Clamp(currentLesson,   0, lessons.Count - 1);

        nextButton?.onClick.AddListener(NextExercise);
        prevButton?.onClick.AddListener(PrevExercise);

        LoadCurrent();
    }

    public void NextExercise()
    {
        var lesson = lessons[currentLesson];
        int total  = GetExerciseCount(lesson);

        if (currentExercise < total - 1)
        {
            currentExercise++;
        }
        else if (currentLesson < lessons.Count - 1)
        {
            currentLesson++;
            currentExercise = 0;
        }
        else
        {
            OnAllLessonsComplete();
            return;
        }

        // SaveProgress();
        LoadCurrent();
    }

    public void PrevExercise()
    {
        if (currentExercise > 0)
        {
            currentExercise--;
        }
        else if (currentLesson > 0)
        {
            currentLesson--;
            var prevLesson = lessons[currentLesson];
            currentExercise = Mathf.Max(0, GetExerciseCount(prevLesson) - 1);
        }

        // SaveProgress();
        LoadCurrent();
    }
    
    public void ResetProgress()
    {
        currentLesson   = 0;
        currentExercise = 0;
        // SaveProgress();
        LoadCurrent();
    }

    void LoadCurrent()
    {
        if (lessons == null || lessons.Count == 0) return;

        var lesson = lessons[currentLesson];
        currentExercise = Mathf.Clamp(currentExercise, 0, GetExerciseCount(lesson) - 1);

        UpdateProgressLabel(lesson);
        UpdateNavButtons(lesson);

        switch (lesson.exerciseType)
        {
            case ExerciseType.FillBlank:
                ShowPanel(fillBlankPanel, makeSentencePanel);
                fillBlankManager?.LoadExercise(lesson.fillBlankExercises[currentExercise]);
                break;

            case ExerciseType.MakeSentence:
                ShowPanel(makeSentencePanel, fillBlankPanel);
                makeSentenceManager?.LoadExercise(lesson.makeSentenceExercises[currentExercise]);
                break;
        }
    }

    int GetExerciseCount(LessonConfig lesson)
    {
        return lesson.exerciseType == ExerciseType.FillBlank
            ? lesson.fillBlankExercises?.Count ?? 0
            : lesson.makeSentenceExercises?.Count ?? 0;
    }

    void ShowPanel(GameObject show, GameObject hide)
    {
        if (show) show.SetActive(true);
        if (hide) hide.SetActive(false);
    }

    void UpdateProgressLabel(LessonConfig lesson)
    {
        if (progressLabel == null) return;
        int total = GetExerciseCount(lesson);
        progressLabel.text = $"{lesson.lessonName}  •  {currentExercise + 1} / {total}";
    }

    void UpdateNavButtons(LessonConfig lesson)
    {
        bool isFirst = currentLesson == 0 && currentExercise == 0;
        bool isLast  = currentLesson == lessons.Count - 1
                    && currentExercise == GetExerciseCount(lesson) - 1;

        if (prevButton) prevButton.interactable = !isFirst;
        if (nextButton) nextButton.interactable = !isLast;
    }

    // void SaveProgress()
    // {
    //     PlayerPrefs.SetInt(PREF_LESSON,   currentLesson);
    //     PlayerPrefs.SetInt(PREF_EXERCISE, currentExercise);
    //     PlayerPrefs.Save();
    // }

    void OnAllLessonsComplete()
    {
        if (progressLabel)
            progressLabel.text = "Все уроки пройдены!";
        if (nextButton)
            nextButton.interactable = false;
        Debug.Log("GrammarProgressManager: все уроки завершены.");
    }
}