using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Ade/Popup Button Slot")]
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class AdePopupButtonSlot : MonoBehaviour
{
    public enum SlotType
    {
        None,
        Go,
        Claim,
        Close,
    }

    [SerializeField] SlotType slot = SlotType.None;

    Button cachedButton;

    public SlotType Slot => slot;

    public void SetSlot(SlotType value)
    {
        slot = value;
    }

    public Button Button
    {
        get
        {
            if (cachedButton == null)
            {
                cachedButton = GetComponent<Button>();
            }

            return cachedButton;
        }
    }
}
