using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InsertArrow : MonoBehaviour {

    Coordinate[] pointedTilesCoord;
    Animator mAnimator;
    SpriteRenderer mRenderer;

    void Awake()
    {
        mAnimator = GetComponent<Animator>();
        mRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        mRenderer.color = Color.clear;
    }

    public Coordinate[] GetPointedCoords()
    {
        return pointedTilesCoord;
    }

    int[] GetRange(int a, int b) // includes b
    {
        int[] rangeArray;

        if (a > b)
        {
            int diff = a - b + 1;
            rangeArray = new int[diff];

            for (int i = 0; i < diff; i++)
            {
                rangeArray[i] = a - i;
            }
        }
        else if (b > a)
        {
            int diff = b - a + 1;
            rangeArray = new int[diff];

            for (int i = 0; i < diff; i++)
            {
                rangeArray[i] = a + i;
            }

        }
        else
        {
            rangeArray = new int[1];
            rangeArray[0] = a;
        }

        return rangeArray;
    }

    public void SetPointedCoords(int xInit, int xFinal, int yInit, int yFinal)
    {
        int[] xComponents = new int[0], yComponents=new int[0];
        int coordArrayLength = Mathf.Max(Mathf.Abs(xFinal - xInit), Mathf.Abs(yFinal - yInit)) + 1;
        pointedTilesCoord = new Coordinate[coordArrayLength];

        if (xInit != xFinal) // horizontal arrow
        {
            xComponents = GetRange(xInit, xFinal);
            yComponents = Enumerable.Repeat(yInit, coordArrayLength).ToArray();    
        }

        if (yInit != yFinal) // vertical arrow
        {
            yComponents = GetRange(yInit, yFinal);
            xComponents = Enumerable.Repeat(xInit, coordArrayLength).ToArray();
        }

        for (int i = 0; i < coordArrayLength; i++)
        {
            pointedTilesCoord[i] = new Coordinate(xComponents[i], yComponents[i]);
        }
    }

    public void SetVisible(bool status)
    {
        if (status)
            mRenderer.color = Color.white;
        else
            mRenderer.color = Color.clear;

    }

    public void SetAnimatorActive(bool status)
    {
        if (status)
        {
            SetVisible(true);
        }
        else
        {
            SetVisible(false);
        }

        mAnimator.SetBool("isActive", status);
    }
}
