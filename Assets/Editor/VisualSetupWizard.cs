using UnityEngine;
using UnityEditor;
using System.IO;

public class VisualSetupWizard {
	[MenuItem("Wave Function Collapse/Setup Visual System")]
	public static void SetupVisualSystem() {
		CreateTriplanarMaterials();
		CreatePaletteAssets();
		AssignToMapBehaviour();
		Debug.Log("Visual system setup complete.");
	}

	private static void CreateTriplanarMaterials() {
		var opaqueShader = Shader.Find("Custom/BuildingTriplanar");
		var transparentShader = Shader.Find("Custom/BuildingTriplanar_Transparent");

		if (opaqueShader == null) {
			Debug.LogError("Could not find Custom/BuildingTriplanar shader. Make sure it compiles.");
			return;
		}

		var opaque = new Material(opaqueShader);
		opaque.name = "BuildingTriplanar";
		opaque.SetFloat("_TexScale", 0.5f);
		opaque.SetFloat("_BlendSharpness", 4f);
		opaque.SetFloat("_Glossiness", 0.3f);
		opaque.SetFloat("_Metallic", 0f);
		opaque.SetFloat("_WeatherAmount", 0.3f);
		opaque.SetFloat("_WeatherHeight", 4f);
		opaque.SetFloat("_WeatherNoise", 0.2f);
		opaque.SetFloat("_ColorJitter", 0.05f);
		opaque.SetColor("_TopColor", new Color(0.8f, 0.8f, 0.8f));
		opaque.SetColor("_SideColor", new Color(0.7f, 0.7f, 0.7f));
		opaque.SetColor("_BottomColor", new Color(0.5f, 0.5f, 0.5f));
		opaque.SetColor("_WeatherColor", new Color(0.25f, 0.22f, 0.18f));
		opaque.enableInstancing = true;
		AssetDatabase.CreateAsset(opaque, "Assets/Materials/BuildingTriplanar.mat");

		if (transparentShader != null) {
			var transparent = new Material(transparentShader);
			transparent.name = "BuildingTriplanar_Transparent";
			transparent.SetFloat("_TexScale", 0.5f);
			transparent.SetFloat("_BlendSharpness", 4f);
			transparent.SetFloat("_Glossiness", 0.3f);
			transparent.SetFloat("_Metallic", 0f);
			transparent.SetFloat("_Alpha", 1f);
			transparent.SetFloat("_WeatherAmount", 0.3f);
			transparent.SetFloat("_WeatherHeight", 4f);
			transparent.SetFloat("_WeatherNoise", 0.2f);
			transparent.SetFloat("_ColorJitter", 0.05f);
			transparent.SetColor("_TopColor", new Color(0.8f, 0.8f, 0.8f));
			transparent.SetColor("_SideColor", new Color(0.7f, 0.7f, 0.7f));
			transparent.SetColor("_BottomColor", new Color(0.5f, 0.5f, 0.5f));
			transparent.SetColor("_WeatherColor", new Color(0.25f, 0.22f, 0.18f));
			transparent.enableInstancing = true;
			AssetDatabase.CreateAsset(transparent, "Assets/Materials/BuildingTriplanar_Transparent.mat");
		}
	}

	private static void CreatePaletteAssets() {
		CreatePalette("WarmResidential",
			"Warm Residential",
			new Color(0.85f, 0.75f, 0.65f),
			new Color(0.72f, 0.45f, 0.35f),
			new Color(0.55f, 0.45f, 0.40f),
			new Color(0.30f, 0.22f, 0.18f),
			new Color(1.0f, 0.85f, 0.65f), 1f,
			new Color(0.6f, 0.55f, 0.5f), 0.015f,
			0.35f);

		CreatePalette("CoolCommercial",
			"Cool Commercial",
			new Color(0.75f, 0.78f, 0.82f),
			new Color(0.55f, 0.62f, 0.72f),
			new Color(0.40f, 0.42f, 0.48f),
			new Color(0.20f, 0.22f, 0.25f),
			new Color(0.85f, 0.92f, 1.0f), 1.2f,
			new Color(0.5f, 0.55f, 0.65f), 0.02f,
			0.30f);

		CreatePalette("Industrial",
			"Industrial",
			new Color(0.60f, 0.55f, 0.50f),
			new Color(0.50f, 0.42f, 0.35f),
			new Color(0.35f, 0.30f, 0.28f),
			new Color(0.35f, 0.28f, 0.20f),
			new Color(1.0f, 0.75f, 0.50f), 0.9f,
			new Color(0.55f, 0.48f, 0.42f), 0.035f,
			0.20f);

		CreatePalette("ParkGreen",
			"Park Green",
			new Color(0.50f, 0.65f, 0.45f),
			new Color(0.65f, 0.62f, 0.55f),
			new Color(0.40f, 0.38f, 0.32f),
			new Color(0.28f, 0.30f, 0.20f),
			new Color(0.95f, 1.0f, 0.85f), 0.8f,
			new Color(0.45f, 0.55f, 0.48f), 0.01f,
			0.15f);

		CreatePalette("NightNeon",
			"Night Neon",
			new Color(0.15f, 0.15f, 0.20f),
			new Color(0.20f, 0.18f, 0.25f),
			new Color(0.10f, 0.10f, 0.12f),
			new Color(0.08f, 0.08f, 0.10f),
			new Color(0.7f, 0.3f, 1.0f), 1.5f,
			new Color(0.15f, 0.12f, 0.25f), 0.025f,
			0.10f);
	}

	private static void CreatePalette(string fileName, string paletteName,
		Color top, Color side, Color bottom, Color weather,
		Color light, float lightIntensity,
		Color fog, float fogDensity,
		float probability) {

		var palette = ScriptableObject.CreateInstance<MaterialPalette>();
		palette.PaletteName = paletteName;
		palette.TopColor = top;
		palette.SideColor = side;
		palette.BottomColor = bottom;
		palette.WeatherColor = weather;
		palette.LightColor = light;
		palette.LightIntensity = lightIntensity;
		palette.FogColor = fog;
		palette.FogDensity = fogDensity;
		palette.Probability = probability;

		AssetDatabase.CreateAsset(palette, "Assets/Materials/Palettes/" + fileName + ".asset");
	}

	private static void AssignToMapBehaviour() {
		var mapBehaviour = Object.FindObjectOfType<MapBehaviour>();
		if (mapBehaviour == null) {
			Debug.LogWarning("No MapBehaviour found in scene. Open your scene first, then run setup again.");
			return;
		}

		var opaqueMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BuildingTriplanar.mat");
		var transparentMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BuildingTriplanar_Transparent.mat");

		if (opaqueMat != null) {
			mapBehaviour.TriplanarMaterial = opaqueMat;
		}
		if (transparentMat != null) {
			mapBehaviour.TriplanarTransparentMaterial = transparentMat;
		}

		var paletteGuids = AssetDatabase.FindAssets("t:MaterialPalette", new[] { "Assets/Materials/Palettes" });
		var palettes = new MaterialPalette[paletteGuids.Length];
		for (int i = 0; i < paletteGuids.Length; i++) {
			var path = AssetDatabase.GUIDToAssetPath(paletteGuids[i]);
			palettes[i] = AssetDatabase.LoadAssetAtPath<MaterialPalette>(path);
		}
		mapBehaviour.Palettes = palettes;

		// Set up compatibility: each palette is compatible with itself and neighbors
		for (int i = 0; i < palettes.Length; i++) {
			palettes[i].CompatibleWith = new bool[palettes.Length];
			for (int j = 0; j < palettes.Length; j++) {
				int dist = Mathf.Abs(i - j);
				palettes[i].CompatibleWith[j] = dist <= 1;
			}
			palettes[i].CompatibleWith[i] = true;
			EditorUtility.SetDirty(palettes[i]);
		}

		// Add ChunkLightPlacer if not present
		if (mapBehaviour.GetComponent<ChunkLightPlacer>() == null) {
			mapBehaviour.gameObject.AddComponent<ChunkLightPlacer>();
		}

		// Add ChunkAtmosphere if not present
		if (mapBehaviour.GetComponent<ChunkAtmosphere>() == null) {
			mapBehaviour.gameObject.AddComponent<ChunkAtmosphere>();
		}

		// Add DecorationPlacer if not present
		if (mapBehaviour.GetComponent<DecorationPlacer>() == null) {
			mapBehaviour.gameObject.AddComponent<DecorationPlacer>();
		}

		EditorUtility.SetDirty(mapBehaviour);
		AssetDatabase.SaveAssets();
		Debug.Log("Assigned materials, palettes, and components to MapBehaviour.");
	}

	[MenuItem("Wave Function Collapse/Setup Visual System", true)]
	private static bool ValidateSetup() {
		return !Application.isPlaying;
	}
}
