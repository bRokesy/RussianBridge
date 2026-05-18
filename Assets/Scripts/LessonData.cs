using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Lesson Data/Lesson", fileName = "NewLesson")]
public class LessonData : ScriptableObject
{
    public enum ExerciseType
    {
        FillBlank,
        MakeSentence,
        Translate,
        Writing,
        Flashcards
    }

    [System.Serializable]
    public class LessonEntry
    {
        public ExerciseType type;

        // Заполни только поле соответствующее type
        public FillBlankData     fillBlank;
        public MakeSentenceData  makeSentence;
        public TranslateData     translate;
        public WritingData       writing;
        public FlashcardDeckData flashcards;

        public bool IsValid()
        {
            switch (type)
            {
                case ExerciseType.FillBlank:
                    return fillBlank != null;
                case ExerciseType.MakeSentence:
                    return makeSentence != null;
                case ExerciseType.Translate:
                    return translate != null;
                case ExerciseType.Writing:
                    return writing != null;
                case ExerciseType.Flashcards:
                    return flashcards != null;
                default:
                    return false;
            }
        }
    }

    [Header("Info")]
    public string lessonName;

    [Header("Упражнения по порядку")]
    public List<LessonEntry> exercises = new List<LessonEntry>();

    public int Count => exercises?.Count ?? 0;

    public LessonEntry GetExercise(int index)
    {
        if (exercises == null || index < 0 || index >= exercises.Count)
            return null;

        return exercises[index];
    }

    // Обратная совместимость с ProgressManager
    public ExerciseType exerciseType =>
        exercises != null && exercises.Count > 0
            ? exercises[0].type
            : ExerciseType.FillBlank;
}
