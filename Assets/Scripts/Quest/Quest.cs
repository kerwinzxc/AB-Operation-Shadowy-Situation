﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Utility class to store information related to any quest.
 */
public abstract class Quest {
	public enum QUEST_STATUS { UNKNOWN = -1, INACTIVE = 0, ACTIVE = 1, COMPLETED = 2 };
	public QUEST_STATUS status;

	public int id;
	public string name;
	public string description;

	protected List<Stage> stageList = new List<Stage>();
	protected int currentStage;

	public Quest(int id, string name, string description) {
		this.id = id;
		this.name = name;
		this.description = description;

		this.status = (QUEST_STATUS) PlayerPrefs.GetInt("quest-" + id + "-status", (int) QUEST_STATUS.INACTIVE);
		this.currentStage = PlayerPrefs.GetInt("quest-" + id + "-stage", 0);

		initStages();

		if(status == QUEST_STATUS.ACTIVE) {
			if(stageList[currentStage] != null)
				stageList[currentStage].setup();
		}
	}

	public abstract void initStages();

	public bool progress(QuestProgress progress) {
		if(status != QUEST_STATUS.ACTIVE) // Ignore progress calls if there is nothing to do here
			return false;

		if(currentStage < stageList.Count && stageList[currentStage].update(progress))
			nextStage();

		return true;
	}

	protected void nextStage() {
		// Fire the finish stage event
		if(stageList[currentStage] != null)
			stageList[currentStage].finish();

		// Update the current stage pointer, so new progress events will be redirected to there.
		currentStage++;

		// Save the current stage in the preferences
		PlayerPrefs.SetInt("quest-" + id + "-stage", currentStage);

		// Check if the current stage as the last one, and fire the quest finish event 
		if(currentStage >= stageList.Count) {
			complete();
		} else {
			// Send the setup event to the new stage
			stageList[currentStage].setup();

			GameController.questManager.stageUpdateEvent(stageList[currentStage]);
		}
	}

	protected void complete() {
		// Update the status to COMPLETED
		setStatus(QUEST_STATUS.COMPLETED);

		// Fire the quest finished event
		GameController.questManager.questFinishedEvent(this);
	}

	/**
	 * Used to reset the Quest to it's starting point
	 **/
	public void reset() {
		setStatus(QUEST_STATUS.ACTIVE);

		currentStage = 0;
		PlayerPrefs.SetInt("quest-" + id + "-stage", currentStage);

		if(stageList[currentStage] != null)
			stageList[currentStage].setup();

		GameController.questManager.questStartedEvent(this);
	}

	/**
	 * Return the current status of the quest.
	 **/
	public QUEST_STATUS getStatus() {
		return status;
	}

	public void setStatus(QUEST_STATUS status) {
		Debug.Log("Quest " + name + " (" + id + ") changed from " + this.status.ToString() + " to " + status.ToString());

		this.status = status;
		PlayerPrefs.SetInt("quest-" + id + "-status", (int) status);
	}

	public List<Stage> getStages() {
		return stageList;
	}

	public int getCurrentStage() {
		return currentStage;
	}

	public abstract class Stage {

		/**
		 * Used to setup any stage-related mechanics.
		 * Like spawn a mob, reset a door, etc.
		 **/
		abstract public void setup();

		/**
		 * Function to update the current objective inside the current stage.
		 * Returns  true  if the objective was reached; false otherwise.
		 **/
		abstract public bool update(QuestProgress progress);

		/**
		 * Function called when the stage is complete.
		 **/
		abstract public void finish();

		/**
		 * Used to generate a text that states the current status of this stage.
		 * e.g. Talk to Deliora.
		 * 		Picked up 3 out of 9 flowers.
		 **/
		abstract public string getText();
	}
}
