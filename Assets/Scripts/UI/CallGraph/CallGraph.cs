using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;
using VRVis.IO;

public class CallGraph : MonoBehaviour {

    public GameObject nodePrefab;

    private IEnumerable<Edge> edges;
    private Dictionary<string, CallGraphNode> nodes;

    void Start() {
        var edgeLoader = ApplicationLoader.GetInstance().GetEdgeLoader();
        edges = edgeLoader.GetEdges();

        foreach (var edge in edges) {
            if (!nodes.ContainsKey(edge.GetFrom().file)) {

            }

        }
    }

    private void CreateNodesFromEdge(Edge edge) {
        var node = Instantiate(nodePrefab);
        node.transform.SetParent(transform, false);

        var callGraphNodeComponent = node.GetComponent<CallGraphNode>();
        callGraphNodeComponent.filePath = edge.GetFrom().file;
        callGraphNodeComponent.methodStartLine = edge.GetFrom().lines.from;
        callGraphNodeComponent.methodEndLine = edge.GetFrom().lines.to;
    }
}
