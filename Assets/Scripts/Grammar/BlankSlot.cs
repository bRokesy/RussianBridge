using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BlankSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public Image background;
    public TextMeshProUGUI placeholderText;

    [Header("Colors")]
    public Color emptyColor = new Color(0.85f, 0.85f, 0.9f, 1f);
    public Color filledColor = new Color(0.8f, 0.95f, 0.8f, 1f);
    public Color hoverColor = new Color(0.7f, 0.85f, 1f, 1f);

    [Header("Size")]
    public float minWidth = 80f;
    public float padding = 16f;

    [HideInInspector] public string correctAnswer;

    public DraggableWord CurrentChip { get; private set; }

    private RectTransform rectTransform;
    private LayoutElement layoutElement;
    private float defaultWidth;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        layoutElement = GetComponent<LayoutElement>();
        defaultWidth = rectTransform != null ? rectTransform.sizeDelta.x : minWidth;
    }

    private void Start()
    {
        SetEmpty();
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableWord chip = eventData.pointerDrag?.GetComponent<DraggableWord>();
        if (chip == null)
            return;

        CurrentChip?.ReturnToBank();

        if (chip.CurrentSlot != null)
            chip.CurrentSlot.ClearSlot();

        PlaceChip(chip);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.dragging && background != null)
            background.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (background != null)
            background.color = CurrentChip != null ? filledColor : emptyColor;
    }

    public void PlaceChip(DraggableWord chip)
    {
        if (chip == null)
            return;

        CurrentChip = chip;
        chip.PlaceInSlot(this);

        placeholderText?.gameObject.SetActive(false);

        if (background != null)
            background.color = filledColor;

        ResizeToChip(chip);
    }

    public void ClearSlot()
    {
        CurrentChip = null;
        ResetSize();
        SetEmpty();
    }

    public bool IsCorrect()
    {
        return CurrentChip != null && ProjectUtilities.SameAnswer(CurrentChip.Word, correctAnswer);
    }

    private void ResizeToChip(DraggableWord chip)
    {
        LayoutElement chipLayout = chip.GetComponent<LayoutElement>();
        RectTransform chipRect = chip.GetComponent<RectTransform>();
        TextMeshProUGUI chipText = chip.GetComponentInChildren<TextMeshProUGUI>();

        float chipWidth = minWidth;

        if (chipText != null)
            chipWidth = Mathf.Max(chipText.preferredWidth + padding, minWidth);
        else if (chipLayout != null && chipLayout.preferredWidth > 0)
            chipWidth = Mathf.Max(chipLayout.preferredWidth, minWidth);
        else if (chipRect != null)
            chipWidth = Mathf.Max(chipRect.sizeDelta.x, minWidth);

        SetWidth(chipWidth);
    }

    private void SetWidth(float width)
    {
        if (rectTransform != null)
        {
            Vector2 size = rectTransform.sizeDelta;
            rectTransform.sizeDelta = new Vector2(width, size.y);
        }

        if (layoutElement != null)
        {
            layoutElement.preferredWidth = width;
            layoutElement.minWidth = width;
        }

        if (transform.parent is RectTransform parent)
            LayoutRebuilder.MarkLayoutForRebuild(parent);
    }

    private void ResetSize()
    {
        SetWidth(defaultWidth > 0 ? defaultWidth : minWidth);
    }

    private void SetEmpty()
    {
        placeholderText?.gameObject.SetActive(true);

        if (background != null)
            background.color = emptyColor;
    }
}
