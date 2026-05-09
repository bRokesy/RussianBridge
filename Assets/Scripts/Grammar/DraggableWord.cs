using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableWord : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("References")]
    public TextMeshProUGUI label;

    public string Word { get; private set; }
    public BlankSlot CurrentSlot { get; private set; }

    private Transform wordBank;
    private Transform answerZone;
    private Transform originalParent;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Image background;

    public void Start()
    {
        background = GetComponent<Image>();
    }

    public void Init(string word, Transform bank, Transform answer = null)
    {
        Word = word;

        if (label == null)
            label = GetComponentInChildren<TextMeshProUGUI>();

        if (label != null)
            label.text = word;
        else
            Debug.LogError($"DraggableWord: нет TextMeshProUGUI на {gameObject.name}");

        wordBank       = bank;
        answerZone     = answer;
        originalParent = bank;

        rectTransform = GetComponent<RectTransform>();
        canvasGroup   = GetComponent<CanvasGroup>();
        rootCanvas    = GetComponentInParent<Canvas>();

        if (rootCanvas == null)
            Debug.LogError("DraggableWord: объект не под Canvas");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CurrentSlot != null)
        {
            CurrentSlot.ClearSlot();
            CurrentSlot = null;
        }

        originalParent = transform.parent;
        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.85f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if (transform.parent == rootCanvas.transform)
            ReturnToBank();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (answerZone == null) return;

        if (transform.parent == wordBank)
            MoveToZone(answerZone);
        else if (transform.parent == answerZone)
            MoveToZone(wordBank);
    }

    public void ReturnToBank()
    {
        CurrentSlot = null;
        transform.SetParent(wordBank, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        if (background) background.enabled = true; 
    }

    public void MoveToZone(Transform target, int insertIndex = -1)
    {
        transform.SetParent(target, false);

        if (insertIndex >= 0 && insertIndex < target.childCount)
            transform.SetSiblingIndex(insertIndex);
    }

    public void PlaceInSlot(BlankSlot slot)
    {
        CurrentSlot = slot;
        transform.SetParent(slot.transform, false);

        rectTransform.anchorMin        = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax        = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        if (background) background.enabled = false;
    }
}