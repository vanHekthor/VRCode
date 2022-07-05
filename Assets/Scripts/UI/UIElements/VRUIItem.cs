using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace VRVis.UI.UIElements {

    /// <summary>
    /// Helps when using UI Elements in VR to automatically add and auto-transform a BoxCollider to the UI Element size.
    /// Box Colliders are needed to recognize hover and/or collision with the hand which is a requirement for Interactables.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VRUIItem : MonoBehaviour, IPointerClickHandler {
        [Serializable]
        public class PointerClickEvent : UnityEvent<PointerEventData> { };
        public PointerClickEvent onClick;

        private BoxCollider _boxCollider;
        private RectTransform _rectTransform;

        private void OnEnable() {
            ValidateCollider();
        }

        private void Update() {
            ValidateCollider();
        }

        private void OnValidate() {
            ValidateCollider();
        }

        /// <summary>
        /// Matches the size of the BoxCollider to the Rect Transform of the GameObject.
        /// </summary>
        private void ValidateCollider() {
            if (!_rectTransform) _rectTransform = GetComponent<RectTransform>();

            if (!_boxCollider) _boxCollider = GetComponent<BoxCollider>();
            if (!_boxCollider) _boxCollider = gameObject.AddComponent<BoxCollider>();

            _boxCollider.size = _rectTransform.rect.size;

            // Centering BoxCollider.
            // The actual center of the RectTransform should be the center of the collider and not the pivot.
            var boxCenter = new Vector2();

            boxCenter = new Vector2(
                (0.5f - _rectTransform.pivot.x) * _rectTransform.rect.width, 
                (0.5f - _rectTransform.pivot.y) * _rectTransform.rect.height);

            _boxCollider.center = new Vector3(boxCenter.x, boxCenter.y);
        }

        public void OnPointerClick(PointerEventData eventData) {
            onClick.Invoke(eventData);    
        }
    }
}