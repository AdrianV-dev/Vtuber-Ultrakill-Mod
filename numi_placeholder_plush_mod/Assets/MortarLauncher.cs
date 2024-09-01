using UnityEngine;

public class MortarLauncher : MonoBehaviour
{
	private EnemyIdentifier eid;

	public Transform shootPoint;

	public Projectile mortar;

	private float cooldown = 1f;

	public float firingDelay;

	public float firstFireDelay = 1f;

	public float projectileForce;

	public UltrakillEvent onFire;

	private Animator anim;

	private int difficulty;

	private float difficultySpeedModifier = 1f;

	private void Start()
	{
		eid = GetComponentInParent<EnemyIdentifier>();
		anim = GetComponent<Animator>();
		cooldown = firstFireDelay;
		if (eid.difficultyOverride >= 0)
		{
			difficulty = eid.difficultyOverride;
		}
		else
		{
			difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
		}
		if (difficulty == 1)
		{
			difficultySpeedModifier = 0.8f;
		}
		else if (difficulty == 0)
		{
			difficultySpeedModifier = 0.6f;
		}
	}

	private void Update()
	{
		cooldown = Mathf.MoveTowards(cooldown, 0f, Time.deltaTime * eid.totalSpeedModifier * difficultySpeedModifier);
		if (cooldown == 0f && eid.target != null)
		{
			cooldown = firingDelay;
			ShootHoming();
			onFire?.Invoke();
		}
	}

	public void ShootHoming()
	{
		if (eid.target != null)
		{
			Projectile projectile = Object.Instantiate(mortar, shootPoint.position, shootPoint.rotation);
			projectile.target = eid.target;
			projectile.GetComponent<Rigidbody>().velocity = shootPoint.forward * projectileForce;
			projectile.damage *= eid.totalDamageModifier;
			projectile.safeEnemyType = eid.enemyType;
			projectile.turningSpeedMultiplier *= difficultySpeedModifier;
			projectile.gameObject.SetActive(value: true);
			if ((bool)anim)
			{
				anim.Play("Shoot", 0, 0f);
			}
		}
	}

	public void ChangeFiringDelay(float target)
	{
		firingDelay = target;
		if (cooldown > firingDelay)
		{
			cooldown = firingDelay;
		}
	}
}
