using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Lesson Data/Fill Blank Data", fileName = "NewFillBlank")]
public class FillBlankData : ScriptableObject
{
    [System.Serializable]
    public class Question
    {
        [Header("Display")]
        public string taskTitle;
        [TextArea] public string hint;

        [Header("Sentence")]
        [Tooltip("Используй ___ для каждого пропуска. Пример: 'Он ___ на поезде ___ вчера'")]
        [TextArea] public string sentenceWithBlanks;

        [Header("Answers")]
        [Tooltip("Правильные слова для пропусков — по порядку")]
        public List<string> correctAnswers = new List<string>();

        [Header("Word Bank")]
        [Tooltip("Все слова в банке (включая лишние/дистракторы)")]
        public List<string> wordBankWords = new List<string>();
    }

    [Header("Display")]
    public string lessonTitle;

    [Header("Questions")]
    public List<Question> questions = new List<Question>();
}
