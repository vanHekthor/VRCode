using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;
using VRVis.IO;
using static VRVis.JSON.Serialization.JSONEdge;

public class CallGraph : MonoBehaviour {

    public GameObject nodePrefab;
    public GameObject connectionPrefab;
    public Transform nodesContainer;
    public bool generateNodesFromEdges;

    private IEnumerable<Edge> edges;
    private Dictionary<string, CallGraphNode> nodes = new Dictionary<string, CallGraphNode>();

    void Start() {
        if (!generateNodesFromEdges) return;
        
        var edgeLoader = ApplicationLoader.GetInstance().GetEdgeLoader();
        edges = edgeLoader.GetEdges();

        foreach (var edge in edges) {
            CreateNodesFromEdge(edge);
            //CreateConnection(edge);
        }
    }

    private void CreateNodesFromEdge(Edge edge) {
        string filePath = edge.GetTo().file.Replace('/', '.');
        int methodStartLine = edge.GetTo().lines.from;
        int methodEndLine = edge.GetTo().lines.to;

        string nodeId = $"{filePath}:{methodStartLine}";

        if (nodes.ContainsKey(nodeId)) {
            if (nodes[nodeId].methodStartLine < nodes[nodeId].methodEndLine) {
                return;
            }
            Destroy(nodes[nodeId].gameObject);
        }

        var node = Instantiate(nodePrefab);
        node.name = nodeId;
        node.transform.SetParent(nodesContainer, false);        

        var callGraphNodeComponent = node.GetComponent<CallGraphNode>();
        SetupNode(callGraphNodeComponent, edge.GetTo(), nodeId, edge.GetLabel());

        if (!nodes.ContainsKey(nodeId)) {
            nodes.Add(nodeId, callGraphNodeComponent);
        }
        else {
            nodes[nodeId] = callGraphNodeComponent;
        }

        //filePath = edge.GetFrom().file.Replace('/', '.');
        //methodStartLine = edge.GetFrom().lines.from;
        //methodEndLine = edge.GetFrom().lines.to;

        //nodeId = $"{filePath}:{methodStartLine}";

        //if (nodes.ContainsKey(nodeId)) {
        //    return;
        //}

        //node = Instantiate(nodePrefab);
        //node.name = nodeId;
        //node.transform.SetParent(nodesContainer, false);

        //callGraphNodeComponent = node.GetComponent<CallGraphNode>();
        //SetupNode(callGraphNodeComponent, edge.GetFrom(), nodeId, edge.GetLabel() + "-target");

        //nodes.Add(nodeId, callGraphNodeComponent);
    }

    public void CreateConnection(Edge edge) {
        string startNodeId = $"{edge.GetFrom().file.Replace('/', '.')}:{edge.GetFrom().lines.from}";
        string endNodeId = $"{edge.GetTo().file.Replace('/', '.')}:{edge.GetTo().lines.from}";

        var connectionObject = Instantiate(connectionPrefab);
        var connectionComponent = connectionObject.GetComponent<Connection>();

        var startNode = nodesContainer.Find(startNodeId).GetComponent<RectTransform>();
        var endNode = nodesContainer.Find(endNodeId).GetComponent<RectTransform>();

        connectionComponent.target[0] = startNode;
        connectionComponent.target[1] = endNode;

        connectionComponent.Edge = edge;
    }

    private void SetupNode(CallGraphNode node, NodeLocation nodeLocation, string nodeId, string edgeLabel) {
        node.filePath = nodeLocation.file;
        node.methodStartLine = nodeLocation.lines.from;
        node.methodEndLine = nodeLocation.lines.to;
        node.SetId(nodeId);

        //var splitLabel = edgeLabel.Split('.');
        //string methodCallName = splitLabel.Length == 1 ? splitLabel[0] : splitLabel[splitLabel.Length - 1];
        string methodCallName = edgeLabel;
        node.methodName = methodCallName;

        node.ChangeNodeLabel(methodCallName);
    }
}
