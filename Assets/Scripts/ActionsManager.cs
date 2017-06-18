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
		
		if(GameManager.instance.player[GameManager.instance.activePlayerIndex].coinCount < MovingCost){
			GameManager.instance.panelControler.ShowMessage (3f, MessageType.NotEnoughtMoney);
		}
		else if(!boardManager.SelectedTile (out selectedTile))
		{
			GameManager.instance.panelControler.ShowMessage (3f, MessageType.SelectTileFirst);
		}
		else	
		{
			TileController tile = selectedTile.GetComponent <TileController> ();
			if(tile.creature == null)
			{
				GameManager.instance.panelControler.ShowMessage (3f, MessageType.NoCreatureThere);
			}
			else
			{
				if(tile.creature.belongsToPlayer != GameManager.instance.activePlayerIndex)
				{
					GameManager.instance.panelControler.ShowMessage (3f, MessageType.NotYourCreature);
				}
				else if(tile.creature.isTired)
				{
					GameManager.instance.panelControler.ShowMessage (3f, MessageType.CreatureTooTired);
				}
				else
				{
					if(!boardManager.hasNeighbours (tile.xIndex, tile.zIndex, out neighbours))
					{	
						GameManager.instance.panelControler.ShowMessage (3f, MessageType.CantPerformAction);
					}
					else
					{
						actualState = ActionState.PerformingAction;
						tile.creature.isTired = true;
						GameManager.instance.tiredCreatures.Add (tile.creature);
						GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (MovingCost);
						StartCoroutine (MoveCreature (tile));
					}
				}
			}
		}

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
