using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Lesson Data/Flashcard Deck Data", fileName = "FlashcardDeckData")]
public class FlashcardDeckData : ScriptableObject
{
    public string lessonTitle;
    public bool isGrammarCards;
    public List<FlashcardEntry> cards = new List<FlashcardEntry>();
}

[System.Serializable]
public class FlashcardEntry
{
    [TextArea] public string foreignWord;
    [TextArea] public string translation;
    public Sprite image;

    [Tooltip("Опционально — пример использования слова")]
    public string exampleForeign;
    public string exampleTranslation;

    [Tooltip("Опционально — аудио лицевой стороны (иностранное слово)")]
    public AudioClip frontAudio;

    [Tooltip("Опционально — аудио обратной стороны (перевод)")]
    public AudioClip backAudio;
}
