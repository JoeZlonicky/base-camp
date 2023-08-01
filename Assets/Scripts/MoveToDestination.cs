using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MoveToDestination : MonoBehaviour
{
    public Transform goal;
    
    private void Start()
    {
        if (goal == null)
        {
            return;
        }
        
        var agent = GetComponent<NavMeshAgent>();
        agent.destination = goal.position;
    }
}
