using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mutation.View
{
    /// <summary>
    /// Dumb hover relay: surfaces uGUI pointer enter/exit as plain C# events so sibling
    /// views can react without implementing the EventSystem interfaces themselves.
    /// </summary>
    public class PointerHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action Entered;
        public event Action Exited;

        public void OnPointerEnter(PointerEventData eventData)
        {
            Entered?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Exited?.Invoke();
        }
    }
}
