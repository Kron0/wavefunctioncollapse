using System.Collections.Generic;
using UnityEngine;

public class ChunkAtmosphere : MonoBehaviour, IMapGenerationCallbackReceiver {
	private MapBehaviour mapBehaviour;
	private MaterialWFC materialWFC;

	private Dictionary<Vector3Int, MaterialPalette> chunkPalettes = new Dictionary<Vector3Int, MaterialPalette>();

	[Range(0.5f, 5f)]
	public float BlendDistance = 2f;

	private Transform playerTransform;

	public void Initialize(MaterialWFC wfc, Transform player) {
		this.materialWFC = wfc;
		this.playerTransform = player;
	}

	public void OnEnable() {
		this.mapBehaviour = this.GetComponent<MapBehaviour>();
		this.GetComponent<GenerateMapNearPlayer>().RegisterMapGenerationCallbackReceiver(this);
	}

	public void OnDisable() {
		this.GetComponent<GenerateMapNearPlayer>().UnregisterMapGenerationCallbackReceiver(this);
	}

	public void OnGenerateChunk(Vector3Int chunkAddress, GenerateMapNearPlayer source) {
		if (this.materialWFC == null && this.mapBehaviour != null) {
			this.materialWFC = this.mapBehaviour.GetMaterialWFC();
		}
		if (this.materialWFC == null) {
			return;
		}
		if (this.playerTransform == null) {
			var generator = this.GetComponent<GenerateMapNearPlayer>();
			if (generator != null) {
				this.playerTransform = generator.Target;
			}
		}

		int chunkSize = source.ChunkSize;
		var paletteCounts = new Dictionary<MaterialPalette, int>();

		for (int x = chunkSize * chunkAddress.x; x < chunkSize * (chunkAddress.x + 1); x++) {
			for (int z = chunkSize * chunkAddress.z; z < chunkSize * (chunkAddress.z + 1); z++) {
				var pos = new Vector3Int(x, 0, z);
				var palette = this.materialWFC.GetPalette(pos);
				if (!paletteCounts.ContainsKey(palette)) {
					paletteCounts[palette] = 0;
				}
				paletteCounts[palette]++;
			}
		}

		MaterialPalette dominant = null;
		int maxCount = 0;
		foreach (var kvp in paletteCounts) {
			if (kvp.Value > maxCount) {
				maxCount = kvp.Value;
				dominant = kvp.Key;
			}
		}

		if (dominant != null) {
			this.chunkPalettes[chunkAddress] = dominant;
		}
	}

	void Update() {
		if (this.playerTransform == null || this.mapBehaviour == null || !this.mapBehaviour.Initialized) {
			return;
		}

		var playerPos = this.playerTransform.position;
		var mapPos = this.mapBehaviour.GetMapPosition(playerPos);
		int chunkSize = 4;
		var generator = this.GetComponent<GenerateMapNearPlayer>();
		if (generator != null) {
			chunkSize = generator.ChunkSize;
		}

		float chunkWorldSize = AbstractMap.BLOCK_SIZE * chunkSize;
		int chunkX = Mathf.FloorToInt((playerPos.x + AbstractMap.BLOCK_SIZE / 2f) / chunkWorldSize);
		int chunkZ = Mathf.FloorToInt((playerPos.z + AbstractMap.BLOCK_SIZE / 2f) / chunkWorldSize);
		var currentChunk = new Vector3Int(chunkX, 0, chunkZ);

		Color blendedFogColor = RenderSettings.fogColor;
		float blendedFogDensity = RenderSettings.fogDensity;
		Color blendedAmbient = RenderSettings.ambientLight;

		float totalWeight = 0f;
		Color fogColorAccum = Color.black;
		float fogDensityAccum = 0f;
		Color ambientAccum = Color.black;

		for (int x = currentChunk.x - 1; x <= currentChunk.x + 1; x++) {
			for (int z = currentChunk.z - 1; z <= currentChunk.z + 1; z++) {
				var chunk = new Vector3Int(x, 0, z);
				if (!this.chunkPalettes.ContainsKey(chunk)) {
					continue;
				}

				var palette = this.chunkPalettes[chunk];
				var chunkCenter = new Vector3(
					(chunk.x + 0.5f) * chunkWorldSize - AbstractMap.BLOCK_SIZE / 2f,
					playerPos.y,
					(chunk.z + 0.5f) * chunkWorldSize - AbstractMap.BLOCK_SIZE / 2f);

				float dist = Vector3.Distance(playerPos, chunkCenter);
				float weight = Mathf.Max(0f, 1f - dist / (chunkWorldSize * this.BlendDistance));
				weight = weight * weight;

				if (weight > 0f) {
					fogColorAccum += palette.FogColor * weight;
					fogDensityAccum += palette.FogDensity * weight;
					ambientAccum += palette.FogColor * 0.3f * weight;
					totalWeight += weight;
				}
			}
		}

		if (totalWeight > 0f) {
			blendedFogColor = fogColorAccum / totalWeight;
			blendedFogDensity = fogDensityAccum / totalWeight;
			blendedAmbient = ambientAccum / totalWeight;
		}

		float lerpSpeed = Time.deltaTime * 2f;
		RenderSettings.fog = true;
		RenderSettings.fogMode = FogMode.Exponential;
		RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, blendedFogColor, lerpSpeed);
		RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, blendedFogDensity, lerpSpeed);
		RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, blendedAmbient, lerpSpeed);
	}
}
