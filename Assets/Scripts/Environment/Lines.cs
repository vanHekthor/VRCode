using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// See also: https://docs.unity3d.com/ScriptReference/GL.html
public class Lines : MonoBehaviour {

    public bool isActive = true;
    public bool showGizmo = true;

    public float grid_width = 50;
    public float grid_height = 50;
    public float grid_lines = 50;
    public Color grid_color = new Color(0.5f, 0.5f, 0.5f, 0.1f);

    public Transform centerTransform;

    public Material lineMaterial;
	

    /**
     * Creates the material used by lines.
     */
     /*
    void CreateLineMaterial() {
        if (!lineMaterial) {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;

            // get property IDs
            int ID_SrcBlend = Shader.PropertyToID("_SrcBlend");
            int ID_DstBlend = Shader.PropertyToID("_DstBlend");
            int ID_Cull = Shader.PropertyToID("_Cull");
            int ID_ZWrite = Shader.PropertyToID("_ZWrite");

            // turn on alpha blending (how new fragment values will be added/blend into the existing color values)
            lineMaterial.SetInt(ID_SrcBlend, (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt(ID_DstBlend, (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            // turn backface culling off (show triangles that look away from us as well)
            lineMaterial.SetInt(ID_Cull, (int) UnityEngine.Rendering.CullMode.Off);

            // turn off depth writes (see https://docs.unity3d.com/Manual/SL-CullAndDepth.html)
            lineMaterial.SetInt(ID_ZWrite, 0);
        }
    }
    */

    void CreateLineMaterial() {
        if (!lineMaterial) {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;

            // get property IDs
            int ID_SrcBlend = Shader.PropertyToID("_SrcBlend");
            int ID_DstBlend = Shader.PropertyToID("_DstBlend");
            int ID_Cull = Shader.PropertyToID("_Cull");
            int ID_ZWrite = Shader.PropertyToID("_ZWrite");

            // turn on alpha blending (how new fragment values will be added/blend into the existing color values)
            lineMaterial.SetInt(ID_SrcBlend, (int) UnityEngine.Rendering.BlendMode.OneMinusDstAlpha);
            lineMaterial.SetInt(ID_DstBlend, (int) UnityEngine.Rendering.BlendMode.DstAlpha);

            // turn backface culling off (show triangles that look away from us as well)
            lineMaterial.SetInt(ID_Cull, (int) UnityEngine.Rendering.CullMode.Off);

            // turn off depth writes (see https://docs.unity3d.com/Manual/SL-CullAndDepth.html)
            lineMaterial.SetInt(ID_ZWrite, 0);
        }
    }


    /**
     * Show the grid we render as a Gizmo.
     */
    void OnDrawGizmos() {
        if (showGizmo && isActive) {
            RenderEnvironmentLines();
        }
    }


    /**
     * Called after a camera has finished rendering the scene.
     */
    void OnPostRender() {
        if (isActive) { RenderEnvironmentLines(); }
    }


    void RenderEnvironmentLines() {

        // creates the line material (only if it does not exist yet)
        CreateLineMaterial();

        // set material (activate for rendering)
        lineMaterial.SetPass(0);

        //GL.PushMatrix();
	    //GL.MultMatrix(transform.localToWorldMatrix);

        // draw the lines
        GL.Begin(GL.LINES);
        GL.Color(grid_color);

        // center of grid
        Vector3 center = Vector3.zero;
        if (centerTransform) { center = centerTransform.position; }

        // start and end of the x-axis line on the line axis
        float half_width = grid_width * 0.5f;
        float half_height = grid_height * 0.5f;
        Vector3 x_start_left = center + Vector3.left * half_width + Vector3.back * half_height;
        Vector3 x_start_right = center + Vector3.right * half_width + Vector3.back * half_height;
        //Debug.Log("x_start_left: " + x_start_left + ", x_start_right: " + x_start_right);
        Vector3 y_start_down = center + Vector3.back * half_height + Vector3.left * half_width;
        Vector3 y_start_up = center + Vector3.forward * half_height + Vector3.left * half_width;

        for (int i = 0; i < grid_lines; i++) {

            float perc = (float) i / (float) (grid_lines - 1);

            // draw x grid line
            Vector3 x_move_up = perc * grid_height * Vector3.forward;
            Vector3 x_from = x_start_left + x_move_up;
            Vector3 x_to = x_start_right + x_move_up;
            //Debug.Log("[" + i + "] x_from: " + x_from + ", x_to: " + x_to);
            GL.Vertex3(x_from.x, x_from.y, x_from.z);
            GL.Vertex3(x_to.x, x_to.y, x_to.z);

            // draw y grid line
            Vector3 y_move_right = perc * grid_width * Vector3.right;
            Vector3 y_from = y_start_down + y_move_right;
            Vector3 y_to = y_start_up + y_move_right;
            //Debug.Log("[" + i + "] y_from: " + y_from + ", y_to: " + y_to);
            GL.Vertex3(y_from.x, y_from.y, y_from.z);
            GL.Vertex3(y_to.x, y_to.y, y_to.z);
        }

        GL.End();
    }

}
