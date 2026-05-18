using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual Feedback")]
    public Image background;
    public Color normalColor = new Color(0.9f, 0.9f, 0.95f, 1f);
    public Color hoverColor = new Color(0.75f, 0.88f, 1f, 1f);

    private void Start()
    {
        SetBackgroundColor(normalColor);
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableWord chip = eventData.pointerDrag?.GetComponent<DraggableWord>();
        if (chip == null)
            return;

        chip.MoveToZone(transform, GetInsertIndex(eventData.position));
        SetBackgroundColor(normalColor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.dragging)
            SetBackgroundColor(hoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetBackgroundColor(normalColor);
    }

    private int GetInsertIndex(Vector2 screenPosition)
    {
        int childCount = transform.childCount;
        if (childCount == 0)
            return 0;

        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child != null && screenPosition.x < child.position.x)
                return i;
        }

        return childCount;
    }

    private void SetBackgroundColor(Color color)
    {
        if (background != null)
            background.color = color;
    }
}
