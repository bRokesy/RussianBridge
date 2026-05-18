using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Lesson Data/Make Sentence Data", fileName = "NewMakeSentence")]
public class MakeSentenceData : ScriptableObject
{
    [System.Serializable]
    public class Question
    {
        [Tooltip("Header shown above the exercise")]
        public string taskTitle;

        [TextArea]
        [Tooltip("Translation or grammar hint shown to the right")]
        public string hint;

        [Tooltip("Words shown in the word bank (will be shuffled)")]
        public List<string> shuffledWords = new List<string>();

        [Tooltip("All accepted correct sentences")]
        public List<string> correctSentences = new List<string>();

        public List<string> GetShuffled()
        {
            return ProjectUtilities.ShuffledCopy(shuffledWords);
        }
    }

    [Header("Display")]
    public string lessonTitle;

    [Header("Questions")]
    public List<Question> questions = new List<Question>();
}
