using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Code by github.com/S1r0hub.
 * 
 * Created: 2018/11/22
 * Updated: 2018/11/22
 */
namespace VRVis.Interaction.LaserPointer {

    abstract public class IUILaserPointerPickup : ALaserPointer {

        protected override void Update() {
            // Don't do anything.
            // Done by "HandAttachedUpdate" function instead.
        }

    }

}
