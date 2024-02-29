using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory {

    public const int hotbarSize = 10;

    public int slotIndex = 0;
    public BlockID[] items;

    private Transform hotbar;

    public Inventory() {
		try {

            items = new BlockID[hotbarSize];
            hotbar = GameObject.Find("Hotbar").transform;

        } catch (Exception ex) {
            Debug.LogError("Error in Inventory constructor: " + ex.Message);
        }
	}

    public void ChangeActiveSlot(float scroll) {
		try {
            
            Image oldImg = hotbar.GetChild(slotIndex).GetComponent<Image>();
            oldImg.color = Color.grey;

            if (scroll > 0) {
                if (slotIndex == 0) {
                    slotIndex = hotbarSize - 1;
                } else {
                    slotIndex--;
                }
            } else {
                if (slotIndex == hotbarSize - 1) {
                    slotIndex = 0;
                } else {
                    slotIndex++;
                }
            }

            Image newImg = hotbar.GetChild(slotIndex).GetComponent<Image>();
            newImg.color = Color.white;

        } catch (Exception ex) {
            Debug.LogError("Error in Inventory ChangeActiveSlot: " + ex.Message);
        }
	}
}