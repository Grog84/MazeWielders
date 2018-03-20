using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Player : MonoBehaviour
{
    /*[HideInInspector]*/ public int playerID;
    /*[HideInInspector]*/ public Coordinate coordinate;
    

    public abstract ButtonSelection Decision();

    public abstract DirectionSelection InputDirection();

    public abstract IEnumerator Move();

    public abstract IEnumerator SelectTileInsertion(SelectionArrowGroup arrows);

    public abstract IEnumerator SelectRotation(CursorMoving rotationCursor);

    public abstract IEnumerator UseCrystal();

    public abstract IEnumerator AttackPlayerOnSlide( Player other );

    public abstract IEnumerator AttackPlayer(Tile tile, bool isStasisActive = false);

    public abstract IEnumerator ActivateBarrier(bool state);

    public abstract void UpdateCoordinates(Coordinate newCoordinates);

    public abstract void SetDescription(PlayerDescription description);

}
