using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class NoAdTicketImageSwitcher : MonoBehaviour
{
    [SerializeField] Image targetImage;
    [SerializeField] Sprite noAdTicketSprite;
    [SerializeField] Sprite adSprite;

    void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
    }

    void OnEnable()
    {
        NoAdTicketManager.CountChanged += UpdateImage;
        UpdateImage(NoAdTicketManager.GetCount());
    }

    void OnDisable()
    {
        NoAdTicketManager.CountChanged -= UpdateImage;
    }

    void UpdateImage(int count)
    {
        if (targetImage == null)
        {
            return;
        }

        targetImage.sprite = count > 0 ? noAdTicketSprite : adSprite;
    }
}
