using UnityEngine;

// Drives the animation on a spawned landmark beacon.
public class LandmarkBeacon : MonoBehaviour {
	private Transform topPivot;
	private Light beaconLight;
	private Renderer orbRenderer;
	private Color accentColor;

	private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

	public void Init(Transform topPivot, Light beaconLight, Renderer orbRenderer, Color accentColor) {
		this.topPivot     = topPivot;
		this.beaconLight  = beaconLight;
		this.orbRenderer  = orbRenderer;
		this.accentColor  = accentColor;
	}

	void Update() {
		float t = Time.time;

		if (this.topPivot != null) {
			this.topPivot.Rotate(0f, 28f * Time.deltaTime, 0f, Space.World);
		}

		float pulse = 0.5f + 0.5f * Mathf.Sin(t * 2.1f);

		if (this.beaconLight != null) {
			this.beaconLight.intensity = 1.5f + pulse * 2.5f;
		}

		if (this.orbRenderer != null) {
			this.orbRenderer.material.SetColor(EmissionColorID, this.accentColor * (3f + pulse * 3f));
		}
	}
}
