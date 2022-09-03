using System.Collections.Generic;
using UnityEngine;

public class Pathfinding
{
    #region Variables / Components

    private Grid<PathCell> _pathGrid;
    private Grid<GridCell> _referenceGrid;

    private List<PathCell> _openList = new List<PathCell>();
    private List<PathCell> _closedList = new List<PathCell>();

    #endregion

    #region Contructor

    public Pathfinding(Grid<GridCell> referenceGrid)
    {
        _pathGrid = new Grid<PathCell>(referenceGrid.width, referenceGrid.height, referenceGrid.depth,
            referenceGrid.cellSize, referenceGrid.Origin);

        for (int x = 0; x < referenceGrid.width; x++)
        {
            for (int y = 0; y < referenceGrid.height; y++)
            {
                for (int z = 0; z < referenceGrid.depth; z++)
                {
                    var pathCell = new PathCell(_pathGrid, x, y, z);
                    
                    pathCell.GCost = int.MaxValue;
                    pathCell.CalculateFCost();
                    pathCell.CameFromCell = null;
                    
                    _pathGrid.SetValue(x, y, z, pathCell);
                }
            }
        }
        
        _referenceGrid = referenceGrid;
    }

    #endregion

    #region Methods

    public List<PathCell> FindPath(Vector3Int startPoint, Vector3Int endPoint)
    {
        var startCell = _pathGrid.GetValue(startPoint.x, startPoint.y, startPoint.z);
        var endCell = _pathGrid.GetValue(endPoint.x, endPoint.y, endPoint.z);

        _openList.Add(startCell);

        startCell.GCost = 0;
        startCell.HCost = CalculateDistanceCost(startCell, endCell);
        startCell.CalculateFCost();

        while (_openList.Count > 0)
        {
            var currentCell = GetLowestFCostCell(_openList);

            if (currentCell == endCell)
                return CalculatePath(endCell);

            _openList.Remove(currentCell);
            _closedList.Add(currentCell);

            foreach (var neighbour in GetNeighbours(currentCell))
            {
                if(_closedList.Contains(neighbour)) continue;

                var tentativeGCost = currentCell.GCost + CalculateDistanceCost(currentCell, neighbour);

                if (tentativeGCost >= neighbour.GCost) continue;
                
                neighbour.CameFromCell = currentCell;
                neighbour.GCost = tentativeGCost;
                neighbour.HCost = CalculateDistanceCost(neighbour, endCell);
                neighbour.CalculateFCost();

                if (!_openList.Contains(neighbour))
                    _openList.Add(neighbour);
            }
        }
        
        return null;
    }

    private int CalculateDistanceCost(PathCell a, PathCell b)
    {
        var xDistance = Mathf.Abs(a.GridPosition.x - b.GridPosition.x);
        var yDistance = Mathf.Abs(a.GridPosition.y - b.GridPosition.y);
        var zDistance = Mathf.Abs(a.GridPosition.z - b.GridPosition.z);
        var remaining = Mathf.Abs(xDistance - zDistance);

        return 14 * Mathf.Min(xDistance, zDistance) + 10 * remaining;
    }

    private PathCell GetLowestFCostCell(List<PathCell> pathCells)
    {
        var lowestFCostCell = pathCells[0];

        foreach (var pathCell in pathCells)
        {
            if (pathCell.FCost < lowestFCostCell.FCost)
                lowestFCostCell = pathCell;
        }

        return lowestFCostCell;
    }

    private List<PathCell> GetNeighbours(PathCell currentCell)
    {
        var neighbours = new List<PathCell>();
        
        if (currentCell.GridPosition.x - 1 >= 0 && _referenceGrid.GetValue(currentCell.GridPosition.x - 1, currentCell.GridPosition.y, currentCell.GridPosition.z).IsAvailable)
            neighbours.Add(_pathGrid.GetValue(currentCell.GridPosition.x - 1, currentCell.GridPosition.y, currentCell.GridPosition.z));
        
        if (currentCell.GridPosition.x + 1 < _pathGrid.width && _referenceGrid.GetValue(currentCell.GridPosition.x + 1, currentCell.GridPosition.y, currentCell.GridPosition.z).IsAvailable)
            neighbours.Add(_pathGrid.GetValue(currentCell.GridPosition.x + 1, currentCell.GridPosition.y, currentCell.GridPosition.z));
        
        if (currentCell.GridPosition.z - 1 >= 0 && _referenceGrid.GetValue(currentCell.GridPosition.x, currentCell.GridPosition.y, currentCell.GridPosition.z - 1).IsAvailable)
            neighbours.Add(_pathGrid.GetValue(currentCell.GridPosition.x, currentCell.GridPosition.y, currentCell.GridPosition.z - 1));
        
        if (currentCell.GridPosition.z + 1 < _pathGrid.depth && _referenceGrid.GetValue(currentCell.GridPosition.x, currentCell.GridPosition.y, currentCell.GridPosition.z + 1).IsAvailable)
            neighbours.Add(_pathGrid.GetValue(currentCell.GridPosition.x, currentCell.GridPosition.y, currentCell.GridPosition.z + 1));

        return neighbours;
    }

    private List<PathCell> CalculatePath(PathCell endCell)
    {
        var path = new List<PathCell> { endCell };
        var currentCell = endCell;

        while (currentCell.CameFromCell != null)
        {
            path.Add(currentCell.CameFromCell);
            currentCell = currentCell.CameFromCell;
        }
        
        path.Reverse();
        return path;
    }

    #endregion
}

public class PathCell
{
    #region Properties

    private Grid<PathCell> Grid { get; set; }
    public Vector3Int GridPosition { get; set; }
    public int GCost { get; set; }
    public int HCost { get; set; }
    public int FCost { get; set; }
    public PathCell CameFromCell { get; set; }

    #endregion
    
    #region Constructor

    public PathCell(Grid<PathCell> grid, int x, int y, int z)
    {
        Grid = grid;
        GridPosition = new Vector3Int(x, y, z);
    }

    #endregion

    #region Methods

    public void CalculateFCost()
    {
        FCost = GCost + HCost;
    }

    #endregion
}