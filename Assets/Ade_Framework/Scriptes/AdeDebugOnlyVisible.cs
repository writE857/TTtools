using UnityEngine;
using UnityEngine.UI;

public class AdeDebugOnlyVisible : MonoBehaviour
{
    public static AdeDebugOnlyVisible Instance { get; private set; }

    [SerializeField] private GameObject targetObject;

    Text targetText;

    void Awake()
    {
        SetInstance(this);
        CacheReferences();
        RefreshVisibleState();
    }

    void OnEnable()
    {
        SetInstance(this);
        CacheReferences();
        RefreshVisibleState();
    }

    void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RefreshVisibleState()
    {
#if Ade_Debug
        SetVisible(true);
#else
        SetVisible(false);
#endif
    }

    public void SetVisible(bool isVisible)
    {
        if (gameObject.activeSelf == isVisible)
        {
            return;
        }

        gameObject.SetActive(isVisible);
    }

    public void SetTmpText(string value)
    {
        CacheReferences();
        if (targetText == null)
        {
            return;
        }

        string textValue = "主页=>" + value;
        targetText.text = textValue;
    }

    void CacheReferences()
    {
        if (targetText != null)
        {
            return;
        }

        Transform targetTransform = targetObject != null ? targetObject.transform : transform;

        targetText = targetTransform.GetComponent<Text>();
        if (targetText == null)
        {
            targetText = targetTransform.GetComponentInChildren<Text>(true);
        }
    }

    void Reset()
    {
        if (targetObject == null)
        {
            targetObject = gameObject;
        }
    }

    void OnValidate()
    {
        if (targetObject == null)
        {
            targetObject = gameObject;
        }

        targetText = null;
    }

    static void SetInstance(AdeDebugOnlyVisible instance)
    {
        Instance = instance;
    }
}
