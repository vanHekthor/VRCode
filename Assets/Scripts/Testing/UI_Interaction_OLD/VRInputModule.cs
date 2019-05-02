using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


/**
 * VRInput Module Class for VR laser pointer.
 * Inspired by:
 * https://github.com/wacki/Unity-VRInputModule/blob/master/Assets/VRInputModule/Scripts/LaserPointerInputModule.cs
 * https://gist.github.com/flarb/052467190b84657f10d2
 */
public class VRInputModule : BaseInputModule
{
    public static VRInputModule instance { get { return _instance; } }
    private static VRInputModule _instance = null;

    protected override void Awake() {

        base.Awake();

        // ensure that there is only one instance
        if (_instance != null) {
            Debug.LogWarning("There can only be one VRInputModule instance!");
            DestroyImmediate(this);
            return;
        }

        _instance = this;
    }

    // Process the current tick for the module.
    // Executed once per Update call.
    // https://docs.unity3d.com/ScriptReference/EventSystems.BaseInputModule.Process.html
    public override void Process() {
        
        
    }
}
