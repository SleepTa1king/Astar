using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Grid : MonoBehaviour {
	public GameObject NodeWall;
	public GameObject Node;

	// 节点半径
	public float NodeRadius = 0.5f;
	// 过滤墙体所在的层
	public LayerMask WhatLayer;

	// 玩家
	public Transform player;
	// 目标
	public Transform destPos;


	/// <summary>
	/// 寻路节点
	/// </summary>
	public class NodeItem {
		// 是否是墙
		public bool isWall;
		// 位置
		public Vector3 pos;
		// 格子坐标
		public int x, y;

		// 与起点的长度
		public int gCost;
		// 与目标点的长度
		public int hCost;

		// 总的路径长度
		public int fCost {
			get {return gCost + hCost; }
		}

		// 父节点
		public NodeItem parent;

		public NodeItem(bool isWall, Vector3 pos, int x, int y) {
			this.isWall = isWall;
			this.pos = pos;
			this.x = x;
			this.y = y;
		}
	}

	private NodeItem[,] grid;
	private int w, h;

    public int gridWidth { get { return w; } }
    public int gridHeight { get { return h; } }

    public void ClearGridCosts() {
        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                grid[x, y].gCost = 1000000; // 设置为一个极大值
                grid[x, y].hCost = 0;
                grid[x, y].parent = null;
            }
        }
    }

	private GameObject WallRange, PathRange;
	private List<GameObject> pathObj = new List<GameObject> ();

	void Awake() {
		// 初始化格子 (确保 w, h 正确对应 localScale * 2)
		w = Mathf.RoundToInt(transform.localScale.x * 2.0f);
		h = Mathf.RoundToInt(transform.localScale.y * 2.0f);
		grid = new NodeItem[w, h];

		WallRange = new GameObject ("WallRange");
		PathRange = new GameObject ("PathRange");

		// 将墙的信息写入格子中
		for (int x = 0; x < w; x++) {
			for (int y = 0; y < h; y++) {
				Vector3 pos = new Vector3 (x*0.5f, y*0.5f, -0.25f);
				// 优化：使用稍微小一点的半径(0.2f)，避免检测到相邻的地面或其他物体
				bool isWall = Physics.CheckSphere (pos, 0.2f, WhatLayer);
				// 构建一个节点
				grid[x, y] = new NodeItem (isWall, pos, x, y);
				// 如果是墙体，则画出不可行走的区域
				if (isWall && NodeWall != null) {
					GameObject obj = GameObject.Instantiate (NodeWall, pos, Quaternion.identity) as GameObject;
					obj.transform.SetParent (WallRange.transform);
				}
			}
		}
	}

    // 在编辑器中画出网格，方便调试
    void OnDrawGizmos() {
        if (grid == null) return;

        foreach (var node in grid) {
            Gizmos.color = node.isWall ? Color.red : Color.white;
            // 如果是在路径中的节点，可以画成其他颜色（可选）
            Gizmos.DrawCube(node.pos, Vector3.one * 0.4f);
        }
    }

	// 根据坐标获得一个节点
	public NodeItem getItem(Vector3 position) {
		int x = Mathf.RoundToInt (position.x * 2.0f);
		int y = Mathf.RoundToInt (position.y * 2.0f);
		x = Mathf.Clamp (x, 0, w - 1);
		y = Mathf.Clamp (y, 0, h - 1);
		return grid [x, y];
	}

	// 取得周围的节点
	public List<NodeItem> getNeibourhood(NodeItem node, bool allowDiagonal = true) {
		List<NodeItem> list = new List<NodeItem> ();
		for (int i = -1; i <= 1; i++) {
			for (int j = -1; j <= 1; j++) {
				// 如果是自己，则跳过
				if (i == 0 && j == 0)
					continue;
				
				// 如果不允许斜向移动
				if (!allowDiagonal && Mathf.Abs(i) + Mathf.Abs(j) > 1)
					continue;

				int x = node.x + i;
				int y = node.y + j;
				// 判断是否越界，如果没有，加到列表中
				if (x < w && x >= 0 && y < h && y >= 0)
					list.Add (grid [x, y]);
			}
		}
		return list;
	}

	// 更新路径
	public void updatePath(List<NodeItem> lines) {
		int curListSize = pathObj.Count;
		for (int i = 0, max = lines.Count; i < max; i++) {
            // 获取节点位置
            Vector3 targetPos = lines[i].pos;

            // 如果是最后一个点，直接对齐到目标物体的实际位置
            if (i == lines.Count - 1 && destPos != null) {
                targetPos = destPos.position;
            }
            // 如果是第一个点，直接对齐到玩家的实际位置
            else if (i == 0 && player != null) {
                targetPos = player.position;
            }

            // 统一 Z 轴，确保与物体在同一平面（防止透视偏移）
            targetPos.z = -0.25f; 

			if (i < curListSize) {
				pathObj [i].transform.position = targetPos;
				pathObj [i].SetActive (true);
			} else {
				GameObject obj = GameObject.Instantiate (Node, targetPos, Quaternion.identity) as GameObject;
				obj.transform.SetParent (PathRange.transform);
				pathObj.Add (obj);
			}
		}
		for (int i = lines.Count; i < curListSize; i++) {
			pathObj [i].SetActive (false);
		}
	}
}
