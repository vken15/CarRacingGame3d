using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CarRacingGame3d
{
    public class ResizableUI : MonoBehaviour, IDragHandler
    {
        [SerializeField] private RectTransform targetRectTransform;
        [SerializeField] private Vector2 xSizeBoundary;
        [SerializeField] private Vector2 ySizeBoundary;
        [SerializeField] private float scaleFactor = 1.2f;

        public void OnDrag(PointerEventData eventData)
        {
            float newSizeX = targetRectTransform.sizeDelta.x + eventData.delta.x;
            float newSizeY = targetRectTransform.sizeDelta.y + eventData.delta.y;

            float xDelta = Mathf.Clamp(newSizeX, xSizeBoundary.x, xSizeBoundary.y) - targetRectTransform.sizeDelta.x;
            float yDelta = Mathf.Clamp(newSizeY, ySizeBoundary.x, ySizeBoundary.y) - targetRectTransform.sizeDelta.y;

            targetRectTransform.sizeDelta = new Vector2(xDelta, yDelta);
            targetRectTransform.anchoredPosition = new Vector2(xDelta, yDelta) / 2 / scaleFactor;
        }
    }
}
