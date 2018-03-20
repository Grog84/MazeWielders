using System;
using UnityEngine;

[Serializable]
public class Coordinate
{
    public int x, y;

    float tileSize = 10.0f;

    public Coordinate(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public int GetX()
    {
        return x;
    }

    public int GetY()
    {
        return y;
    }

    public Coordinate GetCopy()
    {
        return new Coordinate(x, y);
    }

    public void SetCoordinate (int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public bool IsEqual(int x, int y)
    {
        if (this.x == x && this.y == y)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool IsEqual(Coordinate other)
    {
        if ((other.GetX() == x) && (other.GetY() == y))
            return true;
        else
            return false;           
    }

    public int FindInGroup(Coordinate[] collection)
    {
        for (int i = 0; i < collection.Length; i++)
        {
            if (IsEqual(collection[i]))
                return i;
        }
        return -1;
    }

    public Vector3 GetVect3()
    {
        return new Vector3((float)x, (float)y, 0f) * tileSize;
    }

    public Vector3 GetVect3WithZ()
    {
        return new Vector3((float)x, (float)y, 0.0001f * (float)y) * tileSize;
    }

    public Vector3 GetPositionFromCoords(int columns, int rows)
    {
        var relativePosition = GetVect3();
        float xShift = -(columns-1) * tileSize / 2f;
        float yShift = -(rows-1) * tileSize / 2f;

        var position = new Vector3(relativePosition.x + xShift, relativePosition.y + yShift, 0f);
        return position;
    }

    public void GetCoordsFromPosition(Vector3 position, int columns, int rows)
    {
        float[] x_bin = GeneralMethods.CreateBins(tileSize, -columns * tileSize / 2f, columns + 1);
        float[] y_bin = GeneralMethods.CreateBins(tileSize, -rows * tileSize / 2f, rows + 1);

        x = GeneralMethods.FindValInBins(x_bin, position.x);
        y = GeneralMethods.FindValInBins(y_bin, position.y);
    }

    override public string ToString()
    {
        return x.ToString() + " " + y.ToString();
    }
}