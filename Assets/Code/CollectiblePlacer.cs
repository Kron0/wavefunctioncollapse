using UnityEngine;

public class CollectiblePlacer : MonoBehaviour, IMapGenerationCallbackReceiver4D {
	private GenerateMap4DNearPlayer generator;
	private MapBehaviour4D mapBehaviour;

	public static int TotalCollected = 0;
	public static int TotalPlaced = 0;

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
		if (this.mapBehaviour == null || this.mapBehaviour.Map == null) {
			return;
		}

		var rng = new System.Random(chunkAddress.GetHashCode() ^ 0xC011EC7);
		int chunkSize = source.ChunkSize;
		int topY = this.mapBehaviour.MapHeight - 1;

		for (int dx = 0; dx < chunkSize; dx++) {
			for (int dz = 0; dz < chunkSize; dz++) {
				for (int dw = 0; dw < chunkSize; dw++) {
					var pos = new Vector4Int(
						chunkAddress.x * chunkSize + dx,
						topY,
						chunkAddress.z * chunkSize + dz,
						chunkAddress.w * chunkSize + dw);
					var slot = this.mapBehaviour.Map.GetSlot(pos);
					if (slot == null || !slot.Collapsed || slot.GameObject == null) {
						continue;
					}
					if (!slot.Module.Prototype.Up.Walkable) {
						continue;
					}
					if (rng.NextDouble() > 0.12f) {
						continue;
					}
					Vector3 worldPos = slot.GameObject.transform.position + Vector3.up * AbstractMap4D.BLOCK_SIZE * 0.8f;
					this.SpawnCollectible(worldPos, pos.w);
				}
			}
		}
	}

	private void SpawnCollectible(Vector3 worldPos, int wLayer) {
		var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		go.name = "Collectible";
		go.transform.position = worldPos;
		go.transform.localScale = Vector3.one * 0.3f;
		go.transform.SetParent(this.transform);

		var mat = new Material(Shader.Find("Standard"));
		mat.color = new Color(1f, 0.9f, 0.2f);
		mat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0f) * 2f);
		mat.EnableKeyword("_EMISSION");
		go.GetComponent<Renderer>().material = mat;

		var lightGO = new GameObject("Collectible_Light");
		lightGO.transform.SetParent(go.transform);
		lightGO.transform.localPosition = Vector3.zero;
		var light = lightGO.AddComponent<Light>();
		light.type = LightType.Point;
		light.color = new Color(1f, 0.9f, 0.2f);
		light.intensity = 0.8f;
		light.range = 5f;

		var item = go.AddComponent<CollectibleItem>();
		item.requiredWLayer = wLayer;

		TotalPlaced++;
	}
}
