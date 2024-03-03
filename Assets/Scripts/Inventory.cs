using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory {

    public const int hotbarSize = 10;

    public Planet planet;
    public int slotIndex = 0;
    public BlockID[] items;

    private Transform hotbar;

    public Inventory(Planet planet) {
		try {

            this.planet = planet;
            this.items = new BlockID[hotbarSize];
            this.hotbar = GameObject.Find("Hotbar").transform;

            SetItem(0, BlockID.Dirt);
            SetItem(1, BlockID.Stone);
            SetItem(2, BlockID.Sand);
            SetItem(3, BlockID.Wood);
            SetItem(4, BlockID.Leaf);
            SetItem(5, BlockID.Bedrock);
            SetItem(6, BlockID.Glass);
            SetItem(7, BlockID.Water);
            SetItem(8, BlockID.Air);
            SetItem(9, BlockID.Air);

        } catch (Exception ex) {
            Debug.LogError("Error in Inventory constructor: " + ex.Message);
        }
	}

    public void SetItem(int index, BlockID blockID) {
        try {

            if (index < 0 || index >= hotbarSize) {
                return;
            }

            items[index] = blockID;
            Block block = this.planet.blockTypes[(int)blockID];

            Image icon = hotbar.GetChild(index).GetChild(1).GetComponent<Image>();
            icon.sprite = block.icon;
            icon.enabled = blockID != BlockID.Air;

        } catch (Exception ex) {
            Debug.LogError("Error in Inventory SetItem: " + ex.Message);
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