using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Lesson Data/Translate Data", fileName = "NewTranslateData")]
public class TranslateData : ScriptableObject
{
    [System.Serializable]
    public class Question
    {
        public string foreignWord;
        public string correctTranslation;
        public string[] options = System.Array.Empty<string>();

        [Tooltip("Опционально — картинка к вопросу")]
        public Sprite image;

        [Tooltip("Опционально — аудио к вопросу")]
        public AudioClip audio;
    }

    [Header("Display")]
    public string lessonTitle;

    [Header("Questions")]
    public List<Question> questions = new List<Question>();
}
