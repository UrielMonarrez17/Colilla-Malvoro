using UnityEngine;

public class GridCell 
{
    public Vector2Int gridPosition;
    public CellType roadType;
    public GameObject instantiatedObject;

    public GridCell(int x, int z) 
    {
        this.gridPosition = new Vector2Int(x, z);
        this.roadType = CellType.Empty;
    }
}