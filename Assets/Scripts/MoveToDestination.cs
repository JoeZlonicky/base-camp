using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MoveToDestination : MonoBehaviour
{
    public Transform[] goals;

    private int _currentDestinationIndex = -1;
    private NavMeshAgent _agent;
    
    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        NextDestination();
    }

    private void NextDestination()
    {
        if (_currentDestinationIndex >= goals.Length - 1)
        {
            return;
        }

        _currentDestinationIndex += 1;
        _agent.destination = goals[_currentDestinationIndex].position;
    }

    void Update()
    {
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            NextDestination();
        }
    }
}
