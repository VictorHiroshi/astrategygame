﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Script to handle general game information, such as score points and turn information.

public class GameManager : MonoBehaviour {

	[HideInInspector]public BoardManager boardScript;
	[HideInInspector]public PlayerController [] player;

	public int coinsPerTurn = 2;
	public PanelController panelControler;
	public CameraController m_camera;
	public static GameManager instance = null;
	public GameObject tileHighlightObject;
	public GameObject [] creature;
	public Color[] playersColors;

	private int actualTurn;
	private int activePlayerIndex;

	void Awake () {
		// Defining this object as a singleton.
		if (instance == null)
			instance = this;
		else
			Destroy (this);

		boardScript = GetComponent <BoardManager> ();
		InitializeGame ();	
	}
		
	void InitializeGame ()
	{
		activePlayerIndex = 3;
		actualTurn = 0;
		AssignPlayers ();
		boardScript.SetupScene ();
		NextTurn ();
		panelControler.selectedUI = HighlightType.None;
	}

	public void NextTurn()
	{
		activePlayerIndex += 1;
		activePlayerIndex = activePlayerIndex % player.Length;

		if(activePlayerIndex == 0)
		{
			TurnChangingIncome ();
			actualTurn += 1;
			panelControler.ChangeTurnText (actualTurn);
		}

		panelControler.ChangeActivePlayer ("Player " + (activePlayerIndex + 1), player [activePlayerIndex].coinCount);
		FocusCameraOn (player [activePlayerIndex]);
	}

	private void AssignPlayers ()
	{
		int xMax = boardScript.columns;
		int zMax = boardScript.rows;

		player = new PlayerController[creature.Length];
		
		for (int i = 0; i < player.Length; i++) {
			player [i] = new PlayerController ();
			player [i].controlledTiles = new List<string> ();
			player [i].creature = creature [i];
			player [i].coinCount = coinsPerTurn;
			player [i].playerNumber = i;
		}

		player[0].controlledTiles.Add (TileController.GetStringID (xMax -1, zMax-1));
		player[1].controlledTiles.Add (TileController.GetStringID (xMax -1, 0));
		player[2].controlledTiles.Add (TileController.GetStringID (0, zMax-1));
		player[3].controlledTiles.Add (TileController.GetStringID (0, 0));
	}

	private void FocusCameraOn (PlayerController player)
	{
		string id = player.controlledTiles [0];
		Transform target = boardScript.getTile (id).spawnPoint;
		m_camera.MoveToTarget (target);
	}

	private void TurnChangingIncome ()
	{
		// TODO: Give turn changing money for all players.
		for (int i = 0; i < player.Length; i++) {
			player [i].coinCount += coinsPerTurn;
		}

	}
}
