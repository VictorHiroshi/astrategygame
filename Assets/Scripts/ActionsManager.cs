using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionsManager : MonoBehaviour {

	public PanelController controlPanel;
	public BoardManager boardManager;

	private TileController selectedTileController;
	//private BoardManager boardManager;
	private GameObject selectedTile;

/*	void OnAwake()
	{
		boardManager = gameObject.GetComponent <BoardManager> ();
	}*/

	void OnStart()
	{
		if(controlPanel == null)
		{
			Debug.LogError ("Assign a panel controller!");
		}
	}

	public void MoveCreatureFromSelectedTile()
	{
		if(boardManager.SelectedTile (out selectedTile))
		{
			// TODO: highlight tiles that player can move with this unit.
		}

		else
		{
			controlPanel.CantPerformActionMessage (3f);
		}
	}
}
