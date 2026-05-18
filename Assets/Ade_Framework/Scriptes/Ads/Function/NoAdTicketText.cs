using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class NoAdTicketText : MonoBehaviour
{
    [SerializeField] string prefix = "X";
    [SerializeField] Text targetText;

    void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<Text>();
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
        if (targetText == null)
        {
            return;
        }

        targetText.text = $"{prefix}{count}";
    }
}
