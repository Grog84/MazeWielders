using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiamondStatus
{
    public Coordinate coordinates;
    public Coordinate startingCoordinates;

    public bool stasisActive = false;
    public int diamondOwner = -1;

    public int turnsBeforeStasisCounter = 3;
    public int turnsBeforeStasisIsActive = 3;

    public GameObject diamondFX;
    public GameObject diamond;
    public GameObject stasisEffect;

    [HideInInspector] public float stasisAnimationDuration = 1.0f;

    Vector3 parkingPosition;
    
    public void Initialize(Coordinate startingCoordinates, GameObject diamond, GameObject diamondFX, GameObject stasisEffect)
    {
        this.startingCoordinates = startingCoordinates;

        coordinates = startingCoordinates;

        parkingPosition = new Vector3(-1000f, 0, 0);

        this.diamond        =   diamond;
        this.diamondFX      =   diamondFX;
        this.stasisEffect   =   stasisEffect;

        stasisEffect.transform.position = parkingPosition;
        diamondFX.transform.position = parkingPosition;

    }

    // Assignment of the Diamond Aura

    public void AssignDiamondAura(Player player)
    {
        diamondFX.transform.SetParent(player.GetComponentInParent<Transform>());
        diamondFX.transform.localPosition = new Vector3(0f, 0f, -0.1f);
    }

    public void RemoveDiamondAura()
    {
        diamondFX.transform.parent = null;
        diamondFX.transform.position = parkingPosition;
    }

    // Updates of the diamond position

    public void UpdateDiamondPosition(Coordinate coords)
    {
        coordinates = coords;
    }

    public void UpdateDiamondPosition(Player player)
    {
        if (player.playerID == diamondOwner)
        {
            coordinates = player.coordinate;
        }
    }

    public void UpdateDiamondPosition(Tile tile)
    {
        coordinates = tile.GetCoordinatesCopy();
    }

    public void UpdateDiamondPosition()
    {
        Tile tile = diamond.GetComponentInParent<Tile>();
        coordinates = tile.GetCoordinatesCopy();
    }

    // Diamond Collection

    private bool ChecksForDiamond(Player player)
    {
        if (player.coordinate.IsEqual(coordinates) && diamondOwner == -1)
            return true;
        else
            return false;
    }

    public void CollectDiamond(Player player)
    {
        diamond.transform.position = parkingPosition;
        diamondOwner = player.playerID;

        AssignDiamondAura(player);
        UpdateDiamondPosition(player);

        // maks the dimond usable when collected
        turnsBeforeStasisCounter = 0;
    }

    // Diamond repositioning

    public void MoveDiamondAtCoords(Coordinate coords)
    {
        Vector3 pos = coords.GetVect3();
        pos.z = -10;
        diamond.transform.position = pos;
    }

    public void DropDiamond(Player player)
    {
        RemoveDiamondAura();
        diamondOwner = -1;
        diamond.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10);
        coordinates = player.coordinate;

        ResetTurnsBeforeStasis();
    }

    public void ResetDiamondToStartingPosition()
    {
        MoveDiamondAtCoords(startingCoordinates);
        coordinates = startingCoordinates;
    }

    public void ActivateDiamondStasis(Player player)
    {
        stasisEffect.GetComponent<Animator>().SetTrigger("Restart");
        stasisActive = true;
        Vector3 pos = player.transform.position;
        pos.z -= 0.1f;
        stasisEffect.transform.position = pos;
    }

    public void RemoveStasisEffect()
    {
        stasisEffect.GetComponent<Animator>().SetTrigger("Restart");
        stasisEffect.GetComponent<Animator>().SetTrigger("Finish");
        stasisActive = false;
        turnsBeforeStasisCounter = turnsBeforeStasisIsActive;
        
    }

    public void ParkStasis()
    {
        stasisEffect.transform.position = parkingPosition;
    }

    // Player Diamond

    public void ResetTurnsBeforeStasis()
    {
        turnsBeforeStasisCounter = turnsBeforeStasisIsActive;
    }

    public bool GetStasisStatus()
    {
        return stasisActive;
    }

    //public void ActivateStasis()
    //{
    //    UnchildFromTile();
    //    turnsBeforeStasisCounter = turnsBeforeStasisIsActive;
    //    canActivateStasis = false;
    //    isStasisActive = true;
    //    InstantiateStasisEffect();
    //}

    //public void DeactivateStasis()
    //{
    //    //turnsBeforeStasisCounter = turnsBeforeStasisIsActive;
    //    coordinates.GetCoordsFromPosition(transform.position, mapManagerGO.GetComponent<MapManager>().columns, mapManagerGO.GetComponent<MapManager>().rows);

    //    GameObject myTile = mapManager.PickTileObject(coordinates);
    //    transform.SetParent(myTile.transform);

    //    canActivateStasis = false;
    //    isStasisActive = false;
    //    StartCoroutine(DestroyStasisEffect());
    //}


}
