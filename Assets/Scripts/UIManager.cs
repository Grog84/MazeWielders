using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    // UI Buttons
    public GameObject xButton, aButton, bButton, diamondButton;
    public GameObject passButton;

    public GameObject pauseMenu;

    public Animator[] portraits;

    public GameObject winScreen;

    Animator activePortrait = null;
    Animator[] buttonsAnimator;

    private void Awake()
    {
        // Assign buttons animators
        buttonsAnimator = new Animator[5];

        buttonsAnimator[0] = xButton.GetComponent<Animator>();
        buttonsAnimator[1] = aButton.GetComponent<Animator>();
        buttonsAnimator[2] = bButton.GetComponent<Animator>();
        buttonsAnimator[3] = diamondButton.GetComponent<Animator>();
        buttonsAnimator[4] = passButton.GetComponent<Animator>();
    }

    // Buttons animations

    public void ResetButtons(DiamondStatus diamond, Player activePlayer)
    {
        buttonsAnimator[0].SetBool("canWalk", true);
        buttonsAnimator[1].SetBool("canTerraform", true);

        StartCoroutine(ActivatePanel(PanelSelection.BASE));

        if (activePlayer.playerID == diamond.diamondOwner)
        {
            Debug.Log("Reset UI with diamond");
            StartCoroutine(SetDiamondAnimation(diamond.turnsBeforeStasisCounter));        
            buttonsAnimator[3].SetBool("isActive", false);
        }
        else
        {
            StartCoroutine(SetDiamondAnimation(3));
            buttonsAnimator[3].SetBool("isActive", false);
        }

    }

    public IEnumerator SetDiamondAnimation(int anim)
    {
        if(anim <= 4)
            buttonsAnimator[3].SetFloat("ActiveStatus", anim);
        else if (anim == 5)
            buttonsAnimator[3].SetBool("isActive", true);
        yield return null;

    }

    public void SetWalkButton(bool status)
    {
        buttonsAnimator[0].SetBool("canWalk", status);
    }

    public void SetTerraformButton(bool status)
    {
        buttonsAnimator[1].SetBool("canTerraform", status);
    }

    public void SetPauseButton(int playerID)
    {
        buttonsAnimator[4].SetInteger("Player", playerID + 1);
    }

    public void SwitchActivePortrait(int playerID)
    {
        if (activePortrait != null)
            activePortrait.GetComponent<Animator>().SetBool("isActive", false);

        activePortrait = portraits[playerID];
        activePortrait.GetComponent<Animator>().SetBool("isActive", true);
    }

    public Card GetActiveCards()
    {
        return activePortrait.GetComponentInChildren<Card>();
    }

 
    public void Pause()
    {
        pauseMenu.SetActive(true); 
    }

    public void Resume()
    {     
        pauseMenu.SetActive(false);
    }

    public void ShowWinScreen(int playerID)
    {
        winScreen.GetComponent<WinScript>().winner(playerID);

        winScreen.SetActive(true);
    }

    // Selection Panel Animation

    public IEnumerator ActivatePanel(PanelSelection panelType)
    {
        // 0 is the flipping state
        SetButtonStatus(0);

        yield return null;

        SetButtonStatus((int)panelType + 1);

        yield return new WaitForSeconds(0.2f);
    }

    void SetButtonStatus(int val)
    {
        // The first 3 are the selection buttons
        for (int i = 0; i < 3; i++)
        {
            buttonsAnimator[i].SetInteger("ButtonStatus", val);
        }
    }

}
