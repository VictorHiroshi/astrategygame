using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ActionState {WaitingActionCall, PerformingAction, WaitingActionFinish};
public enum ActionType {Move, Duplicate, LExploit, HExploit, Attack, Convert, Oppress, Defend, None};

public class ActionsManager : MonoBehaviour {

	public static ActionsManager instance;

	public PanelController controlPanel;
	public BoardManager boardManager;
	public int MovingCost = 1;

	[HideInInspector]public ActionState actualState;

	private TileController selectedTile;
	private TileController targetTile;
	private List<GameObject> neighbours;
	private ActionType performing = ActionType.None;

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
		GameManager.instance.panelControler.selectedUI = HighlightType.None;
		GameManager.instance.panelControler.canChangePlayerText = true;
		actualState = ActionState.WaitingActionCall;
	}

	public void SetTargetTile(TileController target)
	{
		targetTile = target;
	}

	public void CancelAction()
	{
		Debug.Log ("Cancel action");
	}

	public void ActionMove()
	{
		if(CanPerformAction ())
		{
			if (!selectedTile.creature.moved) {
				selectedTile.creature.moved = true;
			} else {
				selectedTile.creature.moved = false;
				selectedTile.creature.isTired = true;
				GameManager.instance.tiredCreatures.Add (selectedTile.creature);
			}

			selectedTile.Unselect ();
			GameManager.instance.panelControler.canChangePlayerText = false;
			GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (MovingCost);
			performing = ActionType.Move;
			StartCoroutine (PerformingActionState (false));
		}
	}

	private void MoveCreature()
	{
		selectedTile.creature.MoveToTarget (targetTile.spawnPoint);
		targetTile.creature = selectedTile.creature;
		GameManager.instance.player [GameManager.instance.activePlayerIndex].
		controlledTiles.Remove (TileController.GetStringID (selectedTile.xIndex, selectedTile.zIndex));
		selectedTile.creature = null;
		GameManager.instance.player [GameManager.instance.activePlayerIndex].
		controlledTiles.Add (TileController.GetStringID (targetTile.xIndex, targetTile.zIndex));
	}

	private IEnumerator PerformingActionState(bool creatureTarget)
	{
		actualState = ActionState.PerformingAction;

		HighlightNeighbours (creatureTarget);

		while (targetTile == null)
		{
			yield return null;
		}

		switch (performing){
		case ActionType.Move:
			MoveCreature ();
			break;
		}

		StartCoroutine (WaitingActionFinishState ());
	}

	private IEnumerator WaitingActionFinishState()
	{
		actualState = ActionState.WaitingActionFinish;

		while (actualState != ActionState.WaitingActionCall)
		{
			yield return null;
		}

		performing = ActionType.None;
		selectedTile = null;
		targetTile = null;

		GameManager.instance.ClearSelections ();
	}

	private void HighlightNeighbours(bool withCreatues)
	{
		TileController neighbourTileController;

		foreach(GameObject neighbour in neighbours)
		{
			neighbourTileController = neighbour.GetComponent <TileController> ();
			if(neighbourTileController!=null)
			{
				if ((neighbourTileController.creature!=null) == withCreatues)
				{
					neighbourTileController.Highlight ();
				}
			}
		}
	}

	private bool CanPerformAction()
	{
		GameObject tile;
		if(GameManager.instance.player[GameManager.instance.activePlayerIndex].coinCount < MovingCost){
			GameManager.instance.panelControler.ShowMessage (3f, MessageType.NotEnoughtMoney);
			return false;
		}
		else if(!boardManager.SelectedTile (out tile))
		{
			GameManager.instance.panelControler.ShowMessage (3f, MessageType.SelectTileFirst);
			return false;
		}
		else	
		{
			selectedTile = tile.GetComponent <TileController> ();
			if(selectedTile.creature == null)
			{
				GameManager.instance.panelControler.ShowMessage (3f, MessageType.NoCreatureThere);
				return false;
			}
			else
			{
				if(selectedTile.creature.belongsToPlayer != GameManager.instance.activePlayerIndex)
				{
					GameManager.instance.panelControler.ShowMessage (3f, MessageType.NotYourCreature);
					return false;
				}
				else if(selectedTile.creature.isTired)
				{
					GameManager.instance.panelControler.ShowMessage (3f, MessageType.CreatureTooTired);
					return false;
				}
				else
				{
					if(!boardManager.hasNeighbours (selectedTile.xIndex, selectedTile.zIndex, out neighbours))
					{	
						GameManager.instance.panelControler.ShowMessage (3f, MessageType.CantPerformAction);
						return false;
					}
				}
			}
		}
		return true;

	}
}
