using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanPlayer : Player
{
    float walkingTime;

    ButtonSelection mDecision = ButtonSelection.NONE;
    DirectionSelection mDirection = DirectionSelection.NONE;

    [HideInInspector] public int isPlayerTurn;

    SpriteRenderer myRenderer;
    Sprite mySprite;

    Animator myAnimator, myBarrierAnimator;

    [HideInInspector] public bool moving = false;

    [HideInInspector] public TurnManager turnManager;
    [HideInInspector] public MapManager mapManager;

    bool trapHasTriggered   = false;
    bool attackHasHappened  = false;

    private Coordinate startingPoint;
    [HideInInspector] public int attackActivated = 0;

    GameObject myBarrier;

    /* Interface Methods Override */

    public override void SetDescription(PlayerDescription description)
    {
        playerID = description.playerID;

        mySprite = description.GetSprite();
        myRenderer.sprite = mySprite;

        myAnimator = transform.GetChild(0).GetComponent<Animator>();
        myAnimator.runtimeAnimatorController = description.playerAnimator;

        myBarrierAnimator.runtimeAnimatorController = description.barrierAnimator;

        walkingTime = description.walkingTime;
    }

    public override void UpdateCoordinates(Coordinate newCoordinates)
    {
        coordinate = newCoordinates;
    }

    public override DirectionSelection InputDirection()         // unloads the input direction and returns it
    {
        DirectionSelection tmpDir = mDirection;
        mDirection = DirectionSelection.NONE;
        return tmpDir;
    }       

    public override ButtonSelection Decision()
    {
        ButtonSelection tmpDecision = mDecision;                // unload the decision and returns it
        mDecision = ButtonSelection.NONE;
        return tmpDecision;
    }

    public override IEnumerator Move()
    {
        
        mDecision = ButtonSelection.NONE;                       // Reset mDecision in case a selection was still active

        
        while (mDecision != ButtonSelection.MIDDLE &&           // MIDDLE and RIGHT corresponds to confirm and abort respectively
            mDecision != ButtonSelection.RIGHT)
        {
            DirectionSelection direction = InputDirection();
            if (direction != DirectionSelection.NONE)
            {
                yield return MoveCO(direction);
            }

            if (trapHasTriggered)                       // Movement phase interruption
            {
                trapHasTriggered = false;
                break;
            }

            if (attackHasHappened)                      // Movement phase interruption
            {
                attackHasHappened = false;
                break;
            }

            yield return null;
        }
    }

    public override IEnumerator SelectTileInsertion(SelectionArrowGroup arrows)
    { 
        mDecision = ButtonSelection.NONE;                       // Reset mDecision in case a selection was still active
 
        while (mDecision != ButtonSelection.MIDDLE &&           // MIDDLE and RIGHT corresponds to confirm and abort respectively
            mDecision != ButtonSelection.RIGHT)
        {
            DirectionSelection direction = InputDirection();

            if (direction != DirectionSelection.NONE)
            {
                arrows.SetAnimatorActive(false);

                arrows.MoveSelection(direction);

                arrows.SetAnimatorActive(true);
            }

            yield return new WaitForSeconds(0.1f);              // Waits in order to prevent too sudden movements
        }

        yield return null;


    }

    public override IEnumerator SelectRotation(CursorMoving rotationCursor)
    {
        // Reset mDecision in case a selection was still active
        mDecision = ButtonSelection.NONE;

        // Move arrow selection
        while (mDecision == ButtonSelection.NONE)
        {
            DirectionSelection direction = InputDirection();

            if (direction != DirectionSelection.NONE)
            {
                yield return StartCoroutine(rotationCursor.Move(direction));
            }

            yield return null;

        }
    }

    public override IEnumerator UseCrystal()
    {
        throw new System.NotImplementedException();
    }


    public void SetAttackActivated(int val)
    {
        attackActivated = val;
    }

    public override IEnumerator AttackPlayerOnSlide( Player otherPlayer)
    {
        CameraMovement myCamera = turnManager.GetCameraComponent();
        myCamera.MoveToHighlight(GeneralMethods.GetVect3Midpoint(transform.position, otherPlayer.GetComponent<Transform>().position));

        Tile tile = mapManager.PickTileComponent(otherPlayer.coordinate);
        StartAnimationAttack();

       
        while (attackActivated == 0)                                        // Waits for the animation to reach the attack pose. activated by animation trigger
        {
            yield return null;
        }

        turnManager.dialogueManager.GetComponent<Speaker>().PlayPvP(playerID, otherPlayer.playerID);
        turnManager.DropDiamond(otherPlayer);

        yield return StartCoroutine(CastBlackHole(tile, otherPlayer));      // Waits for blackhole animation

        StopAnimaitionAttack();

        while (attackActivated == 1)                                        // Waits for the normal animation pose. activated by animation trigger
        {
            yield return null;
        }

        otherPlayer.coordinate = new Coordinate( -1, -1 );

        yield return null;
    }

    public override IEnumerator AttackPlayer(Tile tile, bool isStasisActive = false)
    {
        turnManager.myCamera.SetOldCameraPosition(GeneralMethods.GetVect3Midpoint(transform.position, tile.GetComponent<Transform>().position));
        turnManager.myCamera.SetOldCameraSize();

        Player otherPlayer = turnManager.GetAllPayers()[tile.occupiedID];      

        CameraMovement myCamera = turnManager.GetCameraComponent();
        myCamera.MoveToHighlight(GeneralMethods.GetVect3Midpoint(transform.position, tile.transform.position));

        StartAnimationAttack();             // Set animator variable in order to reach the attack position animation
   
        while (attackActivated == 0)        // Waits for the animation to reach the attack pose. activated by animation trigger
        {
            yield return null;
        }

        turnManager.dialogueManager.GetComponent<Speaker>().PlayPvP(playerID, otherPlayer.playerID);
        turnManager.DropDiamond(otherPlayer);

        yield return StartCoroutine(CastBlackHole(tile, otherPlayer));      // Waits for the blackhole animation

        StopAnimaitionAttack();

        while (attackActivated == 1)        // Waits for the normal animation pose. activated by animation trigger
        {
            yield return null;
        }

        float elapsedTime = 0;
        float animTime = 1f;

        Vector3 destination = tile.GetComponent<Transform>().position;
        destination.z--;
        StartWalking();
        yield return null;
        StopWalking();

        while (elapsedTime < animTime)      // Moves to the enemy tile
        {
            transform.position = Vector3.Lerp(transform.position, destination, elapsedTime / animTime);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        myCamera.MoveToPositionAndZoom(turnManager.myCamera.GetOldCameraPosition(), turnManager.myCamera.GetOldCameraSize());

        Coordinate baseTileCoord = mapManager.GetAllCornerCoordinates()[otherPlayer.playerID];
        Tile baseTile = mapManager.myMapTiles[baseTileCoord.GetX(), baseTileCoord.GetY()];
        yield return StartCoroutine(turnManager.blackHole.BlackHoleRespawn(baseTile, otherPlayer));     // Respawn the defeated player


        attackHasHappened = true;

        yield return null;
    }

    public override IEnumerator ActivateBarrier(bool state)
    {
        myBarrierAnimator.SetBool("isActive", state);

        if (state)
        {
            myBarrier.transform.localPosition = new Vector3(0f, 0f, -0.01f);        // Set it over the player
        }
        else
        {     
            yield return new WaitForSeconds(0.5f);                                  // Waits for the animation to be over          
            myBarrier.transform.localPosition = new Vector3(0f, 0f, 10f);           // Hides the barrier below the field
        }

        yield return null;
    }

    /* Movement Coroutine */

    public IEnumerator MoveCO(DirectionSelection direction)
    {
        // Mirrors the sprite if needed
        if ((direction == DirectionSelection.LEFT && transform.GetChild(0).transform.localScale.x > 0f) ||
            (direction == DirectionSelection.RIGHT && transform.GetChild(0).transform.localScale.x < 0f))
        {
            InvertTransformX();
        }

        Coordinate newCoord = new Coordinate(-1, -1);

        // Connections
        // [0] -> Top       (T)
        // [1] -> Right     (R)
        // [2] -> Bottom    (B)
        // [3] -> Left      (L)

        // Index of the effective connection needed in order to enter the tile
        int checkConnectionIdx = -1;

        // Defines the coordinates of the destination and assignes the proper connection index to check
        if (direction == DirectionSelection.RIGHT)
        {
            checkConnectionIdx = 3;
            newCoord.SetCoordinate(coordinate.GetX() + 1, coordinate.GetY());
        }
        else if (direction == DirectionSelection.LEFT)
        {
            checkConnectionIdx = 1;
            newCoord.SetCoordinate(coordinate.GetX() - 1, coordinate.GetY());
        }
        else if (direction == DirectionSelection.UP)
        {
            checkConnectionIdx = 2;
            newCoord.SetCoordinate(coordinate.GetX(), coordinate.GetY() + 1);
        }
        else if (direction == DirectionSelection.DOWN)
        {
            checkConnectionIdx = 0;
            newCoord.SetCoordinate(coordinate.GetX(), coordinate.GetY() - 1);
        }

        // Moves only if the coordinates are valid
        if ( newCoord.GetX() >= 0 && newCoord.GetX() < mapManager.columns &&
             newCoord.GetY() >= 0 && newCoord.GetY() < mapManager.rows)
        {
            // Checks if the coordinates are accessible from the current tile
            Tile destinationTile = mapManager.myMapTiles[newCoord.GetX(), newCoord.GetY()];

            if (destinationTile.effectiveConnections[checkConnectionIdx] == true)
            {
                float elapsedTime = 0;

                Vector3 destination = destinationTile.transform.position;
                destination.z--;

                // Checks if the destination tile is the base of another player
                if (!CheckForOtherPlayerCorner(destinationTile.myCoord))
                {
                    // Checks if the destination tile is occupied by another player
                    if (CheckForOtherPlayer(destinationTile))
                    {
                        yield return StartCoroutine(AttackPlayer(destinationTile));
                    }

                    turnManager.UpdateTileOccupiedID(-1, coordinate);
                    StartWalking();

                    yield return null;

                    while (elapsedTime < walkingTime)
                    {
                        transform.position = Vector3.Lerp(transform.position, destination, elapsedTime / walkingTime);

                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }

                    StopWalking();

                    UpdateCoordinates(newCoord);
                    turnManager.UpdateTileOccupiedID(playerID, coordinate);

                    yield return StartCoroutine(CheckForTraps(destinationTile));

                }
                yield return null;

            }
        }


        yield return null;
    }

    public IEnumerator CheckForTraps(Tile tile)
    {
        if (tile.GetIsTrapped())
        {
            Trap trap = tile.GetTrap();

            if (trap.GetIsActive() && trap.GetPlayerDropping() - 1 != playerID)
            {
                CameraMovement myCamera = turnManager.GetCameraComponent();
                myCamera.MoveToHighlight(tile.GetComponent<Transform>().position);

                turnManager.dialogueManager.GetComponent<Speaker>().PlayTrapTrigger(tile.myTrapComponent.GetPlayerDropping());

                myCamera.StopFollowing();
                
                turnManager.DropDiamond(this);                                                          // Drops the diamond only if the player is the owner

                yield return StartCoroutine(tile.myTrapComponent.Trigger());
                yield return StartCoroutine(turnManager.blackHole.StartRemoveBlackHole(tile, this));    // Waits for the trap blackhole

                turnManager.SetTrapHasTriggered(true);

                Coordinate baseTileCoord = mapManager.GetAllCornerCoordinates()[playerID];
                Tile baseTile = mapManager.myMapTiles[baseTileCoord.GetX(), baseTileCoord.GetY()];

                // Camera movements
                yield return new WaitForSeconds(1f);
                myCamera.StopFollowing();
                myCamera.MoveToPosition(baseTile.transform.position);
                yield return new WaitForSeconds(1f);

                trapHasTriggered = true;

                yield return StartCoroutine(turnManager.blackHole.BlackHoleRespawn(baseTile, this));    // Respawn player

            }
            else if (!trap.GetIsActive() && trap.GetPlayerDropping() == 0)                              // Trap is neutral
            {
                turnManager.dialogueManager.GetComponent<Speaker>().PlayTrapActivated(playerID);
                trap.SetPlayerDropping(playerID + 1);                                                   // Becomes the owner
                turnManager.AddToActivateTrapList(trap);
                yield return null;
            }
        }
    }

    public bool CheckForOtherPlayer(Tile tile)
    {
        if (tile.occupiedID != -1)
        {
            return true;
        }
        else
        {
            foreach (var thisPlayer in turnManager.GetAllPayers())
            {
                if (thisPlayer.coordinate.Equals(tile.GetCoordinates()))
                {
                    return true;
                }
            }
        }
        return false;
    }


    /* Animation */

    public void InvertTransformX()
    {
        transform.GetChild(0).transform.localScale = new Vector3(transform.GetChild(0).transform.localScale.x * -1,
                                                                     transform.GetChild(0).transform.localScale.y,
                                                                     transform.GetChild(0).transform.localScale.z);
    }

    public void StartWalking()
    {
        myAnimator.SetBool("isWalking", true);
    }

    public void StopWalking()
    {
        myAnimator.SetBool("isWalking", false);
    }

    public void StartAnimationAttack()
    {
        myAnimator.SetBool("isAttacking", true);
    }

    public void StopAnimaitionAttack()
    {
        myAnimator.SetBool("isAttacking", false);
        myAnimator.SetBool("isAttackingEnding", true);
    }

    private IEnumerator CastBlackHole(Tile tile, Player other)
    {
        yield return StartCoroutine(turnManager.blackHole.StartRemoveBlackHole(tile, other));
        yield return null;
    }

    /* Initial Assignments */

    public void SetStartingPoint()
    {
        switch (playerID)
        {

            case 1:
                startingPoint = new Coordinate(0, mapManager.rows - 1);
                break;
            case 2:
                startingPoint = new Coordinate(mapManager.columns - 1, mapManager.rows - 1);
                break;
            case 3:
                startingPoint = new Coordinate(0, 0);
                break;
            case 4:
                startingPoint = new Coordinate(mapManager.columns - 1, 0);
                break;
            default:
                break;
        }
    }

    // Walking Path

    public bool CheckForOtherPlayerCorner(Coordinate coord)
    {
        bool isOtherPlayerCorner = false;

        Coordinate[] allCornerCoord = mapManager.GetAllCornerCoordinates();
        int idx = GeneralMethods.FindElementIdx(allCornerCoord, coord);

        if (idx != -1 && idx != playerID)
            isOtherPlayerCorner = true;

        return isOtherPlayerCorner;
    }


    /* Player position update */

    public void ResetToStartingPosition()
    {
        TeleportAtCoordinates(startingPoint);
    }

    public void TeleportAtCoordinates(Coordinate coord)
    {
        UpdateCoordinates(coord);
        Vector3 pos = coord.GetVect3();
        transform.position = pos;
    }

    public void TeleportOffScreen()
    {
        transform.position = new Vector3(-1000, 0, -5);
    }


    void Awake()
    {
        myRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        myAnimator = GetComponent<Animator>();

        GameObject mapManagerGO, turnManagerGO;

        mapManagerGO = GameObject.FindGameObjectWithTag("MapManager");
        mapManager = mapManagerGO.GetComponent<MapManager>();

        turnManagerGO = GameObject.FindGameObjectWithTag("TurnManager");
        turnManager = turnManagerGO.GetComponent<TurnManager>();

        SetStartingPoint();
        
        myBarrier = Instantiate((GameObject)Resources.Load("Barrier"));

        myBarrier.transform.SetParent(transform);
        myBarrierAnimator = myBarrier.GetComponent<Animator>();
    }

    void Update()
    {

        if (turnManager.isAcceptingInputs && !turnManager.isInPause)   
        {
            if (!moving)         // Reads XY Inputs
            {
                if (Input.GetKeyDown(KeyCode.D) || Input.GetAxis("HorizontalJoy") == 1 || Input.GetAxis("HorizontalAnalog") >= 0.9f)
                {
                    mDirection = DirectionSelection.RIGHT;
                }
                else if (Input.GetKeyDown(KeyCode.A) || Input.GetAxis("HorizontalJoy") == -1 || Input.GetAxis("HorizontalAnalog") <= -0.9f)
                {
                    mDirection = DirectionSelection.LEFT;
                }
                else if (Input.GetKeyDown(KeyCode.W) || Input.GetAxis("VerticalJoy") == 1 || Input.GetAxis("VerticalAnalog") >= 0.9f)
                {
                    mDirection = DirectionSelection.UP;
                }
                else if (Input.GetKeyDown(KeyCode.S) || Input.GetAxis("VerticalJoy") == -1 || Input.GetAxis("VerticalAnalog") <= -0.9f)
                {
                    mDirection = DirectionSelection.DOWN;
                }
                else
                {
                    mDirection = DirectionSelection.NONE;
                }
            }

            // Reads Button Inputs

            if (Input.GetKeyDown(KeyCode.Z) || Input.GetButtonDown("Fire3joy"))
            {
                mDecision = ButtonSelection.LEFT;
            }
            else if (Input.GetKeyDown(KeyCode.X) || Input.GetButtonDown("Fire1joy"))
            {
                mDecision = ButtonSelection.MIDDLE;
            }
            else if (Input.GetKeyDown(KeyCode.C) || Input.GetButtonDown("Fire2joy"))
            {
                mDecision = ButtonSelection.RIGHT;
            }
            else if (Input.GetKeyDown(KeyCode.P) || Input.GetButtonDown("StartButton"))
            {
                mDecision = ButtonSelection.PAUSE;
            }
            else if (Input.GetKeyDown(KeyCode.M) || Input.GetButtonDown("Fire4joy"))
            {
                mDecision = ButtonSelection.DIAMOND;
            }
            
        }
    }

}


