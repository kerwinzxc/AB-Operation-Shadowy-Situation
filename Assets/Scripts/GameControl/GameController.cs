﻿using UnityEngine;
using System.Collections;
using UnityStandardAssets.ImageEffects;

public class GameController : MonoBehaviour {
	public static GameController INSTANCE { get; private set; }
	private new GUITexture guiTexture;

	public static TextManager textManager;
	public static AudioManager audioManager;
	public static SpriteManager spriteManager;
	public static PrefabManager prefabManager;
	public static NPCManager npcManager;

	public static QuestManager questManager;
	public static CutsceneManager cutsceneManager;
	public static DialogueManager dialogueManager;
	public static MaterialManager materialManager;
	public static CraftingManager craftingManager;
	public static EnemyManager enemyManager;

	public static PlayerController playerController;
	public static PathfindHelper playerPathfind;

	public static FPSCounter fpsCounter;

	private static bool isPaused = false;
	private static bool isFocused = false;
	
	public float fadeSpeed;
	public bool fade = true;
	private bool fadeIn = false; // Fade to black (true); or fade from black (false)

	public GameObject uiPause;
	public GameObject uiCrosshair;

	private static VignetteAndChromaticAberration vignette;
	private static bool vignetteEnabled;
	private static float vignetteIntensity;
	private static float vignetteTargetIntensity = 5f;
	private static float vignetteAberration;
	private static float vignetteTargetAberration = 7f;

	void Awake() {
		INSTANCE = this;
		guiTexture = GetComponent<GUITexture>();

		textManager = gameObject.GetComponent<TextManager>();
		audioManager = gameObject.GetComponent<AudioManager>();
		spriteManager = gameObject.GetComponent<SpriteManager>();
		prefabManager = gameObject.GetComponent<PrefabManager>();
		npcManager = gameObject.GetComponent<NPCManager>();

		questManager = gameObject.GetComponent<QuestManager>();
		cutsceneManager = gameObject.GetComponent<CutsceneManager>();
		dialogueManager = gameObject.GetComponent<DialogueManager>();
		materialManager = gameObject.GetComponent<MaterialManager>();
		craftingManager = gameObject.GetComponent<CraftingManager>();
		enemyManager = gameObject.GetComponent<EnemyManager>();

		GameObject player = GameObject.FindGameObjectWithTag(Tags.player);
		playerController = player.GetComponent<PlayerController>();
		playerPathfind = player.GetComponent<PathfindHelper>();

		fpsCounter = transform.Find("ui/fps_counter").GetComponent<FPSCounter>();

		guiTexture.color = Color.black;
		guiTexture.pixelInset = new Rect(0f, 0f, Screen.width, Screen.height);

		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;

		// Reset the fader
		fade = true;
		fadeIn = false;

		// Load the player coordinates from the preferences
		loadPlayerState();

		// Run first-launch settings
		checkFirstRun();
	}

	void Update() {
		if(fade) {
			guiTexture.enabled = true;
			if(fadeIn) { // To black
				guiTexture.color = Color.Lerp(guiTexture.color, Color.black, fadeSpeed * Time.deltaTime);

				if(guiTexture.color.a >= .975f) {
					guiTexture.color = Color.black;
					fade = fadeIn = false;
				}
			} else { // To transparent
				guiTexture.color = Color.Lerp(guiTexture.color, Color.clear, fadeSpeed * Time.deltaTime);
				
				if(guiTexture.color.a <= .0125f) {
					guiTexture.color = Color.clear;
					fade = guiTexture.enabled = false;
					fadeIn = true;
				}
			}
		}


		// Try to obtain an instance of the camera vignette
		if(vignette == null) {
			vignette = Camera.main.gameObject.GetComponent<VignetteAndChromaticAberration>();
		} else {
			if(isPaused) {
				vignette.intensity = Mathf.Lerp(vignette.intensity, vignetteTargetIntensity, Time.deltaTime * 10);
				vignette.chromaticAberration = Mathf.Lerp(vignette.chromaticAberration, vignetteTargetAberration, Time.deltaTime * 10);
			} else {
				vignette.intensity = Mathf.Lerp(vignette.intensity, vignetteIntensity, Time.deltaTime * 15);
				vignette.chromaticAberration = Mathf.Lerp(vignette.chromaticAberration, vignetteAberration, Time.deltaTime * 15);
			}
		}
	
		checkCancelInput();
	}
	
	public void btnClickedMainMenu() {
		fade = true;
		Application.LoadLevel(0);
	}

	public void btnClickedExit() {
		exitPause();

		fade = true;
		Application.Quit();
	}

	public static bool getPaused() {
		return isPaused;
	}

	private static void setPaused(bool state) {
		// Prevent the same event being called more than one time in a row
		if(state == isPaused) return;

		isPaused = state;
		if(isPaused)
			enterPause();
		else
			exitPause();
	}


	public static bool getFocused() {
		return isFocused;
	}

	public static void setFocused(bool state, bool lockCursor = true) {
		isFocused = state;
		if(isFocused)
			enterFocus(lockCursor);
		else
			exitFocus();
	}

	public static bool isPausedOrFocused() {
		return isPaused || isFocused;
	}

	private void checkCancelInput() {
		if(InputManager.getKeyDown("cancel")) {
			if(!isFocused) {
				setPaused(!isPaused);
			}

			cutsceneManager.cancelBtnClicked();
		}
	}

	private static void enterPause() {
		//Debug.Log("Pausing game");

		isPaused = true;

		INSTANCE.uiPause.SetActive(true);
		INSTANCE.uiCrosshair.SetActive(false);

		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.Confined;

		// Save preferences in the event of a crash
		INSTANCE.OnApplicationQuit();

		
		if(vignette != null) {
			vignetteEnabled = vignette.enabled;
			if(!vignette.enabled) vignette.enabled = true;
			
			vignetteIntensity = vignette.intensity;
			vignetteAberration = vignette.chromaticAberration;
		}
	}

	private static void exitPause() {
		//Debug.Log("Resuming game");

		isPaused = false;

		INSTANCE.uiPause.SetActive(false);
		INSTANCE.uiCrosshair.SetActive(true);

		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
	}

	private static void enterFocus(bool lockCursor = true) {
		//Debug.Log("Entering focus mode");

		INSTANCE.uiCrosshair.SetActive(false);

		Cursor.visible = !lockCursor;
		Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.Confined;
	}

	private static void exitFocus() {
		//Debug.Log("Exiting focus mode");

		INSTANCE.uiCrosshair.SetActive(!isPaused);

		Cursor.visible = isPaused;
		Cursor.lockState = isPaused ? CursorLockMode.Confined : CursorLockMode.Locked;
	}

	void OnApplicationQuit() {
		savePlayerState();
		PlayerPrefs.Save();
	}

	private void loadPlayerState() {
		// Position
		playerController.transform.position = new Vector3(PlayerPrefs.GetFloat("player_pos_x"), PlayerPrefs.GetFloat("player_pos_y"), PlayerPrefs.GetFloat("player_pos_z"));
		// Rotation
		playerController.transform.rotation = Quaternion.Euler(PlayerPrefs.GetFloat("player_rot_x"), PlayerPrefs.GetFloat("player_rot_y"), PlayerPrefs.GetFloat("player_rot_z"));
	}

	private void savePlayerState() {
		Vector3 position = playerController.transform.position;
		PlayerPrefs.SetFloat("player_pos_x", position.x);
		PlayerPrefs.SetFloat("player_pos_y", position.y);
		PlayerPrefs.SetFloat("player_pos_z", position.z);

		Vector3 rotation = playerController.transform.eulerAngles;
		PlayerPrefs.SetFloat("player_rot_x", rotation.x);
		PlayerPrefs.SetFloat("player_rot_y", rotation.y);
		PlayerPrefs.SetFloat("player_rot_z", rotation.z);

		// Trigger save event on the PlayerHP script
		PlayerHP.save();
	}

	private void checkFirstRun() {
		if(!PlayerPrefs.HasKey("first-run")) {
			// Initialize the quest list first, because the GameController script is called before the QuestManager one.
			QuestManager.initQuests();
			// And start the forst quest
			QuestManager.getQuest("00_LEARN").reset();

			// Set anything in the first run preference to lock this method from being called in future game launches.
			PlayerPrefs.SetInt("first-run", 0);
        }
	}
}