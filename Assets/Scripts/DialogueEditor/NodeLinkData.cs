using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class NodeLinkData
{
    public string baseNodeGuid;
    public string portName;
    public string targetNodeGuid;
}
