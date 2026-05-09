using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WordQuizView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color correctColor = new Color(0.6f, 1f, 0.6f);
    [SerializeField] private Color wrongColor = new Color(1f, 0.6f, 0.6f);

    public TMP_InputField InputField => inputField;

    private void Awake()
    {
        if (backgroundImage) backgroundImage.color = defaultColor;
    }

    public void ResetView()
    {
        if (backgroundImage) backgroundImage.color = defaultColor;
        if (inputField) inputField.text = "";
    }

    public void SetCorrect()
    {
        if (backgroundImage) backgroundImage.color = correctColor;
    }

    public void SetWrong()
    {
        if (backgroundImage) backgroundImage.color = wrongColor;
    }
}