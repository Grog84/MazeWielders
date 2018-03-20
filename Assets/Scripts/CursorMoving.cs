using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorMoving : MonoBehaviour {

    bool moving;
    Coordinate coordinate;
    public MapManager mapManager;

    void Start()
    {
        coordinate = new Coordinate(0, 0);
    }

    // Activation/Deactivation

    public void CursorActivate (Coordinate pcoord)
    {
        int x = Mathf.Clamp(pcoord.GetX(), 0, mapManager.columns-2);
        int y = Mathf.Clamp(pcoord.GetY(), 1, mapManager.rows-1);
        coordinate.SetCoordinate(x, y);

        Vector3 destination = mapManager.myMap[x,y].transform.position;
        destination.z = -2;
        transform.position = destination;   
    }

    // Movement Utilities

    public void MoveAtPosition(Vector3 position)
    {
        transform.position = position;
    }

    public Coordinate[] GetSelectedCoords()
    {
        var selectedCoords = new Coordinate[4];
        selectedCoords[0] = new Coordinate(coordinate.GetX(), coordinate.GetY());
        selectedCoords[1] = new Coordinate(coordinate.GetX(), coordinate.GetY() - 1);
        selectedCoords[2] = new Coordinate(coordinate.GetX()+1, coordinate.GetY()-1);
        selectedCoords[3] = new Coordinate(coordinate.GetX()+1, coordinate.GetY());

        return selectedCoords;
    }

    // Movement Coroutines

    public IEnumerator Move(DirectionSelection direction)
    {
        moving = true;

        float elapsedTime = 0;
        float animTime = 0.2f;

        int newX = -1;
        int newY = -1;

        if (direction == DirectionSelection.UP)
        {
            newX = Mathf.Clamp(coordinate.GetX(), 0, mapManager.columns - 2);
            newY = Mathf.Clamp(coordinate.GetY() + 1, 1, mapManager.rows - 1);
        }
        else if (direction == DirectionSelection.RIGHT)
        {
            newX = Mathf.Clamp(coordinate.GetX() + 1, 0, mapManager.columns - 2);
            newY = Mathf.Clamp(coordinate.GetY(), 1, mapManager.rows - 1);
        }
        else if (direction == DirectionSelection.DOWN)
        {
            newX = Mathf.Clamp(coordinate.GetX(), 0, mapManager.columns - 2);
            newY = Mathf.Clamp(coordinate.GetY() - 1, 1, mapManager.rows - 1);
        }
        else if (direction == DirectionSelection.LEFT)
        {
            newX = Mathf.Clamp(coordinate.GetX() - 1, 0, mapManager.columns - 2);
            newY = Mathf.Clamp(coordinate.GetY(), 1, mapManager.rows - 1);
        }

        GameObject destinationTile = mapManager.myMap[newX, newY];
        Vector3 destination = destinationTile.transform.position;
        destination.z--;

        while (elapsedTime < animTime)
        {
            transform.position = Vector3.Lerp(transform.position, destination, elapsedTime / animTime);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        coordinate.SetCoordinate(newX, newY);
        moving = false;
    }

}
