using Ade_Framework;
using UnityEngine;
using UnityEngine.UI;

public class MoreGames : MonoBehaviour
{
    void Start()
    {
#if Ade_TT
        Button button = GetComponent<Button>();
        if (button == null)
        {
            gameObject.SetActive(false);
            return;
        }

        button.onClick.AddListener(OnClick);
#else
        gameObject.SetActive(false);
#endif
    }

    public void OnClick()
    {
#if Ade_TT
        ADManager.Instance.ShowMoreGames();
#else
        gameObject.SetActive(false);
#endif
    }
}
