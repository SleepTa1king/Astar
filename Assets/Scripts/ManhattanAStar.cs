using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class ManhattanAStar : MonoBehaviour {
    private Grid grid;

    void Start() {
        grid = GetComponent<Grid>();
        if (grid == null) {
            Debug.LogError("ManhattanAStar: 找不到 Grid 组件！");
        }
    }

    void Update() {
        if (grid != null && grid.player != null && grid.destPos != null) {
            FindPath(grid.player.position, grid.destPos.position);
        }
    }

    void FindPath(Vector3 startPos, Vector3 endPos) {
        Grid.NodeItem startNode = grid.getItem(startPos);
        Grid.NodeItem endNode = grid.getItem(endPos);

        grid.ClearGridCosts();
        
        startNode.gCost = 0;
        startNode.hCost = GetManhattanDistance(startNode, endNode);

        // 使用自定义优先级队列代替 List
        SimplePriorityQueue openSet = new SimplePriorityQueue();
        HashSet<Grid.NodeItem> closeSet = new HashSet<Grid.NodeItem>();
        
        openSet.Enqueue(startNode);

        while (openSet.Count > 0) {
            // 现在取最小值是 O(log n)，非常快！
            Grid.NodeItem curNode = openSet.Dequeue();

            if (closeSet.Contains(curNode)) continue;
            closeSet.Add(curNode);

            if (curNode == endNode) {
                GeneratePath(startNode, endNode);
                return;
            }

            foreach (var neighbor in grid.getNeibourhood(curNode, false)) {
                if (neighbor.isWall || closeSet.Contains(neighbor)) {
                    continue;
                }

                int newMovementCostToNeighbor = curNode.gCost + 10;

                if (newMovementCostToNeighbor < neighbor.gCost) {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetManhattanDistance(neighbor, endNode);
                    neighbor.parent = curNode;

                    // 重新加入队列（对于重复节点，closeSet 会在 Dequeue 时过滤）
                    openSet.Enqueue(neighbor);
                }
            }
        }
        
        grid.updatePath(new List<Grid.NodeItem>());
    }

    int GetManhattanDistance(Grid.NodeItem a, Grid.NodeItem b) {
        return 10 * (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y));
    }

    void GeneratePath(Grid.NodeItem startNode, Grid.NodeItem endNode) {
        List<Grid.NodeItem> path = new List<Grid.NodeItem>();
        Grid.NodeItem temp = endNode;

        while (temp != null) {
            path.Add(temp);
            if (temp == startNode) break;
            temp = temp.parent;
        }
        path.Reverse();

        grid.updatePath(path);
    }
}

/// <summary>
/// 一个简单的基于最小堆实现的优先级队列
/// </summary>
public class SimplePriorityQueue {
    private List<Grid.NodeItem> nodes = new List<Grid.NodeItem>();

    public int Count => nodes.Count;

    public void Enqueue(Grid.NodeItem node) {
        nodes.Add(node);
        int i = nodes.Count - 1;
        while (i > 0) {
            int p = (i - 1) / 2;
            if (Compare(nodes[i], nodes[p]) >= 0) break;
            Swap(i, p);
            i = p;
        }
    }

    public Grid.NodeItem Dequeue() {
        Grid.NodeItem root = nodes[0];
        int last = nodes.Count - 1;
        nodes[0] = nodes[last];
        nodes.RemoveAt(last);

        int i = 0;
        while (true) {
            int left = i * 2 + 1;
            int right = i * 2 + 2;
            int smallest = i;

            if (left < nodes.Count && Compare(nodes[left], nodes[smallest]) < 0) smallest = left;
            if (right < nodes.Count && Compare(nodes[right], nodes[smallest]) < 0) smallest = right;

            if (smallest == i) break;
            Swap(i, smallest);
            i = smallest;
        }
        return root;
    }

    private int Compare(Grid.NodeItem a, Grid.NodeItem b) {
        if (a.fCost != b.fCost) return a.fCost.CompareTo(b.fCost);
        return a.hCost.CompareTo(b.hCost); // fCost 相等时，hCost 小的优先
    }

    private void Swap(int i, int j) {
        Grid.NodeItem temp = nodes[i];
        nodes[i] = nodes[j];
        nodes[j] = temp;
    }
}
