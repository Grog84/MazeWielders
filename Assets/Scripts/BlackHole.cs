using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackHole : MonoBehaviour {

    public TurnManager turnManager;
    public MapManager mapManager;

    Vector3 parkingPosition = new Vector3(-1000f, 0, 0);
    Animator mAnimator;

    Tile currentTile        = null;
    Player removePlayer     = null;
    Player respawnPlayer    = null;

    bool animationPlaying = false;

    private void Awake()
    {
        mAnimator = GetComponent<Animator>();
        Park();
    }

    public void ClearTile()
    {
        if (removePlayer != null)
        {
            currentTile.occupiedID = -1;
            removePlayer.transform.parent = null;
            removePlayer.transform.position = parkingPosition;
            removePlayer.coordinate = new Coordinate( -1, -1 );

            currentTile     = null;
            removePlayer    = null;

            animationPlaying = false;
           
        }
    }

    public IEnumerator StartRemoveBlackHole(Tile tile, Player player)
    {
        Vector3 position = mapManager.GetVector3FromCoords(tile.myCoord);
        position.z = -3f;
        transform.position = position;
        mAnimator.speed = 1;

        currentTile = tile;
        removePlayer = player;

        animationPlaying = true;

        while (animationPlaying)
        {
            yield return null;
        }

        EndAnimation();
    }

    public void EndAnimation()
    {
        mAnimator.SetBool("Loop", false);
        mAnimator.speed = 1;
    }

    public void RespawnPlayer()
    {
        if (respawnPlayer != null)
        {
            respawnPlayer.coordinate = currentTile.myCoord;

            Vector3 pos = mapManager.GetVector3FromCoords(currentTile.myCoord);
            pos.z = -1f;
            respawnPlayer.transform.position = pos;

            currentTile.occupiedID = respawnPlayer.playerID;

            currentTile     = null;
            respawnPlayer   = null;

            animationPlaying = false;
        }

    }

    public IEnumerator BlackHoleRespawn(Tile tile, Player player)
    {
        Vector3 position = mapManager.GetVector3FromCoords(tile.myCoord);
        position.z = -3f;
        transform.position = position;
        mAnimator.speed = 1;

        currentTile = tile;
        respawnPlayer = player;

        animationPlaying = true;

        while (animationPlaying)
        {
            yield return null;
        }

        EndAnimation();
    }

    public void Park()
    {
        mAnimator.SetBool("Loop", true);
        mAnimator.speed = 0;

        transform.position = parkingPosition;
    }
    
}
