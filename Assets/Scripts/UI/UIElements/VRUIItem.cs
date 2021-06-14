using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.UI.UIElements {

    /// <summary>
    /// Helps when using UI Elements in VR to automatically add and auto-transform a BoxCollider to the UI Element size.
    /// Box Colliders are needed to recognize hover and/or collision with the hand which is a requirement for Interactables.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VRUIItem : MonoBehaviour {
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
        }
    }
}