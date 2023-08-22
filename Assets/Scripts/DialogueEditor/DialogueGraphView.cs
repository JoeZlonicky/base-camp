using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphView : GraphView
{
    public readonly Vector2 _defaultNodeSize = new Vector2(300, 200);
    
    public  DialogueGraphView()
    {
        styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/DialogueEditor/DialogueGraph.uss"));
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
        
        AddElement(GenerateEntryPointNode());
    }

    private Port GeneratePort(DialogueNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        ports.ForEach((port) =>
        {
            if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
            {
                compatiblePorts.Add(port);
            }
        });
        return compatiblePorts;
    }
    
    private DialogueNode GenerateEntryPointNode()
    {
        var node = new DialogueNode()
        {
            title = "ENTRY POINT",
            Guid = Guid.NewGuid().ToString(),
            DialogueText = "ENTRY POINT",
            EntryPoint = true
        };

        var generatedPort = GeneratePort(node, Direction.Output);
        generatedPort.portName = "Next";
        node.outputContainer.Add(generatedPort);

        node.capabilities |= Capabilities.Snappable;
        
        node.RefreshExpandedState();
        node.RefreshPorts();
        
        node.SetPosition(new Rect(100, 200, _defaultNodeSize.x, _defaultNodeSize.y));
        return node;
    }

    public void CreateNode(string nodeName)
    {
        AddElement(CreateDialogueNode(nodeName));
    }
    
    public DialogueNode CreateDialogueNode(string nodeText)
    {
        var node = new DialogueNode
        {
            title = LimitNodeTitleLength(nodeText, 20),
            DialogueText = nodeText,
            Guid = Guid.NewGuid().ToString()
        };

        var inputPort = GeneratePort(node, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        node.inputContainer.Add(inputPort);

        var button = new Button(() =>
        {
            AddChoicePort(node);
        })
        {
            text = "New Choice"
        };
        node.titleContainer.Add(button);

        var textField = new TextField(string.Empty);
        textField.RegisterValueChangedCallback(e =>
        {
            node.DialogueText = e.newValue;
            node.title = LimitNodeTitleLength(e.newValue, 20);
        });
        textField.SetValueWithoutNotify(node.DialogueText);
        node.mainContainer.Add(textField);
        
        node.capabilities |= Capabilities.Snappable;
        
        node.RefreshExpandedState();
        node.RefreshPorts();
        node.SetPosition(new Rect(Vector2.zero, _defaultNodeSize));
        return node;
    }

    public void AddChoicePort(DialogueNode node, string overridePortName = "")
    {
        var generatedPort = GeneratePort(node, Direction.Output);

        var oldLabel = generatedPort.contentContainer.Q<Label>("type");
        generatedPort.contentContainer.Remove(oldLabel);
        var connector = generatedPort.contentContainer.Q<VisualElement>("connector");
        connector.pickingMode = PickingMode.Position;
        
        var outputPortCount = node.outputContainer.Query("connector").ToList().Count;
        var portName = string.IsNullOrEmpty(overridePortName) ? $"Choice {outputPortCount}" : overridePortName;

        var textField = new TextField
        {
            name = string.Empty,
            value = portName,
        };
        
        textField.RegisterValueChangedCallback(e => generatedPort.portName = e.newValue);
        textField.AddToClassList("DialogueChoiceTextField");
        
        var deleteButton = new Button(() => RemovePort(node, generatedPort))
        {
            text = "X"
        };
        generatedPort.contentContainer.Add(new Label("  "));
        generatedPort.contentContainer.Add(textField);
        
        generatedPort.contentContainer.Add(deleteButton);


        generatedPort.portName = portName;
        node.outputContainer.Add(generatedPort);
        node.RefreshPorts();
        node.RefreshExpandedState();
    }

    private void RemovePort(DialogueNode node, Port generatedPort)
    {
        var targetEdge = edges.ToList().Where(x =>
            x.output.portName == generatedPort.portName && x.output.node == generatedPort.node);

        if (targetEdge.Any())
        {
            var edge = targetEdge.First();
            edge.input.Disconnect(edge);
            RemoveElement(targetEdge.First());
        }

        node.outputContainer.Remove(generatedPort);
        node.RefreshPorts();
        node.RefreshExpandedState();
    }

    private string LimitNodeTitleLength(string title, int maxLength)
    {
        if (title.Length > maxLength)
        {
            return title.Substring(0, maxLength) + "...";
        }
        return title;
    }
}