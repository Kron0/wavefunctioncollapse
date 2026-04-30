using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MaterialWFC {
	private static readonly MaterialPalette[] defaultPalettes = CreateDefaultPalettes();

	private MaterialPalette[] palettes;
	private bool[,] compatibility;
	private System.Random random;

	public Dictionary<Vector3Int, MaterialPalette> SlotPalettes { get; private set; }

	public MaterialWFC(MaterialPalette[] customPalettes = null) {
		this.palettes = customPalettes != null && customPalettes.Length > 0 ? customPalettes : defaultPalettes;
		this.random = new System.Random();
		this.SlotPalettes = new Dictionary<Vector3Int, MaterialPalette>();
		this.InitializeCompatibility();
	}

	private void InitializeCompatibility() {
		int count = this.palettes.Length;
		this.compatibility = new bool[count, count];

		for (int i = 0; i < count; i++) {
			if (this.palettes[i].CompatibleWith != null && this.palettes[i].CompatibleWith.Length == count) {
				for (int j = 0; j < count; j++) {
					this.compatibility[i, j] = this.palettes[i].CompatibleWith[j];
				}
			} else {
				for (int j = 0; j < count; j++) {
					this.compatibility[i, j] = true;
				}
				// Default: same palette and adjacent indices are always compatible
				// Distant palettes have reduced compatibility
				for (int j = 0; j < count; j++) {
					int dist = Mathf.Abs(i - j);
					this.compatibility[i, j] = dist <= 1 || (i == 0 && j == count - 1) || (j == 0 && i == count - 1);
				}
			}
		}
	}

	public void AssignChunk(Vector3Int chunkStart, Vector3Int chunkSize, AbstractMap map) {
		var positions = new List<Vector3Int>();
		for (int x = 0; x < chunkSize.x; x++) {
			for (int y = 0; y < chunkSize.y; y++) {
				for (int z = 0; z < chunkSize.z; z++) {
					positions.Add(chunkStart + new Vector3Int(x, y, z));
				}
			}
		}
		AssignPositions(positions, map);
	}

	public void AssignPositions(List<Vector3Int> positions, AbstractMap map) {
		var unassigned = new List<Vector3Int>(positions);

		// Shuffle for variety
		for (int i = unassigned.Count - 1; i > 0; i--) {
			int j = this.random.Next(i + 1);
			var temp = unassigned[i];
			unassigned[i] = unassigned[j];
			unassigned[j] = temp;
		}

		foreach (var pos in unassigned) {
			if (this.SlotPalettes.ContainsKey(pos)) {
				continue;
			}

			var slot = map.GetSlot(pos);
			if (slot == null || !slot.Collapsed) {
				continue;
			}

			float[] weights = new float[this.palettes.Length];
			float totalWeight = 0f;

			for (int i = 0; i < this.palettes.Length; i++) {
				weights[i] = this.palettes[i].Probability;
			}

			// Check neighbors and bias toward compatible palettes
			// 20% wildcard: ignore neighbor weights entirely for more variety
			bool useWildcard = this.random.NextDouble() < 0.20;
			if (!useWildcard) {
				for (int d = 0; d < 6; d++) {
					var neighborPos = pos + Orientations.Direction[d];
					if (this.SlotPalettes.ContainsKey(neighborPos)) {
						int neighborIdx = System.Array.IndexOf(this.palettes, this.SlotPalettes[neighborPos]);
						if (neighborIdx >= 0) {
							for (int i = 0; i < this.palettes.Length; i++) {
								if (!this.compatibility[i, neighborIdx]) {
									weights[i] *= 0.05f;
								} else if (i == neighborIdx) {
									weights[i] *= 1.5f;  // Reduced from 3x — less clustering
								} else {
									weights[i] *= 1.2f;  // Reduced from 1.5x
								}
							}
						}
					}
				}

				// Vertical coherence: moderate preference for same palette above/below
				var belowPos = pos + Vector3Int.down;
				if (this.SlotPalettes.ContainsKey(belowPos)) {
					int belowIdx = System.Array.IndexOf(this.palettes, this.SlotPalettes[belowPos]);
					if (belowIdx >= 0) {
						weights[belowIdx] *= 3f;  // Reduced from 10x
					}
				}
			}

			for (int i = 0; i < weights.Length; i++) {
				totalWeight += weights[i];
			}

			float roll = (float)(this.random.NextDouble() * totalWeight);
			float cumulative = 0f;
			int selected = 0;
			for (int i = 0; i < weights.Length; i++) {
				cumulative += weights[i];
				if (cumulative >= roll) {
					selected = i;
					break;
				}
			}

			this.SlotPalettes[pos] = this.palettes[selected];
		}
	}

	public MaterialPalette GetPalette(Vector3Int position) {
		return GetPalette(position, -1);
	}

	// wLayer = -1 means no layer bias (3D map or unspecified)
	public MaterialPalette GetPalette(Vector3Int position, int wLayer) {
		if (this.SlotPalettes.TryGetValue(position, out var palette)) {
			return palette;
		}
		// Fallback for unassigned positions (4D map): hash by XZ column for vertical coherence
		int hash = new Vector3Int(position.x, 0, position.z).GetHashCode();
		var rng = new System.Random(hash);

		float[] weights = new float[this.palettes.Length];
		for (int i = 0; i < weights.Length; i++) {
			weights[i] = this.palettes[i].Probability;
			if (wLayer >= 0) {
				weights[i] *= GetLayerBias(wLayer, i);
			}
		}

		float totalWeight = 0f;
		foreach (var w in weights) totalWeight += w;
		float roll = (float)(rng.NextDouble() * totalWeight);
		float cumulative = 0f;
		for (int i = 0; i < this.palettes.Length; i++) {
			cumulative += weights[i];
			if (cumulative >= roll) return this.palettes[i];
		}
		return this.palettes[0];
	}

	// Per-layer multipliers for each of the 6 default palettes:
	//  0=WarmResidential, 1=CoolCommercial, 2=Industrial, 3=ParkGreen, 4=NeonFuture, 5=DesertSand
	private static float GetLayerBias(int wLayer, int paletteIndex) {
		int li = ((wLayer % 6) + 6) % 6;
		// [W layer][palette index]
		float[,] bias = {
			{ 1.6f, 0.8f, 0.3f, 1.2f, 0.2f, 0.5f },  // W0 cyan     — warm residential + green
			{ 1.0f, 0.4f, 0.4f, 0.5f, 0.2f, 2.5f },  // W1 amber    — desert sand dominant
			{ 0.3f, 1.4f, 0.8f, 0.3f, 2.5f, 0.2f },  // W2 indigo   — neon future + cool commercial
			{ 0.5f, 0.6f, 0.2f, 3.5f, 0.2f, 0.4f },  // W3 mint     — park green dominant
			{ 0.4f, 0.3f, 2.8f, 0.3f, 1.5f, 0.4f },  // W4 red      — industrial + neon
			{ 0.7f, 2.0f, 0.4f, 0.8f, 0.8f, 0.3f },  // W5 lavender — cool commercial
		};
		if (paletteIndex >= 6) return 1f;  // unknown palette: no bias
		return bias[li, paletteIndex];
	}

	private static MaterialPalette[] CreateDefaultPalettes() {
		return new MaterialPalette[] {
			MaterialPalette.CreateDefault(
				"Warm Residential",
				new Color(0.85f, 0.75f, 0.65f),  // top: warm sandstone
				new Color(0.72f, 0.45f, 0.35f),  // side: brick red
				new Color(0.55f, 0.45f, 0.40f),  // bottom: dark stone
				new Color(0.30f, 0.22f, 0.18f),  // weather: brown dirt
				new Color(1.0f, 0.85f, 0.65f),   // light: warm yellow
				new Color(0.6f, 0.55f, 0.5f),    // fog: warm haze
				0.015f, 0.35f),

			MaterialPalette.CreateDefault(
				"Cool Commercial",
				new Color(0.75f, 0.78f, 0.82f),  // top: light steel
				new Color(0.55f, 0.62f, 0.72f),  // side: blue-gray glass
				new Color(0.40f, 0.42f, 0.48f),  // bottom: dark steel
				new Color(0.20f, 0.22f, 0.25f),  // weather: dark grime
				new Color(0.85f, 0.92f, 1.0f),   // light: cool white
				new Color(0.5f, 0.55f, 0.65f),   // fog: cool blue
				0.02f, 0.30f),

			MaterialPalette.CreateDefault(
				"Industrial",
				new Color(0.60f, 0.55f, 0.50f),  // top: dusty concrete
				new Color(0.50f, 0.42f, 0.35f),  // side: rust
				new Color(0.35f, 0.30f, 0.28f),  // bottom: dark concrete
				new Color(0.35f, 0.28f, 0.20f),  // weather: rust stain
				new Color(1.0f, 0.75f, 0.50f),   // light: sodium orange
				new Color(0.55f, 0.48f, 0.42f),  // fog: smoggy
				0.035f, 0.20f),

			MaterialPalette.CreateDefault(
				"Park Green",
				new Color(0.50f, 0.65f, 0.45f),  // top: mossy
				new Color(0.65f, 0.62f, 0.55f),  // side: weathered stone
				new Color(0.40f, 0.38f, 0.32f),  // bottom: earth
				new Color(0.28f, 0.30f, 0.20f),  // weather: moss/lichen
				new Color(0.95f, 1.0f, 0.85f),   // light: greenish white
				new Color(0.45f, 0.55f, 0.48f),  // fog: misty green
				0.01f, 0.15f),

			MaterialPalette.CreateDefault(
				"Neon Future",
				new Color(0.15f, 0.18f, 0.25f),  // top: dark carbon
				new Color(0.20f, 0.35f, 0.55f),  // side: deep blue-black
				new Color(0.10f, 0.12f, 0.18f),  // bottom: near black
				new Color(0.05f, 0.45f, 0.55f),  // weather: neon cyan grime
				new Color(0.30f, 0.90f, 1.0f),   // light: neon cyan
				new Color(0.10f, 0.15f, 0.25f),  // fog: dark blue haze
				0.04f, 0.12f),

			MaterialPalette.CreateDefault(
				"Desert Sand",
				new Color(0.88f, 0.78f, 0.58f),  // top: sand
				new Color(0.80f, 0.65f, 0.45f),  // side: sandstone
				new Color(0.62f, 0.50f, 0.35f),  // bottom: dark earth
				new Color(0.55f, 0.40f, 0.22f),  // weather: rust/iron stain
				new Color(1.0f, 0.95f, 0.70f),   // light: warm sunlight
				new Color(0.75f, 0.70f, 0.55f),  // fog: dusty beige
				0.008f, 0.08f),
		};
	}
}
