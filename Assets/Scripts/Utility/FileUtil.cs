using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VRVis.Fallback;
using VRVis.Interaction.LaserHand;
using VRVis.Interaction.LaserPointer;
using VRVis.IO.Features;
using VRVis.IO.Structure;
using VRVis.Spawner.File;

namespace VRVis.Utilities {
    public class FileUtil {
        public static void OpenClassFile(PointerEventData pointerEventData, Transform clickedAt, SNode node, Action<CodeFileReferences> callback = null) {
            Debug.Log("Start to open " + node.GetName());
            pointerEventData.Use();

            // called from fallback camera (mouse click)
            if (pointerEventData is MouseNodePickup.MousePickupEventData e) {
                MouseNodePickup mnp = e.GetMNP();
                if (e.button.Equals(PointerEventData.InputButton.Left)) {
                    mnp.AttachFileToSpawn(node, ConfigManager.GetInstance().selectedConfig, e.pointerCurrentRaycast.worldPosition, callback);
                }
            }

            // called from laser pointer controller
            if (pointerEventData is LaserPointerEventData d) {
                var laserPointer = d.controller.GetComponent<ViveUILaserPointerPickup>();
                var laserHand = d.controller.GetComponent<LaserHand>();

                if (laserPointer) {
                    laserPointer.StartCodeWindowPlacement(node, ConfigManager.GetInstance().selectedConfig, clickedAt, callback);
                }

                if (laserPointer == null) {
                    if (laserHand != null) {
                        laserHand.StartCodeWindowPlacement(node, ConfigManager.GetInstance().selectedConfig, clickedAt, callback);
                    }
                }
            }
        }
    }
}