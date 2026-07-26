using UnityEngine;

public class ArcheryScorePopup : MonoBehaviour
{
    public float riseMetersPerSecond = 0.32f;
    public float lifetimeSeconds = 1.5f;
    public float fadeStartSeconds = 0.75f;

    private static Font _sharedFont;

    private TextMesh _text;
    private Color _baseColor;
    private float _age;
    private Transform _cameraTransform;

    public static ArcheryScorePopup Spawn(Vector3 worldPosition, string message, Color color, float characterSize = 0.032f, Font font = null)
    {
        var go = new GameObject("ArcheryScorePopup");
        go.transform.position = worldPosition;
        // MR 模式下背景抑制器按层级扫描渲染物，根级生成的飘分挂上保护标记以免被隐藏。
        go.AddComponent<MrKeepVisible>();

        var popup = go.AddComponent<ArcheryScorePopup>();
        var text = go.AddComponent<TextMesh>();
        text.text = message;
        // 真机上必须优先用项目自带的中文字体：Android/PICO 的 OS 字体名解析不可靠，
        // 回落字体没有中文字形会导致飘分显示为方块。
        text.font = font != null ? font : ResolveFont();
        text.fontSize = 72;
        text.characterSize = characterSize;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null && text.font != null)
        {
            renderer.sharedMaterial = text.font.material;
        }

        popup._text = text;
        popup._baseColor = color;
        return popup;
    }

    private void Start()
    {
        _cameraTransform = Camera.main != null ? Camera.main.transform : null;
        FaceCamera();
    }

    private void Update()
    {
        _age += Time.deltaTime;
        if (_age >= lifetimeSeconds)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += Vector3.up * (riseMetersPerSecond * Time.deltaTime);
        FaceCamera();

        if (_text != null && _age > fadeStartSeconds)
        {
            var fade01 = Mathf.InverseLerp(lifetimeSeconds, fadeStartSeconds, _age);
            var color = _baseColor;
            color.a = _baseColor.a * fade01;
            _text.color = color;
        }
    }

    private void FaceCamera()
    {
        if (_cameraTransform == null) return;

        var toPopup = transform.position - _cameraTransform.position;
        toPopup.y = 0f;
        if (toPopup.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(toPopup.normalized, Vector3.up);
    }

    private static Font ResolveFont()
    {
        if (_sharedFont != null) return _sharedFont;

        _sharedFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Source Han Sans SC", "Arial" },
            72);

        if (_sharedFont == null)
        {
            _sharedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return _sharedFont;
    }
}
