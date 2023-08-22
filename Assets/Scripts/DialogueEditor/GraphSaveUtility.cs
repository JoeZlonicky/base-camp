using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphSaveUtility
{
    private DialogueGraphView _targetGraphView;
    private DialogueContainer _containerCache;

    private List<Edge> Edges => _targetGraphView.edges.ToList();
    private List<DialogueNode> Nodes => _targetGraphView.nodes.ToList().Cast<DialogueNode>().ToList();
    
    public static GraphSaveUtility GetInstance(DialogueGraphView targetGraphView)
    {
        return new GraphSaveUtility()
        {
            _targetGraphView = targetGraphView
        };
    }

    public void SaveGraph(string fileName)
    {
        if (!Edges.Any()) return;

        var dialogueContainer = ScriptableObject.CreateInstance<DialogueContainer>();

        var connectedPorts = Edges.Where(x => x.input.node != null).ToArray();
        foreach (var t in connectedPorts)
        {
            var outputNode = t.output.node as DialogueNode;
            var inputNode = t.input.node as DialogueNode;
            
            dialogueContainer.nodeLinks.Add(new NodeLinkData
            {
                baseNodeGuid = outputNode.Guid,
                portName = t.output.portName,
                targetNodeGuid = inputNode.Guid
            });
        }

        foreach (var dialogueNode in Nodes.Where(node => !node.EntryPoint))
        {
            dialogueContainer.dialogueNodeData.Add(new DialogueNodeData {
                    guid = dialogueNode.Guid,
                    dialogueText = dialogueNode.DialogueText,
                    position = dialogueNode.GetPosition().position
                }
            );
        }
        
        AssetDatabase.CreateAsset(dialogueContainer, $"Assets/Scripts/DialogueEditor/Resources/{fileName}.asset");
        AssetDatabase.SaveAssets();
    }

    public void LoadGraph(string fileName)
    {
        _containerCache = Resources.Load<DialogueContainer>(fileName);
        if (_containerCache == null)
        {
            EditorUtility.DisplayDialog("File Not Found", "Target dialogue graph does not exist!", "OK");
            return;
        }

        ClearGraph();
        CreateNodes();
        ConnectNodes();
    }

    private void ConnectNodes()
    {
        foreach (var node in Nodes)
        {
            var connections = _containerCache.nodeLinks.Where(x => x.baseNodeGuid == node.Guid).ToList();
            for (var i = 0; i < connections.Count; i++)
            {
                var connection = connections[i];
                var targetNodeGuid = connection.targetNodeGuid;
                var targetNode = Nodes.First(x => x.Guid == targetNodeGuid);
                LinkNodes(node.outputContainer[i].Q<Port>(), (Port) targetNode.inputContainer[0]);
                
                targetNode.SetPosition(new Rect(
                    _containerCache.dialogueNodeData.First(x => x.guid == targetNodeGuid).position,
                    _targetGraphView._defaultNodeSize));
            }
        }
    }

    private void LinkNodes(Port output, Port input)
    {
        var tempEdge = new Edge
        {
            output = output,
            input = input
        };
        tempEdge.input.Connect(tempEdge);
        tempEdge.output.Connect(tempEdge);
        _targetGraphView.Add(tempEdge);
    }

    private void CreateNodes()
    {
        foreach (var nodeData in _containerCache.dialogueNodeData)
        {
            var tempNode = _targetGraphView.CreateDialogueNode(nodeData.dialogueText);
            tempNode.Guid = nodeData.guid;
            _targetGraphView.AddElement(tempNode);

            var nodePorts = _containerCache.nodeLinks.Where(x => x.baseNodeGuid == nodeData.guid).ToList();
            nodePorts.ForEach(x => _targetGraphView.AddChoicePort(tempNode, x.portName));
        }
    }

    private void ClearGraph()
    {
        Nodes.Find(x => x.EntryPoint).Guid = _containerCache.nodeLinks[0].baseNodeGuid;

        foreach (var node in Nodes)
        {
            if (node.EntryPoint) continue;
            
            Edges.Where(x => x.input.node == node).ToList().ForEach(edge => _targetGraphView.RemoveElement(edge));
            
            _targetGraphView.RemoveElement(node);
        }
    }
}