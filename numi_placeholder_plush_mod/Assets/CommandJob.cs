using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ParticleSystemJobs;

[BurstCompile]
internal struct CommandJob : IJobParticleSystemParallelFor
{
	public Matrix4x4 transform;

	public NativeArray<RaycastCommand> raycasts;

	[ReadOnly]
	public NativeArray<RaycastHit> lastFrameHits;

	public int layerMask;

	public float deltaTime;

	public bool worldSpace;

	public Vector3 center;

	public void Execute(ParticleSystemJobData jobData, int i)
	{
		ParticleSystemNativeArray4 customData = jobData.customData1;
		Vector4 vector = customData[i];
		if (worldSpace && vector == Vector4.zero)
		{
			vector = center;
		}
		int index = (int)vector.w;
		Vector3 point = lastFrameHits[index].point;
		if (point.x != 0f && point.y != 0f && point.z != 0f)
		{
			NativeArray<float> aliveTimePercent = jobData.aliveTimePercent;
			aliveTimePercent[i] = 100f;
		}
		Vector3 vector2 = transform.MultiplyPoint(vector);
		Vector3 vector3 = transform.MultiplyPoint(jobData.positions[i]);
		raycasts[i] = new RaycastCommand(vector2, math.normalize(vector3 - vector2), math.length(vector3 - vector2), layerMask);
		customData[i] = new float4(jobData.positions[i], i);
	}
}
