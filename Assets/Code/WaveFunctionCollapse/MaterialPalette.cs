using UnityEngine;

[CreateAssetMenu(menuName = "Wave Function Collapse/Material Palette", fileName = "palette.asset")]
public class MaterialPalette : ScriptableObject {
	public string PaletteName;

	public Color TopColor = new Color(0.8f, 0.8f, 0.8f);
	public Color SideColor = new Color(0.7f, 0.7f, 0.7f);
	public Color BottomColor = new Color(0.5f, 0.5f, 0.5f);
	public Color WeatherColor = new Color(0.25f, 0.22f, 0.18f);

	[Range(0f, 1f)]
	public float Probability = 0.25f;

	public Color LightColor = new Color(1f, 0.9f, 0.8f);
	public float LightIntensity = 1f;

	public Color FogColor = new Color(0.5f, 0.5f, 0.6f);
	[Range(0f, 0.1f)]
	public float FogDensity = 0.02f;

	public bool[] CompatibleWith;

	public static MaterialPalette CreateDefault(string name, Color top, Color side, Color bottom, Color weather, Color light, Color fog, float fogDensity, float probability) {
		var palette = ScriptableObject.CreateInstance<MaterialPalette>();
		palette.PaletteName = name;
		palette.TopColor = top;
		palette.SideColor = side;
		palette.BottomColor = bottom;
		palette.WeatherColor = weather;
		palette.LightColor = light;
		palette.LightIntensity = 1f;
		palette.FogColor = fog;
		palette.FogDensity = fogDensity;
		palette.Probability = probability;
		return palette;
	}
}
