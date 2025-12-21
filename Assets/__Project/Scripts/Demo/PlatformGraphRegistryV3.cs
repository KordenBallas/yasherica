using System.Collections.Generic;
using UnityEngine;

public class PlatformGraphRegistry : MonoBehaviour
{
    public static PlatformGraphRegistry Instance;

    public class Node
    {
        public int id;
        public GameObject platform;
        public Vector3 worldPos;
        public List<Vector3> topBoundary = new(); // ordered top outline
        public List<Node> neighbors = new List<Node>();
    }

    public List<Node> nodes = new List<Node>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    
    public void RegisterNode(Node node)
    {
        nodes.Add(node);
        Debug.Log("Registered platform node ID " + node.id + " at " + node.worldPos);
    }

    public Node GetNearestNode(Vector3 worldPosition)
    {
        Node best = null;
        float bestDist = float.MaxValue;

        foreach (var n in nodes)
        {
            float d = Vector3.Distance(worldPosition, n.worldPos);
            if (d < bestDist)
            {
                bestDist = d;
                best = n;
            }
        }
        return best;
    }
}