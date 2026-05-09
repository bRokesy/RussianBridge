using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionButtonUI : MonoBehaviour
{
    private Button button;
    private Image background;
    private TextMeshProUGUI label;

    private System.Action<OptionButtonUI> onClick;

    public string Value { get; private set; }

    private Color defaultColor;

    private void Awake()
    {
        button = GetComponent<Button>();
        background = GetComponent<Image>();
        label = GetComponentInChildren<TextMeshProUGUI>(true);

        if (background != null)
            defaultColor = background.color;

        if (button != null)
            button.onClick.AddListener(() => onClick?.Invoke(this));
    }

    public void Setup(string value, System.Action<OptionButtonUI> clickCallback)
    {
        Value = value;
        onClick = clickCallback;

        if (label) label.text = value;

        ResetColor();
        SetInteractable(true);
    }

    public void SetCorrect()
    {
        if (background) background.color = new Color(0.6f, 1f, 0.6f);
    }

    public void SetWrong()
    {
        if (background) background.color = new Color(1f, 0.6f, 0.6f);
    }

    public void ResetColor()
    {
        if (background) background.color = defaultColor;
    }

    public void SetInteractable(bool state)
    {
        if (button) button.interactable = state;
    }
}