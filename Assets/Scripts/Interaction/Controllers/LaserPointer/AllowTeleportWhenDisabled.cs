using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace VRVis.Interaction.LaserPointer {

    /// <summary>
    /// Requires that the AllowTeleportWhileAttachedToHand component from SteamVR
    /// is attached to the same game object.<para/>
    /// Changes its state based on the callback of the laser pointer to the according method.<para/>
    /// Created: 19.09.2019 (Leon H.)<para/>
    /// Updated: 19.09.2019
    /// </summary>
    [RequireComponent(typeof(AllowTeleportWhileAttachedToHand))]
    public class AllowTeleportWhenDisabled : MonoBehaviour {

        AllowTeleportWhileAttachedToHand at;

        void Start() {
            at = GetComponent<AllowTeleportWhileAttachedToHand>();
        }

        /// <summary>
        /// Should be called by the callback method of the laser pointer.<para/>
        /// If the laser pointer is disabled, the teleport is allowed.
        /// </summary>
	    public void LaserPointerActiveStateChanged(bool active) {
            if (!at) { return; }
            at.teleportAllowed = !active;
        }

    }
}
