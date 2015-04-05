﻿using UnityEngine;
using System.Collections;

public class Quest_00_LEARN : Quest {

	public Quest_00_LEARN() : base(1, "Learning the Ropes", "This is the description for the test quest that was setup just to test the... Quest System") {
		// NO-OP
	}

	public override void initStages() {
		stageList.Add(new Stage1());
		stageList.Add(new Stage2());
	}
	
	protected class Stage1 : Stage {
		private Vector3 objective = new Vector3(0,0,0);
		private GameObject sentinel;
		
		public Stage1() {
		}

		public override void setup() {
			// Create a sentinel to check whenever the player sets foot on the target
			sentinel = GameObject.Instantiate(GameController.prefabManager.marker, objective, Quaternion.Euler(0, 0, 0)) as GameObject;
			sentinel.GetComponent<PositionSentinel>().setup();
		}
		
		public override bool update(QuestProgress progress) {
			if(progress.type == QuestProgress.ProgressType.POSITION) {
				if(Vector3.Distance(progress.getPosition(), objective) <= 5f) {
					GameController.questManager.stageUpdateEvent(this);
					return true;
				}
			}

			return false;
		}

		public override void finish() {
			// Destroy the sentinel when it is no longer needed
			MonoBehaviour.Destroy(sentinel);
		}
		
		public override string getText() {
			return "Go to the indicated zone.";
		}
	}

	protected class Stage2 : Stage {
		
		public Stage2() {
		}
		
		public override void setup() {
			GameObject.FindGameObjectWithTag(Tags.npc).GetComponent<Interaction>().dialogue = "DialogueQ_00_LEARN_1";
		}
		
		public override bool update(QuestProgress progress) {
			if(progress.type == QuestProgress.ProgressType.DIALOGUE) {
				if(progress.getStr().Equals("Yurippe")) {
					GameController.questManager.stageUpdateEvent(this);
					return true;
				}
			}
			
			return false;
		}

		public override void finish() {
			// NO-OP
		}
		
		public override string getText() {
			return "Talk to Yurippe.";
		}
	}
}
