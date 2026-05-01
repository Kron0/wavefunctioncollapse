using UnityEngine;

public static class SlotMaterializer {
	private static Material opaqueTriplanar;
	private static Material transparentTriplanar;

	private static readonly int TopColorID     = Shader.PropertyToID("_TopColor");
	private static readonly int SideColorID    = Shader.PropertyToID("_SideColor");
	private static readonly int BottomColorID  = Shader.PropertyToID("_BottomColor");
	private static readonly int WeatherColorID = Shader.PropertyToID("_WeatherColor");
	private static readonly int AlphaID        = Shader.PropertyToID("_Alpha");
	private static readonly int BrickStrID     = Shader.PropertyToID("_BrickStrength");
	private static readonly int GlassStrID     = Shader.PropertyToID("_GlassStrength");

	// Reused across calls to avoid per-frame GC allocations
	private static readonly MaterialPropertyBlock sharedBlock = new MaterialPropertyBlock();

	public static void Initialize(Material opaque, Material transparent) {
		opaqueTriplanar = opaque;
		transparentTriplanar = transparent;
	}

	public static void EnsureInitialized() {
		if (opaqueTriplanar == null) {
			var shader = Shader.Find("Custom/BuildingTriplanar");
			if (shader != null) {
				opaqueTriplanar = new Material(shader);
			}
		}
		if (transparentTriplanar == null) {
			var shader = Shader.Find("Custom/BuildingTriplanar_Transparent");
			if (shader != null) {
				transparentTriplanar = new Material(shader);
			}
		}
	}

	public static void Apply(Slot slot, GameObject gameObject, MaterialPalette palette) {
		ApplyInternal(gameObject, palette, slot.Position.GetHashCode(), 1f, false);
	}

	public static void Apply(Slot4D slot, GameObject gameObject, MaterialPalette palette, float alpha) {
		// Hash only XZ so every floor of the same column shares the same color jitter
		int xzHash = slot.Position.x * 1664525 + slot.Position.z * 1013904223;
		ApplyInternal(gameObject, palette, xzHash, alpha, alpha < 1f);
	}

	// Called each frame by UpdateSlotPositions to keep alpha in sync with W-distance
	public static void UpdateAlpha(GameObject gameObject, float alpha) {
		bool useTransparent = alpha < 1f;
		Material baseMat = useTransparent ? transparentTriplanar : opaqueTriplanar;
		foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>()) {
			if (baseMat != null && renderer.sharedMaterial != baseMat) {
				renderer.sharedMaterial = baseMat;
			}
			renderer.GetPropertyBlock(sharedBlock);
			sharedBlock.SetFloat(AlphaID, alpha);
			renderer.SetPropertyBlock(sharedBlock);
		}
	}

	private static void ApplyInternal(GameObject gameObject, MaterialPalette palette, int positionHash, float alpha, bool useTransparent) {
		if (palette == null) {
			return;
		}

		EnsureInitialized();

		var renderers = gameObject.GetComponentsInChildren<Renderer>();

		// Use different hash bits for independent per-channel jitter
		float jitterR = ((positionHash & 0xFF) / 255f - 0.5f) * 0.12f;
		float jitterG = (((positionHash >> 8) & 0xFF) / 255f - 0.5f) * 0.10f;
		float jitterB = (((positionHash >> 16) & 0xFF) / 255f - 0.5f) * 0.08f;

		Color topColor = JitterColorRGB(palette.TopColor, jitterR, jitterG, jitterB);
		Color sideColor = JitterColorRGB(palette.SideColor, jitterR * 0.8f, jitterG * 0.9f, jitterB);
		Color bottomColor = JitterColorRGB(palette.BottomColor, jitterR * 0.6f, jitterG, jitterB * 1.2f);

		// Pattern strengths keyed to palette style
		float brickStrength = 0f;
		float glassStrength = 0f;
		switch (palette.PaletteName) {
			case "Warm Residential": brickStrength = 0.92f; break;
			case "Cool Commercial":  glassStrength = 0.88f; break;
			case "Industrial":       brickStrength = 0.55f; break;
			case "Park Green":       brickStrength = 0.45f; break;
			case "Neon Future":      glassStrength = 1.00f; break;
			case "Desert Sand":      brickStrength = 0.70f; break;
			case "Night Neon":       glassStrength = 0.95f; break;
		}

		Material baseMat = useTransparent ? transparentTriplanar : opaqueTriplanar;
		foreach (var renderer in renderers) {
			if (baseMat != null) {
				var mats = new Material[renderer.sharedMaterials.Length];
				for (int i = 0; i < mats.Length; i++) mats[i] = baseMat;
				renderer.sharedMaterials = mats;
			}

			renderer.GetPropertyBlock(sharedBlock);
			sharedBlock.SetColor(TopColorID, topColor);
			sharedBlock.SetColor(SideColorID, sideColor);
			sharedBlock.SetColor(BottomColorID, bottomColor);
			sharedBlock.SetColor(WeatherColorID, palette.WeatherColor);
			sharedBlock.SetFloat(BrickStrID, brickStrength);
			sharedBlock.SetFloat(GlassStrID, glassStrength);
			if (useTransparent) {
				sharedBlock.SetFloat(AlphaID, alpha);
			}
			renderer.SetPropertyBlock(sharedBlock);
		}
	}

	private static Color JitterColorRGB(Color baseColor, float r, float g, float b) {
		return new Color(
			Mathf.Clamp01(baseColor.r + r),
			Mathf.Clamp01(baseColor.g + g),
			Mathf.Clamp01(baseColor.b + b),
			baseColor.a);
	}
}
