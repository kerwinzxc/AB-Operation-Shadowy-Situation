﻿using UnityEngine;
using System.Collections;

public class WeaponTest : Weapon {

	public WeaponTest() {
		this.name = "M9";
		this.icon = GameController.spriteManager.weaponM9;
		this.weaponPrefab = GameController.prefabManager.weaponM9;
		this.type = Type.Pistol;

		this.damage = 1f;

		populateSounds(GameController.audioManager.weaponPistolShoot, GameController.audioManager.weaponPistolShootNoAmmo, GameController.audioManager.weaponPistolReload);
		populateCraftingStatus();
	}
}
