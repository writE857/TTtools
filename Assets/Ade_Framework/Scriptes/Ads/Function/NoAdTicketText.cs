using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class NoAdTicketText : MonoBehaviour
{
    [SerializeField] string prefix = "X";
    [SerializeField] TMP_Text targetText;

    void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }
    }

    void OnEnable()
    {
        NoAdTicketManager.CountChanged += UpdateText;
        UpdateText(NoAdTicketManager.GetCount());
    }

    void OnDisable()
    {
        NoAdTicketManager.CountChanged -= UpdateText;
    }

    void UpdateText(int count)
    {
        if (targetText != null)
        {
            targetText.text = $"{prefix}{count}";
        }
    }
}
