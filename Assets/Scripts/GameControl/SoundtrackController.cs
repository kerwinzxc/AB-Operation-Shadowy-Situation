﻿using UnityEngine;
using System.Collections;

public class SoundtrackController : MonoBehaviour {
	public AudioClip[] soundtracks;
	private int current = -1;

	private AudioSource source;

	public static SoundtrackController INSTANCE;

	void Start() {
		INSTANCE = this;
	}
	
	public void init() {
		source = GetComponent<AudioSource>();
		if(source == null)
			gameObject.AddComponent<AudioSource>();

		// Set the volume according to the settings (also, applying master volume)
		source.volume = AudioManager.getAmbienceVolume();

		// Initiate the loop
		nextClip();
	}

	private void nextClip() {
		// Update the clip and play it
		source.clip = getRandomClip();
		source.Play();

		// Invoke this method again after the music ends to play a new one
		Invoke("nextClip", source.clip.length);
	}

	private AudioClip getRandomClip() {
		if(soundtracks.Length < 1) // If the array is empty, ignore
			return null;
		else if(soundtracks.Length == 1) // If onlt one element, send him
			return soundtracks[0];

		// Get a random index NOT equal to the previous one
		int index;
		do {
			index = Random.Range(0, soundtracks.Length);
		} while(index == current);

		current = index;
		return soundtracks[index];
	}
}
