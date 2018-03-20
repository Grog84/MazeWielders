using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardButton : MonoBehaviour
{
    private Image myImage;
    public TileTypes tileType;
    public int rotation;
    private Sprite mySprite;
    private int[] curveTypesInClockwiseRotation = new int[4] { 0 , 1, 3, 2 };
    private bool isTrapped;

    public void SetTileType(TileTypes type, bool isTrapped = false)
    {
        Texture2D myTexture = null;
        tileType = type;
        this.isTrapped = isTrapped;
        rotation = 0;

        switch (tileType)
        {
            case TileTypes.Curve_BR:
                myTexture = (Texture2D)Resources.Load("TileProva/curva");
                break;
            case TileTypes.Curve_LB:
                myTexture = (Texture2D)Resources.Load("TileProva/curva2");
                break;
            case TileTypes.Curve_RT:
                myTexture = (Texture2D)Resources.Load("TileProva/curva3");
                break;
            case TileTypes.Curve_TL:
                myTexture = (Texture2D)Resources.Load("TileProva/curva4");
                break;
            case TileTypes.Straight_V:
                myTexture = (Texture2D)Resources.Load("TileProva/Straight");
                break;
            case TileTypes.Straight_H:
                myTexture = (Texture2D)Resources.Load("TileProva/Straight2");
                break;
            case TileTypes.T_B:
                myTexture = (Texture2D)Resources.Load("TileProva/t");
                break;
            case TileTypes.T_L:
                myTexture = (Texture2D)Resources.Load("TileProva/t2");
                break;
            case TileTypes.T_T:
                myTexture = (Texture2D)Resources.Load("TileProva/t3");
                break;
            case TileTypes.T_R:
                myTexture = (Texture2D)Resources.Load("TileProva/t4");
                break;
            case TileTypes.Cross:
                myTexture = (Texture2D)Resources.Load("TileProva/cross");
                break;
            default:
                break;
        }

        mySprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f));
        myImage.sprite = mySprite;
    }

    public void RotateTile(int rotation)  // 1 is clockwise, -1 is counterclockwise
    {
        myImage.transform.Rotate(Vector3.forward * rotation * 90);
        //myImage.transform.Rotate(Vector3.forward * rotation * 90);
        this.rotation -= rotation;
    }

    public TileTypes GetTileType()
    {
        TileTypes myType = TileTypes.NONE;

        if (rotation % 4 == 0) // no rotation
        {
            myType = tileType;
        }

        else
        {
            if ((int)tileType <= 3) // curve
            {
                int myRotationValue = rotation % 4;
                int tileIDX = GeneralMethods.FindElementIdx(curveTypesInClockwiseRotation, (int)tileType);
                if (myRotationValue > 0)
                {
                    tileIDX = (tileIDX + myRotationValue) % 4;
                }
                else
                {
                    tileIDX = (tileIDX + (4 + myRotationValue) ) % 4;
                }
                myType = (TileTypes)curveTypesInClockwiseRotation[tileIDX];

            }
            else if (tileType == TileTypes.Straight_H || tileType == TileTypes.Straight_V)
            {
                int myRotationValue = rotation % 2;

                if(myRotationValue == 1)
                {
                    if (tileType == TileTypes.Straight_H)
                        myType = TileTypes.Straight_V;
                    else if (tileType == TileTypes.Straight_V)
                        myType = TileTypes.Straight_H;
                }

            }
            else if (tileType >= TileTypes.T_B && tileType <= TileTypes.T_R)
            {
                int myRotationValue = rotation % 4;

                if (myRotationValue < 0)
                {
                    myRotationValue = (4 + myRotationValue);
                }

                myType = (TileTypes)(6 + (((int)tileType-6) + myRotationValue) % 4);
            }
            else
                myType = tileType;
        }

        tileType = myType;
        rotation = 0;
        return myType;
    }

    public void ResetCardRotation()
    {
        myImage.transform.rotation = new Quaternion(0, 0 ,0, 0);
    }

    public bool GetTrappedStatus()
    {
        return isTrapped;
    }

    // Use this for initialization
    void Start()
    {
        myImage = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
