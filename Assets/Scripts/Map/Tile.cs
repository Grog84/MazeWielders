using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Tile : MonoBehaviour {

    // ID corresponds to the player ID
    // If the tile is not occupied the value is -1
    public int occupiedID = -1;
    public Coordinate myCoord;


    public TileTypes type;

    public Sprite mySprite;
    public SpriteRenderer myRenderer;
    public Texture2D myTexture;
    public GameObject trap, blackHole;

    public bool canBeMoved = true;

    // Connections
    // [0] -> Top       (T)
    // [1] -> Right     (R)
    // [2] -> Bottom    (B)
    // [3] -> Left      (L)

    public bool[] possibleConnections, effectiveConnections;

    private BoxCollider2D myCollider;
    public GameObject myTrap;
    public Trap myTrapComponent;
    public bool isTrapped, hasDiamond;

    private Animator myAnimator;

    // Animation

    public void SetSprite(TileTypes type)
    {
        switch (type)
        {
            case TileTypes.Curve_BR:
                myTexture = (Texture2D)Resources.Load("Tiles/curva");
                myAnimator.runtimeAnimatorController = (RuntimeAnimatorController)Resources.Load("TilesAnimators/curva");
                this.type = type;
                break;
            case TileTypes.Curve_LB:
                myTexture = (Texture2D)Resources.Load("Tiles/curva2");
                myAnimator.runtimeAnimatorController = (RuntimeAnimatorController)Resources.Load("TilesAnimators/curva2");
                this.type = type;
                break;
            case TileTypes.Curve_RT:
                myTexture = (Texture2D)Resources.Load("Tiles/curva3");
                myAnimator.runtimeAnimatorController = (RuntimeAnimatorController)Resources.Load("TilesAnimators/curva3");
                this.type = type;
                break;
            case TileTypes.Curve_TL:
                myTexture = (Texture2D)Resources.Load("Tiles/curva4");
                myAnimator.runtimeAnimatorController = (RuntimeAnimatorController)Resources.Load("TilesAnimators/curva4");
                this.type = type;
                break;
            case TileTypes.Straight_V:
                myTexture = (Texture2D)Resources.Load("Tiles/Straight");
                myAnimator.runtimeAnimatorController = (RuntimeAnimatorController)Resources.Load("TilesAnimators/Straight");
                this.type = type;
                break;
            case TileTypes.Straight_H:
                myTexture = (Texture2D)Resources.Load("Tiles/Straight2");
                myAnimator.runtimeAnimatorController = (RuntimeAnimatorController)Resources.Load("TilesAnimators/Straight2");
                this.type = type;
                break;
            case TileTypes.T_B:
                myTexture = (Texture2D)Resources.Load("Tiles/t");
                myAnimator.runtimeAnimatorController = (RuntimeAnimatorController)Resources.Load("TilesAnimators/t");
                this.type = type;
                break;
            case TileTypes.T_L:
                myTexture = (Texture2D)Resources.Load("Tiles/t2");
                myAnimator.runtimeAnimatorController = (RuntimeAnimatorController)Resources.Load("TilesAnimators/t2");
                this.type = type;
                break;
            case TileTypes.T_T:
                myTexture = (Texture2D)Resources.Load("Tiles/t3");
                myAnimator.runtimeAnimatorController = (RuntimeAnimatorController)Resources.Load("TilesAnimators/t3");
                this.type = type;
                break;
            case TileTypes.T_R:
                myTexture = (Texture2D)Resources.Load("Tiles/t4");
                myAnimator.runtimeAnimatorController = (RuntimeAnimatorController)Resources.Load("TilesAnimators/t4");
                this.type = type;
                break;
            case TileTypes.Cross:
                myTexture = (Texture2D)Resources.Load("Tiles/cross");
                myAnimator.runtimeAnimatorController = (RuntimeAnimatorController)Resources.Load("TilesAnimators/cross");
                this.type = type;
                break;
            case TileTypes.Curve_BR_alt:
                myTexture = (Texture2D)Resources.Load("Tiles/curva_alt");
                this.type = TileTypes.Curve_BR;
                break;
            case TileTypes.Curve_LB_alt:
                myTexture = (Texture2D)Resources.Load("Tiles/curva2_alt");
                this.type = TileTypes.Curve_LB;
                break;
            case TileTypes.Curve_RT_alt:
                myTexture = (Texture2D)Resources.Load("Tiles/curva3_alt");
                this.type = TileTypes.Curve_RT;
                break;
            case TileTypes.Curve_TL_alt:
                myTexture = (Texture2D)Resources.Load("Tiles/curva4_alt");
                this.type = TileTypes.Curve_TL;
                break;
            case TileTypes.T_B_alt:
                myTexture = (Texture2D)Resources.Load("Tiles/t_alt");
                this.type = TileTypes.T_B;
                break;
            case TileTypes.T_L_alt:
                myTexture = (Texture2D)Resources.Load("Tiles/t2_alt");
                this.type = TileTypes.T_L;
                break;
            case TileTypes.T_T_alt:
                myTexture = (Texture2D)Resources.Load("Tiles/t3_alt");
                this.type = TileTypes.T_T;
                break;
            case TileTypes.T_R_alt:
                myTexture = (Texture2D)Resources.Load("Tiles/t4_alt");
                this.type = TileTypes.T_R;
                break;
            case TileTypes.Goal:
                myTexture = (Texture2D)Resources.Load("Tiles/goal");
                myAnimator.runtimeAnimatorController = (RuntimeAnimatorController)Resources.Load("TilesAnimators/goal");

                this.type = TileTypes.Cross;
                break;
            default:
                break;
        }

        mySprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.66f));
        myRenderer.sprite = mySprite;
        myCollider.size = new Vector2(myTexture.width / 100f, myTexture.height / 100f);
    }

    public void SetSelected(bool status)
    {
        myAnimator.SetBool("isSelected", status);
    }

    

    // Tiles connectivity

    public void SetPossibleConnections(TileTypes type)
    {
        switch (type)
        {
            case TileTypes.Curve_BR:
            case TileTypes.Curve_BR_alt:
                possibleConnections[0] = false;
                possibleConnections[1] = true;
                possibleConnections[2] = true;
                possibleConnections[3] = false;
                break;
            case TileTypes.Curve_LB:
            case TileTypes.Curve_LB_alt:
                possibleConnections[0] = false;
                possibleConnections[1] = false;
                possibleConnections[2] = true;
                possibleConnections[3] = true;
                break;
            case TileTypes.Curve_RT:
            case TileTypes.Curve_RT_alt:
                possibleConnections[0] = true;
                possibleConnections[1] = true;
                possibleConnections[2] = false;
                possibleConnections[3] = false;
                break;
            case TileTypes.Curve_TL:
            case TileTypes.Curve_TL_alt:
                possibleConnections[0] = true;
                possibleConnections[1] = false;
                possibleConnections[2] = false;
                possibleConnections[3] = true;
                break;
            case TileTypes.Straight_V:
                possibleConnections[0] = true;
                possibleConnections[1] = false;
                possibleConnections[2] = true;
                possibleConnections[3] = false;
                break;
            case TileTypes.Straight_H:
                possibleConnections[0] = false;
                possibleConnections[1] = true;
                possibleConnections[2] = false;
                possibleConnections[3] = true;
                break;
            case TileTypes.T_B:
            case TileTypes.T_B_alt:
                possibleConnections[0] = false;
                possibleConnections[1] = true;
                possibleConnections[2] = true;
                possibleConnections[3] = true;
                break;
            case TileTypes.T_L:
            case TileTypes.T_L_alt:
                possibleConnections[0] = true;
                possibleConnections[1] = false;
                possibleConnections[2] = true;
                possibleConnections[3] = true;
                break;
            case TileTypes.T_T:
            case TileTypes.T_T_alt:
                possibleConnections[0] = true;
                possibleConnections[1] = true;
                possibleConnections[2] = false;
                possibleConnections[3] = true;
                break;
            case TileTypes.T_R:
            case TileTypes.T_R_alt:
                possibleConnections[0] = true;
                possibleConnections[1] = true;
                possibleConnections[2] = true;
                possibleConnections[3] = false;
                break;
            case TileTypes.Cross:
            case TileTypes.Goal:
                possibleConnections[0] = true;
                possibleConnections[1] = true;
                possibleConnections[2] = true;
                possibleConnections[3] = true;
                break;
                       
            default:
                break;
        }
    }

    public void CheckConnections(Tile other, int lato, int playerPlayingNbr = -1)
    {
        if (other != null)
        {

            if (other.occupiedID == -1 || other.occupiedID == playerPlayingNbr)
            {
                switch (lato)
                {
                    case 0:
                        if (possibleConnections[lato] && other.possibleConnections[2])
                        {
                            effectiveConnections[lato] = true;
                        }
                        break;
                    case 1:
                        if (possibleConnections[lato] && other.possibleConnections[3])
                        {
                            effectiveConnections[lato] = true;
                        }
                        break;
                    case 2:
                        if (possibleConnections[lato] && other.possibleConnections[0])
                        {
                            effectiveConnections[lato] = true;
                        }
                        break;
                    case 3:
                        if (possibleConnections[lato] && other.possibleConnections[1])
                        {
                            effectiveConnections[lato] = true;
                        }
                        break;
                    default:
                        break;

                }
            }
        }
        
    }

    // Position update

    public IEnumerator MoveToPosition(Vector2 movement, float animTime)
    {
        // Might there be a child unchild issue?

        //float elapsedTime = 0;

        Vector3 destination = new Vector3(transform.position.x + movement[0], transform.position.y + movement[1], transform.position.z);
        transform.DOMove(destination, animTime);

        //while (elapsedTime < animTime)
        //{
        //    transform.position = Vector3.Lerp(transform.position, destination, elapsedTime / animTime);
        //    elapsedTime += Time.deltaTime;
        //    yield return null;
        //}

        yield return null;
    }

    public void SetCoordinates(int x, int y)
    {
        myCoord = new Coordinate(x, y);
    }

    public Coordinate GetCoordinates()
    {
        return myCoord;
    }

    public Coordinate GetCoordinatesCopy()
    {
        return new Coordinate(myCoord.GetX(), myCoord.GetY());
    }

    public void ResetEffectiveConnectionMap()
    {
        effectiveConnections = new bool[4] { false, false, false, false };
    }

    public void UpdateZOrder()
    {
        Vector3 position = new Vector3(transform.position.x, transform.position.y, 0.0001f * transform.position.y);
        transform.position = position;
    }

    //public void SetPlayerPosition()
    //{
    //    playerPosition = transform.position + new Vector3(0f, playerOffset, 0f);
    //}

    // Trap

    public bool GetIsTrapped()
    {
        return isTrapped;
    }

    public void SetIsTrapped(bool status)
    {
        isTrapped = status;
    }

    public void SetTrap(int playerNbr)
    {
        myTrap = Instantiate(trap, transform);
        myTrapComponent = myTrap.GetComponent<Trap>();
        myTrapComponent.SetPlayerDropping(playerNbr);
        myTrapComponent.SetCoordiantes(myCoord);
        isTrapped = true;

    }

    public Trap GetTrap()
    {
        return myTrapComponent;
    }

    // Unity Specific methods

    void Awake () {
        myRenderer = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<BoxCollider2D>();
        myAnimator = GetComponent<Animator>();
        possibleConnections = new bool[4];
        effectiveConnections = new bool[4];
        occupiedID = -1;
        hasDiamond = false;
    }

}
