using UnityEngine;

public class Grid<TGridObject>
{
    #region Variables / Components

        private int _width = 0;
        private int _height = 0;
        private int _depth = 0;
        private float _cellSize = 0;
        private  Vector3 _origin = Vector3.zero;
        private TGridObject[,,] _gridArray;

    #endregion

    #region Properties

        public int width => _width;
        public int height => _height;
        public int depth => _depth;
        public float cellSize => _cellSize;
        public Vector3 Origin => _origin;
        public TGridObject[,,] gridArray => _gridArray;

    #endregion
        
    #region Constructor

        public Grid(int width, int height,int depth, float cellSize, Vector3 origin)
        {
            _width = width;
            _height = height;
            _depth = depth;
            _cellSize = cellSize;
            _origin = origin;
            _gridArray = new TGridObject[width, height, depth];
        }

    #endregion

    #region Methods

        public Vector3 GetWorldPosition(int x, int y, int z)
        {
            return new Vector3(x, y, z) * _cellSize + _origin;
        }

        public void GetXYZ(Vector3 worldPosition, out  int x, out int y, out int z)
        {
            x = Mathf.FloorToInt((worldPosition - _origin).x / _cellSize);
            y = Mathf.FloorToInt((worldPosition - _origin).y / _cellSize);
            z = Mathf.FloorToInt((worldPosition - _origin).z / _cellSize);
        }

        public void SetValue(int x, int y, int z, TGridObject value)
        {
            if (x >= 0 && y >= 0 && z >= 0 && x < _width && y < _height && z < _depth)
                _gridArray[x, y, z] = value;
        }
        
        public void SetValueWorld(Vector3 worldPosition, TGridObject value)
        {
            GetXYZ(worldPosition, out var x, out var y, out var z);
            SetValue(x, y, z, value);
        }

        public TGridObject GetValue(int x, int y, int z)
        {
            if (x >= 0 && y >= 0 && z >= 0 && x < _width && y < _height && z < _depth)
                 return _gridArray[x, y, z];
            
            return default;
        }
        
        public TGridObject GetValueWorld(Vector3 worldPosition)
        {
            GetXYZ(worldPosition, out var x, out var y, out var z);
            return GetValue(x, y, z);
        }
        
        public bool IsCornerTile(Vector3 tilePosition)
        {
            if (GetWorldPosition(0, 0, 0) == tilePosition) return true;
            if (GetWorldPosition(_width - 1, 0, 0) == tilePosition) return true;
            if (GetWorldPosition(0, 0, _depth - 1) == tilePosition) return true;
            if (GetWorldPosition(_width - 1, 0, _depth - 1) == tilePosition) return true;
            
            if (GetWorldPosition(0, _height - 1, 0) == tilePosition) return true;
            if (GetWorldPosition(_width - 1, _height - 1, 0) == tilePosition) return true;
            if (GetWorldPosition(0, _height - 1, _depth - 1) == tilePosition) return true;
            if (GetWorldPosition(_width - 1, _height - 1, _depth - 1) == tilePosition) return true;
        
            return false;
        }

        public bool IsNeighbour(GridCell a, GridCell b)
        {
            if (a.GridPosition.x - 1 >= 0 && a.GridPosition.x + 1 < _width)
                if(Mathf.Abs(a.GridPosition.x - b.GridPosition.x) == 1)
                    return true;
            
            if (a.GridPosition.z - 1 >= 0 && a.GridPosition.z + 1 < _depth)
                if(Mathf.Abs(a.GridPosition.z - b.GridPosition.z) == 1)
                    return true;

            return false;
        }

    #endregion
}

public class GridCell
{
    #region Properties

    public Vector3Int GridPosition { get; set; }

    public Transform TileTransform { get; set; }

    public bool IsAvailable { get; set; }

    #endregion

    #region Constructor

    public GridCell(Vector3Int gridPosition, bool isAvailable)
    {
        GridPosition = gridPosition;
        IsAvailable = isAvailable;
    }

    #endregion
}