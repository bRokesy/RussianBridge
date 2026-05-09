using System.Collections;
using UnityEngine;

public class UIFlashcardSpawner : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private FlashcardDeckData deck;

    [Header("UI")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject grammarCardPrefab;
    [SerializeField] private Transform contentParent;

    public Transform ContentParent => contentParent;

    void Start()
    {
        if (deck != null) SpawnAll();
    }

    public void LoadDeck(FlashcardDeckData newDeck)
    {
        deck = newDeck;
        StartCoroutine(SpawnNextFrame());
    }

    IEnumerator SpawnNextFrame()
    {
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        yield return null;
        SpawnAll();
    }

    public void SpawnAll()
    {
        if (deck == null || cardPrefab == null || contentParent == null) return;

        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        foreach (var entry in deck.cards)
        {
            GameObject card;
            
            if (deck.isGrammarCards)
            {
                card = Instantiate(grammarCardPrefab, contentParent);
            } else
            {
                card = Instantiate(cardPrefab, contentParent);   
            }

            card.SetActive(false);
            card.GetComponent<UIFlashcardFlip>()?.SetData(
                entry.foreignWord,
                entry.translation,
                entry.image,
                entry.exampleForeign,
                entry.exampleTranslation,
                entry.frontAudio,
                entry.backAudio
            );
        }

        GetComponent<FlashcardDeckManager>()?.OnDeckLoaded();
    }
}