/* 
 * The turn manager component is the one in charge of the turn phases management and hence
 * one of the most pivotal implementation in he game
 * 
 * The turn structure relies on the coroutine event handling system for temporization
 * It is arranged as a series of coroutines that goes from dealing with very broad tasks
 * (like the Turn coroutine, managing the whole player turn) to increasingly detailed one
 * (like the insert InsertTile coroutine) waiting for each other.
 * 
 * In many situations the deepest coroutine level is found in other scripts, since it can
 * requires the intervention of some actor (e.g. the player attack coroutine)
 * 
 * The only exceptions to the nested coroutine structure are:
 *     - The pause coroutine: always running in order to listen for a pause menu request
 *     - The pass coroutine: always running during a player turn in order to listen for
 *       a pass turn request
 *     - The diamond coroutine: when the player is the diamond holder the system
 *       runs a second coroutine listening for a possible usage of the diamond power
 * 
*/

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TurnManager : MonoBehaviour
{
    public CameraMovement myCamera;
    public FadeManager fade;

    public BlackHole blackHole;
    public GameObject diamondFX;
    public GameObject stasis;

    [HideInInspector] public bool isAcceptingInputs = true;
    [HideInInspector] public bool isInPause = false;
    [HideInInspector] bool insertedTrapFromSlide;

    // Informs the Game Manager that the game is over
    [HideInInspector] public bool gameActive = false;

    /* Turn Status. Indicate the moves still available during the player turn */

    bool canMove;
    bool canTerraform;
    bool canUseDiamond;

    PanelSelection selectionDepth;

    bool[] isFirstTurn;                 // Keeps track of the player first turn in order to play the correct audio

    bool isGameOver = false;            // Used to indicate that the match is over and display the victory screen

    DiamondStatus diamond;              // Keesp track of all the dimond relevant information (position, active status, owner...)

    /* Players Reference */

    Player activePlayer;
    Player[] allPlayers;

    int[] playerOrder;                  // sorts the player playing order by index

    Card activeCard;                    // Player card description

    /* Map Elements */

    MapManager mapManager;              // Main component dealing with map related variations and information
  
    SelectionArrowGroup arrows;         // Arrows used during the tile insertion phase
  
    CursorMoving rotationCursor;        // Cursor used for the tiles rotation

    List<Trap> trapsToActivate = new List<Trap>();  // List containing the traps positioned duriing the active turn


    /* UI */

    UIManager uiManager;                // Component controlling the UI animations


    /* Audio */

    public GameObject dialogueManager;


    /* Coroutines related variables. Needed in order to communicate between different coroutines */

    Player fallingPlayer;
    Player attackingPlayer;

    bool trapHasTriggered = false;
    bool attackHasHappened = false;
    bool slideAttack = false;

    // Object pooling position
    Vector3 parkingPosition;
    

    enum MyButtons
    {
        Walk, Terraform, Crystal, Pass
    };

    void Awake()
    {
        // Corresponds to player playing
        isFirstTurn = new bool[4] { true, true, true, true };

        /* At the moment the player order is predefined but is already implementd the
           possibility of using a random order:

           playerOrder = GeneralMethods.ReshuffleArray(playerOrder); */

        playerOrder = new int[4] { 0, 1, 2, 3 };

        parkingPosition = new Vector3(-1000f, 0, 0);

        uiManager = GetComponent<UIManager>();
    }


    /* Turn Manager Initialization. Used after the map has been correctly initialized */

    public void Initialize(MapManager inputMapManager)
    {
        selectionDepth = PanelSelection.BASE;
        mapManager = inputMapManager;

        ArrangePlayersInTurnOrder();
        myCamera.CameraSetRowsAndColumns(mapManager);

        arrows = new SelectionArrowGroup();
        arrows.Initialize(mapManager.GetAllInstancedArrows(), mapManager.rows, mapManager.columns);

        diamond = new DiamondStatus();
        diamond.Initialize(mapManager.diamondCoords, GameObject.FindGameObjectWithTag("Diamond"), diamondFX, stasis);

        rotationCursor = GameObject.FindGameObjectWithTag("RotationCursor").GetComponent<CursorMoving>();
    }

    // Game Activation/Deactivation

    public void SetGameActive(bool val)
    {
        gameActive = val;
    }

    public bool GetGameActive()
    {
        return gameActive;
    }

    /* 
     * Section describing all the coroutines managing the player turn
     */

    // Nested coroutine entrance
    public IEnumerator Turn()
    {
        if (canUseDiamond)
        {
            StartCoroutine(DiamondCO());        // Coroutine listening for the diamond activation
        }

        StartCoroutine(PassTurnCO());           // Pass turn listener


        while (canMove || canTerraform || canUseDiamond)
        {
            yield return StartCoroutine(BaseLevelSelectionCO());    // Entrance level of the nested coroutine system
        }


        yield return null;
    }

    // Parallel coroutine listening for a pass turn call
    IEnumerator PassTurnCO()
    {
        while (canMove || canTerraform || canUseDiamond)
        {
            if ((Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("PassTurn")) && isAcceptingInputs)
            {
                ActivatePassTurnCondition();
            }

            yield return null;
        }
    }

    // Parallel coroutine listening to a use diamond call
    IEnumerator DiamondCO()
    {
        // Self interrupts on player switch
        while (canUseDiamond)
        {
            if ((Input.GetKeyDown(KeyCode.M) || Input.GetButtonDown("Fire4joy")) && isAcceptingInputs)
            {
                yield return StartCoroutine(ActivateDiamondStasisCO());

                // Activates Pass turn condition
                canUseDiamond = false;
                canTerraform = false;
                canMove = false;
            }

            yield return null;
        }
    }

    /* Diamond related sub-coroutines */

    IEnumerator ActivateDiamondStasisCO()
    {
        diamond.ActivateDiamondStasis(activePlayer);
        yield return new WaitForSeconds(diamond.stasisAnimationDuration);

        uiManager.SetDiamondAnimation(5);       // 5 corresponds to the diamond active animation

        dialogueManager.GetComponent<Speaker>().PlayStasys(0);
        yield return null;
    }

    IEnumerator DeActivateDiamondStasisCO()
    {
        diamond.RemoveStasisEffect();
        yield return new WaitForSeconds(diamond.stasisAnimationDuration);
        diamond.ParkStasis();
    }

    // Starting panel coroutine
    IEnumerator BaseLevelSelectionCO()
    {
        selectionDepth = PanelSelection.BASE;

        // Definition and older decisions unload
        ButtonSelection decision = activePlayer.Decision();

        // Checks the pass turn condition
        bool isTurnStillActive = true;

        while (selectionDepth == PanelSelection.BASE && isTurnStillActive)
        {
            decision = activePlayer.Decision();

            if (decision == ButtonSelection.LEFT && canMove)                // Walk
            {
                StartCoroutine(uiManager.ActivatePanel(PanelSelection.WALK));
                yield return StartCoroutine(ActivateMovementPhase());
            }
            else if (decision == ButtonSelection.MIDDLE && canTerraform)    // Terraform
            {
                StartCoroutine(uiManager.ActivatePanel(PanelSelection.TERRAFORM));
                yield return StartCoroutine(TerraformLevelSelectionCO());
            }

            isTurnStillActive = canMove || canTerraform || canUseDiamond;   // End Turn Condition

            yield return null;
        }
 
    }

    // Terraform panel coroutine
    IEnumerator TerraformLevelSelectionCO()
    {
        selectionDepth = PanelSelection.TERRAFORM;
        ButtonSelection decision = ButtonSelection.NONE;

        while (selectionDepth == PanelSelection.TERRAFORM)
        {
            decision = activePlayer.Decision();

            if (decision == ButtonSelection.LEFT)           // Slide
            {
                StartCoroutine(uiManager.ActivatePanel(PanelSelection.SLIDE));
                yield return StartCoroutine(ScrollTileInsertionSelection());
            }
            else if (decision == ButtonSelection.MIDDLE)    // Rotation
            {
                StartCoroutine(uiManager.ActivatePanel(PanelSelection.ROTATE));
                yield return StartCoroutine(ActivateRotationCursor());
            }
            else if (decision == ButtonSelection.RIGHT)     // Back to base panel
            {
                StartCoroutine(uiManager.ActivatePanel(PanelSelection.BASE));
                selectionDepth = PanelSelection.BASE;
                yield return null;
            }

            yield return null;
        }
    }

    IEnumerator EndTerraform()
    {
        mapManager.UpdateTilesConnection(activePlayer.playerID);

        canTerraform = false;

        uiManager.SetTerraformButton(false);

        // Return to base selection level
        selectionDepth = PanelSelection.BASE;
        StartCoroutine(uiManager.ActivatePanel(PanelSelection.BASE));

        mapManager.UpdateTilesZOrder();

        yield return null;
    }

    /* Tile Slide Related Coroutines */

    IEnumerator InsertTile(InsertArrow arrow, SlideDirection slideDirection)
    {
        Coordinate[] lineCoordinates = arrow.GetPointedCoords();
        lineCoordinates = mapManager.KeepMovableTiles(lineCoordinates);

        // Checks for PvP actions and resolves it
        yield return StartCoroutine(CheckPvPLinear(lineCoordinates));

        // Checks for Player on destroyed tile and resolves it
        Tile lastTile = mapManager.PickTileComponent(lineCoordinates[lineCoordinates.Length - 1]);
        yield return StartCoroutine(PlayerOutOfBounds(lineCoordinates, lastTile));

        // Checks if the diamond is found on the last tile and would be otherwise destroyed
        bool bindDiamond = !lastTile.myCoord.IsEqual(diamond.coordinates) && !slideAttack;
        
        // Binds all the players and the diamond to the moving tiles
        BindAllToTiles(bindDiamond);

        // Slides the Tiles on the selected line
        yield return StartCoroutine(mapManager.SlideLine(lineCoordinates));

        // Unbinds after the end of the slide
        UnbindAllFromTiles();

        TileTypes tileType = activeCard.GetTileType();

        Tile myNewTile;
        if (activeCard.GetTrappedStatus())
            myNewTile = mapManager.InstantiateTileLive(tileType, lineCoordinates[0], true);  // places a trap on top of the tile
        else
            myNewTile = mapManager.InstantiateTileLive(tileType, lineCoordinates[0]);  // default is false

        if (fallingPlayer != null)
        {
            Coordinate cornerCoords = mapManager.GetAllCornerCoordinates()[fallingPlayer.playerID];
            yield return StartCoroutine(blackHole.BlackHoleRespawn( mapManager.myMapTiles[cornerCoords.GetX(), cornerCoords.GetY()] , fallingPlayer));
            fallingPlayer = null;
            slideAttack = false;
        }

        TileTypes newCardType = lastTile.type;
        bool trapStatus = lastTile.GetIsTrapped();

        yield return null;

        activeCard.AssignType(newCardType, trapStatus);

        if (attackingPlayer != null)
        {
            PickUpDiamond(attackingPlayer);
            attackingPlayer = null;
        }

        arrows.SetAnimatorActive(false);
        arrows.SetAllVisible(false);

        myCamera.MoveToPosition(activePlayer.GetComponentInParent<Transform>().position);
        yield return StartCoroutine(EndTerraform());

        // Debug.Log(mapManager.diamondCoords.ToString());
        yield return null;
    }

    IEnumerator ScrollTileInsertionSelection()
    {
        StartCoroutine(myCamera.ZoomToCenter());

        arrows.SelectFirst();
        arrows.SetAnimatorActive(true);

        // yield return null;

        //bool firstClick = false;
        //float firstClickWaitingTime = 0.5f;
        
        yield return StartCoroutine(activePlayer.SelectTileInsertion(arrows));
        ButtonSelection decision = activePlayer.Decision();

        // BACK
        if (decision == ButtonSelection.RIGHT)
        {
            arrows.SetAnimatorActive(false);
            arrows.SetAllVisible(false);

            myCamera.MoveToPosition(activePlayer.GetComponentInParent<Transform>().position);  // rly?
            StartCoroutine(uiManager.ActivatePanel(PanelSelection.TERRAFORM));
        }

        // INSERT TILE
        else if (decision == ButtonSelection.MIDDLE)
        {
            arrows.SetVisible(true);
            SlideDirection mySlideDirection = arrows.GetCurrentSlideDirection();

            yield return StartCoroutine(InsertTile(arrows.GetArrow(), mySlideDirection));
        }

        yield return null;
    }

    IEnumerator CheckPvPLinear(Coordinate[] lineCoordinates)
    {
        // Checks how many players are found in the group of slided tiles
        int[] playerIdxInCoords = GetPlayersAtCoordinatesIDX(lineCoordinates);

        if (playerIdxInCoords.Length > 1) // more than one player is on the slided tiles
        {
            Array.Sort(playerIdxInCoords);
            for (int i = 1; i < playerIdxInCoords.Length; i++)
            {
                // players are positioned one tile away from each other in the slide direction
                if (playerIdxInCoords[i] - playerIdxInCoords[i - 1] == 1)
                {
                    // checks whether or not the player in the front has the stasis active
                    Player frontPlayer = GetPlayerAtCoordinates(lineCoordinates[playerIdxInCoords[i]]);

                    if (diamond.stasisActive && frontPlayer.playerID == diamond.diamondOwner)
                    {
                        attackingPlayer = allPlayers[mapManager.PickTileComponent(lineCoordinates[playerIdxInCoords[i - 1]]).occupiedID];
                        yield return StartCoroutine(attackingPlayer.AttackPlayerOnSlide(frontPlayer));
                        StartCoroutine(DeActivateDiamondStasisCO());
                        fallingPlayer = frontPlayer;
                        slideAttack = true;
                    }
                    
                }
            }
        }

        yield return null;
    }

    IEnumerator PlayerOutOfBounds(Coordinate[] lineCoordinates, Tile lastTile)
    {

        if (lastTile.occupiedID != -1)
        {
            myCamera.MoveToHighlight(lineCoordinates[lineCoordinates.Length - 1].GetPositionFromCoords(mapManager.columns, mapManager.rows));
            yield return null;

            int playerIdx = GeneralMethods.FindElementIdx(playerOrder, lastTile.occupiedID);
            fallingPlayer = allPlayers[playerIdx];
            if (diamond.diamondOwner == fallingPlayer.playerID)
            {
                diamond.DropDiamond(fallingPlayer);
                StartCoroutine(uiManager.SetDiamondAnimation(3));
            }

            yield return StartCoroutine(blackHole.StartRemoveBlackHole(lastTile, fallingPlayer));

            StartCoroutine(myCamera.ZoomToCenter());
            yield return new WaitForSeconds(1f);
            
            dialogueManager.GetComponent<Speaker>().PlaySomeoneFall();
        }
        yield return null;
    }

    /* Tile Rotation Related Coroutines */

    IEnumerator ActivateRotation(RotationDirection direction)
    {
        Coordinate[] selectedCoords = rotationCursor.GetSelectedCoords();
        rotationCursor.MoveAtPosition(parkingPosition);

        // Possible PvP
        yield return CheckPvPRotation(selectedCoords, direction);

        // Binds all the players and the diamond to the moving tiles
        // true means that the diamond will also be bound to the tiles
        BindAllToTiles(!slideAttack);

        yield return StartCoroutine(mapManager.RotateTiles(selectedCoords, direction));

        // Unbinds after the end of the slide
        UnbindAllFromTiles();

        slideAttack = false;

        if (attackingPlayer != null)
        {
            PickUpDiamond(attackingPlayer);
            attackingPlayer = null;
        }

        if (fallingPlayer != null)
        {
            Coordinate cornerCoords = mapManager.GetAllCornerCoordinates()[fallingPlayer.playerID];
            yield return StartCoroutine(blackHole.BlackHoleRespawn(mapManager.myMapTiles[cornerCoords.GetX(), cornerCoords.GetY()], fallingPlayer));
            fallingPlayer = null;
            slideAttack = false;
        }

        yield return StartCoroutine(EndTerraform());

    }

    IEnumerator ActivateRotationCursor()
    {
        selectionDepth = PanelSelection.ROTATIONCURSOR;
        rotationCursor.CursorActivate(activePlayer.coordinate);

        yield return StartCoroutine(activePlayer.SelectRotation(rotationCursor));
        ButtonSelection decision = activePlayer.Decision();

        if (decision == ButtonSelection.RIGHT || Input.GetButtonDown("StartButton"))
        {
            rotationCursor.MoveAtPosition(parkingPosition);
            selectionDepth = PanelSelection.TERRAFORM;
            StartCoroutine(uiManager.ActivatePanel(PanelSelection.TERRAFORM));
        }
        else if (decision == ButtonSelection.LEFT)
        {
            canTerraform = false;
            yield return StartCoroutine(ActivateRotation(RotationDirection.CLOCKWISE));
        }
        else if (decision == ButtonSelection.MIDDLE)
        {
            canTerraform = false;
            yield return StartCoroutine(ActivateRotation(RotationDirection.COUNTERCLOCKWISE));
        }

        yield return null;

    }

    IEnumerator CheckPvPRotation(Coordinate[] selectedCoords, RotationDirection direction)
    {
        // Checks for players on the slided tiles
        int[] playerIdxInCoords = GetPlayersAtCoordinatesIDX(selectedCoords);

        // More than one player is found on the slided tiles
        if (playerIdxInCoords.Length > 1) 
        {
            Array.Sort(playerIdxInCoords);
            for (int i = 1; i < playerIdxInCoords.Length; i++)
            {
                int directionDistance = -1, directionDistanceAlt = -3;
                if (direction == RotationDirection.COUNTERCLOCKWISE)
                {
                    directionDistance = 1;
                    directionDistanceAlt = 3;
                }


                // Players are positioned one tile away from each other in the slide direction
                if (playerIdxInCoords[i] - playerIdxInCoords[i - 1] == directionDistance ||
                    playerIdxInCoords[i] - playerIdxInCoords[i - 1] == directionDistanceAlt) 
                {
                    // Checks whether or not the player in the front has the stasis active
                    Player frontPlayer = GetPlayerAtCoordinates(selectedCoords[playerIdxInCoords[i]]);
                        
                    if (diamond.stasisActive && frontPlayer.playerID == diamond.diamondOwner)
                    {
                        attackingPlayer = allPlayers[mapManager.PickTileComponent(selectedCoords[playerIdxInCoords[i - 1]]).occupiedID];
                        yield return StartCoroutine(attackingPlayer.AttackPlayerOnSlide(frontPlayer));
                        StartCoroutine(DeActivateDiamondStasisCO());
                        fallingPlayer = frontPlayer;
                        slideAttack = true;
                    }
                }

            }
        }

        yield return null;
    }


    /* Player Movement Coroutines */

    IEnumerator ActivateMovementPhase()
    {
        // Camera starts following the player
        myCamera.StartFollowing(activePlayer.transform);

        // Waits for the protective barrier to be removed before moving
        yield return StartCoroutine(activePlayer.ActivateBarrier(false));

        // Stored in case the movement is not confirmed
        Coordinate movementStartingPosition = activePlayer.coordinate.GetCopy();

        // Calculate the path and bright up the connected tiles
        mapManager.UpdateTilesConnection(activePlayer.playerID);
        mapManager.BrightPossibleTiles(activePlayer.coordinate, activePlayer.playerID);

        // Movement Phase
        yield return StartCoroutine(activePlayer.Move());
        ButtonSelection decision = activePlayer.Decision();

        // Movement not confirmed
        if (decision == ButtonSelection.RIGHT)
        {
            // Reset the player indication on the tile when the abort choice is performed
            Tile currentPlayerTile = mapManager.myMapTiles[activePlayer.coordinate.GetX(), activePlayer.coordinate.GetY()];
            currentPlayerTile.occupiedID = -1;

            // Teleports to the tile where the movement phase beginned
            Vector3 pos = mapManager.GetVector3FromCoords(movementStartingPosition);
            pos.z = -1f;
            activePlayer.transform.position = pos;
            activePlayer.coordinate = movementStartingPosition;

            // Set the player indication on the teleported tile
            currentPlayerTile = mapManager.myMapTiles[activePlayer.coordinate.GetX(), activePlayer.coordinate.GetY()];
            currentPlayerTile.occupiedID = activePlayer.playerID;

            myCamera.MoveToPosition(activePlayer.transform.position);
            ResetActivatedTraps();
        }

        // Movement confirmed or interrupted. Equivalent to:
        // if (decision == ButtonSelection.MIDDLE || trapHasTriggered || attackHasHappened)
        else
        {
            if (activePlayer.coordinate.IsEqual(diamond.coordinates) && diamond.diamondOwner != activePlayer.playerID)
            {
                PickUpDiamond(activePlayer);
            }

            if (CheckVictoryCondition())
            {
                EndGame(activePlayer);
            }

            if (activePlayer.playerID == diamond.diamondOwner)
            {
                diamond.UpdateDiamondPosition(activePlayer);
            }

            uiManager.SetWalkButton(false);
            canMove = false;
        }

        mapManager.SwitchOffTiles();

        if (activePlayer.playerID != diamond.diamondOwner)
            StartCoroutine(activePlayer.ActivateBarrier(true));

        yield return null;

        // Return to base selection level
        selectionDepth = PanelSelection.BASE;
        StartCoroutine(uiManager.ActivatePanel(PanelSelection.BASE));

        myCamera.StopFollowing();

        yield return null;
    }

    public void PickUpDiamond(Player player)
    {
        dialogueManager.GetComponent<Speaker>().PlayCrystalGrab(player.playerID);

        diamond.CollectDiamond(player);

        if (player.playerID == activePlayer.playerID)
        {
            StartCoroutine(uiManager.SetDiamondAnimation(0));
        }
    }

    public void DropDiamond(Player player) 
    {
        if (player.playerID == diamond.diamondOwner)
        {
            diamond.DropDiamond(player);
        }
    }

    // Pass Turn

    void ActivatePassTurnCondition()
    {
        canUseDiamond = false;
        canTerraform = false;
        canMove = false;
    }

    public IEnumerator PassTurn()
    {
        // Reset abilities usage
        canMove = true;
        canTerraform = true;

        // Change active Player and selects the first during the first turn
        if (activePlayer != null)
        {
            activePlayer = allPlayers[(activePlayer.playerID + 1) % 4];
        }
        else
        {
            activePlayer = allPlayers[0];
        }

        // Checks if the player has the diamond
        if (activePlayer.playerID == diamond.diamondOwner)
        {
            if (diamond.stasisActive)
            {
                StartCoroutine(DeActivateDiamondStasisCO());
                diamond.ResetTurnsBeforeStasis();
            }
            else
            {
                // Checks if the dimaond status is active
                canUseDiamond = diamond.turnsBeforeStasisCounter == 0;

                if (!canUseDiamond)
                {
                    diamond.turnsBeforeStasisCounter--;
                }
            }
        }

        // Modifies the pause button
        uiManager.SetPauseButton(activePlayer.playerID);

        // Camera movement tot the next player
        yield return StartCoroutine(MoveToNextPlayer());

        // Updates the effective tile connection
        mapManager.UpdateTilesConnection(activePlayer.playerID);

        // diamond.CheckDiamondStatusTimer(activePlayer.playerID);

        uiManager.SwitchActivePortrait(activePlayer.playerID);

        activeCard = uiManager.GetActiveCards();

        uiManager.ResetButtons(diamond, activePlayer);
        ActivateTraps();

        // Plays the intro dialogue if it is the player first turn
        if (isFirstTurn[activePlayer.playerID])
        {
            dialogueManager.GetComponent<Speaker>().PlayIntros(activePlayer.playerID);
            isFirstTurn[activePlayer.playerID] = false;
        }

    }

    IEnumerator MoveToNextPlayer()
    {
        myCamera.MoveToCenter();
        yield return new WaitForSeconds(2f);
        myCamera.MoveToPosition(activePlayer.GetComponentInParent<Transform>().position);
        yield return null;
    }

    // Pause

    public void PauseGame()
    {
        uiManager.Pause();
        isInPause = true;
    }

    public void ResumeGame()
    {
        uiManager.Resume();
        isInPause = false;
    }

    // Access methods

    public CameraMovement GetCameraComponent()
    {
        return myCamera;
    }

    public int GetActivePlayer()
    {
        return activePlayer.playerID;
    }

    public Player[] GetAllPayers()
    {
        return allPlayers;
    }

    void ArrangePlayersInTurnOrder()
    {
        // set it up to work with random order even though at the moment the playerOrder is predefined
        GameObject[] tmpPlayers = GetComponentInParent<MapManager>().allPlayers;
        allPlayers = new Player[4];

        //  here we should get also the component defining the plaer movement
        for (int i = 0; i < playerOrder.Length; i++)
        {
            allPlayers[playerOrder[i]] = tmpPlayers[i].GetComponent<Player>();
        }
    }

    /* End Game Related methods */

    public bool CheckVictoryCondition()
    {
        if (activePlayer.playerID == diamond.diamondOwner &&
            activePlayer.coordinate.IsEqual(mapManager.GetPlayerCornerCoordinates(activePlayer.playerID)))
            return true;
        else
            return false;

    }

    public void EndGame(Player player)
    {
        dialogueManager.GetComponent<Speaker>().PlayVictory(player.playerID);
        uiManager.ShowWinScreen(activePlayer.playerID);
        isGameOver = true;
    }

    // Player binding to tiles methods designed for terraforming actions

    void BindAllToTiles(bool bindDiamond)
    {
        foreach (var pl in allPlayers)
        {
            // If the coordinate is -1 the player is currently out of bounds
            if (pl.coordinate.GetX() != -1)
            {
                // If there is no diamond active there is no need for further cecks on the player ID
                if ((!diamond.stasisActive) ||
                    (diamond.stasisActive && pl.playerID != diamond.diamondOwner))
                {
                    pl.transform.parent = mapManager.myMap[pl.coordinate.GetX(), pl.coordinate.GetY()].transform;
                }
            }
        }

        if (bindDiamond)
        {
            diamond.diamond.transform.parent = mapManager.myMap[diamond.coordinates.GetX(), diamond.coordinates.GetY()].transform;
        }

    }

    void UnbindAllFromTiles()
    {
        foreach (var pl in allPlayers)
        {
            
            pl.transform.parent = null;
            
        }

        diamond.diamond.transform.parent = null;
      
    }

    public void UpdateTileOccupiedID(int id, Coordinate coords)
    {
        mapManager.myMapTiles[coords.GetX(), coords.GetY()].occupiedID = id;
    }

    public void UpdateDiamondPosition(Coordinate coords)
    {
        diamond.UpdateDiamondPosition(coords);
    }

    public void SetAttackHasHappened(bool status)
    {
        attackHasHappened = status;
    }
    
    // Traps

    public void SetTrapHasTriggered(bool status)
    {
        trapHasTriggered = status;
    }

    public void AddToActivateTrapList(Trap trap)
    {
        trapsToActivate.Add(trap);
    }

    public void ActivateTraps()
    {
        for (int i = trapsToActivate.Count - 1; i >= 0; i--)
        {
            Trap thisTrap = trapsToActivate[i];
            bool isOverlappedToPlayer = false;
            foreach (Player player in allPlayers)
            {
                isOverlappedToPlayer = isOverlappedToPlayer || (player.coordinate.IsEqual(thisTrap.GetCoordiantes()));
            }

            if (!isOverlappedToPlayer)
            {
                thisTrap.Activate();
                trapsToActivate.Remove(thisTrap);
                //trapsToActivate.RemoveAt(i);
            }
        }
    }

    public void ResetActivatedTraps()
    {
        for (int i = trapsToActivate.Count - 1; i >= 0; i--)
        {
            if (insertedTrapFromSlide && i == 0)
                continue;
            else
            {
                Trap thisTrap = trapsToActivate[i];
                thisTrap.SetPlayerDropping(0);
                trapsToActivate.Remove(thisTrap);
            }
        }
    }

    // General Methods

    public Player GetPlayerAtCoordinates(Coordinate coord)
    {
        Player myPlayer = null;
        foreach (Player player in allPlayers)
        {
            if (coord.IsEqual(player.coordinate))
            {
                myPlayer = player;
                break;
            }
        }
        return myPlayer;

    }
    
    private Player[] GetPlayersAtCoordinates(Coordinate[] coords)
    {
        List<Player> playersInCoords = new List<Player>();
        foreach (Player player in allPlayers)
        {
            if (player.coordinate.FindInGroup(coords) != -1)
                playersInCoords.Add(player);
        }

        return playersInCoords.ToArray();
    }

    private int[] GetPlayersAtCoordinatesIDX(Coordinate[] coords)
    {
        List<int> playersIdxInCoords = new List<int>();
        for (int i = 0; i < allPlayers.Length; i++)
        {
            int idx = allPlayers[i].coordinate.FindInGroup(coords);
            if (idx != -1)
                playersIdxInCoords.Add(idx);
        }

        return playersIdxInCoords.ToArray();
    }

    void Update()
    {
        if (isGameOver && Input.GetButtonDown("Fire1joy"))
            SetGameActive(false);

        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("StartButton")) && !isInPause)
        {
            uiManager.Pause();
            isInPause = true;
        }
        else if ((Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("StartButton")) && isInPause)
        {
            uiManager.Resume();
            isInPause = false;
        }

        // For Debugging
        if (isAcceptingInputs && !isInPause)
        { 
            if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene("_Scenes/Game");
        }

        
    }
}
