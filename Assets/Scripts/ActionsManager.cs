using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ActionState {WaitingActionCall, PerformingAction, WaitingActionFinish};
public enum ActionType {Move, Duplicate, LExploit, HExploit, Attack, Convert, Oppress, Defend, None};

public class ActionsManager : MonoBehaviour {

	public static ActionsManager instance;

	public int movingCost = 1;
	public int duplicateCost = 2;
	public int exploitCost = 1;
	public int attackCost = 2;
	public int defenseCost = 1;
	public int convertingCost = 2;
	public int oppressingCost = 3;
	public int healingCost = 1;
	public int defendingDamage = 5;
	public int lightExploitProfit = 3;
	public int heavyExploitProfit = 6;

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

		targetTile = null;

		GameManager.instance.ClearSelections ();
		GameManager.instance.panelControler.canChangePlayerText = true;
		GameManager.instance.panelControler.HideCancelButton ();

		selectedTile.Select ();

		GameManager.instance.panelControler.EnableAllButtons ();
	}

	public void ActionMove()
	{
		if(CanPerformAction (actionCost: movingCost, halfAction: true, needNeighbours: true))
		{
			performing = ActionType.Move;
			selectedTile.Unselect ();
			GameManager.instance.panelControler.canChangePlayerText = false;
			actualStateCoroutine = PerformingActionState (false);
			StartCoroutine (actualStateCoroutine);
		}
	}

	public void ActionDuplicate()
	{
		if(CanPerformAction (actionCost: duplicateCost, halfAction: false,  needNeighbours: true))
		{
			performing = ActionType.Duplicate;
			selectedTile.Unselect ();
			GameManager.instance.panelControler.canChangePlayerText = false;
			actualStateCoroutine = PerformingActionState (false);
			StartCoroutine (actualStateCoroutine);
		}
	}

	public void ActionLightExploit()
	{
		if(CanPerformAction (actionCost: exploitCost, halfAction: false,  needNeighbours: false))
		{
			if(selectedTile.resource==null)
			{
				GameManager.instance.panelControler.ShowMessage (3f, MessageType.NoStone);
			}
			else
			{
				StartCoroutine (WaitingActionFinishState ());
				selectedTile.creature.animatorController.SetTrigger ("Explores");
				GameManager.instance.player [GameManager.instance.activePlayerIndex].Receive (lightExploitProfit);
			}
		}
	}

	public void ActionHeavyExploit()
	{
		if(CanPerformAction (actionCost: exploitCost, halfAction: false,  needNeighbours: false))
		{
			if(selectedTile.resource==null)
			{
				GameManager.instance.panelControler.ShowMessage (3f, MessageType.NoStone);
			}
			else
			{
				StartCoroutine (WaitingActionFinishState ());
				selectedTile.creature.animatorController.SetTrigger ("Explores");
				GameManager.instance.player [GameManager.instance.activePlayerIndex].Receive (heavyExploitProfit);
				selectedTile.CheckIfResourceExhausted ();
			}
		}
	}

	public void ActionAttack()
	{
		if(CanPerformAction (actionCost: attackCost, halfAction: false,  needNeighbours: true))
		{
			performing = ActionType.Attack;
			selectedTile.Unselect ();
			GameManager.instance.panelControler.canChangePlayerText = false;
			actualStateCoroutine = PerformingActionState (true);
			StartCoroutine (actualStateCoroutine);
		}
	}

	public void ActionDefense()
	{
		performing = ActionType.Defend;

		if(CanPerformAction (actionCost: defenseCost, halfAction: true, needNeighbours: false))
		{
			selectedTile.Unselect ();

			if(selectedTile.creature.IsDefending ())
			{
				selectedTile.creature.TurnDefense (false);
			}
			else{
				selectedTile.creature.TurnDefense (true);
				GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (defenseCost);
			}
				
		}

		performing = ActionType.None;
	}

	public void ActionConvert()
	{
		if(CanPerformAction (actionCost: convertingCost, halfAction: false, needNeighbours: true))
		{
			performing = ActionType.Convert;
			selectedTile.Unselect ();
			GameManager.instance.panelControler.canChangePlayerText = false;
			actualStateCoroutine = PerformingActionState (true);
			StartCoroutine (actualStateCoroutine);
		}
	}

	public void ActionOppress()
	{
		if(CanPerformAction (actionCost: oppressingCost, halfAction: false, needNeighbours: true))
		{
			performing = ActionType.Oppress;
			selectedTile.Unselect ();
			GameManager.instance.panelControler.canChangePlayerText = false;
			actualStateCoroutine = PerformingActionState (true);
			StartCoroutine (actualStateCoroutine);
		}
	}

/*	public void EnemyLostControlOverTarget()
	{
		GameManager.instance.player [targetTile.creature.belongsToPlayer.playerNumber].LeaveTile (targetTile);
	}

	public void ActivePlayerControllNewTile ()
	{
		GameManager.instance.player [GameManager.instance.activePlayerIndex].ControllNewTile (targetTile);
	}*/

	private IEnumerator MoveCreature()
	{

		// Spend money and move the creature.
		GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (movingCost);
		yield return StartCoroutine (selectedTile.creature.MoveToTarget (targetTile));
	}

	private IEnumerator DuplicateCreature()
	{
		CreatureController newCreature;

		// Spend money and duplicate the creature.
		GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (duplicateCost);

		newCreature = selectedTile.creature.DuplicateToTarget (targetTile);
		yield return StartCoroutine (newCreature.MoveToTarget (targetTile));

	}

	private IEnumerator Attack ()
	{
		// Spend money.
		GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (attackCost);

		yield return StartCoroutine (selectedTile.creature.Attack (selectedTile, targetTile));

	}

	private void ConvertCreature ()
	{
		// Spend money.
		GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (convertingCost);

		selectedTile.creature.Convert (selectedTile.spawnPoint, targetTile.spawnPoint, targetTile.creature);

	}

	private void OppressCreature ()
	{
		GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (oppressingCost);

		selectedTile.creature.Oppress (selectedTile.spawnPoint, targetTile.spawnPoint, targetTile.creature);
	}


	private IEnumerator PerformingActionState(bool creatureTarget)
	{
		GameManager.instance.panelControler.ShowCancelButton ();
		actualState = ActionState.PerformingAction;

		GameManager.instance.panelControler.DisableAllButtons ();

		if(!HighlightNeighbours (creatureTarget))
		{
			if(performing == ActionType.Attack || performing == ActionType.Convert || performing == ActionType.Oppress)
				GameManager.instance.panelControler.ShowMessage (3f, MessageType.NoEnemy);
			else
				GameManager.instance.panelControler.ShowMessage (3f, MessageType.CantPerformAction);

			GameManager.instance.panelControler.HideCancelButton ();
			GameManager.instance.panelControler.EnableAllButtons ();

			performing = ActionType.None;
			actualState = ActionState.WaitingActionCall;
		}
		else
		{
			while (targetTile == null) {
				yield return null;
			}

			GameManager.instance.panelControler.HideCancelButton ();

			switch (performing) {
			case ActionType.Move:
				StartCoroutine(MoveCreature ());
				break;
			case ActionType.Duplicate:
				StartCoroutine(DuplicateCreature ());
				break;
			case ActionType.Attack:
				StartCoroutine(Attack ());
				break;
			case ActionType.Convert:
				ConvertCreature ();
				break;
			case ActionType.Oppress:
				OppressCreature ();
				break;
			}

			StartCoroutine (WaitingActionFinishState ());
		}
	}

	private IEnumerator WaitingActionFinishState()
	{
		actualState = ActionState.WaitingActionFinish;

		GameManager.instance.ClearSelections ();

		while (actualState != ActionState.WaitingActionCall)
		{
			yield return null;
		}

		targetTile = null;
		performing = ActionType.None;


		GameManager.instance.panelControler.EnableAllButtons ();
		selectedTile.Select ();
	}


	private bool HighlightNeighbours(bool withCreatues)
	{
		TileController neighbourTileController;
		bool hasHighlight = false;
		MessageType message = MessageType.CantPerformAction;

		foreach(GameObject neighbour in neighbours)
		{
			neighbourTileController = neighbour.GetComponent <TileController> ();

			if(neighbourTileController!=null)
			{
				if (withCreatues && neighbourTileController.creature!=null)
				{
					if(CreatureIsOppressed ())
					{
						if(neighbourTileController.creature.belongsToPlayer.playerNumber != selectedTile.creature.belongsToPlayer.playerNumber
							&& selectedTile.creature.oppressedByPlayer.playerNumber == GameManager.instance.activePlayerIndex
							&& neighbourTileController.creature.belongsToPlayer.playerNumber != GameManager.instance.activePlayerIndex)
						{
							neighbourTileController.Highlight ();
							hasHighlight = true;
						}
					}
					else
					{
						if(neighbourTileController.creature.belongsToPlayer.playerNumber == GameManager.instance.activePlayerIndex)
						{
							if (performing == ActionType.Convert) {
								if (neighbourTileController.creature.influencedByPlayer != null) {

									neighbourTileController.Highlight ();
									hasHighlight = true;
								}
							} 
							else if (performing == ActionType.Oppress) 
							{
								if (neighbourTileController.creature.oppressedByPlayer != null) 
								{
									neighbourTileController.Highlight ();
									hasHighlight = true;
								}
							}
						}
						else{
							neighbourTileController.Highlight ();
							hasHighlight = true;
						}
					}
				}
				else if(!withCreatues && neighbourTileController.creature==null)
				{
					if(CreatureIsOppressed ())
					{
						if(CreatureIsOppressedByActivePlayer(ref message) && performing!= ActionType.Duplicate)
						{
							neighbourTileController.Highlight ();
							hasHighlight = true;
						}
					} 
					else
					{
						neighbourTileController.Highlight ();
						hasHighlight = true;
					}
				}
			}
		}

		return hasHighlight;
	}

	private bool CanPerformAction(int actionCost, bool halfAction, bool needNeighbours)
	{
		MessageType message = MessageType.None;
		bool canPerformAction = HasSelectedTile (ref message) && HasEnoughtMoney (actionCost, ref message);

		if(canPerformAction)
		// Has a selected tile and enought money.
		{
			canPerformAction = HasSelectedCreature (ref message);

			if(canPerformAction)
			// Has a creature in selected tile.
			{
				if(CreatureIsOppressed ())
				{
					canPerformAction = CreatureIsOppressedByActivePlayer (ref message);
				}
				else
				{
					canPerformAction = CreatureBelongsToActivePlayer (ref message);
				}

				if (canPerformAction) 
				// Creature can be controlled by active player.
				{
					canPerformAction = !CreatureIsTired (halfAction, ref message);
				}

				if(canPerformAction)
				// Creature is not tired.
				{
					canPerformAction = !CreatureIsDefending (ref message);
				}

				if(canPerformAction && needNeighbours)
				// Creature is not defending.
				{
					canPerformAction = CreatureHasNeighbours (ref message);
				}
			}
		}
		if(!canPerformAction)
		{
			GameManager.instance.panelControler.ShowMessage (3f, message);
		}



		return canPerformAction;
	}

	private bool HasSelectedTile(ref MessageType message)
	{
		GameObject tile;
		bool check = GameManager.instance.boardScript.SelectedTile (out tile);

		if (!check)
		{
			if(message == MessageType.None)
			{
				message = MessageType.SelectTileFirst;
			}
			return false;
		}
		selectedTile = tile.GetComponent <TileController> ();

		return true;
	}

	private bool HasEnoughtMoney(int actionCost, ref MessageType message)
	{
		bool check = GameManager.instance.player [GameManager.instance.activePlayerIndex].coinCount >= actionCost;

		if(!check && message == MessageType.None)
		{
			message = MessageType.NotEnoughtMoney;
		}

		return check;
	}

	private bool HasSelectedCreature(ref MessageType message)
	{
		bool check = selectedTile.creature != null;

		if(!check && message == MessageType.None)
		{
			message = MessageType.NoCreatureThere;
		}

		return check;
	}

	private bool CreatureBelongsToActivePlayer(ref MessageType message)
	{
		bool check = selectedTile.creature.belongsToPlayer.playerNumber == GameManager.instance.activePlayerIndex;

		if(!check && message == MessageType.None)
		{
			message = MessageType.NotYourCreature;
		}

		return check;
	}

	private bool CreatureIsOppressed()
	{
		return selectedTile.creature.oppressedByPlayer != null;
	}

	private bool CreatureIsOppressedByActivePlayer(ref MessageType message)
	{
		bool check = selectedTile.creature.oppressedByPlayer.playerNumber == GameManager.instance.activePlayerIndex;

		if(!check && message == MessageType.None)
		{
			message = MessageType.NotYourCreature;
		}

		return check;
	}

	private bool CreatureIsTired(bool halfAction, ref MessageType message)
	{
		bool check = false;

		if (selectedTile.creature.isTired)
			check = true;
		
		if (!check && !halfAction)
			check = selectedTile.creature.moved;

		if(check && message == MessageType.None)
		{
			message = MessageType.CreatureTooTired;
		}

		return check;
	}

	private bool CreatureHasNeighbours(ref MessageType message)
	{
		bool check = GameManager.instance.boardScript.hasNeighbours (selectedTile.xIndex, selectedTile.zIndex, out neighbours);

		if(!check && message == MessageType.None)
		{
			message = MessageType.CantPerformAction;
		}

		return check;
	}

	private bool CreatureIsDefending(ref MessageType message)
	{
		bool check = selectedTile.creature.IsDefending ();

		if (performing == ActionType.Defend)
			check = false;
		
		if(check && message == MessageType.None && performing != ActionType.Defend)
		{
			message = MessageType.CreatureDefending;
		}
			
		return check;
	}
}
