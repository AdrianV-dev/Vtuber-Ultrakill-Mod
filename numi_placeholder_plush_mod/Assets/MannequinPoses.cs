using UnityEngine;

public class MannequinPoses : MonoBehaviour
{
	private Animator anim;

	private int poseNum;

	[SerializeField]
	private bool altar;

	private void Start()
	{
		anim = GetComponent<Animator>();
		if (altar)
		{
			anim.Play("Altar");
			base.enabled = false;
		}
		else
		{
			RandomPose();
			SlowUpdate();
		}
	}

	private void SlowUpdate()
	{
		Invoke("SlowUpdate", Random.Range(1f, 3f));
		if (Vector3.Dot(MonoSingleton<CameraController>.Instance.transform.forward, base.transform.position - MonoSingleton<CameraController>.Instance.transform.position) < -0.33f)
		{
			RandomPose();
		}
	}

	private void RandomPose()
	{
		ChangePose(Random.Range(1, 10));
	}

	private void ChangePose(int num)
	{
		poseNum = num;
		anim.SetInteger("TargetPose", poseNum);
	}
}
