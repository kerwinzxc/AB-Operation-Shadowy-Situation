﻿using System;

public class CommandEnemySpawn : CommandEnemy {

	public CommandEnemySpawn() : base("spawn") { }

	public override string parse(string text) {
		string[] values = text.Split(' ');

		if(values.Length < 3) {
			return " <color=red>Invalid parameter.</color> " + use();
		} else {
			float x, y, z;

			if(!float.TryParse(values[0], out x))
				return " \"" + values[0] + "\" is not a valid number. " + use();

			if(!float.TryParse(values[1], out y))
				return " \"" + values[1] + "\" is not a valid number. " + use();

			if(!float.TryParse(values[2], out z))
				return " \"" + values[2] + "\" is not a valid number. " + use();


			UnityEngine.Vector3 target = new UnityEngine.Vector3(x, y, z);
			GameController.enemyManager.spawn(target);

			return "Enemies spawned at " + target.ToString();
		}
	}

	public override string use() {
		return "Syntax: " + name + " <x> <y> <z>";
	}

	protected override void registerSubcommands() { }
}
