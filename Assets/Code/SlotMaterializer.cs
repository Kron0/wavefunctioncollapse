using UnityEngine;

public static class SlotMaterializer {
	private static Material opaqueTriplanar;
	private static Material transparentTriplanar;

	private static readonly int TopColorID = Shader.PropertyToID("_TopColor");
	private static readonly int SideColorID = Shader.PropertyToID("_SideColor");
	private static readonly int BottomColorID = Shader.PropertyToID("_BottomColor");
	private static readonly int WeatherColorID = Shader.PropertyToID("_WeatherColor");
	private static readonly int AlphaID = Shader.PropertyToID("_Alpha");

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
		ApplyInternal(gameObject, palette, slot.Position.GetHashCode(), alpha, alpha < 1f);
	}

	// Called each frame by UpdateSlotPositions to keep alpha in sync with W-distance
	public static void UpdateAlpha(GameObject gameObject, float alpha) {
		bool useTransparent = alpha < 1f;
		Material baseMat = useTransparent ? transparentTriplanar : opaqueTriplanar;
		var block = new MaterialPropertyBlock();
		foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>()) {
			if (baseMat != null && renderer.sharedMaterial != baseMat) {
				renderer.sharedMaterial = baseMat;
			}
			renderer.GetPropertyBlock(block);
			block.SetFloat(AlphaID, alpha);
			renderer.SetPropertyBlock(block);
		}
	}

	private static void ApplyInternal(GameObject gameObject, MaterialPalette palette, int positionHash, float alpha, bool useTransparent) {
		if (palette == null) {
			return;
		}

		EnsureInitialized();

		var renderers = gameObject.GetComponentsInChildren<Renderer>();
		var block = new MaterialPropertyBlock();

		// Use different hash bits for independent per-channel jitter
		float jitterR = ((positionHash & 0xFF) / 255f - 0.5f) * 0.22f;
		float jitterG = (((positionHash >> 8) & 0xFF) / 255f - 0.5f) * 0.18f;
		float jitterB = (((positionHash >> 16) & 0xFF) / 255f - 0.5f) * 0.15f;

		Color topColor = JitterColorRGB(palette.TopColor, jitterR, jitterG, jitterB);
		Color sideColor = JitterColorRGB(palette.SideColor, jitterR * 0.8f, jitterG * 0.9f, jitterB);
		Color bottomColor = JitterColorRGB(palette.BottomColor, jitterR * 0.6f, jitterG, jitterB * 1.2f);

		foreach (var renderer in renderers) {
			Material baseMat = useTransparent ? transparentTriplanar : opaqueTriplanar;
			if (baseMat != null) {
				var mats = new Material[renderer.sharedMaterials.Length];
				for (int i = 0; i < mats.Length; i++) {
					mats[i] = baseMat;
				}
				renderer.sharedMaterials = mats;
			}

			renderer.GetPropertyBlock(block);
			block.SetColor(TopColorID, topColor);
			block.SetColor(SideColorID, sideColor);
			block.SetColor(BottomColorID, bottomColor);
			block.SetColor(WeatherColorID, palette.WeatherColor);
			if (useTransparent) {
				block.SetFloat(AlphaID, alpha);
			}
			renderer.SetPropertyBlock(block);
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
