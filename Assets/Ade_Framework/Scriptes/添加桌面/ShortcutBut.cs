using Ade_Framework;
using UnityEngine;
using UnityEngine.UI;

public class ShortcutBut : MonoBehaviour
{
    Button shortcutButton;

    void Start()
    {
#if Ade_TT || Ade_KS || Ade_BiliBili || UNITY_EDITOR || Ade_Debug
        shortcutButton = GetComponent<Button>();
        if (shortcutButton == null)
        {
            gameObject.SetActive(false);
            return;
        }

        AdeSDK.Instance.CheckShortcut((isAdded) =>
        {
            gameObject.SetActive(!isAdded);
        });

        shortcutButton.onClick.AddListener(() =>
        {
            AdeSDK.Instance.AddShortcut(() =>
            {
                gameObject.SetActive(false);
            });
        });
#else
        gameObject.SetActive(false);
#endif
    }
}
