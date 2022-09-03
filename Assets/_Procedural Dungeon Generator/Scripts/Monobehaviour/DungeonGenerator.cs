using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class DungeonGenerator : MonoBehaviour
{
    #region Variables / Components

    [Header("Dungeon Settings")]
    [SerializeField] private int _width = 1;
    [SerializeField] private int _height = 1;
    [SerializeField] private int _depth = 1;
    [SerializeField] [Range(1, 10)] private int _minDistanceBetweenRooms = 1;
    [SerializeField] private Transform _originTransform = default;
    [SerializeField] private float _tileSize = 0;
    [SerializeField] private bool _debugGrid;

    [Header("Room Settings")]
    [SerializeField] private int _maxRoomSize;
    [SerializeField] private int _minRoomSize;
    [SerializeField] private int _maxNumberOfDoors;
    [SerializeField] private int _minNumberOfDoors;
    [SerializeField] private int _maxNumberOfWindows;
    [SerializeField] private int _minNumberOfWindows;
    
    [SerializeField] private bool _useMaxWallDecoration = false;
    [SerializeField] private int _numberOfWallDecorations = 0;
    [SerializeField] private bool _useMaxPropDecoration = false;
    [SerializeField] private int _numberOfPropDecorations = 0;
    
    [Header("Seeds")]
    [Range(0,99999)] [SerializeField] private int _dungeonSeed = 0;
    
    [Header("References")]
    [SerializeField] private List<RoomGenerator> _roomPrefabs = new List<RoomGenerator>();
    [SerializeField] private Transform _floorTile;
    [SerializeField] private Transform _wallTile;

    private Grid<GridCell> _dungeonGrid;
    private List<RoomGenerator> _spawnedRooms = new List<RoomGenerator>();
    private List<Transform> _spawnedCorridorTiles = new List<Transform>();

    #endregion

    #region Monobehaviour

    private void Start()
    {
        Random.InitState(_dungeonSeed);
        
        InitializeGrid();
        SetupRooms();
        EnableRooms();
    }

    private void OnDisable()
    {
        foreach (var room in _spawnedRooms)
            room.OnRoomReady -= ConnectRooms;
    }

    private void OnDrawGizmos()
    {
        if(!Application.isPlaying || !_debugGrid) return;
        
        Gizmos.color = Color.red;

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int z = 0; z < _depth; z++)
                {
                    if (_dungeonGrid.GetValue(x, y, z).IsAvailable)
                        Gizmos.DrawCube(_dungeonGrid.GetWorldPosition(x, y, z), Vector3.one * _tileSize * .5f);
                }
            }
        }
    }

    #endregion

    #region Methods

    private void InitializeGrid()
    {
        _dungeonGrid = new Grid<GridCell>(_width, _height, _depth, _tileSize, _originTransform.position);
        
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int z = 0; z < _depth; z++)
                {
                    var newGridCell = new GridCell(new Vector3Int(x, y, z), true);
                    _dungeonGrid.SetValue(x, y, z, newGridCell);
                }
            }
        }
    }
    
    private void SetupRooms()
    {
        var safeCheck = 30;

        while (safeCheck > 0)
        {
            var originX = Random.Range(1, _width - 2);
            var originY = Random.Range(1, _height - 2);
            var originZ = Random.Range(1, _depth - 2);
            
            if (!IsRoomValid(originX,originY,originZ))
            {
                safeCheck--;
                continue;
            }    

            var roomWidth = Random.Range(_minRoomSize, _maxRoomSize);
            var roomDepth = Random.Range(_minRoomSize, _maxRoomSize);
            var cells = new List<GridCell>();
            var canCreate = true;

            for (int x = originX - _minDistanceBetweenRooms; x < originX + roomWidth + _minDistanceBetweenRooms; x++)
            {
                for (int y = originY; y < _height; y++)
                {
                    for (int z = originZ - _minDistanceBetweenRooms; z < originZ + roomDepth + _minDistanceBetweenRooms; z++)
                    {
                        if (_dungeonGrid.GetValue(x, y, z) != null)
                        {
                            if (!_dungeonGrid.GetValue(x, y, z).IsAvailable)
                                canCreate = false;
                            else
                                cells.Add(_dungeonGrid.GetValue(x, y, z));
                        }
                        else
                            canCreate = false;
                    }
                }
            }

            if (!canCreate)
            {
                safeCheck--;
                continue;
            }

            foreach (var cell in cells)
                cell.IsAvailable = false;

            SpawnRoom(originX, originY, originZ, roomWidth, roomDepth);
            safeCheck = 30;
        }
    }

    private void EnableRooms()
    {
        foreach (var room in _spawnedRooms)
            room.gameObject.SetActive(true);
    }
    
    private void SpawnRoom(int originX, int originY, int originZ, int roomWidth, int roomDepth)
    {
        var newRoom = Instantiate(_roomPrefabs[Random.Range(0, _roomPrefabs.Count)],
            _dungeonGrid.GetWorldPosition(originX, originY, originZ), Quaternion.identity);

        newRoom.RoomGrid = _dungeonGrid;
        newRoom.Width = roomWidth;
        newRoom.Height = 1;
        newRoom.Depth = roomDepth;
        newRoom.TileSize = _tileSize;
        newRoom.UseMaxWallDecoration = _useMaxWallDecoration;
        newRoom.UseMaxPropDecoration = _useMaxPropDecoration;
        newRoom.NumberOfWallDecorations = _numberOfWallDecorations;
        newRoom.NumberOfPropDecorations = _numberOfPropDecorations;
        newRoom.NumberOfDoors = Random.Range(_minNumberOfDoors, _maxNumberOfDoors);
        newRoom.NumberOfWindows = Random.Range(_minNumberOfWindows, _maxNumberOfWindows);
        newRoom.RoomSeed = Random.Range(0, 99999);
        newRoom.DecorationSeed = Random.Range(0, 99999);
        newRoom.OnRoomReady += ConnectRooms;
        
        _spawnedRooms.Add(newRoom);
    }

    private void ResetCellsAvailability()
    {
        foreach (var room in _spawnedRooms)
        {
            _dungeonGrid.GetXYZ(room.transform.position, out var originX, out var originY, out var originZ);
            
            for (int x = originX - _minDistanceBetweenRooms; x <= originX + room.Width + _minDistanceBetweenRooms; x++)
            {
                if (_dungeonGrid.GetValue(x, originY, originZ + room.Depth) != null)
                    _dungeonGrid.GetValue(x, originY, originZ + room.Depth).IsAvailable = true;
                
                if (_dungeonGrid.GetValue(x, originY, originZ - _minDistanceBetweenRooms) != null)
                    _dungeonGrid.GetValue(x, originY, originZ - _minDistanceBetweenRooms).IsAvailable = true;
            }
            
            for (int z = originZ - _minDistanceBetweenRooms; z <= originZ + room.Depth + _minDistanceBetweenRooms; z++)
            {
                if (_dungeonGrid.GetValue(originX + room.Width, 0, z) != null)
                    _dungeonGrid.GetValue(originX + room.Width, 0, z).IsAvailable = true;
                
                if (_dungeonGrid.GetValue(originX - _minDistanceBetweenRooms, originY, z) != null)
                    _dungeonGrid.GetValue(originX - _minDistanceBetweenRooms, originY, z).IsAvailable = true;
            }
        }
    }
    
    private void ConnectRooms()
    {
        foreach (var room in _spawnedRooms)
            if(!room.IsReady) return;

        ResetCellsAvailability();

        var safeCheck = 30;
        var openRooms = new List<RoomGenerator>(_spawnedRooms);

        while (openRooms.Count > 1)
        {
            var startRoom = openRooms[Random.Range(0, openRooms.Count)];
            var endRoom = openRooms[Random.Range(0, openRooms.Count)];

            while (endRoom == startRoom)
                endRoom = openRooms[Random.Range(0, openRooms.Count)];

            var startDoor = startRoom.GetOpenDoors()[Random.Range(0, startRoom.GetOpenDoors().Count)];
            var endDoor = endRoom.GetOpenDoors()[Random.Range(0, endRoom.GetOpenDoors().Count)];
            var pathfinding = new Pathfinding(_dungeonGrid);

            if (startDoor.DoorCell == null || endDoor.DoorCell == null)
            {
                safeCheck--;
                
                if(safeCheck <= 0)
                    break;
                
                continue;
            }
            
            var path = pathfinding.FindPath(
                new Vector3Int(startDoor.DoorCell.GridPosition.x, startDoor.DoorCell.GridPosition.y,
                    startDoor.DoorCell.GridPosition.z),
                new Vector3Int(endDoor.DoorCell.GridPosition.x, endDoor.DoorCell.GridPosition.y,
                    endDoor.DoorCell.GridPosition.z));

            if (path == null)
            {
                safeCheck--;
                
                if(safeCheck <= 0)
                    break;
                
                continue;
            }

            safeCheck = 30;
            SpawnPath(path);

            startDoor.IsOpen = false;
            endDoor.IsOpen = false;

            if (startRoom.GetOpenDoors().Count <= 0)
                openRooms.Remove(startRoom);
            
            if (endRoom.GetOpenDoors().Count <= 0)
                openRooms.Remove(endRoom);
        }
        
        SpawnWalls();
    }

    private void SpawnPath(List<PathCell> path)
    {
        if(path == null) return;

        foreach (var pathCell in path)
        {
            var floor =  Instantiate(_floorTile, _dungeonGrid.GetWorldPosition(pathCell.GridPosition.x, pathCell.GridPosition.y, pathCell.GridPosition.z),
                Quaternion.identity);
            
            _spawnedCorridorTiles.Add(floor);
        }
    }

    private void SpawnWalls()
    {
        if(_spawnedCorridorTiles.Count <= 0) return;
        
        foreach (var tile in _spawnedCorridorTiles)
            _dungeonGrid.GetValueWorld(tile.position).IsAvailable = false;

        foreach (var tile in _spawnedCorridorTiles)
        {
            var tileCell = _dungeonGrid.GetValueWorld(tile.position);
            var tilePosition = _dungeonGrid.GetWorldPosition(tileCell.GridPosition.x, tileCell.GridPosition.y,
                tileCell.GridPosition.z);

            foreach (var direction in GetWallDirections(tileCell))
            {
                var wallSpawned = Instantiate(_wallTile, tilePosition + (direction - tilePosition).normalized * (_tileSize * 0.5f), Quaternion.identity);
                wallSpawned.transform.LookAt(tilePosition);
            }
        }
    }
    
    private bool IsRoomValid(int x, int y, int z)
    {
        if (_dungeonGrid.GetValue(x, y, z) == null) return false;
        if (!_dungeonGrid.GetValue(x, y, z).IsAvailable) return false;

        return true;
    }
    
    private List<Vector3> GetWallDirections(GridCell cell)
    {
        var directions = new List<Vector3>();
        
        if (cell.GridPosition.x - 1 >= 0 && _dungeonGrid.GetValue(cell.GridPosition.x - 1, cell.GridPosition.y, cell.GridPosition.z).IsAvailable)
            directions.Add(_dungeonGrid.GetWorldPosition(cell.GridPosition.x - 1, cell.GridPosition.y, cell.GridPosition.z));
        
        if (cell.GridPosition.x + 1 < _dungeonGrid.width && _dungeonGrid.GetValue(cell.GridPosition.x + 1, cell.GridPosition.y, cell.GridPosition.z).IsAvailable)
            directions.Add(_dungeonGrid.GetWorldPosition(cell.GridPosition.x + 1, cell.GridPosition.y, cell.GridPosition.z));
        
        if (cell.GridPosition.z - 1 >= 0 && _dungeonGrid.GetValue(cell.GridPosition.x, cell.GridPosition.y, cell.GridPosition.z - 1).IsAvailable)
            directions.Add(_dungeonGrid.GetWorldPosition(cell.GridPosition.x, cell.GridPosition.y, cell.GridPosition.z - 1));
        
        if (cell.GridPosition.z + 1 < _dungeonGrid.depth && _dungeonGrid.GetValue(cell.GridPosition.x, cell.GridPosition.y, cell.GridPosition.z + 1).IsAvailable)
            directions.Add(_dungeonGrid.GetWorldPosition(cell.GridPosition.x, cell.GridPosition.y, cell.GridPosition.z + 1));

        return directions;
    }
    
    #endregion
}