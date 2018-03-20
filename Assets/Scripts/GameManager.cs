using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

    public TurnManager turnManager;

    public FadeManager fade;

    Speaker dialogueManager;
    MapManager mapManager;
    float[][] strartingPositions;


    void Awake()
    {
        mapManager = GetComponentInChildren<MapManager>();
        dialogueManager = GetComponentInChildren<Speaker>();
    }

    void Start () {
 
        mapManager.Initialize();
        turnManager.Initialize(mapManager);

        StartCoroutine(RunGame());
    }

    IEnumerator RunGame ()
    {
        dialogueManager.PlayBegin();
        float wait = (dialogueManager.getClipDuration()) + 0.7f;

        yield return new WaitForSeconds(wait);

        turnManager.SetGameActive(true);

        StartCoroutine(fade.FadeInUI());

        while (turnManager.gameActive)
        {
            yield return(turnManager.PassTurn());

            yield return StartCoroutine(turnManager.Turn());

            if (!turnManager.gameActive)
                continue;
        }

        // TODO controllare probabilente questo va incapsulato in un altro while che aspetta il fire
        StartCoroutine(fade.FadeOutUI("_Scenes/CubeScene"));

        yield return null;
    }
}
