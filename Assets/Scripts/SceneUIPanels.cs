using UnityEngine;

public class SceneUIPanels : MonoBehaviour
{
    [Header("Назначь только те панели, которые есть в этой сцене")]
    public GameObject fillBlankPanel;
    public GameObject makeSentencePanel;
    public GameObject translatePanel;
    public GameObject writingPanel;
    public GameObject flashcardsPanel;

    private void Awake()
    {
        SetAll(false);
    }

    private void Start()
    {
        ProgressManager.Instance?.RegisterPanels(this);
    }

    public void ShowOnly(LessonData.ExerciseType type)
    {
        SetAll(false);

        switch (type)
        {
            case LessonData.ExerciseType.FillBlank:
                fillBlankPanel?.SetActive(true);
                break;
            case LessonData.ExerciseType.MakeSentence:
                makeSentencePanel?.SetActive(true);
                break;
            case LessonData.ExerciseType.Translate:
                translatePanel?.SetActive(true);
                break;
            case LessonData.ExerciseType.Writing:
                writingPanel?.SetActive(true);
                break;
            case LessonData.ExerciseType.Flashcards:
                flashcardsPanel?.SetActive(true);
                break;
        }
    }

    private void SetAll(bool state)
    {
        fillBlankPanel?.SetActive(state);
        makeSentencePanel?.SetActive(state);
        translatePanel?.SetActive(state);
        writingPanel?.SetActive(state);
        flashcardsPanel?.SetActive(state);
    }
}
