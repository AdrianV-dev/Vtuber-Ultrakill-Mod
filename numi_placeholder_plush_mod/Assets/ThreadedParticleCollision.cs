using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.ParticleSystemJobs;

public class ThreadedParticleCollision : MonoBehaviour
{
	public ParticleSystem particles;

	public Bloodsplatter bloodsplatter;

	public NativeArray<RaycastCommand> raycasts;

	public NativeArray<RaycastHit> results;

	private CommandJob commandJob;

	private JobHandle handle;

	private List<Vector4> customData = new List<Vector4>();

	private BloodsplatterManager bsm;

	private static Matrix4x4 identityMatrix = Matrix4x4.identity;

	public event Action<NativeSlice<RaycastHit>> collisionEvent;

	private void Awake()
	{
		commandJob.layerMask = LayerMaskDefaults.Get(LMD.Environment);
		if (StockMapInfo.Instance.continuousGibCollisions)
		{
			commandJob.layerMask = (commandJob.layerMask |= 16);
		}
		particles.SetCustomParticleData(customData, ParticleSystemCustomData.Custom1);
		results = new NativeArray<RaycastHit>(particles.main.maxParticles, Allocator.Persistent);
		raycasts = new NativeArray<RaycastCommand>(particles.main.maxParticles, Allocator.Persistent);
		commandJob.raycasts = raycasts;
		commandJob.lastFrameHits = results;
	}

	private void OnEnable()
	{
		bsm = MonoSingleton<BloodsplatterManager>.Instance;
		MonoSingleton<BloodsplatterManager>.Instance.ParticleCollisionStep += Step;
	}

	private void OnDisable()
	{
		if ((bool)MonoSingleton<BloodsplatterManager>.Instance)
		{
			MonoSingleton<BloodsplatterManager>.Instance.ParticleCollisionStep -= Step;
		}
	}

	private unsafe void Step(float dt)
	{
		if (!handle.IsCompleted)
		{
			return;
		}
		handle.Complete();
		if (results.IsCreated)
		{
			particles.GetCustomParticleData(customData, ParticleSystemCustomData.Custom1);
			int particleCount = particles.particleCount;
			RaycastHit* unsafeBufferPointerWithoutChecks = (RaycastHit*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results);
			for (int i = 0; i < particleCount; i++)
			{
				RaycastHit source = unsafeBufferPointerWithoutChecks[(int)customData[i].w];
				if (source.distance != 0f)
				{
					bloodsplatter.CreateBloodstain(ref Unsafe.AsRef(in source), bsm);
				}
			}
		}
		Transform transform = particles.transform;
		if (transform.hasChanged)
		{
			transform.hasChanged = false;
			if (particles.main.simulationSpace == ParticleSystemSimulationSpace.Local)
			{
				commandJob.transform = transform.localToWorldMatrix;
				commandJob.worldSpace = false;
			}
			else
			{
				commandJob.transform = identityMatrix;
				commandJob.worldSpace = true;
				commandJob.center = transform.position;
			}
		}
		JobHandle dependsOn = commandJob.Schedule(particles, 128);
		handle = RaycastCommand.ScheduleBatch(raycasts, results, 128, dependsOn);
	}

	private void OnDestroy()
	{
		raycasts.Dispose(handle);
		results.Dispose(handle);
	}
}
