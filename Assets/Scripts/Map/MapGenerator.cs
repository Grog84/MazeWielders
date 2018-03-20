using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/MapGenerator")]
public class MapGenerator : ScriptableObject
{
    // Public map parameters
    public int columns = 0;
    public int rows = 0;
    public float tileSize;

    [Space(10)]
    public Count curvesCount = new Count(4, 8);
    public Count straightCount = new Count(4, 8);
    public Count tCount = new Count(4, 8);
    public Count crossCount = new Count(4, 8);

    // Map Prefabs List 
    [Space(10)]
    public GameObject tile, player, insertArrow, diamond;
    [HideInInspector] public GameObject stasisEffectPrefab, stasisEffect;

    [HideInInspector] public GameObject myDiamondInstance;

    public PlayerDescription[] allPlayersDescription;

    [HideInInspector] public Transform mapManagerTransform;

    [HideInInspector] public GameObject[] allPlayers;
    [HideInInspector] public GameObject[,] myMap;
    [HideInInspector] public Tile[,] myMapTiles;
    [HideInInspector] public GameObject[] allInsertArrows;
    [HideInInspector] public Coordinate diamondCoords;

    int[] nbrOfTiles;
    int[] noBottom = { 2, 3, 5, 8 };
    int[] noRight = { 1, 3, 4, 7 };
    int[] noTop = { 0, 1, 5, 6 };
    int[] noLeft = { 0, 2, 4, 9 };

    [HideInInspector] public Vector3 finalShift;

    // Map Generation methods

    // Players Creation

    public void CreatePlayers()
    {
        for (int i = 0; i < 4; i++)
        {
            InstantiatePlayer(i);
        }
    }

    void InstantiatePlayer(int playerNbr)
    {
        GameObject playerInstance = Instantiate(player, new Vector3(0f, 0, 0), Quaternion.identity); ;
        Player iplayer = playerInstance.GetComponent<Player>();

        if (playerNbr == 0)
        {
            playerInstance.transform.position =  new Vector3(0f, rows - 1, -1f) * tileSize;
            iplayer.coordinate = new Coordinate(0, rows - 1);
        }
        else if (playerNbr == 1)
        {
            playerInstance.transform.position = new Vector3(columns - 1, rows - 1, -1f) * tileSize;
            iplayer.coordinate = new Coordinate(columns - 1, rows - 1);
        }
        else if (playerNbr == 2)
        {
            playerInstance.transform.position = new Vector3(0f, 0f, -1f) * tileSize;
            iplayer.coordinate = new Coordinate(0, 0);
        }
        else if (playerNbr == 3)
        {
            playerInstance.transform.position = new Vector3(columns - 1, 0f, -1f) * tileSize;
            iplayer.coordinate = new Coordinate(columns - 1, 0);
        }

        iplayer.SetDescription(allPlayersDescription[playerNbr]);

        allPlayers[playerNbr] = playerInstance;

    }

    // Insertion arrows creation

    public void CreateInsertArrows()
    {
        allInsertArrows = new GameObject[2 * columns + 2 * rows];
        float arrowZOrder = -2f;
        GameObject arrowInstance;

        for (int i = 0; i < columns; i++) // bot arrows
        {
            arrowInstance = Instantiate(insertArrow, new Vector3((i * tileSize), -tileSize, arrowZOrder), Quaternion.identity);
            arrowInstance.transform.Rotate(Vector3.forward * 90);
            arrowInstance.GetComponent<InsertArrow>().SetPointedCoords(i, i, 0, columns - 1);
            arrowInstance.transform.SetParent(mapManagerTransform);
            allInsertArrows[i] = arrowInstance;

        }
        for (int i = 0; i < rows; i++) // right arrows
        {
            arrowInstance = Instantiate(insertArrow, new Vector3(columns * tileSize, (i * tileSize), arrowZOrder), Quaternion.identity);
            arrowInstance.transform.Rotate(Vector3.forward * 180);
            arrowInstance.GetComponent<InsertArrow>().SetPointedCoords(columns - 1, 0, i, i);
            arrowInstance.transform.SetParent(mapManagerTransform);
            allInsertArrows[i + columns] = arrowInstance;

        }
        for (int i = 0; i < columns; i++) // top arrows
        {
            arrowInstance = Instantiate(insertArrow, new Vector3((columns - 1) * tileSize - ((i * tileSize)), rows * tileSize, arrowZOrder), Quaternion.identity);
            arrowInstance.transform.Rotate(Vector3.forward * -90);
            arrowInstance.GetComponent<InsertArrow>().SetPointedCoords(columns - 1 - i, columns - 1 - i, rows - 1, 0);
            arrowInstance.transform.SetParent(mapManagerTransform);
            allInsertArrows[i + (columns + rows)] = arrowInstance;

        }
        for (int i = 0; i < rows; i++) // left arrows
        {
            arrowInstance = Instantiate(insertArrow, new Vector3(-tileSize, (rows - 1) * tileSize - ((i * tileSize)), arrowZOrder), Quaternion.identity);
            arrowInstance.GetComponent<InsertArrow>().SetPointedCoords(0, columns - 1, rows - 1 - i, rows - 1 - i);
            arrowInstance.transform.SetParent(mapManagerTransform);
            allInsertArrows[i + (2 * columns + rows)] = arrowInstance;

        }

    }

    // Creation of the tiles forming the board

    int[] GenerateInitialTiles()
    {
        // Array storing all the information about the number of tiles per type
        int[] nbrOfTiles;

        nbrOfTiles = new int[4] { 0, 0, 0, 0 }; // 1st index is curves number, 2nd is straights, 3rd is ts, 4th is crosses

        //Defines the number of tiles per type
        for (int i = 0; i < 4; i++)
        {
            if (i == 0)
                nbrOfTiles[i] = Random.Range(curvesCount.minimum, curvesCount.maximum + 1); // nbr of curve tiles
            else if (i == 1)
                nbrOfTiles[i] = Random.Range(straightCount.minimum, straightCount.maximum + 1); // nbr of straight tiles
            else if (i == 2)
                nbrOfTiles[i] = Random.Range(tCount.minimum, tCount.maximum + 1); // nbr of T tiles
            else if (i == 3)
                nbrOfTiles[i] = Random.Range(crossCount.minimum, crossCount.maximum + 1); // nbr of cross tiles
        }

        return nbrOfTiles;
    }

    void GenerateSpawningAreas()
    {
        int nbrOfSpawningAreas = 4;
        int myTile;

        for (int i = 0; i < nbrOfSpawningAreas; i++)
        {
            InstantiateStartingPos(i);

            if (i == 0)
            {
                while (true)
                {
                    myTile = noBottom[Random.Range(0, 4)];
                    bool isInRange = CheckTileRange(myTile);
                    if (isInRange)
                    {
                        InstantiateTile((TileTypes)myTile, new Coordinate(0, 1));
                        break;
                    }

                }

                while (true)
                {
                    myTile = noLeft[Random.Range(0, 4)];
                    bool isInRange = CheckTileRange(myTile);
                    if (isInRange)
                    {
                        InstantiateTile((TileTypes)myTile, new Coordinate(1, 0));
                        break;
                    }

                }

            }
            else if (i == 1)
            {
                while (true)
                {
                    myTile = noBottom[Random.Range(0, 4)];
                    bool isInRange = CheckTileRange(myTile);
                    if (isInRange)
                    {
                        InstantiateTile((TileTypes)myTile, new Coordinate(columns - 1, 1));
                        break;
                    }

                }

                while (true)
                {
                    myTile = noRight[Random.Range(0, 4)];
                    bool isInRange = CheckTileRange(myTile);
                    if (isInRange)
                    {
                        InstantiateTile((TileTypes)myTile, new Coordinate(columns - 2, 0));
                        break;
                    }

                }
            }
            else if (i == 2)
            {
                while (true)
                {
                    myTile = noLeft[Random.Range(0, 4)];
                    bool isInRange = CheckTileRange(myTile);
                    if (isInRange)
                    {
                        InstantiateTile((TileTypes)myTile, new Coordinate(1, rows - 1));
                        break;
                    }

                }

                while (true)
                {
                    myTile = noTop[Random.Range(0, 4)];
                    bool isInRange = CheckTileRange(myTile);
                    if (isInRange)
                    {
                        InstantiateTile((TileTypes)myTile, new Coordinate(0, rows - 2));
                        break;
                    }

                }

            }
            else
            {
                while (true)
                {
                    myTile = noRight[Random.Range(0, 4)];
                    bool isInRange = CheckTileRange(myTile);
                    if (isInRange)
                    {
                        InstantiateTile((TileTypes)myTile, new Coordinate(columns - 2, rows - 1));
                        break;
                    }

                }

                while (true)
                {
                    myTile = noTop[Random.Range(0, 4)];
                    bool isInRange = CheckTileRange(myTile);
                    if (isInRange)
                    {
                        InstantiateTile((TileTypes)myTile, new Coordinate(columns - 1, rows - 2));
                        break;
                    }

                }

            }

        }
    }

    void GenerateCentralArea()
    {
        int centralX = columns / 2, centralY = rows / 2;
        Coordinate myCoordinate;

        for (int i = centralX - 1; i < centralX + 2; i++)
        {
            for (int j = centralY - 1; j < centralY + 2; j++)
            {
                myCoordinate = new Coordinate(i, j);
                if (myCoordinate.IsEqual(centralX - 1, centralY - 1))
                {
                    InstantiateTile(TileTypes.Curve_RT, myCoordinate, true);
                }
                else if (myCoordinate.IsEqual(centralX - 1, centralY))
                {
                    InstantiateTile(TileTypes.T_R, myCoordinate, true);
                }
                else if (myCoordinate.IsEqual(centralX - 1, centralY + 1))
                {
                    InstantiateTile(TileTypes.Curve_BR, myCoordinate, true);
                }
                else if (myCoordinate.IsEqual(centralX, centralY - 1))
                {
                    InstantiateTile(TileTypes.T_T, myCoordinate, true);
                }
                else if (myCoordinate.IsEqual(centralX, centralY))
                {
                    InstantiateTile(TileTypes.Goal, myCoordinate, false);
                    InstantiateDiamond(myCoordinate);
                }
                else if (myCoordinate.IsEqual(centralX, centralY + 1))
                {
                    InstantiateTile(TileTypes.T_B, myCoordinate, true);
                }
                else if (myCoordinate.IsEqual(centralX + 1, centralY - 1))
                {
                    InstantiateTile(TileTypes.Curve_TL, myCoordinate, true);
                }
                else if (myCoordinate.IsEqual(centralX + 1, centralY))
                {
                    InstantiateTile(TileTypes.T_L, myCoordinate, true);
                }
                else if (myCoordinate.IsEqual(centralX + 1, centralY + 1))
                {
                    InstantiateTile(TileTypes.Curve_LB, myCoordinate, true);
                }
            }
        }

    }

    private void PlaceNeutralTraps()
    {
        int distanceFromBorder = 2;

        // Makes sure that the traps will be fairly spread when the game begins
        // The method selects four traps locations, one for each section of the field
        var topLeft = new Coordinate(Random.Range(distanceFromBorder, columns / 2 - 2), Random.Range(rows / 2 + 2, rows - distanceFromBorder));
        var topRight = new Coordinate(Random.Range(columns / 2 + 2, columns - distanceFromBorder), Random.Range(rows / 2 + 2, rows - distanceFromBorder));
        var botLeft = new Coordinate(Random.Range(distanceFromBorder, columns / 2 - 2), Random.Range(distanceFromBorder, rows / 2 - 2));
        var botRight = new Coordinate(Random.Range(columns / 2 + 2, columns - distanceFromBorder), Random.Range(distanceFromBorder, rows / 2 - 2));

        var myCoords = new Coordinate[4] { topLeft, topRight, botLeft, botRight };

        foreach (var coord in myCoords)
        {
            Tile tile = myMapTiles[coord.GetX(), coord.GetY()];

            tile.SetTrap(0); // places a neutral trap
        }

    }

    public void MapSetup()
    {
        myMap = new GameObject[columns, rows];
        myMapTiles = new Tile[columns, rows];
        allPlayers = new GameObject[4];

        stasisEffectPrefab = (GameObject)Resources.Load("Stasis");

        int finalTilesNbr = columns * rows;
        int randomTilesNbr = finalTilesNbr - 21;
        int generatedTilesNbr = 0;

        TileTypes[] tilesArray;
        finalShift = new Vector3(-(columns - 1) * tileSize / 2.0f, -(rows - 1) * tileSize / 2.0f, 0f);

        nbrOfTiles = GenerateInitialTiles();

        // Check of the number of tiles generated
        for (int i = 0; i < nbrOfTiles.Length; i++)
            generatedTilesNbr += nbrOfTiles[i];

        // Adds or Removes the tiles in order to match the wanted amount
        if (generatedTilesNbr != randomTilesNbr)
        {
            if (generatedTilesNbr > randomTilesNbr)
            {
                while (generatedTilesNbr != randomTilesNbr)
                {
                    int indx = Random.Range(0, 4); // defines the tile type to me removed

                    if (indx == 0 && nbrOfTiles[indx] > curvesCount.minimum) // removes one curve
                    {
                        nbrOfTiles[indx]--;
                        generatedTilesNbr--;
                    }
                    else if (indx == 1 && nbrOfTiles[indx] > straightCount.minimum) // removes one straight
                    {
                        nbrOfTiles[indx]--;
                        generatedTilesNbr--;
                    }
                    else if (indx == 2 && nbrOfTiles[indx] > tCount.minimum) // removes one T
                    {
                        nbrOfTiles[indx]--;
                        generatedTilesNbr--;
                    }
                    else if (indx == 3 && nbrOfTiles[indx] > crossCount.minimum) // removes one cross
                    {
                        nbrOfTiles[indx]--;
                        generatedTilesNbr--;
                    }
                }

            }
            else
            {
                while (generatedTilesNbr != randomTilesNbr)
                {
                    int indx = Random.Range(0, 4);

                    if (indx == 0 && nbrOfTiles[indx] < curvesCount.maximum)
                    {
                        nbrOfTiles[indx]++;
                        generatedTilesNbr++;
                    }
                    else if (indx == 1 && nbrOfTiles[indx] < straightCount.maximum)
                    {
                        nbrOfTiles[indx]++;
                        generatedTilesNbr++;
                    }
                    else if (indx == 2 && nbrOfTiles[indx] < tCount.maximum)
                    {
                        nbrOfTiles[indx]++;
                        generatedTilesNbr++;
                    }
                    else if (indx == 3 && nbrOfTiles[indx] < crossCount.maximum)
                    {
                        nbrOfTiles[indx]++;
                        generatedTilesNbr++;
                    }
                }
            }
        }

        int tmp = 0, tmpIdx = 0;

        // Creates the tiles inside the array containing them all by index
        tilesArray = new TileTypes[randomTilesNbr];
        for (int i = 0; i < nbrOfTiles.Length; i++)
        {
            for (int j = 0; j < nbrOfTiles[i]; j++)
            {
                if (i == 0)
                    tmp = Random.Range(0, 4);
                else if (i == 1)
                    tmp = Random.Range(4, 6);
                else if (i == 2)
                    tmp = Random.Range(6, 10);
                else if (i == 3)
                    tmp = 10;

                tilesArray[tmpIdx] = (TileTypes)tmp;
                tmpIdx++;
            }
        }

        // Scramble the tiles inside the array
        tilesArray = GeneralMethods.ReshuffleArray(tilesArray);

        // Places the tiles
        tmpIdx = 0;

        // Generates the 4 corners of the map
        GenerateSpawningAreas();

        // Generates the central area
        GenerateCentralArea();

        // Generates all the remaining tiles
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                bool isSpecial;

                isSpecial = (i <= 1 && j == 0) || // bot-left
                            (i == 0 && j <= 1) ||
                            (i <= 1 && j == rows - 1) || // top-left
                            (i == 0 && j >= rows - 2) ||
                            (i >= columns - 2 && j == rows - 1) || // top-right
                            (i == columns - 1 && j >= rows - 2) ||
                            (i >= columns - 2 && j == 0) || // bot-right
                            (i == columns - 1 && j <= 1) ||

                            (i >= columns / 2 - 1 && i <= columns / 2 + 1 && j >= rows / 2 - 1 && j <= rows / 2 + 1); // central core 

                if (!isSpecial)
                {
                    InstantiateTile(tilesArray[tmpIdx], new Coordinate(i, j));
                    tmpIdx++;
                }

            }
        }

        CreatePlayers();

        //updateTilesConnection();
        CreateInsertArrows();
        PlaceNeutralTraps();

        mapManagerTransform.position = finalShift;
        foreach (var pl in allPlayers)
        {
            pl.transform.position += finalShift;
        }

        //updateTilesConnection();

    }

    //Objects Instance

    void InstantiateStartingPos(int playerNbr)
    {
        Coordinate coordinate = null;

        TileTypes tileType = TileTypes.NONE;
        string tile_path = "" ;
        string animator_path = "" ;

        if (playerNbr == 0)
        {
            coordinate = new Coordinate(0, rows - 1);
            tile_path = "Tiles/tile_spawn_1P";
            animator_path = "TilesAnimators/tile_spawn_1P";
            tileType = TileTypes.Curve_BR;
        }
        else if (playerNbr == 1)
        {
            coordinate = new Coordinate(columns - 1, rows - 1);
            tile_path = "Tiles/tile_spawn_2P";
            animator_path = "TilesAnimators/tile_spawn_2P";
            tileType = TileTypes.Curve_LB;
        }
        else if (playerNbr == 2)
        {
            coordinate = new Coordinate(0, 0);
            tile_path = "Tiles/tile_spawn_3P";
            animator_path = "TilesAnimators/tile_spawn_3P";
            tileType = TileTypes.Curve_RT;
        }
        else
        {
            coordinate = new Coordinate(columns - 1, 0);
            tile_path = "Tiles/tile_spawn_4P";
            animator_path = "TilesAnimators/tile_spawn_4P";
            tileType = TileTypes.Curve_TL;
        }

        GameObject tileInstance = Instantiate(tile, coordinate.GetVect3WithZ(), Quaternion.identity);
        tileInstance.transform.SetParent(mapManagerTransform);

        Tile myTileComponent = tileInstance.GetComponent<Tile>();
        myTileComponent.myTexture = (Texture2D)Resources.Load(tile_path);
        myTileComponent.SetPossibleConnections(tileType);

        Animator myAnimator = tileInstance.GetComponent<Animator>();
        myAnimator.runtimeAnimatorController = (RuntimeAnimatorController)Resources.Load(animator_path);

        myTileComponent.mySprite = Sprite.Create(myTileComponent.myTexture,
            new Rect(0, 0, myTileComponent.myTexture.width, myTileComponent.myTexture.height),
            new Vector2(0.5f, 0.66f));
        myTileComponent.myRenderer.sprite = myTileComponent.mySprite;
        myTileComponent.canBeMoved = false;
        myTileComponent.myCoord = coordinate;

        myMap[coordinate.GetX(), coordinate.GetY()] = tileInstance;
        myMapTiles[coordinate.GetX(), coordinate.GetY()] = tileInstance.GetComponent<Tile>();
    }

    void InstantiateTile(TileTypes tileType, Coordinate coordinate, bool canBeMoved = true)
    {
        GameObject tileInstance = Instantiate(tile, coordinate.GetVect3WithZ(), Quaternion.identity);
        tileInstance.transform.SetParent(mapManagerTransform);

        Tile myTileComponent = tileInstance.GetComponent<Tile>();
        myTileComponent.SetSprite(tileType);

        myTileComponent.canBeMoved = canBeMoved;
        myTileComponent.myCoord = coordinate;
        myTileComponent.SetPossibleConnections(tileType);

        myMap[coordinate.GetX(), coordinate.GetY()] = tileInstance;
        myMapTiles[coordinate.GetX(), coordinate.GetY()] = tileInstance.GetComponent<Tile>();

    }

    void InstantiateDiamond(Coordinate coordinate)
    {
        Vector3 diamondPosition = coordinate.GetVect3();
        diamondPosition.z = -10;
        myDiamondInstance = Instantiate(diamond, diamondPosition, Quaternion.identity);
        myDiamondInstance.transform.SetParent(myMap[coordinate.GetX(), coordinate.GetY()].transform);

        diamondCoords = new Coordinate(coordinate.GetX(), coordinate.GetY());

        Texture2D myTexture = (Texture2D)Resources.Load("Tiles/crystal 1");
        Sprite mySprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f));
        myDiamondInstance.GetComponent<SpriteRenderer>().sprite = mySprite;
        myDiamondInstance.tag = "Diamond";

    }

    

    // Utility Methods

    // Checks wether or not the tile respects the given boundaries
    bool CheckTileRange(int tileIdx)  
    {

        if (0 <= tileIdx && tileIdx < 4)
        {
            if (nbrOfTiles[0] < curvesCount.maximum)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        else if (4 <= tileIdx && tileIdx < 6)
        {
            if (nbrOfTiles[1] < straightCount.maximum)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        else
        {
            if (nbrOfTiles[2] < tCount.maximum)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

}
