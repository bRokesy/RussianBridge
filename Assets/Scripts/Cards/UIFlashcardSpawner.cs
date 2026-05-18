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

    private void Start()
    {
        if (deck != null)
            SpawnAll();
    }

    public void LoadDeck(FlashcardDeckData newDeck)
    {
        deck = newDeck;
        StartCoroutine(SpawnNextFrame());
    }

    private IEnumerator SpawnNextFrame()
    {
        ClearContent();
        yield return null;
        SpawnAll();
    }

    public void SpawnAll()
    {
        if (deck == null || contentParent == null)
            return;

        GameObject prefab = GetCardPrefab();
        if (prefab == null || deck.cards == null)
            return;

        ClearContent();

        foreach (FlashcardEntry entry in deck.cards)
            SpawnCard(prefab, entry);

        GetComponent<FlashcardDeckManager>()?.OnDeckLoaded();
    }

    private GameObject GetCardPrefab()
    {
        if (deck != null && deck.isGrammarCards && grammarCardPrefab != null)
            return grammarCardPrefab;

        return cardPrefab;
    }

    private void SpawnCard(GameObject prefab, FlashcardEntry entry)
    {
        if (entry == null)
            return;

        GameObject card = Instantiate(prefab, contentParent);
        card.SetActive(false);

        card.GetComponent<UIFlashcardFlip>()?.SetData(
            entry.foreignWord,
            entry.translation,
            entry.image,
            entry.exampleForeign,
            entry.exampleTranslation,
            entry.frontAudio,
            entry.backAudio);
    }

    private void ClearContent()
    {
        ProjectUtilities.DestroyChildren(contentParent);
    }
}
