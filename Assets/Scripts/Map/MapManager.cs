using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using DG.Tweening;

public enum RotationDirection { COUNTERCLOCKWISE, CLOCKWISE }

public partial class MapManager : MonoBehaviour {

    public MapGenerator mapGenerator;

    // Copy of the parameters in MapGenerator for easier access
    [HideInInspector] public int columns = 0;
    [HideInInspector] public int rows = 0;

    TurnManager turnManager;

    [HideInInspector] public GameObject[] allPlayers;
    [HideInInspector] public GameObject[,] myMap;
    [HideInInspector] public Tile[,] myMapTiles;
    [HideInInspector] public GameObject[] allInsertArrows;

    [HideInInspector] public Coordinate diamondCoords;

    [HideInInspector] public List<Tile> brightTiles;

    Coordinate[] allCornerCoords;
    Vector3 finalShift;

    // Map Creation and manager Initialization

    void Awake()
    {
        turnManager = GetComponentInChildren<TurnManager>();
    }

    public void Initialize()
    {
        mapGenerator.mapManagerTransform = transform;

        mapGenerator.MapSetup();                                    // Map creation

        columns         = mapGenerator.columns;
        rows            = mapGenerator.rows;
        allPlayers      = mapGenerator.allPlayers;
        myMap           = mapGenerator.myMap;
        myMapTiles      = mapGenerator.myMapTiles;
        diamondCoords   = mapGenerator.diamondCoords;

        allCornerCoords    = new Coordinate[4];

        allCornerCoords[0] = new Coordinate(0, rows - 1);
        allCornerCoords[1] = new Coordinate(columns - 1, rows - 1);
        allCornerCoords[2] = new Coordinate(0, 0);
        allCornerCoords[3] = new Coordinate(columns - 1, 0);

        allInsertArrows = mapGenerator.allInsertArrows;

        finalShift = mapGenerator.finalShift;

    }

    /* Dynamic Tile creation and destruction */

    public Tile InstantiateTileLive(TileTypes tileType, Coordinate coordinate, bool isTrapped = false, bool canBeMoved = true)
    {
        GameObject tileInstance = Instantiate(mapGenerator.tile, coordinate.GetVect3() + transform.position, Quaternion.identity);
        tileInstance.transform.SetParent(transform);

        Tile myTileComponent = tileInstance.GetComponent<Tile>();

        myTileComponent.SetSprite(tileType);
        
        myTileComponent.canBeMoved = canBeMoved;
        myTileComponent.myCoord = coordinate;
        myTileComponent.SetPossibleConnections(tileType);

        if (isTrapped)
        {      
            myTileComponent.SetTrap(turnManager.GetActivePlayer() + 1 );        // 0 corresponds to the neutral trap
            turnManager.AddToActivateTrapList(myTileComponent.GetTrap());
        }

        myMap[coordinate.GetX(), coordinate.GetY()] = tileInstance;
        myMapTiles[coordinate.GetX(), coordinate.GetY()] = tileInstance.GetComponent<Tile>();

        return myTileComponent;
    }

    void DestroyTile(GameObject myTile)
    {
        Destroy(myTile);
    }

    /* Terraforming Methods */

    Coordinate SlideCoordinate(Coordinate myCoord, SlideDirection slideDir)
    {
        var newCoord = new Coordinate(myCoord.GetX(), myCoord.GetY());

        switch (slideDir)
        {
            case SlideDirection.LEFT_TO_RIGHT:
                newCoord.SetCoordinate(newCoord.GetX() + 1, newCoord.GetY());
                break;
            case SlideDirection.BOT_TO_TOP:
                newCoord.SetCoordinate(newCoord.GetX(), newCoord.GetY() + 1);
                break;
            case SlideDirection.RIGHT_TO_LEFT:
                newCoord.SetCoordinate(newCoord.GetX() - 1, newCoord.GetY());
                break;
            case SlideDirection.TOP_TO_BOT:
                newCoord.SetCoordinate(newCoord.GetX(), newCoord.GetY() - 1);
                break;
            default:
                break;
        }

        return newCoord;
    }

    public IEnumerator SlideLine(Coordinate[] myCoords)
    {
        Tile tmpTile;
        GameObject tmpTileObj;

        float animationTime = 0.5f;

        DestroyTile(PickTileObject(myCoords[myCoords.Length - 1]));     // Destroys the last movable tile, could become a coroutine

        var myMovement = new Vector2(0, 0);

        GetComponent<AudioSource>().Play();
        Camera.main.DOShakePosition(0.9f, 1);

        
        for (int i = myCoords.Length - 2; i >= 0; i--)                  // Slides the line according to the sorting order
        {
            tmpTile = PickTileComponent(myCoords[i]);
            tmpTileObj = PickTileObject(myCoords[i]);

            myMovement[0] = PickTileObject(myCoords[i+1]).transform.position.x - tmpTileObj.transform.position.x;
            myMovement[1] = PickTileObject(myCoords[i + 1]).transform.position.y - tmpTileObj.transform.position.y;
            StartCoroutine(tmpTile.MoveToPosition(myMovement, animationTime));
            tmpTile.SetCoordinates(myCoords[i + 1].GetX(), myCoords[i + 1].GetY());

            myMap[myCoords[i + 1].GetX(), myCoords[i + 1].GetY()] = tmpTileObj;
            myMapTiles[myCoords[i + 1].GetX(), myCoords[i + 1].GetY()] = tmpTile;

            
            if (tmpTileObj.transform.childCount != 0)                   // If a player or diamond is child of the tile updates their coordinates
            {
                foreach (Transform tr in tmpTileObj.transform)
                {
                    Player playerOnTile = tr.GetComponent<Player>();
                    if (playerOnTile != null)
                    {
                        playerOnTile.coordinate = tmpTile.myCoord;
                    }
                    else if (tr.tag == "Diamond")
                    {
                        turnManager.UpdateDiamondPosition(tmpTile.myCoord);
                    }   
                }
            }

        }

        yield return new WaitForSeconds(animationTime);

    }

    public IEnumerator RotateTiles(Coordinate[] selectedCoords, RotationDirection rotationDirection)
    {
        selectedCoords = KeepMovableTiles(selectedCoords);

        if (rotationDirection == RotationDirection.COUNTERCLOCKWISE)
        {
            selectedCoords = GeneralMethods.ReverseArray(selectedCoords);
        }

        Tile tmpTile;
        var tmpTileMatrix = new Tile[selectedCoords.Length];
        GameObject tmpTileObj;
        var tmpTileObjMatrix = new GameObject[selectedCoords.Length];

        GetComponent<AudioSource>().Play();
        Camera.main.DOShakePosition(0.9f, 1);

        float animationTime = 0.5f;
        var myMovement = new Vector2(0, 0);

        for (int i = 0; i < selectedCoords.Length; i++)
        {
            tmpTile = PickTileComponent(selectedCoords[i]);
            tmpTileObj = PickTileObject(selectedCoords[i]);

            myMovement[0] = PickTileObject(selectedCoords[(i + 1) % (selectedCoords.Length)]).transform.position.x - tmpTileObj.transform.position.x;
            myMovement[1] = PickTileObject(selectedCoords[(i + 1) % (selectedCoords.Length)]).transform.position.y - tmpTileObj.transform.position.y;

            StartCoroutine(tmpTile.MoveToPosition(myMovement, animationTime));
            tmpTile.SetCoordinates(selectedCoords[(i + 1) % (selectedCoords.Length)].GetX(), selectedCoords[(i + 1) % (selectedCoords.Length)].GetY());

            tmpTileMatrix[i] = tmpTile;
            tmpTileObjMatrix[i] = tmpTileObj;

            
            if (tmpTileObj.transform.childCount != 0)                               // If a player or diamond is child of the tile updates their coordinates
            {
                foreach (Transform tr in tmpTileObj.transform)
                {
                    Player playerOnTile = tr.GetComponent<Player>();
                    if (playerOnTile != null)
                    {
                        playerOnTile.coordinate = tmpTile.myCoord;
                    }
                    else if (tr.tag == "Diamond")
                    {
                        turnManager.UpdateDiamondPosition(tmpTile.myCoord);
                    }
                }
            }

        }

        for (int i = 0; i < selectedCoords.Length; i++)
        {
            myMap[selectedCoords[(i + 1) % (selectedCoords.Length)].GetX(), selectedCoords[(i + 1) % (selectedCoords.Length)].GetY()] = tmpTileObjMatrix[i];
            myMapTiles[selectedCoords[(i + 1) % (selectedCoords.Length)].GetX(), selectedCoords[(i + 1) % (selectedCoords.Length)].GetY()] = tmpTileMatrix[i];
        }

        yield return new WaitForSeconds(animationTime);

    }

    /* Tiles Management Utilities */

    public GameObject PickTileObject(Coordinate myCoord)
    {
        return myMap[myCoord.GetX(), myCoord.GetY()];
    }

    public void ResetEffectiveConnections()
    {
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                myMapTiles[i, j].ResetEffectiveConnectionMap();
            }
        }
    }

    public void ResetEffectiveConnections(Coordinate topLeft, Coordinate botRight)  // works only in a squared area defined by the 2 coordinates included
    {
        for (int i = topLeft.GetX(); i <= botRight.GetX(); i++)
        {
            for (int j = botRight.GetY(); j <= topLeft.GetY(); j++)
            {
                myMapTiles[i, j].ResetEffectiveConnectionMap();
            }
        }
    }

    public void UpdateTilesConnection(int playerPlayingNbr)                         // Updates the effective tile connection relative to the active player
    {
        ResetEffectiveConnections();

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                Tile thisTile = myMap[i, j].GetComponent<Tile>();

                if (i - 1 >= 0)
                {
                    thisTile.CheckConnections(myMapTiles[i - 1, j], 3, playerPlayingNbr);   // prevents accessing other players spawning areas and occupied tiles
                }
                
                if (j - 1 >= 0)
                {
                    thisTile.CheckConnections(myMapTiles[i, j - 1], 2, playerPlayingNbr);
                }
                
                if (j + 1 < rows)
                {
                    thisTile.CheckConnections(myMapTiles[i, j + 1], 0, playerPlayingNbr);
                }
                
                if (i + 1 < columns)
                {
                    thisTile.CheckConnections(myMapTiles[i + 1, j], 1, playerPlayingNbr);
                }
                
            }
        }

    }

    public void UpdateTilesConnection(Coordinate topLeft, Coordinate botRight)
    {
        ResetEffectiveConnections(topLeft, botRight);

        for (int i = topLeft.GetX(); i <= botRight.GetX(); i++)
        {
            for (int j = botRight.GetY(); j <= topLeft.GetY(); j++)
            {
                Tile thisTile = myMap[i, j].GetComponent<Tile>();

                if (i - 1 > 0)
                {
                    thisTile.CheckConnections(myMap[i - 1, j].GetComponent<Tile>(), 3);
                }

                if (j - 1 > 0)
                {
                    thisTile.CheckConnections(myMap[i, j - 1].GetComponent<Tile>(), 2);
                }

                if (j + 1 < rows)
                {
                    thisTile.CheckConnections(myMap[i, j + 1].GetComponent<Tile>(), 0);
                }

                if (i + 1 < columns)
                {
                    thisTile.CheckConnections(myMap[i + 1, j].GetComponent<Tile>(), 1);
                }

            }
        }
    }   // works only in a squared area defined by the 2 coordinates included

    public void UpdateTilesConnection()                             // Calls the update of the tiles connection for the whole board
    {
        UpdateTilesConnection(new Coordinate( 0, 0 ), new Coordinate( rows - 1, columns - 1));

    }

    public Tile PickTileComponent(Coordinate myCoord)
    {
        return myMapTiles[myCoord.GetX(), myCoord.GetY()];
    }

    public Coordinate[] KeepMovableTiles(Coordinate[] myCoords)     // Filters only the movable tiles
    {
        List<Coordinate> movableCoordsList = new List<Coordinate>();
        for (int i = 0; i < myCoords.Length; i++)
        {
            if (PickTileComponent(myCoords[i]).canBeMoved)
                movableCoordsList.Add(myCoords[i]);
        }

        return movableCoordsList.ToArray();
    }

    public void UpdateTilesZOrder()         // Updates the z order in order to maintain the isometric effect
    {
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                myMapTiles[i, j].UpdateZOrder();
            }
        }
    }

    /* Tiles Animation */
  
    public void BrightPossibleTiles(Coordinate coordinate, int playerID)        // Gets the active player current position and ID
    {
        Tile nextToAdd;
        Tile currentPosition = myMap[coordinate.GetX(), coordinate.GetY()].GetComponent<Tile>();

        brightTiles.Add(currentPosition);
        for (int i = 0; i < brightTiles.Count; i++)
        {
            if (brightTiles[i].effectiveConnections[0])
            {
                nextToAdd = myMap[brightTiles[i].myCoord.GetX(), brightTiles[i].myCoord.GetY() + 1].GetComponent<Tile>();
                if (!brightTiles.Contains(nextToAdd) && !CheckForOtherPlayerCorner(nextToAdd.myCoord, playerID))
                {
                    brightTiles.Add(nextToAdd);
                }
            }

            if (brightTiles[i].effectiveConnections[1])
            {
                nextToAdd = myMap[brightTiles[i].myCoord.GetX() + 1, brightTiles[i].myCoord.GetY()].GetComponent<Tile>();
                if (!brightTiles.Contains(nextToAdd) && !CheckForOtherPlayerCorner(nextToAdd.myCoord, playerID))
                {
                    brightTiles.Add(nextToAdd);
                }
            }

            if (brightTiles[i].effectiveConnections[2])
            {
                nextToAdd = myMap[brightTiles[i].myCoord.GetX(), brightTiles[i].myCoord.GetY() - 1].GetComponent<Tile>();
                if (!brightTiles.Contains(nextToAdd) && !CheckForOtherPlayerCorner(nextToAdd.myCoord, playerID))
                {
                    brightTiles.Add(nextToAdd);
                }
            }

            if (brightTiles[i].effectiveConnections[3])
            {
                nextToAdd = myMap[brightTiles[i].myCoord.GetX() - 1, brightTiles[i].myCoord.GetY()].GetComponent<Tile>();
                if (!brightTiles.Contains(nextToAdd) && !CheckForOtherPlayerCorner(nextToAdd.myCoord, playerID))
                {
                    brightTiles.Add(nextToAdd);
                }
            }
        }

        for (int i = 0; i < brightTiles.Count; i++)
        {
            brightTiles[i].SetSelected(true);
        }
    }

    public void SwitchOffTiles()                                                
    {
        for (int i = 0; i < brightTiles.Count; i++)
        {
            brightTiles[i].SetSelected(false);
        }
        brightTiles.Clear();
    }

    public bool CheckForOtherPlayerCorner(Coordinate coord, int playerID)
    {
        bool isOtherPlayerCorner = false;
        int idx = GeneralMethods.FindElementIdx(allCornerCoords, coord);
        if (idx != -1 && idx != playerID - 1)
            isOtherPlayerCorner = true;
        return isOtherPlayerCorner;
    }

    /* Controlled Access Methods */

    public GameObject[] GetAllInstancedArrows()
    {
        return allInsertArrows;
    }

    public Vector3 GetVector3FromCoords(Coordinate coords)
    {
        return coords.GetVect3() + finalShift;
    }

    public Coordinate GetPlayerCornerCoordinates(int playerID)
    {
        return allCornerCoords[playerID];
    }

    public Coordinate[] GetAllCornerCoordinates()
    {
        return allCornerCoords;
    }


}
