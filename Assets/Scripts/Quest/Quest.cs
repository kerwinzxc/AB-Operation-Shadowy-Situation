﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Utility class to store information related to any quest.
 */
public abstract class Quest {
	public enum STATUS { UNKNOWN = -1, INACTIVE = 0, ACTIVE = 1, COMPLETED = 2 };
	public STATUS status { get; set; }

	public string id { get; protected set; }
	public string name { get; protected set; }
	public string description { get; protected set; }

	public List<Stage> stages { get; protected set; }
	public int currentStage { get; protected set; }

	public Quest(string name, string description) {
		this.name = name;
		this.description = description;

		generateId();

		status = (STATUS) PlayerPrefs.GetInt("quest-" + id + "-status", (int) STATUS.INACTIVE);
		currentStage = PlayerPrefs.GetInt("quest-" + id + "-stage", 0);

		stages = new List<Stage>();
		initStages();

		if(status == STATUS.ACTIVE) {
			// Check if the current stage is bigger than the last stage ID on this quest.
			// In that case, mark the quest as COMPLETED
			if(currentStage >= stages.Count)
				complete();
			else if(stages[currentStage] != null && stages[currentStage].setup()) // In case the stage was already completed on startup, to to the next one
				nextStage();
		}
	}

	private void generateId() {
		string className = GetType().Name;
		int index = className.IndexOf('_');
		if(index != -1) {
			id = className.Substring(index + 1);
		} else {
			Debug.Log("Unable to get quest ID from class name \"" + className + "\"");
			id = "UNKNOWN";
		}
	}

	public abstract void initStages();

	public bool progress(QuestProgress progress) {
		if(status != STATUS.ACTIVE) // Ignore progress calls if there is nothing to do here
			return false;

		if(currentStage < stages.Count && stages[currentStage].update(progress))
			nextStage();

		return true;
	}

	protected void nextStage() {
		// Fire the finish stage event
		if(stages[currentStage] != null)
			stages[currentStage].finish();

		// Update the current stage pointer, so new progress events will be redirected to there.
		currentStage++;

		// Save the current stage in the preferences
		PlayerPrefs.SetInt("quest-" + id + "-stage", currentStage);

		// Check if the current stage as the last one, and fire the quest finish event 
		if(currentStage >= stages.Count) {
			complete();
		} else {
			// Send the setup event to the new stage
			stages[currentStage].setup();

			GameController.questManager.stageUpdateEvent(stages[currentStage]);
		}
	}

	public virtual void complete() {
		// Update the status to COMPLETED
		setStatus(STATUS.COMPLETED);

		// Fire the quest finished event
		GameController.questManager.questFinishedEvent(this);
	}

	/**
	 * Used to start the quest.
	 * Virtually the same as the reset() method.
	 */
	public void start() {
		reset();
	}

	/**
	 * Used to reset the Quest to it's starting point
	 **/
	public void reset() {
		setStatus(STATUS.ACTIVE);

		currentStage = 0;
		PlayerPrefs.SetInt("quest-" + id + "-stage", currentStage);

		if(currentStage + 1 <= stages.Count && stages[currentStage] != null)
			stages[currentStage].setup();

		GameController.questManager.questStartedEvent(this);
	}

	/**
	 * Return the current status of the quest.
	 **/
	public STATUS getStatus() {
		return status;
	}

	public void setStatus(STATUS status) {
		Debug.Log("Quest " + name + " (" + id + ") changed from " + this.status + " to " + status);

		this.status = status;
		PlayerPrefs.SetInt("quest-" + id + "-status", (int) status);
	}

	public abstract class Stage {

		/**
		 * Used to setup any stage-related mechanics.
		 * Like spawn a mob, reset a door, etc.
		 * 
		 * Returns true if this stage's requirements were already met in the setup phase.
		 **/
		public virtual bool setup() {
			return false;
		}

		/**
		 * Function to update the current objective inside the current stage.
		 * Returns  true  if the objective was reached; false otherwise.
		 **/
		abstract public bool update(QuestProgress progress);

		/**
		 * Function called when the stage is complete.
		 **/
		public virtual void finish() {
			// NO-OP
		}

		/**
		 * Used to generate a text that states the current status of this stage.
		 * e.g. Talk to Deliora.
		 * 		Picked up 3 out of 9 flowers.
		 **/
		abstract public string getText();
	}

	/**
	 * 
	 * PRE-CREATED STAGES
	 * 
	**/
	
	protected class GoTo : Stage {
		private Vector3 objective;
		private string text;

		public GoTo(Vector3 pos, string text = null) {
			objective = pos;
			this.text = text;
		}
		
		public override bool setup() {
			// Create a sentinel to check whenever the player sets foot on the target
			GameObject sentinel = Object.Instantiate(GameController.prefabManager.marker, objective, Quaternion.Euler(Vector3.zero)) as GameObject;
			sentinel.GetComponent<PositionSentinel>().setup();

			return false;
		}
		
		public override bool update(QuestProgress progress) {
			if(progress.type == QuestProgress.Type.POSITION) {
				if(Vector3.Distance(progress.getPosition(), objective) <= 5f) {
					GameController.questManager.stageUpdateEvent(this);
					return true;
				}
			}
			
			return false;
		}
		
		public override string getText() {
			return text ?? "Follow the marked line.";
		}
	}

	protected class TalkTo : Stage {
		private Interaction npc;
		private string dialogue;
		private string dialoguePre; // Stores the last dialogue for that NPC (before this stage)
		private string text;

		public TalkTo(Interaction npcScript, string dialogueClass, string text = null) {
			this.npc = npcScript;
			this.dialogue = dialogueClass;
			this.text = text;
		}
		
		public override bool setup() {
			// Save the previous dialogue
			//dialoguePre = npc.dialogue;
            npc.dialogue = dialogue;

			return false;
		}
		
		public override bool update(QuestProgress progress) {
			if(progress.type == QuestProgress.Type.DIALOGUE) {
				if(progress.getStr().Equals(npc.name)) {
					GameController.questManager.stageUpdateEvent(this);
					return true;
				}
			}
			
			return false;
		}

		public override void finish() {
			// Restore the previous dialogue
			//npc.dialogue = dialoguePre;
		}

		public override string getText() {
			return string.Format(text ?? "Talk to <b>{0}</b>.", npc.name);
		}
	}

	protected class Craft : Stage {
		Weapon weapon;
		
		public Craft(Weapon weapon) {
			this.weapon = weapon;
		}

		public override bool setup() {
			return weapon != null && weapon.isCrafted; // In case the weapon was already crafted, complete the stage
		}
		
		public override bool update(QuestProgress progress) {
			// Failsafe check to prevent the player to be stuck in this stage
			if(weapon == null || weapon.isCrafted)
				return true;

			if(progress.type == QuestProgress.Type.ITEM_CRAFT) {
				if(weapon != null && progress.getStr().Equals(weapon.name)) {
					GameController.questManager.stageUpdateEvent(this);
					return true;
				}
			}
			
			return false;
		}
		
		public override string getText() {
			string name = weapon != null ? weapon.name : "UNKNOWN";
			return "Craft <b>" + name + "</b>.";
		}
	}

	protected class MaterialPick : Stage {
		private int current;
		private int ammount;

		private string key;

		public MaterialPick(string key, int ammount) {
			this.ammount = ammount;
			this.key = key;
		}
		
		public override bool setup() {
			current = PlayerPrefs.GetInt(key, 0);

			return current >= ammount; // Finish the stage incase the collected ammount is enough.
		}
		
		public override bool update(QuestProgress progress) {
			if(progress.type == QuestProgress.Type.ITEM_MATERIAL_PICKUP) {
				current += (int) progress.getNumber();
				
				PlayerPrefs.SetInt(key, current);


				if(current >= ammount)
					return true;

				GameController.questManager.stageUpdateEvent(this);
			}
			
			return false;
		}
		
		public override void finish() {
			PlayerPrefs.DeleteKey(key);
		}
		
		public override string getText() {
			return string.Format("Gathered {0} of {1} materials.", current, ammount);
		}
	}

	protected class ItemPick : Stage {
		protected string key;
		protected string itemName;

		protected int current;
		protected int ammount;

		public ItemPick(string itemName, int ammount, string key) {
			this.itemName = itemName;
			this.ammount = ammount;
			this.key = key;
		}

		public override bool setup() {
			current = PlayerPrefs.GetInt(key, 0);

			return current >= ammount; // Finish the stage incase the collected ammount is enough.
		}

		public override bool update(QuestProgress progress) {
			if(progress.type == QuestProgress.Type.ITEM_PICKUP) {
				if(progress.getStr().Equals(itemName)) {
					current += (int)progress.getNumber();

					PlayerPrefs.SetInt(key, current);


					if(current >= ammount)
						return true;

					GameController.questManager.stageUpdateEvent(this);
				}
			}

			return false;
		}

		public override string getText() {
			return string.Format("Gathered {0} of {1} <b>{2}</b>.", current, ammount, itemName);
		}
	}
}
