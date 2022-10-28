using UnityEngine;
using UnityEngine.EventSystems;

namespace VRVis.Interaction.LaserPointer {

    /// <summary>
    /// Stores event data for laser pointer events.
    /// </summary>
    public class LaserPointerEventData : PointerEventData {

        public GameObject current;
        public ALaserPointer controller;

        public LaserPointerEventData(EventSystem e) : base(e) { }

        public override void Reset() {
            current = null;
            controller = null;
            base.Reset();
        }

    }
}
