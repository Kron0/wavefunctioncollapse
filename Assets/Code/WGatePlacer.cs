using System.Collections.Generic;
using UnityEngine;

public class WGatePlacer : MonoBehaviour, IMapGenerationCallbackReceiver4D {
	private GenerateMap4DNearPlayer generator;
	private MapBehaviour4D mapBehaviour;

	void OnEnable() {
		this.generator = this.GetComponent<GenerateMap4DNearPlayer>();
		this.mapBehaviour = this.GetComponent<MapBehaviour4D>();
		if (this.generator != null) {
			this.generator.RegisterCallback4D(this);
		}
	}

	void OnDisable() {
		if (this.generator != null) {
			this.generator.UnregisterCallback4D(this);
		}
	}

	public void OnGenerateChunk4D(Vector4Int chunkAddress, GenerateMap4DNearPlayer source) {
		var rng = new System.Random(chunkAddress.GetHashCode() ^ 0x7A7E7A7E);
		if (rng.NextDouble() > 0.25f) {
			return;
		}
		if (this.mapBehaviour == null || this.mapBehaviour.Map == null) {
			return;
		}

		int chunkSize = source.ChunkSize;
		var candidates = new List<Slot4D>();

		for (int dx = 0; dx < chunkSize; dx++) {
			for (int y = 1; y < this.mapBehaviour.MapHeight - 1; y++) {
				for (int dz = 0; dz < chunkSize; dz++) {
					for (int dw = 0; dw < chunkSize; dw++) {
						var pos = new Vector4Int(
							chunkAddress.x * chunkSize + dx,
							y,
							chunkAddress.z * chunkSize + dz,
							chunkAddress.w * chunkSize + dw);
						var slot = this.mapBehaviour.Map.GetSlot(pos);
						if (slot == null || !slot.Collapsed || slot.GameObject == null) {
							continue;
						}
						var proto = slot.Module.Prototype;
						bool hasWalkableHorizontal = proto.Left.Walkable || proto.Right.Walkable
							|| proto.Forward.Walkable || proto.Back.Walkable;
						if (hasWalkableHorizontal) {
							candidates.Add(slot);
						}
					}
				}
			}
		}

		int gateCount = Mathf.Clamp(candidates.Count / 8, 1, 2);
		for (int i = 0; i < gateCount && candidates.Count > 0; i++) {
			int idx = rng.Next(candidates.Count);
			this.PlaceGate(candidates[idx]);
			candidates.RemoveAt(idx);
		}
	}

	private void PlaceGate(Slot4D slot) {
		float blockSize = AbstractMap4D.BLOCK_SIZE;
		Vector3 worldPos = slot.GameObject.transform.position;

		// Opaque wall — solid from wrong W layers
		var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
		wall.name = "WGate_Wall";
		wall.transform.position = worldPos;
		wall.transform.localScale = new Vector3(blockSize * 0.95f, blockSize * 0.9f, blockSize * 0.08f);
		wall.transform.SetParent(this.transform);

		var wallMat = new Material(Shader.Find("Standard"));
		wallMat.color = new Color(0f, 0.7f, 1f);
		wallMat.SetColor("_EmissionColor", new Color(0f, 0.25f, 0.4f));
		wallMat.EnableKeyword("_EMISSION");
		wall.GetComponent<Renderer>().material = wallMat;

		var blocker = wall.AddComponent<WGateBlocker>();
		// Gate opens at the next W layer — forces player to shift W to pass through
		blocker.wGateLayer = slot.Position.w + 1;

		// Persistent marker — faint glow visible from all W layers, no collider
		var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
		marker.name = "WGate_Marker";
		marker.transform.position = worldPos;
		marker.transform.localScale = new Vector3(blockSize * 0.4f, blockSize * 0.4f, 1f);
		marker.transform.SetParent(this.transform);
		Object.Destroy(marker.GetComponent<MeshCollider>());

		var markerMat = new Material(Shader.Find("Standard"));
		markerMat.color = new Color(0f, 0.8f, 1f, 0.25f);
		markerMat.SetFloat("_Mode", 3f);
		markerMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		markerMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		markerMat.SetInt("_ZWrite", 0);
		markerMat.EnableKeyword("_ALPHABLEND_ON");
		markerMat.renderQueue = 3000;
		markerMat.SetColor("_EmissionColor", new Color(0f, 0.5f, 0.7f));
		markerMat.EnableKeyword("_EMISSION");
		marker.GetComponent<Renderer>().material = markerMat;
	}
}
