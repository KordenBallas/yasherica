using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Combat.Arena.View
{
    /// <summary>
    /// Dumb pointer relay on the board viewport RawImage: converts a click's screen position
    /// into the image's normalized UV (== the stage camera's viewport point) and surfaces it
    /// as a plain event. No logic beyond the rect math.
    /// </summary>
    public class DraftBoardPointerRelay : MonoBehaviour, IPointerClickHandler
    {
        public event Action<Vector2> Clicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            var rectTransform = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, eventData.position, eventData.pressEventCamera, out var local))
            {
                return;
            }

            var rect = rectTransform.rect;
            var uv = new Vector2(
                (local.x - rect.xMin) / rect.width,
                (local.y - rect.yMin) / rect.height);
            if (uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f)
            {
                Clicked?.Invoke(uv);
            }
        }
    }
}
