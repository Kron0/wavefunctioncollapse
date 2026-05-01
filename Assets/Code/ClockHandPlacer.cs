using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Finds spawned clock module GameObjects and attaches animated ClockFace components.
// Works with any module whose name contains "clock".
public class ClockHandPlacer : MonoBehaviour, IMapGenerationCallbackReceiver4D {
	private GenerateMap4DNearPlayer generator;
	private MapBehaviour4D mapBehaviour;

	void OnEnable() {
		this.generator    = this.GetComponent<GenerateMap4DNearPlayer>();
		this.mapBehaviour = this.GetComponent<MapBehaviour4D>();
		if (this.generator != null) this.generator.RegisterCallback4D(this);
	}

	void OnDisable() {
		if (this.generator != null) this.generator.UnregisterCallback4D(this);
	}

	public void OnGenerateChunk4D(Vector4Int chunkAddress, GenerateMap4DNearPlayer source) {
		if (this.mapBehaviour?.Map == null) return;

		int chunkSize = source.ChunkSize;
		var clockSlots = new List<Slot4D>();

		for (int dx = 0; dx < chunkSize; dx++) {
			for (int dy = 0; dy < this.mapBehaviour.MapHeight; dy++) {
				for (int dz = 0; dz < chunkSize; dz++) {
					for (int dw = 0; dw < chunkSize; dw++) {
						var pos = new Vector4Int(
							chunkAddress.x * chunkSize + dx, dy,
							chunkAddress.z * chunkSize + dz,
							chunkAddress.w * chunkSize + dw);
						var slot = this.mapBehaviour.Map.GetSlot(pos);
						if (slot != null && slot.Collapsed
								&& slot.Module?.Name?.ToLower().Contains("clock") == true) {
							clockSlots.Add(slot);
						}
					}
				}
			}
		}

		if (clockSlots.Count > 0) {
			StartCoroutine(this.AttachDeferred(clockSlots));
		}
	}

	private IEnumerator AttachDeferred(List<Slot4D> slots) {
		float deadline = Time.time + 4f;

		foreach (var slot in slots) {
			while (slot.GameObject == null && Time.time < deadline) yield return null;
			if (slot.GameObject == null) continue;
			if (slot.GameObject.GetComponent<ClockFace>() != null) continue;

			this.AttachClockFace(slot);
		}
	}

	private void AttachClockFace(Slot4D slot) {
		float bs       = AbstractMap4D.BLOCK_SIZE;
		var   slotGO   = slot.GameObject;
		var   slotPos  = slotGO.transform.position;

		// Determine which face of the block the clock face is on.
		// The module's Rotation field controls which horizontal face is "front".
		// Rotation 0 = Forward (+Z), 1 = Right (+X), 2 = Back (-Z), 3 = Left (-X).
		int rot = slot.Module.Rotation % 4;
		Vector3 faceNormal;
		switch (rot) {
			case 0:  faceNormal =  slotGO.transform.forward; break;
			case 1:  faceNormal =  slotGO.transform.right;   break;
			case 2:  faceNormal = -slotGO.transform.forward; break;
			default: faceNormal = -slotGO.transform.right;   break;
		}

		// Face centre: block centre + half blocksize along face normal, slightly raised
		Vector3 faceCenter = slotPos + faceNormal * (bs * 0.50f) + Vector3.up * (bs * 0.05f);
		float clockRadius  = bs * 0.32f;

		// Time offset: hash on world position so each clock shows a different time
		float timeOff = (slotPos.x * 1.7f + slotPos.z * 2.3f + slotPos.y * 0.9f) % 43200f;

		var cf = slotGO.AddComponent<ClockFace>();
		cf.Init(faceCenter, faceNormal, clockRadius, timeOff);
	}
}
