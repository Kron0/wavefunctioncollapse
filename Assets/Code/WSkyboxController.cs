using UnityEngine;

public class WSkyboxController : MonoBehaviour {
	[System.Serializable]
	public struct SkyboxTheme {
		public Color SkyTint;
		public Color GroundColor;
		public float AtmosphereThickness;
		public float Exposure;
		public Color AmbientLight;
		public Color FogColor;
		public float FogDensity;
	}

	public FourDController player;

	// One theme per W layer; wraps for W layers beyond the array length
	private static readonly SkyboxTheme[] Themes = {
		// W=0: Normal daytime blue
		new SkyboxTheme {
			SkyTint           = new Color(0.50f, 0.55f, 0.70f),
			GroundColor       = new Color(0.40f, 0.38f, 0.30f),
			AtmosphereThickness = 1.0f,
			Exposure          = 1.3f,
			AmbientLight      = new Color(0.40f, 0.44f, 0.52f),
			FogColor          = new Color(0.70f, 0.75f, 0.85f),
			FogDensity        = 0.005f,
		},
		// W=1: Sunset orange
		new SkyboxTheme {
			SkyTint           = new Color(0.85f, 0.45f, 0.20f),
			GroundColor       = new Color(0.35f, 0.22f, 0.15f),
			AtmosphereThickness = 1.5f,
			Exposure          = 1.1f,
			AmbientLight      = new Color(0.55f, 0.32f, 0.18f),
			FogColor          = new Color(0.80f, 0.50f, 0.30f),
			FogDensity        = 0.008f,
		},
		// W=2: Night — thin atmosphere, dark sky
		new SkyboxTheme {
			SkyTint           = new Color(0.05f, 0.06f, 0.15f),
			GroundColor       = new Color(0.10f, 0.10f, 0.12f),
			AtmosphereThickness = 0.1f,
			Exposure          = 0.5f,
			AmbientLight      = new Color(0.08f, 0.08f, 0.15f),
			FogColor          = new Color(0.05f, 0.05f, 0.12f),
			FogDensity        = 0.015f,
		},
		// W=3: Alien teal sky
		new SkyboxTheme {
			SkyTint           = new Color(0.15f, 0.65f, 0.60f),
			GroundColor       = new Color(0.20f, 0.35f, 0.30f),
			AtmosphereThickness = 1.2f,
			Exposure          = 1.4f,
			AmbientLight      = new Color(0.20f, 0.50f, 0.45f),
			FogColor          = new Color(0.25f, 0.60f, 0.55f),
			FogDensity        = 0.007f,
		},
		// W=4: Volcanic red
		new SkyboxTheme {
			SkyTint           = new Color(0.60f, 0.15f, 0.05f),
			GroundColor       = new Color(0.30f, 0.10f, 0.05f),
			AtmosphereThickness = 2.0f,
			Exposure          = 0.9f,
			AmbientLight      = new Color(0.45f, 0.12f, 0.05f),
			FogColor          = new Color(0.55f, 0.18f, 0.08f),
			FogDensity        = 0.02f,
		},
		// W=5: Soft lavender dusk
		new SkyboxTheme {
			SkyTint           = new Color(0.55f, 0.35f, 0.70f),
			GroundColor       = new Color(0.25f, 0.20f, 0.35f),
			AtmosphereThickness = 1.3f,
			Exposure          = 1.2f,
			AmbientLight      = new Color(0.35f, 0.25f, 0.50f),
			FogColor          = new Color(0.50f, 0.35f, 0.65f),
			FogDensity        = 0.006f,
		},
	};

	private Material skyboxInstance;

	private static readonly int SkyTintID = Shader.PropertyToID("_SkyTint");
	private static readonly int GroundColorID = Shader.PropertyToID("_GroundColor");
	private static readonly int AtmosphereThicknessID = Shader.PropertyToID("_AtmosphereThickness");
	private static readonly int ExposureID = Shader.PropertyToID("_Exposure");

	void Start() {
		if (RenderSettings.skybox != null) {
			this.skyboxInstance = new Material(RenderSettings.skybox);
			RenderSettings.skybox = this.skyboxInstance;
		}

		if (this.player == null) {
			var go = GameObject.FindGameObjectWithTag("Player");
			if (go != null) {
				this.player = go.GetComponent<FourDController>();
			}
		}
	}

	void Update() {
		if (this.player == null || this.skyboxInstance == null) {
			return;
		}

		float wPos = this.player.WPosition;
		int layerA = Mathf.FloorToInt(wPos);
		int layerB = layerA + 1;
		float t = wPos - layerA;

		// Smooth step for less jarring transitions
		t = t * t * (3f - 2f * t);

		SkyboxTheme a = GetTheme(layerA);
		SkyboxTheme b = GetTheme(layerB);

		this.skyboxInstance.SetColor(SkyTintID, Color.Lerp(a.SkyTint, b.SkyTint, t));
		this.skyboxInstance.SetColor(GroundColorID, Color.Lerp(a.GroundColor, b.GroundColor, t));
		this.skyboxInstance.SetFloat(AtmosphereThicknessID, Mathf.Lerp(a.AtmosphereThickness, b.AtmosphereThickness, t));
		this.skyboxInstance.SetFloat(ExposureID, Mathf.Lerp(a.Exposure, b.Exposure, t));

		RenderSettings.ambientLight = Color.Lerp(a.AmbientLight, b.AmbientLight, t);
		RenderSettings.fogColor = Color.Lerp(a.FogColor, b.FogColor, t);
		RenderSettings.fogDensity = Mathf.Lerp(a.FogDensity, b.FogDensity, t);
		RenderSettings.fog = true;
	}

	private static SkyboxTheme GetTheme(int layer) {
		// Clamp to valid range (allow negative by mirroring)
		int idx = ((layer % Themes.Length) + Themes.Length) % Themes.Length;
		return Themes[idx];
	}
}
