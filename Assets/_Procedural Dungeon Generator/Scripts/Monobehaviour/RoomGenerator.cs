using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoomGenerator : MonoBehaviour
{
    #region Variables / Components

    [Header("Room Settings")]
    [SerializeField] private int _width = 1;
    [SerializeField] private int _height = 1;
    [SerializeField] private int _depth = 1;
    [SerializeField] private Transform _originTransform = default;
    [SerializeField] private float _tileSize = 0;
    [SerializeField] private int _numberOfDoors = 0;
    [SerializeField] private int _numberOfWindows = 0;

    [Header("Decoration Settings")]
    [SerializeField] private int _safeArea = 0;
    [SerializeField] private bool _useMaxWallDecoration = false;
    [SerializeField] private int _numberOfWallDecorations = 0;
    [SerializeField] private bool _useMaxPropDecoration = false;
    [SerializeField] private int _numberOfPropDecorations = 0;
    [SerializeField] private float _wallDecorationOffSet = 0;
    [SerializeField] private float _propDecorationOffSet = 0;

    [Header("Seeds")]
    [Range(0,99999)] [SerializeField] private int _roomSeed = 0;
    [Range(0,99999)] [SerializeField] private int _decorationSeed = 0;

    [Header("Tile References")]
    [SerializeField] private List<GameObject> _floors = new List<GameObject>();
    [SerializeField] private List<GameObject> _walls = new List<GameObject>();
    [SerializeField] private List<GameObject> _doors = new List<GameObject>();
    [SerializeField] private List<GameObject> _windows = new List<GameObject>();
    
    [Header("Decoration References")]
    [SerializeField] private List<SO_Decorations> _wallDecorations = new List<SO_Decorations>();
    [SerializeField] private List<SO_Decorations> _propDecorations = new List<SO_Decorations>();

    [Header("Parent References")]
    [SerializeField] private Transform _floorParent;
    [SerializeField] private Transform _wallParent;
    [SerializeField] private Transform _decorationParent;
    
    private Grid<GridCell> _roomGrid;
    private List<Door> _dataDoors = new List<Door>();

    private List<GameObject> _spawnedFloors = new List<GameObject>();
    private List<GameObject> _spawnedWalls = new List<GameObject>();
    private List<GameObject> _spawnedDoors = new List<GameObject>();
    private List<GameObject> _spawnedWindows = new List<GameObject>();

    private List<SpawnedDecoration> _spawnedWallDecorations = new List<SpawnedDecoration>();
    private List<SpawnedDecoration> _spawnedPropDecorations = new List<SpawnedDecoration>();

    private bool _isReady;
    
    #endregion

    #region Properties

    public Grid<GridCell> RoomGrid { get => _roomGrid; set => _roomGrid = value; }
    public int Width { get => _width; set => _width = value; }
    public int Height { get => _height; set => _height = value; }
    public int Depth { get => _depth; set => _depth = value; }
    public float TileSize { get => _tileSize; set => _tileSize = value; }
    public bool UseMaxWallDecoration { get => _useMaxWallDecoration; set => _useMaxWallDecoration = value; }
    public int NumberOfWallDecorations { get => _numberOfWallDecorations; set => _numberOfWallDecorations = value; }
    public bool UseMaxPropDecoration { get => _useMaxPropDecoration; set => _useMaxPropDecoration = value; }
    public int NumberOfPropDecorations { get => _numberOfPropDecorations; set => _numberOfPropDecorations = value; }
    public int RoomSeed { get => _roomSeed; set => _roomSeed = value; }
    public int DecorationSeed { get => _decorationSeed; set => _decorationSeed = value; }
    public int NumberOfDoors { get => _numberOfDoors; set => _numberOfDoors = value; }
    public int NumberOfWindows { get => _numberOfWindows; set => _numberOfWindows = value; }
    public bool IsReady => _isReady;

    #endregion

    #region Events

    public event Action OnRoomReady;

    #endregion
    
    #region Monobehaviour

#if UNITY_EDITOR
    //void OnValidate() { UnityEditor.EditorApplication.delayCall += _OnValidate; }
    void _OnValidate() 
    {
        if (!this) return;
        if(!Application.isPlaying) return;
        
        ClearRoom();
        ClearDecorations();
        
        GenerateRoom();
        GenerateDecorations();
    }
#endif

    private void Start()
    {
        InitializeGrid();
        GenerateRoom();
        GenerateDecorations();
    }

    #endregion

    #region Methods

    #region Room

    private void InitializeGrid()
    {
        if (_roomGrid != null) return;
        
        _roomGrid = new Grid<GridCell>(_width, _height, _depth, _tileSize, _originTransform.position);
            
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int z = 0; z < _depth; z++)
                {
                    var newGridCell = new GridCell(new Vector3Int(x, y, z), true);
                    _roomGrid.SetValue(x, y, z, newGridCell);
                }
            }
        }
    }
    
    private void GenerateRoom()
    {
        Random.InitState(_roomSeed);
        
        GenerateFloors();
        GenerateWalls();
        GenerateDoors();
        GenerateWindows();
    }

    private void GenerateFloors()
    {
        if(_floors.Count <= 0)return;

        _roomGrid.GetXYZ(transform.position, out var originX,out var originY,out var originZ);
        
        for (int x = originX; x < originX + _width; x++)
        {
            for (int y = originY; y < _height; y++)
            {
                for (int z = originZ; z < originZ + _depth; z++)
                {
                    var floorPrefab = _floors[Random.Range(0, _floors.Count)];
                    var spawnPosition = _roomGrid.GetWorldPosition(x, y, z);
                    var spawnedFloor = Instantiate(floorPrefab, _floorParent);

                    spawnedFloor.transform.position = spawnPosition;

                    _roomGrid.GetValue(x,y,z).TileTransform = spawnedFloor.transform;
                    _spawnedFloors.Add(spawnedFloor);
                }
            }
        }
    }

    private void GenerateWalls()
    {
        if(_walls.Count <= 0)return;
        
        _roomGrid.GetXYZ(transform.position, out var originX,out var originY,out var originZ);
        
        for (int x = 0; x < _width; x++)
        {
            var downTileTransform = _roomGrid.GetValue(originX + x, 0,  originZ).TileTransform;
            var upTileTransform = _roomGrid.GetValue(originX+ x, 0,  originZ + _depth - 1).TileTransform;
            
            SpawnWall(downTileTransform.position, -downTileTransform.forward);
            SpawnWall(upTileTransform.position, upTileTransform.forward);
        }
        
        for (int z = 0; z < _depth; z++)
        {
            var leftTileTransform = _roomGrid.GetValue(originX, 0, originZ + z).TileTransform;
            var rightTileTransform = _roomGrid.GetValue( originX + _width - 1, 0, originZ + z).TileTransform;
            
            SpawnWall(leftTileTransform.position, -leftTileTransform.right);
            SpawnWall(rightTileTransform.position, rightTileTransform.right);
        }
    }

    private void GenerateDoors()
    {
        if(_doors.Count <= 0)return;
        if(_spawnedWalls.Count <= 0) return;
        
        for (int i = 0; i < _numberOfDoors; i++)
        {
            var randomWall = GetRandomWall();
            var doorPrefab = _doors[Random.Range(0, _doors.Count)];
            var spawnedDoor = Instantiate(doorPrefab, _wallParent);

            var dataDoor = new Door
            (_roomGrid.GetValueWorld(randomWall.transform.position -
                                     randomWall.transform.forward * (_tileSize * .5f)));

            if (dataDoor.DoorCell != null)
                dataDoor.DoorCell.IsAvailable = true;
            
            spawnedDoor.transform.position = randomWall.transform.position;
            spawnedDoor.transform.rotation = randomWall.transform.rotation;
            
            _spawnedWalls.Remove(randomWall);
            _spawnedDoors.Add(spawnedDoor);
            _dataDoors.Add(dataDoor);
            
            Destroy(randomWall);
        }
    }

    private void GenerateWindows()
    {
        if(_windows.Count <= 0) return;
        if(_spawnedWalls.Count <= 0) return;
        
        for (int i = 0; i < _numberOfWindows; i++)
        {
            var randomWall = GetRandomWall();
            var windowPrefab = _windows[Random.Range(0, _windows.Count)];
            var spawnedWindow = Instantiate(windowPrefab, _wallParent);
            
            spawnedWindow.transform.position = randomWall.transform.position;
            spawnedWindow.transform.rotation = randomWall.transform.rotation;
            
            _spawnedWalls.Remove(randomWall);
            _spawnedWindows.Add(spawnedWindow);
            Destroy(randomWall);
        }
    }

    private void ClearRoom()
    {
        foreach (var floor in _spawnedFloors)
            Destroy(floor);
        foreach (var wall in _spawnedWalls)
            Destroy(wall);
        foreach (var door in _spawnedDoors)
            Destroy(door);
        foreach (var window in _spawnedWindows)
            Destroy(window);
        
        _spawnedFloors.Clear();
        _spawnedWalls.Clear();
        _spawnedDoors.Clear();
        _spawnedWindows.Clear();
    }
    
    private GameObject GetRandomWall()
    {
        var randomWall = _spawnedWalls[Random.Range(0, _spawnedWalls.Count)];
        var safeCheck = 30;
        
        while (safeCheck > 0)
        {
            var isNotNeighbour = true;
                
            foreach (var door in _dataDoors)
            {
                var cellA = _roomGrid.GetValueWorld(randomWall.transform.position +
                                                    randomWall.transform.forward * (_tileSize * .5f));
                if(cellA == null || door.DoorCell == null) continue;
                if (_roomGrid.IsNeighbour(cellA, door.DoorCell))
                    isNotNeighbour = false;
            }
                
            if(isNotNeighbour)
                break;
                
            randomWall = _spawnedWalls[Random.Range(0, _spawnedWalls.Count)];
            safeCheck--;
        }

        return randomWall;
    }
    
    private void SpawnWall(Vector3 tilePosition,Vector3 direction)
    {
        var wallPrefab = _walls[Random.Range(0, _walls.Count)];
        var wallSpawned = Instantiate(wallPrefab, _wallParent);

        wallSpawned.transform.position = tilePosition + direction * (_tileSize * 0.5f);
        wallSpawned.transform.LookAt(tilePosition);
        
        _spawnedWalls.Add(wallSpawned);
    }
    public List<Door> GetOpenDoors()
    {
        var doors = new List<Door>();

        foreach (var door in _dataDoors)
        {
            if (door.IsOpen)
                doors.Add(door);
        }

        return doors;
    }
    
    #endregion

    #region Decoration

    private void GenerateDecorations()
    {
        Random.InitState(_decorationSeed);
        
        GeneratePropDecoration(out var propSpawned);
        GenerateWallDecoration(out var wallSpawned);
        
        while (propSpawned || wallSpawned)
        {
            GeneratePropDecoration(out propSpawned);
            GenerateWallDecoration(out wallSpawned);
        }
        
        _isReady = true;
        OnRoomReady?.Invoke();
    }

    private void GenerateWallDecoration(out bool spawned)
    {
        spawned = false;
        
        if(_wallDecorations.Count <= 0) return;
        
        var safeCheck = 30;

        if (_spawnedWallDecorations.Count >= _numberOfWallDecorations && !_useMaxWallDecoration) return;
        
        while (safeCheck > 0)
        {
            var decoration = _wallDecorations[Random.Range(0, _wallDecorations.Count)];
            var wall = _spawnedWalls[Random.Range(0, _spawnedWalls.Count)].transform;
            var decorationPrefab = Instantiate(decoration.prefab, wall);

            decorationPrefab.transform.localPosition += decoration.positionOffSet;
            decorationPrefab.transform.position += decorationPrefab.transform.forward * _wallDecorationOffSet;

            if (IsValidPosition(decorationPrefab.transform.position, decoration.size))
            {
                decorationPrefab.transform.localEulerAngles += decoration.rotationOffSet;
                _spawnedWallDecorations.Add(new SpawnedDecoration(decorationPrefab, decoration.size));
                spawned = true;
                break;
            }

            Destroy(decorationPrefab);
            safeCheck--;
        }
    }

    private void GeneratePropDecoration(out bool spawned)
    {
        spawned = false;
        
        if(_propDecorations.Count <= 0) return;
        
        var safeCheck = 30;

        if (_spawnedPropDecorations.Count >= _numberOfPropDecorations && !_useMaxPropDecoration) return;

        _roomGrid.GetXYZ(transform.position, out var x, out var y, out var z);
        var minPosition = _roomGrid.GetWorldPosition(x + _safeArea, 0, z + _safeArea);
        var maxPosition = _roomGrid.GetWorldPosition(x + _width - 1 - _safeArea,0, z + _depth - 1 - _safeArea);
        
        while (safeCheck > 0)
        {
            var decoration = _propDecorations[Random.Range(0, _propDecorations.Count)];

            var position =
                new Vector3(Random.Range(minPosition.x, maxPosition.x), 0 + _propDecorationOffSet,
                    Random.Range(minPosition.z, maxPosition.z)) +
                decoration.positionOffSet;
            
            position += decoration.positionOffSet;

            if (IsValidPosition(position, decoration.size))
            {
                var decorationPrefab = Instantiate(decoration.prefab, _decorationParent);

                decorationPrefab.transform.position = position;
                
                if (decoration.allowRandomRotation)
                    decorationPrefab.transform.Rotate(new Vector3(0, 1, 0), Random.Range(0, 360));

                _spawnedPropDecorations.Add(new SpawnedDecoration(decorationPrefab, decoration.size));

                spawned = true;
                break;
            }

            safeCheck--;
        }
    }

    private bool IsValidPosition(Vector3 position, float decorationSize)
    {
        var validPosition = true;
        
        foreach (var spawnedPropDecoration in _spawnedPropDecorations)
        {
            if (Vector3.Distance(position, spawnedPropDecoration.SpawnedObject.transform.position) < spawnedPropDecoration.Size ||
                Vector3.Distance(position, spawnedPropDecoration.SpawnedObject.transform.position) < decorationSize)
                validPosition = false;
        }

        foreach (var spawnedWallDecoration in _spawnedWallDecorations)
        {
            if (Vector3.Distance(position, spawnedWallDecoration.SpawnedObject.transform.position) < spawnedWallDecoration.Size ||
                Vector3.Distance(position, spawnedWallDecoration.SpawnedObject.transform.position) < decorationSize)
                validPosition = false;
        }

        return validPosition;
    }
    
    private void ClearDecorations()
    {
        foreach (var wallDecoration in _spawnedWallDecorations)
            Destroy(wallDecoration.SpawnedObject);
        foreach (var propDecoration in _spawnedPropDecorations)
            Destroy(propDecoration.SpawnedObject);
        
        _spawnedWallDecorations.Clear();
        _spawnedPropDecorations.Clear();
    }

    #endregion

    #endregion
}

public class SpawnedDecoration
{
    #region Properties

    public GameObject SpawnedObject { get; }
    public float Size { get; }

    #endregion

    #region Constructor

    public SpawnedDecoration(GameObject decorationObject, float decorationSize)
    {
        SpawnedObject = decorationObject;
        Size = decorationSize;
    }

    #endregion
}

public class Door
{
    #region Properties

    public GridCell DoorCell { get; }
    public bool IsOpen { get; set; } = true;

    #endregion

    #region Constructor

    public Door(GridCell doorCell)
    {
        DoorCell = doorCell;
    }

    #endregion
}
