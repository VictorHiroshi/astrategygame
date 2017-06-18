using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ActionState {WaitingActionCall, PerformingAction, WaitingActionFinish}

public class ActionsManager : MonoBehaviour {

	public static ActionsManager instance;

	public PanelController controlPanel;
	public BoardManager boardManager;
	public int MovingCost = 1;

	public ActionState actualState;

	private GameObject selectedTile;
	private TileController targetTile;
	private List<GameObject> neighbours;

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
		if(boardManager.SelectedTile (out selectedTile) && 
			GameManager.instance.player[GameManager.instance.activePlayerIndex].coinCount>=MovingCost)
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
					if(!boardManager.hasNeighbours (tile.xIndex, tile.zIndex, out neighbours))
					{	
						CantPerformAction();

					}
					else
					{
						actualState = ActionState.PerformingAction;
						GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (MovingCost);
						StartCoroutine (MoveCreature (tile));
					}
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
			yield return null;
		}

		selectedTile = null;
		targetTile = null;

		GameManager.instance.ClearSelections ();
	}
}
