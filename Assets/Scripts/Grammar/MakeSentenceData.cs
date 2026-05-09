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
        public List<string> shuffledWords;

        [Tooltip("All accepted correct sentences")]
        public List<string> correctSentences;

        public List<string> GetShuffled()
        {
            var copy = new List<string>(shuffledWords);
            for (int i = copy.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }
            return copy;
        }
    }

    [Header("Display")]
    public string lessonTitle;

    [Header("Questions")]
    public List<Question> questions;
}