using UnityEngine;

// Tints all building surfaces with the current W-layer's accent color.
// Toggle with C key. Colour smoothly transitions on layer change.
public class WLayerColorizer : MonoBehaviour {
	public static bool TintEnabled = true;

	[Range(0f, 1f)]
	public float TintStrength = 0.28f;

	[Range(1f, 10f)]
	public float BlendSpeed = 4f;

	private Color currentTint = Color.white;
	private static readonly int WLayerTintID = Shader.PropertyToID("_WLayerTint");

	void Awake() {
		// Ensure no black-flash before first Update by priming the global to white
		Shader.SetGlobalColor(WLayerTintID, Color.white);
	}

	void OnEnable() {
		Shader.SetGlobalColor(WLayerTintID, Color.white);
		this.currentTint = Color.white;
	}

	void OnDisable() {
		Shader.SetGlobalColor(WLayerTintID, Color.white);
	}

	void Update() {
		if (Input.GetKeyDown(KeyCode.C)) {
			TintEnabled = !TintEnabled;
			if (!TintEnabled) {
				this.currentTint = Color.white;
				Shader.SetGlobalColor(WLayerTintID, Color.white);
			}
		}

		if (!TintEnabled) return;

		Color target = TargetTint(MapBehaviour4D.ActiveWLayer, this.TintStrength);
		this.currentTint = Color.Lerp(this.currentTint, target, Time.deltaTime * this.BlendSpeed);
		Shader.SetGlobalColor(WLayerTintID, this.currentTint);
	}

	private static Color TargetTint(int layer, float strength) {
		Color accent = DimensionColors.ForLayer(layer);
		// Slightly boost brightness so the tint doesn't darken buildings
		Color tint = Color.Lerp(Color.white, accent, strength);
		tint.r = Mathf.Clamp(tint.r * 1.06f, 0f, 1.5f);
		tint.g = Mathf.Clamp(tint.g * 1.06f, 0f, 1.5f);
		tint.b = Mathf.Clamp(tint.b * 1.06f, 0f, 1.5f);
		return tint;
	}
}
