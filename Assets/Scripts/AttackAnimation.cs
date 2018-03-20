using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackAnimation : MonoBehaviour {

    HumanPlayer myPlayer;

    public void SetAttackActivated(int val)
    {
        myPlayer.SetAttackActivated(val);
    }

    void Start ()
    {
        myPlayer = transform.parent.gameObject.GetComponent<HumanPlayer>();
	}
	
}
