using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace VRVis.Interaction.Telekinesis {
    public interface ITelekinesable {

        void OnFocus(Hand hand);

        void OnUnfocus(Hand hand);

        void OnGrab();

        void OnPull();

        void OnRelease(Ray ray);

        void OnDrag(Transform pointer);
    }
}
