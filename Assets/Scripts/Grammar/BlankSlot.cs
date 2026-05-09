using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class BlankSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public Image background;
    public TextMeshProUGUI placeholderText;

    [Header("Colors")]
    public Color emptyColor  = new Color(0.85f, 0.85f, 0.9f, 1f);
    public Color filledColor = new Color(0.8f,  0.95f, 0.8f, 1f);
    public Color hoverColor  = new Color(0.7f,  0.85f, 1f,   1f);

    [Header("Size")]
    public float minWidth  = 80f;
    public float padding   = 16f;  // горизонтальный отступ внутри слота

    [HideInInspector] public string correctAnswer;

    public DraggableWord CurrentChip { get; private set; }

    private RectTransform rectTransform;
    private LayoutElement layoutElement;
    private float defaultWidth;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        layoutElement = GetComponent<LayoutElement>();
        defaultWidth  = rectTransform.sizeDelta.x;
    }

    void Start()
    {
        SetEmpty();
    }

    // ─── IDropHandler ────────────────────────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        DraggableWord chip = eventData.pointerDrag?.GetComponent<DraggableWord>();
        if (chip == null) return;

        if (CurrentChip != null)
            CurrentChip.ReturnToBank();

        if (chip.CurrentSlot != null)
            chip.CurrentSlot.ClearSlot();

        PlaceChip(chip);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.dragging)
            background.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        background.color = CurrentChip != null ? filledColor : emptyColor;
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    public void PlaceChip(DraggableWord chip)
    {
        CurrentChip = chip;
        chip.PlaceInSlot(this);

        if (placeholderText) placeholderText.gameObject.SetActive(false);
        background.color = filledColor;

        // Подстроить ширину слота под чип
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
        if (CurrentChip == null) return false;
        return CurrentChip.Word.Trim() == correctAnswer.Trim();
    }

    // ─── Private ─────────────────────────────────────────────────────────────

    void ResizeToChip(DraggableWord chip)
    {
        // Получить предпочтительную ширину чипа
        var chipLE = chip.GetComponent<LayoutElement>();
        var chipRT = chip.GetComponent<RectTransform>();
        var chipTMP = chip.GetComponentInChildren<TMPro.TextMeshProUGUI>();

        float chipWidth = minWidth;

        if (chipTMP != null)
            chipWidth = Mathf.Max(chipTMP.preferredWidth + padding, minWidth);
        else if (chipLE != null && chipLE.preferredWidth > 0)
            chipWidth = Mathf.Max(chipLE.preferredWidth, minWidth);
        else if (chipRT != null)
            chipWidth = Mathf.Max(chipRT.sizeDelta.x, minWidth);

        SetWidth(chipWidth);
    }

    void SetWidth(float width)
    {
        // Обновить RectTransform
        var size = rectTransform.sizeDelta;
        rectTransform.sizeDelta = new Vector2(width, size.y);

        // Обновить LayoutElement чтобы FlowLayoutGroup знал новую ширину
        if (layoutElement != null)
        {
            layoutElement.preferredWidth = width;
            layoutElement.minWidth       = width;
        }

        // Сообщить layout что размер изменился
        LayoutRebuilder.MarkLayoutForRebuild(transform.parent as RectTransform);
    }

    void ResetSize()
    {
        SetWidth(defaultWidth > 0 ? defaultWidth : minWidth);
    }

    void SetEmpty()
    {
        if (placeholderText) placeholderText.gameObject.SetActive(true);
        if (background)      background.color = emptyColor;
    }
}