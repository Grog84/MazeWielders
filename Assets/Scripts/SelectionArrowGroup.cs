using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionArrowGroup
{
    int currentSelection;

    GameObject[] allArrowsGO;
    InsertArrow[] arrows;

    int mapRows = 0;
    int mapColumns = 0;

    int startingIndex = 0;
    int[,] rangesIdx = new int[2, 4];

    Edges currentRange;
    SlideDirection currentSlideDirection;

    //
    // Initialization methods
    //

    public void Initialize(GameObject[] inputArrows, int rows, int cols)
    {
        allArrowsGO = inputArrows;
        arrows = new InsertArrow[allArrowsGO.Length];

        for (int i = 0; i < allArrowsGO.Length; i++)
        {
            arrows[i] = inputArrows[i].GetComponent<InsertArrow>();
        }

        mapRows = rows;
        mapColumns = cols;
        DefineStartingIndex();
        DefineRanges();

    }

    // Defines the index of the top left arrow on the map
    void DefineStartingIndex()
    {
        startingIndex = 2* mapColumns + mapRows;
    }

    // Defines the index ranges corresponding to the top, bottom, left or right edge of the board
    // The first index refers to the min and max value
    // The second index refers to the edge
    void DefineRanges()
    {
        rangesIdx[0, (int)Edges.BOTTOM] = 0;
        rangesIdx[1, (int)Edges.BOTTOM] = mapColumns - 1;

        rangesIdx[0, (int)Edges.RIGHT] = mapColumns;
        rangesIdx[1, (int)Edges.RIGHT] = mapColumns + mapRows - 1;

        rangesIdx[0, (int)Edges.TOP] = mapColumns + mapRows;
        rangesIdx[1, (int)Edges.TOP] = 2 * mapColumns + mapRows - 1;

        rangesIdx[0, (int)Edges.LEFT] = 2 * mapColumns + mapRows;
        rangesIdx[1, (int)Edges.LEFT] = 2 * mapColumns + 2 * mapRows - 1;
    }

    public void SetCurrentSelection(int val)
    {
        currentSelection = val;
    }

    public SlideDirection GetCurrentSlideDirection()
    {
        return currentSlideDirection;
    }

    public InsertArrow GetArrow()
    {
        return arrows[currentSelection];
    }

    public InsertArrow GetArrow(int idx)
    {
        return arrows[idx];
    }

    // Selection

    // Selects the top left arrow
    public void SelectFirst()
    {
        currentSelection = startingIndex;
        UpdateRange();
    }

    // Finds the edge of the currently selected arrow
    void UpdateRange()
    {
        for (int i = 0; i < 4; i++)
        {
            if (currentSelection >= rangesIdx[0, i] && currentSelection <= rangesIdx[1, i])
            {
                currentRange = (Edges)i;
                break;
            }
        }

        currentSlideDirection = (SlideDirection)currentSelection;
    }

    public void MoveSelection(DirectionSelection direction)
    {
        switch (direction)
        {
            case DirectionSelection.LEFT:
                Left();
                break;
            case DirectionSelection.RIGHT:
                Right();
                break;
            case DirectionSelection.DOWN:
                Down();
                break;
            case DirectionSelection.UP:
                Up();
                break;
            case DirectionSelection.NONE:
                break;
            default:
                break;
        }
    }

    // Moves the selection after a right movement input
    void Right()
    {
        if (currentRange == Edges.BOTTOM)
        {    
            currentSelection++;

        }
        else if (currentRange == Edges.TOP)
        {
            currentSelection--;
        }

        UpdateRange();

    }

    // Moves the selection after a left movement input
    void Left()
    {
        if (currentRange == Edges.BOTTOM)
        {
            currentSelection--;
            if (currentSelection < 0)
            {
                currentSelection = 2 * mapColumns + 2 * mapRows - 1;
            }

        }
        else if (currentRange == Edges.TOP)
        {
            currentSelection++;
        }

        UpdateRange();
    }

    // Moves the selection after a up movement input
    void Up()
    {
        if (currentRange == Edges.RIGHT)
        {
            currentSelection++;
        }
        else if (currentRange == Edges.LEFT)
        {
            currentSelection--;
        }

        UpdateRange();
    }

    // Moves the selection after a down movement input
    void Down()
    {
        if (currentRange == Edges.RIGHT)
        {
            currentSelection--;
        }
        else if (currentRange == Edges.LEFT)
        {
            currentSelection++;
            currentSelection %= 2 * mapColumns + 2 * mapRows;
        }

        UpdateRange();
    }

    // Arrows animation and visibility

    public void SetVisible(bool status, int idx)
    {
        arrows[idx].SetVisible(status);
    }

    public void SetVisible(bool status)
    {
        arrows[currentSelection].SetVisible(status);
    }

    public void SetAllVisible(bool status)
    {
        foreach (InsertArrow ar in arrows)
        {
            ar.SetVisible(status);
        }
    }

    public void SetAnimatorActive(bool status, int idx)
    {
        arrows[idx].SetAnimatorActive(status);
    }

    public void SetAnimatorActive(bool status)
    {
        arrows[currentSelection].SetAnimatorActive(status);
    }
}
