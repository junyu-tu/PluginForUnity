using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformationGrid : MonoBehaviour
{
    public Transform prefab;
    public int gridResolution = 10;
    Transform[] grid;

    private void Awake()
    {
        grid = new Transform[gridResolution* gridResolution* gridResolution];
        for (int i = 0,z=0; i < gridResolution; z++)
        {
            for (int y = 0; y < gridResolution; y++)
            {
                for (int x = 0; x < gridResolution; x++,i++)
                {
                    grid[i] = CreateGridPoint(x,y,z);
                }
            }
        }
    }

    /// <summary>
    /// 创建一个点，实际上就是实例化预制件，确定其坐标并为其赋予独特的颜色
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    Transform CreateGridPoint(int x, int y, int z) {
        Transform point = Instantiate(prefab);
        point.localPosition = GetCoordinates(x,y,z);
        point.GetComponent<MeshRenderer>().material.color = new Color(
            (float)x / gridResolution,
            (float)y / gridResolution,
            (float)z / gridResolution
            );
        return point;
    }

    /// <summary>
    /// 网格最明显的形状是一个立方体，所以让我们开始吧。我们将其以原点为中心，因此变换（尤其是旋转和缩放）相对于网格立方体的中点。
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    Vector3 GetCoordinates(int x, int y, int z) {
        return new Vector3(
            x - (gridResolution - 1) * 0.5f,
            y - (gridResolution - 1) * 0.5f,
            z - (gridResolution - 1) * 0.5f
            );
    }
}
