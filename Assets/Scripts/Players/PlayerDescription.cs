using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerDescription", menuName = "Personal Tools/Player Description", order = 3)]
public class PlayerDescription : ScriptableObject
{
    public int playerID;
    public Texture2D playerTexture;
    public RuntimeAnimatorController playerAnimator;
    public RuntimeAnimatorController barrierAnimator;

    public float walkingTime;

    public Sprite GetSprite()
    {
        Sprite mySprite = Sprite.Create(playerTexture,
                                        new Rect(0, 0, playerTexture.width,
                                        playerTexture.height),
                                        new Vector2(0.5f, 0.5f));

        return mySprite;
    }

}
