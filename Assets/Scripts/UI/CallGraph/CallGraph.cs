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
            CreateNodeFromEdgeEnd(edge);
        }

        foreach (var edge in edges) {
            CreateNodeFromEdgeStartAndConnect(edge);
        }
    }

    private void CreateNodeFromEdgeEnd(Edge edge) {
        string filePath = edge.GetTo().file.Replace('/', '.');
        int methodStartLine = edge.GetTo().lines.from;
        int methodEndLine = edge.GetTo().lines.to;

        string nodeId = $"{filePath}:{methodStartLine}";

        if (nodes.ContainsKey(nodeId)) {
            return;
        }

        var node = Instantiate(nodePrefab);
        node.name = nodeId;
        node.transform.SetParent(nodesContainer, false);        

        var callGraphNodeComponent = node.GetComponent<CallGraphNode>();
        SetupToNode(callGraphNodeComponent, edge.GetTo(), nodeId, edge.GetLabel());

        nodes.Add(nodeId, callGraphNodeComponent);
    }

    private void CreateNodeFromEdgeStartAndConnect(Edge edge) {
        string filePath = edge.GetFrom().file.Replace('/', '.');
        int methodStartLine = edge.GetFrom().callMethodLines.from;
        int methodEndLine = edge.GetFrom().callMethodLines.to;

        string nodeId = $"{filePath}:{methodStartLine}";

        CallGraphNode callGraphNodeComponent;
        if (nodes.ContainsKey(nodeId)) {
            callGraphNodeComponent = nodes[nodeId];
        }
        else {
            var node = Instantiate(nodePrefab);
            node.name = nodeId;
            node.transform.SetParent(nodesContainer, false);

            callGraphNodeComponent = node.GetComponent<CallGraphNode>();
            SetupFromNode(callGraphNodeComponent, edge.GetFrom(), nodeId, "Call to " + edge.GetLabel());

            nodes.Add(nodeId, callGraphNodeComponent);
        }

        string targetFilePath = edge.GetTo().file.Replace('/', '.');
        int targetMethodStartLine = edge.GetTo().lines.from;
        string targetNodeId = $"{targetFilePath}:{targetMethodStartLine}";

        if (!nodes.ContainsKey(targetNodeId)) return;

        nodes[targetNodeId].AddPreviousNode(callGraphNodeComponent);

        CreateConnection(callGraphNodeComponent, nodes[targetNodeId]);
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

    public void CreateConnection(CallGraphNode startNode, CallGraphNode endNode) {
        var connectionObject = Instantiate(connectionPrefab);
        var connectionComponent = connectionObject.GetComponent<Connection>();

        var startNodeRectTransform = startNode.transform.GetComponent<RectTransform>();
        var endNodeRectTransform = endNode.transform.GetComponent<RectTransform>();

        connectionComponent.target[0] = startNodeRectTransform;
        connectionComponent.target[1] = endNodeRectTransform;
    }

    private void SetupToNode(CallGraphNode node, NodeLocation nodeLocation, string nodeId, string edgeLabel) {
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

    private void SetupFromNode(CallGraphNode node, NodeLocation nodeLocation, string nodeId, string edgeLabel) {
        node.filePath = nodeLocation.file;
        node.methodStartLine = nodeLocation.callMethodLines.from;
        node.methodEndLine = nodeLocation.callMethodLines.to;
        node.SetId(nodeId);

        //var splitLabel = edgeLabel.Split('.');
        //string methodCallName = splitLabel.Length == 1 ? splitLabel[0] : splitLabel[splitLabel.Length - 1];
        string methodCallName = edgeLabel;
        node.methodName = methodCallName;

        node.ChangeNodeLabel(methodCallName);
    }
}
