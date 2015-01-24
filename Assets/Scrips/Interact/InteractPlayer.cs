﻿using UnityEngine;
using System.Collections;

public class InteractPlayer : MonoBehaviour {
	public float range = 50f;

	// Update is called once per frame
	void Update () {
		if(GameController.isPausedOrFocused()) return; // Avoid showing any HUD during focus events (and useless calculations)

		RaycastHit hit;
		
		// When we left click and our raycast hits something
		if(Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, range)) {
			Interaction other = (Interaction) hit.transform.gameObject.GetComponent(typeof(Interaction));

			if(other != null) {
				if(hit.distance <= other.minDistance)
					text = other.type + " available. Press E to interact with " + other.name;
				else
					text = other.type + " available, but not in range";

				if(Input.GetKeyDown(InputManager.interact))
					other.doAction(this.gameObject);
			} else
				text = "";
		}
	}

	private string text = "";

	void OnGUI() {
		if(!GameController.isPausedOrFocused() && !text.Equals(""))
			GUI.Box(new Rect(Screen.width / 6, Screen.height - 50, Screen.width / 1.5f, 50), text);
	}
}
