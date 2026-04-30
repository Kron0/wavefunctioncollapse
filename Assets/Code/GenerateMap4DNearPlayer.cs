using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(MapBehaviour4D))]
[RequireComponent(typeof(TesseractProjection))]
public class GenerateMap4DNearPlayer : MonoBehaviour {
	private MapBehaviour4D mapBehaviour;
	private InfiniteMap4D map;
	private TesseractProjection projection;

	public Transform Target;

	public int ChunkSize = 4;

	public float Range = 30;

	private Vector3 targetPosition;
	private float targetW;

	private HashSet<Vector4Int> generatedChunks;

	private Thread thread;

	private readonly List<IMapGenerationCallbackReceiver4D> callbacks4D = new List<IMapGenerationCallbackReceiver4D>();
	private readonly ConcurrentQueue<Vector4Int> completedChunks4D = new ConcurrentQueue<Vector4Int>();

	public Vector4Int[] GeneratedChunks {
		get {
			lock (this.generatedChunks) {
				var result = new Vector4Int[this.generatedChunks.Count];
				this.generatedChunks.CopyTo(result);
				return result;
			}
		}
	}

	public void RegisterCallback4D(IMapGenerationCallbackReceiver4D receiver) {
		if (!this.callbacks4D.Contains(receiver)) {
			this.callbacks4D.Add(receiver);
		}
	}

	public void UnregisterCallback4D(IMapGenerationCallbackReceiver4D receiver) {
		this.callbacks4D.Remove(receiver);
	}

	void Start() {
		this.generatedChunks = new HashSet<Vector4Int>();
		this.mapBehaviour = this.GetComponent<MapBehaviour4D>();
		this.projection = this.GetComponent<TesseractProjection>();
		this.mapBehaviour.Initialize();
		this.map = this.mapBehaviour.Map;

		var controller = this.Target.GetComponent<FourDController>();
		if (controller != null) {
			this.mapBehaviour.SetPlayer(controller);
		}

		this.generate();
		this.mapBehaviour.BuildAllSlots();

		this.thread = new Thread(this.generatorThread);
		this.thread.Start();
	}

	public void OnDisable() {
		if (this.thread != null) {
			this.thread.Abort();
		}
	}

	private void generate() {
		float chunkSize = AbstractMap4D.BLOCK_SIZE * this.ChunkSize;

		float targetX = this.targetPosition.x + AbstractMap4D.BLOCK_SIZE / 2;
		float targetZ = this.targetPosition.z + AbstractMap4D.BLOCK_SIZE / 2;

		int chunkX = Mathf.FloorToInt(targetX / chunkSize);
		int chunkZ = Mathf.FloorToInt(targetZ / chunkSize);
		int chunkW = Mathf.FloorToInt(this.targetW / this.ChunkSize);

		Vector4Int closestMissingChunk = Vector4Int.zero;
		float closestDistance = this.Range;
		bool any = false;

		int wRange = this.projection != null ? this.projection.WRenderRange : 2;

		for (int x = Mathf.FloorToInt(chunkX - this.Range / chunkSize); x < chunkX + this.Range / chunkSize; x++) {
			for (int z = Mathf.FloorToInt(chunkZ - this.Range / chunkSize); z < chunkZ + this.Range / chunkSize; z++) {
				for (int w = chunkW - wRange; w <= chunkW + wRange; w++) {
					var chunk = new Vector4Int(x, 0, z, w);
					if (this.generatedChunks.Contains(chunk)) {
						continue;
					}
					float cx = (chunk.x + 0.5f) * chunkSize - AbstractMap4D.BLOCK_SIZE / 2;
					float cz = (chunk.z + 0.5f) * chunkSize - AbstractMap4D.BLOCK_SIZE / 2;
					float cw = (chunk.w + 0.5f) * this.ChunkSize;
					float dx = cx - this.targetPosition.x;
					float dz = cz - this.targetPosition.z;
					float dw = (cw - this.targetW) * 2f;
					float distance = Mathf.Sqrt(dx * dx + dz * dz + dw * dw);

					if (distance < closestDistance) {
						closestMissingChunk = chunk;
						any = true;
						closestDistance = distance;
					}
				}
			}
		}

		if (any) {
			this.createChunk(closestMissingChunk);
		}
	}

	private void createChunk(Vector4Int chunkAddress) {
		var start = new Vector4Int(
			chunkAddress.x * this.ChunkSize,
			0,
			chunkAddress.z * this.ChunkSize,
			chunkAddress.w * this.ChunkSize);
		this.map.rangeLimitCenter = start + new Vector4Int(this.ChunkSize / 2, 0, this.ChunkSize / 2, this.ChunkSize / 2);
		this.map.RangeLimit = this.ChunkSize + 20;
		this.map.Collapse(start, new Vector4Int(this.ChunkSize, this.map.Height, this.ChunkSize, this.ChunkSize));
		lock (this.generatedChunks) {
			this.generatedChunks.Add(chunkAddress);
		}
		this.completedChunks4D.Enqueue(chunkAddress);
	}

	private void generatorThread() {
		try {
			while (true) {
				this.generate();
				Thread.Sleep(50);
			}
		} catch (Exception exception) {
			if (exception is ThreadAbortException) {
				return;
			}
			Debug.LogError(exception);
		}
	}

	void Update() {
		this.targetPosition = this.Target.position;
		var controller = this.Target.GetComponent<FourDController>();
		if (controller != null) {
			this.targetW = controller.WPosition;
			this.projection.UpdatePlayerPosition(this.targetPosition, this.targetW);
			this.mapBehaviour.UpdateSlotPositions();
		}

		while (this.completedChunks4D.TryDequeue(out var chunkAddr)) {
			foreach (var receiver in this.callbacks4D.ToArray()) {
				receiver.OnGenerateChunk4D(chunkAddr, this);
			}
		}
	}
}
