using UnityEngine;

// Shared W-layer accent palette used by HUD, landmark, collectible, and gate visuals.
public static class DimensionColors {
	public static readonly Color[] LayerAccents = {
		new Color(0.00f, 0.78f, 1.00f),  // W0  cyan
		new Color(1.00f, 0.49f, 0.00f),  // W1  amber
		new Color(0.38f, 0.45f, 1.00f),  // W2  indigo
		new Color(0.00f, 1.00f, 0.71f),  // W3  mint
		new Color(1.00f, 0.19f, 0.19f),  // W4  red
		new Color(0.73f, 0.50f, 1.00f),  // W5  lavender
	};

	public static Color ForLayer(int layer) {
		int idx = ((layer % LayerAccents.Length) + LayerAccents.Length) % LayerAccents.Length;
		return LayerAccents[idx];
	}

	// Gold used for collectible artifacts — matches the HUD artifact bar
	public static readonly Color ArtifactGold = new Color(1.00f, 0.85f, 0.25f);
}
