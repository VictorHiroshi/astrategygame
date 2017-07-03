using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ActionState {WaitingActionCall, PerformingAction, WaitingActionFinish};
public enum ActionType {Move, Duplicate, LExploit, HExploit, Attack, Convert, Oppress, Defend, None};

public class ActionsManager : MonoBehaviour {

	public static ActionsManager instance;

	public BoardManager boardManager;
	public int movingCost = 1;
	public int duplicateCost = 2;

	[HideInInspector]public ActionState actualState;

	private TileController selectedTile;
	private TileController targetTile;
	private List<GameObject> neighbours;
	private ActionType performing = ActionType.None;
	private IEnumerator actualStateCoroutine;

	void Awake () {
		// Defining this object as a singleton.
		if (instance == null)
			instance = this;
		else
			Destroy (this);

		actualState = ActionState.WaitingActionCall;
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

		StopCoroutine (actualStateCoroutine);

		performing = ActionType.None;
		actualState = ActionState.WaitingActionCall;
		
		selectedTile = null;
		targetTile = null;

		GameManager.instance.ClearSelections ();
		GameManager.instance.panelControler.canChangePlayerText = true;
		GameManager.instance.panelControler.HideCancelButton ();
	}

	public void ActionMove()
	{
		performing = ActionType.Move;
		if(CanPerformAction (movingCost, true))
		{
			selectedTile.Unselect ();
			GameManager.instance.panelControler.canChangePlayerText = false;
			actualStateCoroutine = PerformingActionState (false);
			StartCoroutine (actualStateCoroutine);
		}
	}

	public void ActionDuplicate()
	{
		performing = ActionType.Duplicate;
		if(CanPerformAction (duplicateCost, false))
		{
			selectedTile.Unselect ();
			GameManager.instance.panelControler.canChangePlayerText = false;
			actualStateCoroutine = PerformingActionState (false);
			StartCoroutine (actualStateCoroutine);
		}
	}

	private void MoveCreature()
	{
		if (!selectedTile.creature.moved) {
			selectedTile.creature.moved = true;
		} else {
			selectedTile.creature.moved = false;
			selectedTile.creature.isTired = true;
			GameManager.instance.tiredCreatures.Add (selectedTile.creature);
		}

		// Spend money and move the creature.
		GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (movingCost);
		selectedTile.creature.MoveToTarget (targetTile.spawnPoint);

		// Remove the creature from source tile.
		targetTile.creature = selectedTile.creature;
		GameManager.instance.player [GameManager.instance.activePlayerIndex].LeaveTile(selectedTile);

		// Assign the creature to the new tile.
		selectedTile.creature = null;
		GameManager.instance.player [GameManager.instance.activePlayerIndex].ControllNewTile (targetTile);
	}

	private void DuplicateCreature()
	{
		CreatureController newCreature;

		selectedTile.creature.isTired = true;
		GameManager.instance.tiredCreatures.Add (selectedTile.creature);

		// Spend money and duplicate the creature.
		GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (duplicateCost);
		selectedTile.creature.DuplicateToTarget (targetTile.spawnPoint, out newCreature);

		// Assign new creature to the new tile.
		targetTile.creature = newCreature;
		GameManager.instance.player [GameManager.instance.activePlayerIndex].
		controlledTiles.Add (TileController.GetStringID (targetTile.xIndex, targetTile.zIndex));
	}

	private IEnumerator PerformingActionState(bool creatureTarget)
	{
		GameManager.instance.panelControler.ShowCancelButton ();
		actualState = ActionState.PerformingAction;

		HighlightNeighbours (creatureTarget);

		while (targetTile == null)
		{
			yield return null;
		}

		GameManager.instance.panelControler.HideCancelButton ();

		switch (performing){
		case ActionType.Move:
			MoveCreature ();
			break;
		case ActionType.Duplicate:
			DuplicateCreature ();
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

	private bool CanPerformAction(int actionCost, bool movingAction)
	{
		GameObject tile;
		if(GameManager.instance.player[GameManager.instance.activePlayerIndex].coinCount < actionCost){
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
					if(!movingAction && selectedTile.creature.moved)
					{
						GameManager.instance.panelControler.ShowMessage (3f, MessageType.CreatureTooTired);
						return false;
					}
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
