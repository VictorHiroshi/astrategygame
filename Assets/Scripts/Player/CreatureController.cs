﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreatureController : MonoBehaviour {
	public Animator animatorController;
	public Slider healthSlider;
	public Image fillSliderImage;
	public int belongsToPlayer;
	public float speed = 0.1f;

	[HideInInspector] public bool moved;
	[HideInInspector] public bool isTired;

	private int health = 10;
	private bool finishedInteractionAnimation = false;
	private GameObject creatureModel;
	private ParticleSystem explosionParticles;
	private ParticleSystem rocksParticles;
	private CreatureController enemy;

	void Awake()
	{
		moved = false;
		isTired = false;
		healthSlider.value = health;
		creatureModel = gameObject;
		animatorController.SetTrigger ("IsIdle");

		explosionParticles = Instantiate (GameManager.instance.boardScript.explosionParticles, transform.position, Quaternion.identity, transform);
		rocksParticles = Instantiate (GameManager.instance.boardScript.rockExplorationParticles, transform.position, Quaternion.identity, transform);
	}

	public void FinishedAnimation()
	{
		finishedInteractionAnimation = true;
	}

	public void TakeDamage(int damage)
	{
		health -= damage;
		if(health<=0)
		{
			health = 0;
			//Die ();
		}
		healthSlider.value = health;
	}

	public void ChangeTeam(int newPlayerIndex)
	{
		
		belongsToPlayer = newPlayerIndex;
		fillSliderImage.color = GameManager.instance.playersColors[belongsToPlayer];
	}

	public void MoveToTarget (Transform target)
	{
		animatorController.SetTrigger ("Moves");
		StartCoroutine (Moving (target));
	}

	public void DuplicateToTarget (Transform target, out CreatureController newCreature)
	{
		Vector3 newPosition = transform.position + (0.3f * (target.position - transform.position));
		GameObject instance = Instantiate (creatureModel, newPosition, Quaternion.identity);
		newCreature = instance.GetComponent <CreatureController> ();

		newCreature.ChangeTeam (belongsToPlayer);

		newCreature.MoveToTarget (target);
	}

	public void Attack(Transform origin, Transform target, ref CreatureController enemy)
	{
		this.enemy = enemy;

		enemy.transform.rotation = Quaternion.LookRotation (origin.position - target.position);

		Vector3 walkingPosition = target.position - ((target.position - origin.position) / GameManager.instance.boardScript.tiles.tileSideSize);

		StartCoroutine (Attacking (origin, target, walkingPosition));

	}

	public void Explore()
	{
		explosionParticles.Play ();
		rocksParticles.Play ();
	}

	public void PlayExplosionParticles()
	{
		explosionParticles.Play ();
	}

	public void Die (CreatureController killer)
	{
		// TODO: Play animation

		killer.FinishedAnimation ();
		Destroy (gameObject);
	}

	private IEnumerator Moving(Transform target)
	{
		transform.rotation = Quaternion.LookRotation (target.position - transform.position);
		while(transform.position!=target.position){
			float step = speed * Time.deltaTime;
			transform.position = Vector3.MoveTowards (transform.position, target.position, step);
			yield return null;
		}
		animatorController.SetTrigger ("IsIdle");


		transform.rotation = Quaternion.identity;
		ActionsManager.instance.FinishAction ();

		/*explosionParticles.transform.position = transform.position;
		rocksParticles.transform.position = transform.position;*/
	}

	private IEnumerator Attacking(Transform origin, Transform target, Vector3 midTarget)
	{
		animatorController.SetTrigger ("Moves");

		transform.rotation = Quaternion.LookRotation (midTarget - transform.position);
		while((transform.position - midTarget).sqrMagnitude > 0.15)
		{
			transform.position = Vector3.Lerp (transform.position, midTarget, Time.deltaTime * speed);
			yield return null;
		}
			
		animatorController.SetTrigger ("Attacks");
		finishedInteractionAnimation = false;
		while(!finishedInteractionAnimation)
		{
			yield return null;
		}

		animatorController.SetTrigger ("IsIdle");
		finishedInteractionAnimation = false;
		enemy.Die (this);
		while(!finishedInteractionAnimation)
		{
			yield return null;
		}

		MoveToTarget (target);
	}
}
