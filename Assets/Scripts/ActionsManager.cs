using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ActionState {WaitingActionCall, PerformingAction, WaitingActionFinish};
public enum ActionType {Move, Duplicate, LExploit, HExploit, Attack, Convert, Oppress, Defend, None};

public class ActionsManager : MonoBehaviour {

	public static ActionsManager instance;

	public BoardManager boardManager;
	public int maxHealth = 10;
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
		if(CanPerformAction (movingCost, true, true) && !IsDefending ())
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
		if(CanPerformAction (duplicateCost, false, true) && !IsDefending ())
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
		if(CanPerformAction (exploitCost, false, false) && !IsDefending ())
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
		if(CanPerformAction (exploitCost, false, false) && !IsDefending ())
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
		if(CanPerformAction (attackCost, false, true) && !IsDefending ())
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
		if(CanPerformAction (defenseCost, true, false))
		{
			selectedTile.Unselect ();

			if(IsDefending ())
			{
				selectedTile.creature.TurnDefense (false);
			}
			else{
				selectedTile.creature.TurnDefense (true);
				GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (defenseCost);
			}

			SetCreatureToTired ();
		}

		performing = ActionType.None;
	}

	public void ActionConvert()
	{
		if(CanPerformAction (convertingCost, false, true) && !IsDefending ())
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
		if(CanPerformAction (convertingCost, false, true) && !IsDefending ())
		{
			performing = ActionType.Oppress;
			selectedTile.Unselect ();
			GameManager.instance.panelControler.canChangePlayerText = false;
			actualStateCoroutine = PerformingActionState (true);
			StartCoroutine (actualStateCoroutine);
		}
	}

	public void EnemyLostControlOverTarget()
	{
		GameManager.instance.player [targetTile.creature.belongsToPlayer.playerNumber].LeaveTile (targetTile);
	}

	public void ActiveControllNewTile ()
	{
		GameManager.instance.player [GameManager.instance.activePlayerIndex].ControllNewTile (targetTile);
	}	

	private void MoveCreature()
	{

		// Spend money and move the creature.
		GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (movingCost);
		selectedTile.creature.MoveToTarget (targetTile.spawnPoint);

		// Remove the creature from source tile.
		targetTile.creature = selectedTile.creature;
		GameManager.instance.player [GameManager.instance.activePlayerIndex].LeaveTile(selectedTile);

		// Assign the creature to the new tile.
		selectedTile.creature = null;
		selectedTile = targetTile;
		ActiveControllNewTile ();
	}

	private void DuplicateCreature()
	{
		CreatureController newCreature;

		// Spend money and duplicate the creature.
		GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (duplicateCost);
		selectedTile.creature.DuplicateToTarget (targetTile.spawnPoint, out newCreature);

		// Assign new creature to the new tile.
		targetTile.creature = newCreature;
		ActiveControllNewTile ();
	}

	private void Attack ()
	{
		// Spend money.
		GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (attackCost);

		selectedTile.creature.Attack (selectedTile.spawnPoint, targetTile.spawnPoint, targetTile.creature);

		if(targetTile.creature.DiesWhenTakeDamage ())
		{
			// Informs GameManager that the enemy don't control the target anymore
			EnemyLostControlOverTarget ();

			// Remove the creature from source tile.
			targetTile.creature = selectedTile.creature;
			GameManager.instance.player [GameManager.instance.activePlayerIndex].LeaveTile (selectedTile);

			// Assign the creature to the new tile.
			/*selectedTile.creature = null;*/
			ActiveControllNewTile ();

			selectedTile = targetTile;
		}
	}

	private void ConvertCreature ()
	{
		// Spend money.
		GameManager.instance.player [GameManager.instance.activePlayerIndex].Spend (convertingCost);

		selectedTile.creature.Convert (selectedTile.spawnPoint, targetTile.spawnPoint, targetTile.creature);

	}

	private void OppressCreature ()
	{
		
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
				MoveCreature ();
				break;
			case ActionType.Duplicate:
				DuplicateCreature ();
				break;
			case ActionType.Attack:
				Attack ();
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

		SetCreatureToTired ();

		targetTile = null;
		performing = ActionType.None;


		GameManager.instance.panelControler.EnableAllButtons ();
		selectedTile.Select ();
	}

	private bool HighlightNeighbours(bool withCreatues)
	{
		TileController neighbourTileController;
		bool hasHighlight = false;


		foreach(GameObject neighbour in neighbours)
		{
			neighbourTileController = neighbour.GetComponent <TileController> ();

			if(neighbourTileController!=null)
			{
				if ((neighbourTileController.creature!=null) == withCreatues)
				{
					if(withCreatues && neighbourTileController.creature.belongsToPlayer.playerNumber == GameManager.instance.activePlayerIndex){
						continue;
					}
					else {
						neighbourTileController.Highlight ();
						hasHighlight = true;
					}
				}
			}
		}

		return hasHighlight;
	}

	private bool CanPerformAction(int actionCost, bool movingAction, bool needNeighbours)
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
				if(selectedTile.creature.belongsToPlayer.playerNumber != GameManager.instance.activePlayerIndex)
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
					if(needNeighbours && !boardManager.hasNeighbours (selectedTile.xIndex, selectedTile.zIndex, out neighbours))
					{	
						GameManager.instance.panelControler.ShowMessage (3f, MessageType.CantPerformAction);
						return false;
					}

				}
			}
		}
		return true;

	}

	private bool IsDefending()
	{
		if(selectedTile.creature.IsDefending () && performing != ActionType.Defend)
		{
			GameManager.instance.panelControler.ShowMessage (3f, MessageType.CreatureDefending);
		}
		return selectedTile.creature.IsDefending ();
	}

	private void SetCreatureToTired()
	{
		if(performing == ActionType.Defend || performing == ActionType.Move)
		{
			if (!selectedTile.creature.moved) {
				selectedTile.creature.moved = true;
				GameManager.instance.tiredCreatures.Add (selectedTile.creature);
			} else {
				selectedTile.creature.moved = false;
				selectedTile.creature.isTired = true;
				selectedTile.creature.animatorController.SetTrigger ("IsTired");
			}
		}
		else 
		{
			selectedTile.creature.isTired = true;
			GameManager.instance.tiredCreatures.Add (selectedTile.creature);
			selectedTile.creature.animatorController.SetTrigger ("IsTired");
		}


	}
}
