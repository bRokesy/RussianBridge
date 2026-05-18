using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Image background;

    private void Awake()
    {
        background = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Init(string word, Transform bank, Transform answer = null)
    {
        Word = word;
        wordBank = bank;
        answerZone = answer;
        rootCanvas = GetComponentInParent<Canvas>();

        if (label == null)
            label = GetComponentInChildren<TextMeshProUGUI>();

        if (label != null)
            label.text = word;
        else
            Debug.LogError($"DraggableWord: нет TextMeshProUGUI на {gameObject.name}");

        if (rootCanvas == null)
            Debug.LogError("DraggableWord: объект не под Canvas");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (rootCanvas == null || canvasGroup == null)
            return;

        if (CurrentSlot != null)
        {
            CurrentSlot.ClearSlot();
            CurrentSlot = null;
        }

        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.85f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null || rootCanvas == null)
            return;

        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        if (rootCanvas != null && transform.parent == rootCanvas.transform)
            ReturnToBank();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (answerZone == null)
            return;

        if (transform.parent == wordBank)
            MoveToZone(answerZone);
        else if (transform.parent == answerZone)
            MoveToZone(wordBank);
    }

    public void ReturnToBank()
    {
        CurrentSlot = null;
        MoveToZone(wordBank);

        ResetAnchors();

        if (background != null)
            background.enabled = true;
    }

    public void MoveToZone(Transform target, int insertIndex = -1)
    {
        if (target == null)
            return;

        transform.SetParent(target, false);

        if (insertIndex >= 0 && insertIndex < target.childCount)
            transform.SetSiblingIndex(insertIndex);
    }

    public void PlaceInSlot(BlankSlot slot)
    {
        if (slot == null)
            return;

        CurrentSlot = slot;
        transform.SetParent(slot.transform, false);

        ResetAnchors();

        if (rectTransform != null)
            rectTransform.anchoredPosition = Vector2.zero;

        if (background != null)
            background.enabled = false;
    }

    private void ResetAnchors()
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
    }
}
