using TMPro;
using UnityEngine;

namespace PicoElderCare.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ElderCareButtonTextFitter : MonoBehaviour
    {
        public RectTransform iconSlot;
        public TMP_Text label;
        public bool forceIconOnly;
        public bool hideTextWhenIconOnly = true;
        public float iconOnlyMaxWidth = 72f;
        public float horizontalPadding = 16f;
        public float verticalPadding = 8f;
        public float iconTextGap = 8f;
        public float fontSizeMin = 20f;
        public float fontSizeMax = 30f;

        private RectTransform _rectTransform;

        public static ElderCareButtonTextFitter Configure(
            GameObject buttonObject,
            RectTransform icon,
            TMP_Text text,
            float minFontSize,
            float maxFontSize,
            bool iconOnly = false)
        {
            if (buttonObject == null) return null;

            var fitter = buttonObject.GetComponent<ElderCareButtonTextFitter>();
            if (fitter == null)
            {
                fitter = buttonObject.AddComponent<ElderCareButtonTextFitter>();
            }

            fitter.iconSlot = icon;
            fitter.label = text;
            fitter.forceIconOnly = iconOnly;
            fitter.fontSizeMin = Mathf.Max(10f, minFontSize);
            fitter.fontSizeMax = Mathf.Max(fitter.fontSizeMin, maxFontSize);
            fitter.ApplyLayout();
            return fitter;
        }

        private void OnEnable()
        {
            ApplyLayout();
        }

        private void OnValidate()
        {
            ApplyLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyLayout();
        }

        public void ApplyLayout()
        {
            if (_rectTransform == null)
            {
                _rectTransform = transform as RectTransform;
            }

            if (_rectTransform == null) return;

            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>(true);
            }

            var hasIcon = iconSlot != null;
            var iconOnly = hasIcon && (forceIconOnly || _rectTransform.rect.width <= iconOnlyMaxWidth);
            ConfigureIconSlot(hasIcon, iconOnly);
            ConfigureTextSlot(hasIcon, iconOnly);
        }

        private void ConfigureIconSlot(bool hasIcon, bool iconOnly)
        {
            if (!hasIcon) return;

            var height = Mathf.Max(1f, _rectTransform.rect.height);
            var effectiveHorizontalPadding = ResolveHorizontalPadding();
            var slotSize = Mathf.Clamp(height - verticalPadding * 2f, 18f, 36f);
            iconSlot.anchorMin = new Vector2(iconOnly ? 0.5f : 0f, 0.5f);
            iconSlot.anchorMax = iconSlot.anchorMin;
            iconSlot.pivot = new Vector2(0.5f, 0.5f);
            iconSlot.sizeDelta = new Vector2(slotSize, slotSize);
            iconSlot.anchoredPosition = iconOnly
                ? Vector2.zero
                : new Vector2(effectiveHorizontalPadding + slotSize * 0.5f, 0f);
            iconSlot.localRotation = Quaternion.identity;
            iconSlot.localScale = Vector3.one;
        }

        private void ConfigureTextSlot(bool hasIcon, bool iconOnly)
        {
            if (label == null) return;

            var labelObject = label.gameObject;
            var shouldShowLabel = !iconOnly || !hideTextWhenIconOnly;
            if (labelObject.activeSelf != shouldShowLabel)
            {
                labelObject.SetActive(shouldShowLabel);
            }

            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(10f, fontSizeMin);
            label.fontSizeMax = Mathf.Max(label.fontSizeMin, fontSizeMax);
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.margin = new Vector4(4f, 2f, 4f, 2f);
            label.raycastTarget = false;

            if (!shouldShowLabel) return;

            var labelRect = label.rectTransform;
            var iconWidth = hasIcon ? Mathf.Max(18f, iconSlot.sizeDelta.x) : 0f;
            var effectiveHorizontalPadding = ResolveHorizontalPadding();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.offsetMin = new Vector2(
                effectiveHorizontalPadding + (hasIcon ? iconWidth + iconTextGap : 0f),
                verticalPadding);
            labelRect.offsetMax = new Vector2(-effectiveHorizontalPadding, -verticalPadding);
            labelRect.localRotation = Quaternion.identity;
            labelRect.localScale = Vector3.one;
            label.alignment = hasIcon ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
        }

        private float ResolveHorizontalPadding()
        {
            return Mathf.Min(horizontalPadding, Mathf.Max(1f, _rectTransform.rect.width * 0.18f));
        }
    }
}
