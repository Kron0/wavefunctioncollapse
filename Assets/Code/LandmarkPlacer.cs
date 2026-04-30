using System.Collections.Generic;
using UnityEngine;

public class LandmarkPlacer : MonoBehaviour, IMapGenerationCallbackReceiver4D {
	private GenerateMap4DNearPlayer generator;
	private MapBehaviour4D mapBehaviour;

	public static readonly List<Vector3> LandmarkWorldPositions = new List<Vector3>();
	public static int LandmarkCount => LandmarkWorldPositions.Count;

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
		// ~1 in 8 chunks becomes a landmark
		var rng = new System.Random(chunkAddress.GetHashCode() ^ 0x5EEDFACE);
		if (rng.NextDouble() > 0.125f) {
			return;
		}
		if (this.mapBehaviour == null || this.mapBehaviour.Map == null) {
			return;
		}

		int chunkSize = source.ChunkSize;
		Slot4D topSlot = null;
		int maxY = -1;

		// Use the first W slice of the chunk for the landmark
		int wBase = chunkAddress.w * chunkSize;
		for (int dx = 0; dx < chunkSize; dx++) {
			for (int dz = 0; dz < chunkSize; dz++) {
				for (int y = this.mapBehaviour.MapHeight - 1; y >= 0; y--) {
					var pos = new Vector4Int(
						chunkAddress.x * chunkSize + dx,
						y,
						chunkAddress.z * chunkSize + dz,
						wBase);
					var slot = this.mapBehaviour.Map.GetSlot(pos);
					if (slot != null && slot.Collapsed && slot.GameObject != null && y > maxY) {
						maxY = y;
						topSlot = slot;
					}
				}
			}
		}

		if (topSlot == null) {
			return;
		}

		Vector3 beaconBase = topSlot.GameObject.transform.position + Vector3.up * AbstractMap4D.BLOCK_SIZE;
		this.SpawnBeacon(beaconBase);
		LandmarkWorldPositions.Add(beaconBase + Vector3.up * 4f);
	}

	private void SpawnBeacon(Vector3 basePos) {
		var beacon = new GameObject("Landmark_Beacon");
		beacon.transform.position = basePos;
		beacon.transform.SetParent(this.transform);

		// Glowing vertical pillar
		var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		pillar.name = "Beacon_Pillar";
		pillar.transform.SetParent(beacon.transform);
		pillar.transform.localPosition = Vector3.up * 2f;
		pillar.transform.localScale = new Vector3(0.2f, 2f, 0.2f);
		Object.Destroy(pillar.GetComponent<Collider>());

		var pillarMat = new Material(Shader.Find("Standard"));
		pillarMat.color = new Color(1f, 0.6f, 0.1f);
		pillarMat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0f) * 1.5f);
		pillarMat.EnableKeyword("_EMISSION");
		pillar.GetComponent<Renderer>().material = pillarMat;

		// Top sphere
		var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		cap.name = "Beacon_Cap";
		cap.transform.SetParent(beacon.transform);
		cap.transform.localPosition = Vector3.up * 4.2f;
		cap.transform.localScale = Vector3.one * 0.5f;
		Object.Destroy(cap.GetComponent<Collider>());
		cap.GetComponent<Renderer>().material = pillarMat;

		// Point light
		var lightGO = new GameObject("Beacon_Light");
		lightGO.transform.SetParent(beacon.transform);
		lightGO.transform.localPosition = Vector3.up * 4.5f;
		var light = lightGO.AddComponent<Light>();
		light.type = LightType.Point;
		light.color = new Color(1f, 0.7f, 0.3f);
		light.intensity = 3f;
		light.range = 35f;
	}
}
