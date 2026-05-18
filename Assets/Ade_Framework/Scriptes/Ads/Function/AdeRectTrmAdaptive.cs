using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class AdeRectTrmAdaptive : MonoBehaviour
{
    public enum AdaptiveMode
    {
        Resize,
        Scale
    }

    [Header("适配方式")]
    public AdaptiveMode adaptiveMode = AdaptiveMode.Resize;

    [Header("无图片时默认大小")]
    public Vector2 defaultSize = new Vector2(1920f, 1080f);

    [Header("图片引用（可不填，默认自动获取）")]
    [SerializeField] private Image targetImage;
    [SerializeField] private RawImage targetRawImage;

    private RectTransform rectTrm;
    private Vector2 originSize;
    private Vector3 originScale;
    private bool inited;

    void Awake()
    {
        Init();
        Adapt();
    }

    void OnEnable()
    {
        Init();
        Adapt();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            inited = false;
            Init();
            Adapt();
        }
    }
#endif

    void Init()
    {
        if (inited)
        {
            return;
        }

        rectTrm = GetComponent<RectTransform>();
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (targetRawImage == null)
        {
            targetRawImage = GetComponent<RawImage>();
        }

        originSize = rectTrm.sizeDelta;
        originScale = rectTrm.localScale;
        inited = true;
    }

    void Adapt()
    {
        if (rectTrm == null)
        {
            return;
        }

        Vector2 baseSize = GetBaseSize();
        if (baseSize.x <= 0f || baseSize.y <= 0f)
        {
            return;
        }

        Vector2 adaptedSize = GetCoverSize(baseSize, new Vector2(Screen.width, Screen.height));

        switch (adaptiveMode)
        {
            case AdaptiveMode.Resize:
                rectTrm.localScale = originScale;
                rectTrm.sizeDelta = adaptedSize;
                break;
            case AdaptiveMode.Scale:
                rectTrm.sizeDelta = baseSize;
                float scale = Mathf.Max(adaptedSize.x / baseSize.x, adaptedSize.y / baseSize.y);
                rectTrm.localScale = originScale * scale;
                break;
        }
    }

    Vector2 GetBaseSize()
    {
        if (targetImage != null && targetImage.sprite != null)
        {
            return GetImageNativeSize(targetImage);
        }

        if (targetRawImage != null && targetRawImage.texture != null)
        {
            return new Vector2(targetRawImage.texture.width, targetRawImage.texture.height);
        }

        if (defaultSize.x > 0f && defaultSize.y > 0f)
        {
            return defaultSize;
        }

        return originSize;
    }

    Vector2 GetCoverSize(Vector2 sourceSize, Vector2 screenSize)
    {
        float scale = Mathf.Max(screenSize.x / sourceSize.x, screenSize.y / sourceSize.y);
        return sourceSize * scale;
    }

    Vector2 GetImageNativeSize(Image image)
    {
        float pixelsPerUnit = 1f;
        Canvas canvas = image.canvas;
        if (canvas != null && canvas.referencePixelsPerUnit > 0f)
        {
            pixelsPerUnit = image.sprite.pixelsPerUnit / canvas.referencePixelsPerUnit;
        }

        if (pixelsPerUnit <= 0f)
        {
            pixelsPerUnit = 1f;
        }

        return new Vector2(
            image.sprite.rect.width / pixelsPerUnit,
            image.sprite.rect.height / pixelsPerUnit
        );
    }
}
