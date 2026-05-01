using UnityEngine;
using UnityEditor;

// Interactive EditorWindow wizard for configuring the 4D city scene.
// Shows live status of required components and lets the user run setup in one click.
public class FourDSetupWindow : EditorWindow {
	private Vector2 scroll;
	private GUIStyle titleStyle;
	private GUIStyle sectionStyle;
	private GUIStyle statusOkStyle;
	private GUIStyle statusMissingStyle;
	private bool stylesBuilt;

	[MenuItem("Wave Function Collapse/Open Setup Window")]
	public static void Open() {
		var w = GetWindow<FourDSetupWindow>("4D City Setup");
		w.minSize = new Vector2(380f, 580f);
		w.Show();
	}

	void OnGUI() {
		this.EnsureStyles();

		this.scroll = EditorGUILayout.BeginScrollView(this.scroll);
		GUILayout.Space(8f);

		// ── Header ────────────────────────────────────────────────────────────
		GUILayout.Label("4D Wave Function Collapse City", this.titleStyle);
		GUILayout.Label("Use the buttons below to configure the scene.", EditorStyles.miniLabel);
		GUILayout.Space(10f);

		// ── Full auto-setup ───────────────────────────────────────────────────
		using (new EditorGUILayout.HorizontalScope()) {
			if (GUILayout.Button("▶  Run Full 4D Setup", GUILayout.Height(32f))) {
				FourDSetupWizard.SetupFourDScene();
			}
			if (GUILayout.Button("▶  Run Visual Setup", GUILayout.Height(32f))) {
				VisualSetupWizard.SetupVisualSystem();
			}
		}

		GUILayout.Space(14f);

		// ── Scene status ──────────────────────────────────────────────────────
		this.DrawSection("Scene Status");
		var map4D = GameObject.Find("Map4D");
		var player = GameObject.Find("Player") ?? GameObject.Find("Player4D");

		this.DrawStatus("Map4D object", map4D != null);
		this.DrawStatus("Player object", player != null);

		if (map4D != null) {
			GUILayout.Space(4f);
			EditorGUI.indentLevel++;
			this.DrawComponentStatus<MapBehaviour4D>(map4D,    "MapBehaviour4D");
			this.DrawComponentStatus<TesseractProjection>(map4D, "TesseractProjection");
			this.DrawComponentStatus<GenerateMap4DNearPlayer>(map4D, "GenerateMap4DNearPlayer");
			this.DrawComponentStatus<DistrictStyler>(map4D,   "DistrictStyler");
			this.DrawComponentStatus<LandmarkPlacer>(map4D,   "LandmarkPlacer");
			this.DrawComponentStatus<CollectiblePlacer>(map4D, "CollectiblePlacer");
			this.DrawComponentStatus<WGatePlacer>(map4D,      "WGatePlacer");
			this.DrawComponentStatus<DecorationPlacer4D>(map4D, "DecorationPlacer4D");
			this.DrawComponentStatus<HumanDetailPlacer>(map4D, "HumanDetailPlacer");
			this.DrawComponentStatus<ClockHandPlacer>(map4D,  "ClockHandPlacer");
			this.DrawComponentStatus<ArtworkPlacer>(map4D,    "ArtworkPlacer");
			this.DrawComponentStatus<GroundPlane>(map4D,      "GroundPlane");
			EditorGUI.indentLevel--;
		}

		if (player != null) {
			GUILayout.Space(4f);
			EditorGUI.indentLevel++;
			this.DrawComponentStatus<FourDController>(player,  "FourDController");
			this.DrawComponentStatus<WLayerHUD>(player,        "WLayerHUD");
			this.DrawComponentStatus<WLayerColorizer>(player,  "WLayerColorizer");
			this.DrawComponentStatus<WTransitionEffect>(player, "WTransitionEffect");
			this.DrawComponentStatus<WSkyboxController>(player, "WSkyboxController");
			this.DrawComponentStatus<ProceduralAmbience>(player, "ProceduralAmbience");
			this.DrawComponentStatus<PauseMenu>(player,        "PauseMenu");
			EditorGUI.indentLevel--;
		}

		GUILayout.Space(14f);

		// ── Individual actions ────────────────────────────────────────────────
		this.DrawSection("Individual Actions");

		if (map4D != null) {
			using (new EditorGUILayout.HorizontalScope()) {
				if (GUILayout.Button("Focus Map4D")) Selection.activeGameObject = map4D;
				if (GUILayout.Button("Focus Player") && player != null) Selection.activeGameObject = player;
			}
			GUILayout.Space(4f);

			var gen = map4D.GetComponent<GenerateMap4DNearPlayer>();
			if (gen != null) {
				EditorGUI.BeginChangeCheck();
				gen.ChunkSize = EditorGUILayout.IntField("Chunk Size", gen.ChunkSize);
				gen.Range     = EditorGUILayout.FloatField("Generation Range", gen.Range);
				if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(gen);
			}

			var styler = map4D.GetComponent<DistrictStyler>();
			if (styler != null) {
				GUILayout.Space(4f);
				EditorGUI.BeginChangeCheck();
				styler.DistrictSeed   = EditorGUILayout.IntField("District Seed", styler.DistrictSeed);
				styler.DistrictCount  = EditorGUILayout.IntField("District Count", styler.DistrictCount);
				styler.BoundaryWidth  = EditorGUILayout.Slider("Boundary Width", styler.BoundaryWidth, 0f, 0.5f);
				styler.Mode           = (DistrictStyler.TextureSource)EditorGUILayout.EnumPopup("Texture Mode", styler.Mode);
				if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(styler);
			}
		}

		GUILayout.Space(14f);

		// ── Tips ──────────────────────────────────────────────────────────────
		this.DrawSection("Tips");
		EditorGUILayout.HelpBox(
			"• Press Q/E to shift W dimension\n" +
			"• Press C to toggle building colour tint\n" +
			"• Cyan/magenta walls are W-gates — reach the shown layer to pass through\n" +
			"• Gold diamonds on rooftops are collectible artifacts\n" +
			"• Orange beacon towers mark landmarks visible from afar",
			MessageType.Info);

		EditorGUILayout.EndScrollView();
	}

	void OnInspectorUpdate() {
		Repaint();
	}

	private void DrawSection(string label) {
		GUILayout.Label(label, this.sectionStyle);
		var rect = GUILayoutUtility.GetLastRect();
		EditorGUI.DrawRect(new Rect(rect.x, rect.yMax, rect.width, 1f), new Color(0.5f, 0.5f, 0.5f, 0.3f));
		GUILayout.Space(4f);
	}

	private void DrawStatus(string label, bool ok) {
		using (new EditorGUILayout.HorizontalScope()) {
			GUILayout.Label(label);
			GUILayout.Label(ok ? "✓ present" : "✗ missing",
				ok ? this.statusOkStyle : this.statusMissingStyle,
				GUILayout.Width(90f));
		}
	}

	private void DrawComponentStatus<T>(GameObject go, string label) where T : Component {
		bool present = go.GetComponent<T>() != null;
		using (new EditorGUILayout.HorizontalScope()) {
			GUILayout.Label(label, EditorStyles.miniLabel);
			GUILayout.Label(present ? "✓" : "✗",
				present ? this.statusOkStyle : this.statusMissingStyle,
				GUILayout.Width(24f));
			if (!present && GUILayout.Button("Add", EditorStyles.miniButton, GUILayout.Width(40f))) {
				go.AddComponent<T>();
				EditorUtility.SetDirty(go);
			}
		}
	}

	private void EnsureStyles() {
		if (this.stylesBuilt) return;
		this.titleStyle = new GUIStyle(EditorStyles.boldLabel) {
			fontSize  = 16,
			alignment = TextAnchor.MiddleLeft,
		};
		this.sectionStyle = new GUIStyle(EditorStyles.boldLabel) {
			fontSize = 11,
		};
		this.statusOkStyle = new GUIStyle(EditorStyles.miniLabel) {
			normal = { textColor = new Color(0.25f, 0.85f, 0.35f) },
			alignment = TextAnchor.MiddleRight,
		};
		this.statusMissingStyle = new GUIStyle(EditorStyles.miniLabel) {
			normal = { textColor = new Color(0.90f, 0.35f, 0.25f) },
			alignment = TextAnchor.MiddleRight,
		};
		this.stylesBuilt = true;
	}
}
