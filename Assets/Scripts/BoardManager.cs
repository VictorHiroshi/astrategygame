﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Class to organize all sorts of tiles.
[System.Serializable]
public class TileLists{
	public GameObject[] normalTiles;
	public GameObject[] rightEdgeTiles;
	public GameObject[] leftEdgeTiles;
	public GameObject[] upEdgeTiles;
	public GameObject[] downEdgeTiles;
	public GameObject[] upperLeftCornerTile;
	public GameObject[] upperRightCornerTile;
	public GameObject[] lowerLeftCornerTile;
	public GameObject[] lowerRightCornerTile;
	public float tileSideSize = 3.0f;
}

// Class to organize all sorts of resources.
[System.Serializable]
public class ResourceList{
	public GameObject[] stone;
	public GameObject[] tree;
	public GameObject[] barrier;
}

// Script to handle general operations of the boardgame.
public class BoardManager : MonoBehaviour {

	public int columns = 20;
	public int rows = 20;
	public TileLists tiles;
	public ResourceList resources;

	private Transform boardHolder;
	private GameObject selectedTile;
	private bool existsSelectedTile;

	private Dictionary<string, GameObject> boardGameByID;


	void Awake()
	{
		GetComponent <BoxCollider> ().size=(new Vector3(columns*tiles.tileSideSize+6, 0, rows*tiles.tileSideSize+6));
		GetComponent <BoxCollider> ().transform.position = (new Vector3 ((columns * tiles.tileSideSize/2)-tiles.tileSideSize, 0,
																		(rows * tiles.tileSideSize/2)-tiles.tileSideSize));
	}
		
	// Creates a new boardgame with all the necessary setups to start a new game.
	public void SetupScene(){
		BoardSetup ();
		CreateResources ();
		CreateInitialCreatures ();
		// TODO: setup HUD.
		selectedTile = null;
		existsSelectedTile = false;
	}

	// Tests if there's any selected tile in the game.
	// If true, returns the selected tile with the out parameter.
	public bool SelectedTile(out GameObject tile)
	{
		tile = selectedTile;
		return existsSelectedTile;
	}

	// Informs the manager that this specific tile is the selected one.
	public void SetSelectedTile(GameObject tile)
	{
		//If another tile was already selected, unselect it
		if (selectedTile != null) {
			TileController controller = selectedTile.GetComponent<TileController> ();
			if (controller != null) {
				controller.Unselect ();
			}
		}
		selectedTile = tile;
		existsSelectedTile = true;
	}

	// Informs the manager that there's no tile selected in the game anymore.
	public void SetSelectionToNull()
	{
		selectedTile = null;
		existsSelectedTile = false;
	}

	// Given an ID, returns the controller of the tile, if exists.
	public TileController getTile(string id)
	{
		// TODO: Test if id is valid;
		TileController tileInstance = boardGameByID [id].GetComponent <TileController>();
		return tileInstance;
	}

	// Tests if the tile has neighbours in the boardgame.
	// If true, populates the neighbours list with it's neighbours.
	public bool hasNeighbours(int x, int z, out List<GameObject> neighbours)
	{

		neighbours = new List<GameObject> ();

		if (x < 0 || x >= rows || z < 0 || z >= columns)
			return false;

		GameObject nighbourTile;

		// Add left neighbour to the list.
		if (x > 0){
			nighbourTile = boardGameByID [TileController.GetStringID (x - 1, z)];
			neighbours.Add(nighbourTile);
		}

		// Add right neighbour to the list.
		if (x < rows - 1) {
			nighbourTile = boardGameByID [TileController.GetStringID (x + 1, z)];
			neighbours.Add(nighbourTile);
		}

		// Add upper neighbour to the list.
		if (z > 0) {
			nighbourTile = boardGameByID [TileController.GetStringID (x, z - 1)];
			neighbours.Add(nighbourTile);
		}

		// Add lower neighbour to the list.
		if (z < columns - 1) {
			nighbourTile = boardGameByID [TileController.GetStringID (x, z + 1)];
			neighbours.Add (nighbourTile);
		}

		return true;
	}

	// Instantiates all tiles of the boardgame.
	private void BoardSetup()
	{

		boardGameByID = new Dictionary<string, GameObject> ();

		boardHolder = new GameObject ("Board").transform;

		for (int z = -1; z < columns + 1; z++) 
		{
			for (int x = -1; x < rows + 1; x++)
			{
				GameObject toInstantiate;
				if (z == -1) {
					if (x == -1)
						toInstantiate = tiles.upperRightCornerTile [Random.Range (0, tiles.upperRightCornerTile.Length)];
					else if (x == rows)
						toInstantiate = tiles.lowerRightCornerTile [Random.Range (0, tiles.lowerRightCornerTile.Length)];
					else
						toInstantiate = tiles.rightEdgeTiles [Random.Range (0, tiles.rightEdgeTiles.Length)];
				} else if (z == columns) {
					if (x == -1)
						toInstantiate = tiles.upperLeftCornerTile [Random.Range (0, tiles.upperLeftCornerTile.Length)];
					else if (x == rows)
						toInstantiate = tiles.lowerLeftCornerTile [Random.Range (0, tiles.lowerLeftCornerTile.Length)];
					else
						toInstantiate = tiles.leftEdgeTiles [Random.Range (0, tiles.leftEdgeTiles.Length)];
				} else if (x == -1) {
					toInstantiate = tiles.upEdgeTiles [Random.Range (0, tiles.upEdgeTiles.Length)];
				} else if (x == rows) {
					toInstantiate = tiles.downEdgeTiles [Random.Range (0, tiles.downEdgeTiles.Length)];
				} else {
					toInstantiate = tiles.normalTiles [Random.Range (0, tiles.normalTiles.Length)];
				}

				GameObject instance = Instantiate (toInstantiate, new Vector3 (z * tiles.tileSideSize, 0f, x * tiles.tileSideSize), Quaternion.identity);
				instance.transform.SetParent (boardHolder);

				TileController tile = instance.GetComponent<TileController> ();

				// Tests is this tile has a TileController script attached.
				if (tile != null) {
					// If it is a playable tile (with TileController), add it to the list of active tiles.
					tile.SetID (x, z);
					boardGameByID.Add (tile.GetStringID (), instance);
				}

			}
		}
	}

	// Generates resources for some tiles on the boardgame.
	// The boardgame is divided into four quadrants to balance the resource distribution.
	private void CreateResources ()
	{
		
	}

	// Populates the boardgame with the initial creatures for all players.
	private void CreateInitialCreatures ()
	{
		PlayerController playerInstance;
		// Player 1:
		playerInstance = GameManager.instance.player1;
		InstantiateCreatures (playerInstance);

		// Player 2:
		playerInstance = GameManager.instance.player2;
		InstantiateCreatures (playerInstance);

		// Player 3:
		playerInstance = GameManager.instance.player3;
		InstantiateCreatures (playerInstance);

		// Player 4:
		playerInstance = GameManager.instance.player4;
		InstantiateCreatures (playerInstance);
	}

	// Instantiates every creature of a given player.
	private void InstantiateCreatures(PlayerController playerInstance)
	{
		TileController tileInstance;
		foreach ( string id in playerInstance.controlledTiles){
			tileInstance = boardGameByID [id].GetComponent <TileController>();

			if(tileInstance!=null){
				tileInstance.InstantiateCreature (playerInstance.creature);
			}
		}
	}
}
