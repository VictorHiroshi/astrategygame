using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ActionState {WaitingActionCall, PerformingAction, WaitingActionFinish}

public class ActionsManager : MonoBehaviour {

	public static ActionsManager instance;

	public PanelController controlPanel;
	public BoardManager boardManager;

	public ActionState actualState;

	private GameObject selectedTile;
	private TileController targetTile;

	void Awake () {
		// Defining this object as a singleton.
		if (instance == null)
			instance = this;
		else
			Destroy (this);

		actualState = ActionState.WaitingActionCall;
	}

	void OnStart()
	{
		if(controlPanel == null)
		{
			Debug.LogError ("Assign a panel controller!");
		}
	}

	public void FinishAction()
	{
		actualState = ActionState.WaitingActionCall;
	}

	public void SetTargetTile(TileController target)
	{
		targetTile = target;
	}

	public void MoveCreatureFromSelectedTile()
	{
		if(boardManager.SelectedTile (out selectedTile))
		{
			TileController tile = selectedTile.GetComponent <TileController> ();
			if(tile.creature == null)
			{
				CantPerformAction ();
			}
			else
			{
				if(tile.creature.belongsToPlayer != GameManager.instance.activePlayerIndex)
				{
					CantPerformAction ();
				}
				else
				{
					actualState = ActionState.PerformingAction;
					StartCoroutine (MoveCreature (tile));
				}
			}
		}

		else
		{
			CantPerformAction();
		}
	}

	private void CantPerformAction()
	{
		controlPanel.CantPerformActionMessage (3f);
	}

	private IEnumerator MoveCreature(TileController tile)
	{
		List<GameObject> neighbours;
		boardManager.hasNeighbours (tile.xIndex, tile.zIndex, out neighbours);

		TileController neighbourTileController;

		foreach(GameObject neighbour in neighbours)
		{
			neighbourTileController = neighbour.GetComponent <TileController> ();
			if(neighbourTileController!=null)
			{
				if (neighbourTileController.creature==null)
				{
					neighbourTileController.Highlight ();
				}
			}
		}

		while (targetTile == null)
		{
			yield return null;
		}

		actualState = ActionState.WaitingActionFinish;

		tile.creature.MoveToTarget (targetTile.spawnPoint);
		targetTile.creature = tile.creature;
		tile.creature = null;

		while (actualState != ActionState.WaitingActionCall)
		{
			Debug.Log (actualState);
			yield return null;
		}

		selectedTile = null;
		targetTile = null;

		GameManager.instance.ClearSelections ();
	}
}
