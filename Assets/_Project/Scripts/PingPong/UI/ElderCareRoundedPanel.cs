using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class ElderCareRoundedPanel : MaskableGraphic
{
    public float cornerRadius = 32f;
    public int cornerSegments = 8;

    [SerializeField]
    private bool drawStroke;

    [SerializeField]
    private Color strokeColor = Color.clear;

    [SerializeField, Min(0f)]
    private float strokeWidth;

    public bool DrawStroke
    {
        get => drawStroke;
        set
        {
            if (drawStroke == value) return;
            drawStroke = value;
            SetVerticesDirty();
        }
    }

    public Color StrokeColor
    {
        get => strokeColor;
        set
        {
            if (strokeColor == value) return;
            strokeColor = value;
            SetVerticesDirty();
        }
    }

    public float StrokeWidth
    {
        get => strokeWidth;
        set
        {
            var clamped = Mathf.Max(0f, value);
            if (Mathf.Approximately(strokeWidth, clamped)) return;
            strokeWidth = clamped;
            SetVerticesDirty();
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        cornerRadius = Mathf.Max(0f, cornerRadius);
        cornerSegments = Mathf.Max(2, cornerSegments);
        strokeWidth = Mathf.Max(0f, strokeWidth);

        if (IsActive())
        {
            SetVerticesDirty();
        }
    }
#endif

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        var rect = rectTransform.rect;
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        var radius = Mathf.Min(cornerRadius, rect.width * 0.5f, rect.height * 0.5f);
        var segments = Mathf.Max(2, cornerSegments);
        var hasStroke = drawStroke && strokeWidth > 0f && strokeColor.a > 0f;
        if (!hasStroke)
        {
            var points = new List<Vector2>();
            AddContour(points, rect, radius, segments, false);
            AddFilledContour(vh, points, rect.center, color);
            return;
        }

        var clampedStrokeWidth = Mathf.Min(strokeWidth, Mathf.Min(rect.width, rect.height) * 0.5f);
        var innerRect = new Rect(
            rect.xMin + clampedStrokeWidth,
            rect.yMin + clampedStrokeWidth,
            Mathf.Max(0f, rect.width - clampedStrokeWidth * 2f),
            Mathf.Max(0f, rect.height - clampedStrokeWidth * 2f));

        var outerPoints = new List<Vector2>();
        AddContour(outerPoints, rect, radius, segments, true);

        if (innerRect.width <= 0.001f || innerRect.height <= 0.001f)
        {
            AddFilledContour(vh, outerPoints, rect.center, strokeColor);
            return;
        }

        var innerRadius = Mathf.Min(
            Mathf.Max(0f, radius - clampedStrokeWidth),
            innerRect.width * 0.5f,
            innerRect.height * 0.5f);
        var innerPoints = new List<Vector2>();
        AddContour(innerPoints, innerRect, innerRadius, segments, true);

        AddStrokeRing(vh, outerPoints, innerPoints, strokeColor);
        AddFilledContour(vh, innerPoints, innerRect.center, color);
    }

    private static void AddContour(List<Vector2> points, Rect rect, float radius, int segments, bool keepSegmentCount)
    {
        if (radius <= 0.001f && !keepSegmentCount)
        {
            AddRect(points, rect);
        }
        else
        {
            AddArc(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, -90f, 0f, segments);
            AddArc(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, segments);
            AddArc(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, segments);
            AddArc(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, segments);
        }
    }

    private static void AddFilledContour(VertexHelper vh, List<Vector2> points, Vector2 center, Color fillColor)
    {
        var c = (Color32)fillColor;
        var centerIndex = vh.currentVertCount;
        vh.AddVert(center, c, Vector2.zero);
        var firstPointIndex = vh.currentVertCount;
        for (var i = 0; i < points.Count; i++)
        {
            vh.AddVert(points[i], c, Vector2.zero);
        }

        for (var i = 0; i < points.Count; i++)
        {
            var current = firstPointIndex + i;
            var next = firstPointIndex + ((i + 1) % points.Count);
            vh.AddTriangle(centerIndex, next, current);
        }
    }

    private static void AddStrokeRing(VertexHelper vh, List<Vector2> outerPoints, List<Vector2> innerPoints, Color ringColor)
    {
        if (outerPoints.Count != innerPoints.Count || outerPoints.Count < 3)
        {
            return;
        }

        var c = (Color32)ringColor;
        var outerStart = vh.currentVertCount;
        for (var i = 0; i < outerPoints.Count; i++)
        {
            vh.AddVert(outerPoints[i], c, Vector2.zero);
        }

        var innerStart = vh.currentVertCount;
        for (var i = 0; i < innerPoints.Count; i++)
        {
            vh.AddVert(innerPoints[i], c, Vector2.zero);
        }

        for (var i = 0; i < outerPoints.Count; i++)
        {
            var next = (i + 1) % outerPoints.Count;
            vh.AddTriangle(outerStart + i, outerStart + next, innerStart + next);
            vh.AddTriangle(outerStart + i, innerStart + next, innerStart + i);
        }
    }

    private static void AddRect(List<Vector2> points, Rect rect)
    {
        points.Add(new Vector2(rect.xMin, rect.yMin));
        points.Add(new Vector2(rect.xMax, rect.yMin));
        points.Add(new Vector2(rect.xMax, rect.yMax));
        points.Add(new Vector2(rect.xMin, rect.yMax));
    }

    private static void AddArc(List<Vector2> points, Vector2 center, float radius, float startDegrees, float endDegrees, int segments)
    {
        for (var i = 0; i <= segments; i++)
        {
            var t = i / (float)segments;
            var angle = Mathf.Lerp(startDegrees, endDegrees, t) * Mathf.Deg2Rad;
            points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }
}
