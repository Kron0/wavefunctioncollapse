using System.Collections.Generic;
using UnityEngine;

public class WLayerHUD : MonoBehaviour {
	public FourDController player;

	// ─── Textures ───────────────────────────────────────────────────────────
	private Texture2D white;
	private Texture2D black;

	// ─── Styles ─────────────────────────────────────────────────────────────
	private GUIStyle headerStyle;
	private GUIStyle valueStyle;
	private GUIStyle subStyle;
	private GUIStyle microStyle;
	private GUIStyle labelStyle;
	private GUIStyle centeredMicro;

	private GenerateMap4DNearPlayer generator;

	// Accent colors per W layer (cycles)
	private static readonly Color[] LayerAccents = {
		new Color(0.00f, 0.78f, 1.00f),  // W0  cyan
		new Color(1.00f, 0.49f, 0.00f),  // W1  amber
		new Color(0.38f, 0.45f, 1.00f),  // W2  indigo
		new Color(0.00f, 1.00f, 0.71f),  // W3  mint
		new Color(1.00f, 0.19f, 0.19f),  // W4  red
		new Color(0.73f, 0.50f, 1.00f),  // W5  lavender
	};

	// ─── State ──────────────────────────────────────────────────────────────
	private float accentPulse;  // 0..1 smooth breathing animation

	// Screen-edge vignette ─────────────────────────────────────────────────
	private Texture2D vignetteTop;
	private Texture2D vignetteBottom;
	private Texture2D vignetteLeft;
	private Texture2D vignetteRight;

	// ─── Layout constants (reference resolution 1920×1080) ─────────────────
	private const float REF_W = 1920f;
	private const float REF_H = 1080f;

	// =========================================================================
	// INIT
	// =========================================================================
	void Start() {
		this.white = Solid(Color.white);
		this.black = Solid(Color.black);

		this.headerStyle = new GUIStyle {
			fontSize = 10,
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleLeft,
			wordWrap = false,
		};
		this.headerStyle.normal.textColor = new Color(1, 1, 1, 0.45f);

		this.valueStyle = new GUIStyle {
			fontSize = 36,
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleLeft,
			wordWrap = false,
		};
		this.valueStyle.normal.textColor = Color.white;

		this.subStyle = new GUIStyle {
			fontSize = 14,
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleLeft,
			wordWrap = false,
		};
		this.subStyle.normal.textColor = new Color(1, 1, 1, 0.75f);

		this.microStyle = new GUIStyle {
			fontSize = 11,
			alignment = TextAnchor.MiddleLeft,
			wordWrap = false,
		};
		this.microStyle.normal.textColor = new Color(1, 1, 1, 0.55f);

		this.labelStyle = new GUIStyle {
			fontSize = 11,
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleLeft,
			wordWrap = false,
		};
		this.labelStyle.normal.textColor = new Color(1, 1, 1, 0.85f);

		this.centeredMicro = new GUIStyle {
			fontSize = 10,
			alignment = TextAnchor.MiddleCenter,
			wordWrap = false,
		};
		this.centeredMicro.normal.textColor = new Color(1, 1, 1, 0.55f);

		this.vignetteTop    = GradientH(new Color(0,0,0,0.55f), new Color(0,0,0,0));
		this.vignetteBottom = GradientH(new Color(0,0,0,0), new Color(0,0,0,0.55f));
		this.vignetteLeft   = GradientV(new Color(0,0,0,0.45f), new Color(0,0,0,0));
		this.vignetteRight  = GradientV(new Color(0,0,0,0), new Color(0,0,0,0.45f));

		if (this.player == null) {
			var go = GameObject.FindGameObjectWithTag("Player");
			if (go != null) this.player = go.GetComponent<FourDController>();
		}
		this.generator = Object.FindObjectOfType<GenerateMap4DNearPlayer>();
	}

	// =========================================================================
	// UPDATE
	// =========================================================================
	void Update() {
		this.accentPulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 1.4f);
	}

	// =========================================================================
	// ONGUI
	// =========================================================================
	void OnGUI() {
		if (this.player == null) return;

		float sw = Screen.width;
		float sh = Screen.height;
		float s  = Mathf.Min(sw / REF_W, sh / REF_H);  // uniform scale factor

		float wPos   = this.player.WPosition;
		int   layer  = MapBehaviour4D.ActiveWLayer;
		float frac   = wPos - Mathf.Floor(wPos);
		Color accent = this.AccentForLayer(layer, this.accentPulse);
		Color accentDim = accent * 0.35f;
		accentDim.a = 0.35f;

		// ── Scale all font sizes ────────────────────────────────────────────
		int origHeader = this.headerStyle.fontSize;
		int origValue  = this.valueStyle.fontSize;
		int origSub    = this.subStyle.fontSize;
		int origMicro  = this.microStyle.fontSize;
		int origLabel  = this.labelStyle.fontSize;
		int origCMicro = this.centeredMicro.fontSize;
		this.headerStyle.fontSize = Mathf.RoundToInt(10 * s);
		this.valueStyle.fontSize  = Mathf.RoundToInt(36 * s);
		this.subStyle.fontSize    = Mathf.RoundToInt(14 * s);
		this.microStyle.fontSize  = Mathf.RoundToInt(11 * s);
		this.labelStyle.fontSize  = Mathf.RoundToInt(11 * s);
		this.centeredMicro.fontSize = Mathf.RoundToInt(10 * s);

		// ── Screen-edge vignette ────────────────────────────────────────────
		float vigH = sh * 0.18f;
		float vigV = sw * 0.12f;
		GUI.color = Color.white;
		GUI.DrawTexture(new Rect(0, 0, sw, vigH), this.vignetteTop);
		GUI.DrawTexture(new Rect(0, sh - vigH, sw, vigH), this.vignetteBottom);
		GUI.DrawTexture(new Rect(0, 0, vigV, sh), this.vignetteLeft);
		GUI.DrawTexture(new Rect(sw - vigV, 0, vigV, sh), this.vignetteRight);

		// ── Thin accent line across top ─────────────────────────────────────
		GUI.color = new Color(accent.r, accent.g, accent.b, 0.6f);
		GUI.DrawTexture(new Rect(0, 0, sw, Mathf.Max(1f, 2f * s)), this.white);
		GUI.color = Color.white;

		// ── W Dimension Panel (top-right) ───────────────────────────────────
		this.DrawDimensionPanel(sw, sh, s, wPos, layer, frac, accent, accentDim);

		// ── Minimap (bottom-left) ────────────────────────────────────────────
		this.DrawMinimap(sw, sh, s, layer, accent);

		// ── Restore font sizes ───────────────────────────────────────────────
		this.headerStyle.fontSize = origHeader;
		this.valueStyle.fontSize  = origValue;
		this.subStyle.fontSize    = origSub;
		this.microStyle.fontSize  = origMicro;
		this.labelStyle.fontSize  = origLabel;
		this.centeredMicro.fontSize = origCMicro;
	}

	// =========================================================================
	// DIMENSION PANEL
	// =========================================================================
	private void DrawDimensionPanel(float sw, float sh, float s,
		float wPos, int layer, float frac, Color accent, Color accentDim) {

		float pw  = 230f * s;
		float ph  = 220f * s;
		float px  = sw - pw - 24f * s;
		float py  = 20f * s;
		float pad = 14f * s;

		// Panel background — layered darks for depth
		this.FillRect(px, py, pw, ph, new Color(0, 0, 0, 0.72f));
		this.FillRect(px, py, pw, 2f * s, accent * 0.9f);                    // top accent bar
		this.FillRect(px + pw - 2f * s, py, 2f * s, ph, accentDim);         // right edge
		this.FillRect(px, py + ph - 2f * s, pw, 2f * s, new Color(1,1,1,0.06f)); // bottom edge

		float cx = px + pad;
		float cy = py + pad;

		// ── "PERSONAL DIMENSION" header ─────────────────────────────────────
		this.headerStyle.normal.textColor = new Color(accent.r, accent.g, accent.b, 0.7f);
		GUI.Label(new Rect(cx, cy, pw, 16f * s), "PERSONAL DIMENSION", this.headerStyle);
		cy += 18f * s;

		// ── Big W value ──────────────────────────────────────────────────────
		this.valueStyle.normal.textColor = Color.white;
		GUI.Label(new Rect(cx, cy, pw, 44f * s), $"W {wPos:+0.00;-0.00;0.00}", this.valueStyle);
		cy += 42f * s;

		// ── Layer dots ───────────────────────────────────────────────────────
		float dotR    = 7f * s;
		float dotGap  = 5f * s;
		int   numDots = Mathf.Min(LayerAccents.Length, 6);
		float dotsW   = numDots * (dotR * 2 + dotGap) - dotGap;

		for (int i = 0; i < numDots; i++) {
			float dx = cx + i * (dotR * 2 + dotGap);
			float dy = cy + 1f * s;
			Color dc = this.AccentForLayer(i, this.accentPulse);

			if (i == layer) {
				// Active layer: bright glow
				this.FillRect(dx - 2f*s, dy - 2f*s, dotR * 2 + 4f*s, dotR * 2 + 4f*s,
					new Color(dc.r, dc.g, dc.b, 0.25f));
				this.FillRect(dx, dy, dotR * 2, dotR * 2, dc);
			} else {
				// Inactive: dim outline only
				this.FillRect(dx, dy, dotR * 2, dotR * 2, new Color(1, 1, 1, 0.12f));
				this.FillRect(dx + s, dy + s, dotR * 2 - 2*s, dotR * 2 - 2*s,
					new Color(0, 0, 0, 0.6f));
			}
		}
		cy += dotR * 2 + 10f * s;

		// ── Layer fractional bar ─────────────────────────────────────────────
		this.microStyle.normal.textColor = new Color(1, 1, 1, 0.4f);
		GUI.Label(new Rect(cx, cy, pw, 14f * s), $"LAYER {layer}", this.microStyle);
		cy += 15f * s;

		float barW = pw - pad * 2;
		float barH = 3f * s;
		this.FillRect(cx, cy, barW, barH, new Color(1, 1, 1, 0.1f));
		this.FillRect(cx, cy, barW * frac, barH, new Color(accent.r, accent.g, accent.b, 0.9f));
		cy += barH + 14f * s;

		// ── Divider ──────────────────────────────────────────────────────────
		this.FillRect(cx, cy, pw - pad * 2, 1f * s, new Color(1, 1, 1, 0.1f));
		cy += 8f * s;

		// ── Collectibles ─────────────────────────────────────────────────────
		int   found  = CollectiblePlacer.TotalCollected;
		int   total  = CollectiblePlacer.TotalPlaced;
		float pct    = total > 0 ? (float)found / total : 0f;

		this.labelStyle.normal.textColor = Color.white;
		this.microStyle.normal.textColor = new Color(1, 1, 1, 0.45f);

		GUI.Label(new Rect(cx, cy, 80f * s, 16f * s), "ARTIFACTS", this.microStyle);
		GUI.Label(new Rect(cx + 85f * s, cy, 60f * s, 16f * s), $"{found} / {total}", this.labelStyle);
		cy += 18f * s;

		float artW = pw - pad * 2;
		float artH = 2f * s;
		this.FillRect(cx, cy, artW, artH, new Color(1, 1, 1, 0.08f));
		this.FillRect(cx, cy, artW * pct, artH, new Color(1f, 0.9f, 0.3f, 0.8f));
		cy += artH + 10f * s;

		// ── Nearest landmark ─────────────────────────────────────────────────
		if (LandmarkPlacer.LandmarkCount > 0 && Camera.main != null) {
			Vector3 nearest     = this.NearestLandmark(this.player.transform.position, out float dist);
			Vector3 viewport    = Camera.main.WorldToViewportPoint(nearest);
			bool    onScreen    = viewport.z > 0f && viewport.x > 0.05f && viewport.x < 0.95f
				&& viewport.y > 0.05f && viewport.y < 0.95f;
			string  bearing     = onScreen ? "•" : this.ViewportArrow(viewport);

			this.microStyle.normal.textColor = new Color(accent.r, accent.g, accent.b, 0.7f);
			GUI.Label(new Rect(cx, cy, 80f * s, 16f * s), "BEACON", this.microStyle);

			this.labelStyle.normal.textColor = Color.white;
			GUI.Label(new Rect(cx + 60f * s, cy, 80f * s, 16f * s),
				$"{bearing}  {dist:F0}m", this.labelStyle);
		}
	}

	// =========================================================================
	// MINIMAP
	// =========================================================================
	private void DrawMinimap(float sw, float sh, float s, int activeLayer, Color accent) {
		if (this.generator == null) return;

		float mapSize = 170f * s;
		float mx = 24f * s;
		float my = sh - mapSize - 24f * s;

		// Panel
		this.FillRect(mx, my, mapSize, mapSize, new Color(0, 0, 0, 0.7f));
		this.FillRect(mx, my, mapSize, 2f * s, new Color(accent.r, accent.g, accent.b, 0.5f));
		this.FillRect(mx, my + mapSize - 2f*s, mapSize, 2f * s, new Color(1,1,1,0.06f));
		this.FillRect(mx + mapSize - 2f*s, my, 2f * s, mapSize, new Color(1,1,1,0.04f));

		// Header
		float mapHeaderH = 18f * s;
		this.headerStyle.normal.textColor = new Color(accent.r, accent.g, accent.b, 0.6f);
		GUI.Label(new Rect(mx + 8f * s, my + 4f * s, mapSize, mapHeaderH), "SPATIAL MAP", this.headerStyle);

		float innerX = mx + 8f * s;
		float innerY = my + mapHeaderH + 4f * s;
		float innerW = mapSize - 16f * s;
		float innerH = mapSize - mapHeaderH - 16f * s;

		// Grid lines
		int gridLines = 6;
		for (int i = 0; i <= gridLines; i++) {
			float gx = innerX + innerW * i / gridLines;
			float gy = innerY + innerH * i / gridLines;
			this.FillRect(gx, innerY, Mathf.Max(0.5f, s * 0.5f), innerH, new Color(1,1,1,0.04f));
			this.FillRect(innerX, gy, innerW, Mathf.Max(0.5f, s * 0.5f), new Color(1,1,1,0.04f));
		}

		// Chunks
		int chunkSize     = this.generator != null ? this.generator.ChunkSize : 4;
		float worldChunkW = AbstractMap4D.BLOCK_SIZE * chunkSize;
		float chunkPx     = Mathf.Min(innerW, innerH) / 16f;  // 16 chunks fits across map

		Vector3 playerPos  = this.player.transform.position;
		float playerChunkX = playerPos.x / worldChunkW;
		float playerChunkZ = playerPos.z / worldChunkW;
		float centerX      = innerX + innerW * 0.5f;
		float centerZ      = innerY + innerH * 0.5f;

		var chunks = this.generator.GeneratedChunks;
		foreach (var c in chunks) {
			float relX = (c.x - playerChunkX) * chunkPx;
			float relZ = (c.z - playerChunkZ) * chunkPx;
			float scx  = centerX + relX - chunkPx * 0.5f;
			float scz  = centerZ - relZ - chunkPx * 0.5f;

			if (scx < innerX || scx + chunkPx > innerX + innerW) continue;
			if (scz < innerY || scz + chunkPx > innerY + innerH) continue;

			int   wi  = ((c.w % LayerAccents.Length) + LayerAccents.Length) % LayerAccents.Length;
			Color lc  = LayerAccents[wi];
			bool  cur = (c.w * chunkSize <= activeLayer && activeLayer < (c.w + 1) * chunkSize);
			float a   = cur ? 0.75f : 0.22f;

			GUI.color = new Color(lc.r, lc.g, lc.b, a);
			GUI.DrawTexture(new Rect(scx + 1, scz + 1, chunkPx - 2, chunkPx - 2), this.white);
		}
		GUI.color = Color.white;

		// Player marker — forward-pointing triangle
		float pdotW = 8f * s;
		float pdotH = 8f * s;
		this.FillRect(centerX - pdotW * 0.5f, centerZ - pdotH * 0.5f, pdotW, pdotH,
			new Color(accent.r, accent.g, accent.b, 1f));

		// W layer color legend dots (bottom strip)
		float legendY = my + mapSize + 4f * s;
		for (int i = 0; i < Mathf.Min(LayerAccents.Length, 6); i++) {
			float lx = mx + i * (14f * s);
			Color lc = LayerAccents[i];
			bool  cur = i == (((activeLayer) % LayerAccents.Length) + LayerAccents.Length) % LayerAccents.Length;
			GUI.color = new Color(lc.r, lc.g, lc.b, cur ? 0.9f : 0.3f);
			GUI.DrawTexture(new Rect(lx, legendY, 10f * s, 5f * s), this.white);
		}
		GUI.color = Color.white;
	}

	// =========================================================================
	// HELPERS
	// =========================================================================
	private void FillRect(float x, float y, float w, float h, Color col) {
		GUI.color = col;
		GUI.DrawTexture(new Rect(x, y, w, h), this.white);
		GUI.color = Color.white;
	}

	private Color AccentForLayer(int layer, float pulse) {
		int idx = ((layer % LayerAccents.Length) + LayerAccents.Length) % LayerAccents.Length;
		Color c = LayerAccents[idx];
		float b = 0.80f + 0.20f * pulse;
		return new Color(c.r * b, c.g * b, c.b * b, 1f);
	}

	private Vector3 NearestLandmark(Vector3 from, out float dist) {
		Vector3 nearest = LandmarkPlacer.LandmarkWorldPositions[0];
		dist = float.MaxValue;
		foreach (var p in LandmarkPlacer.LandmarkWorldPositions) {
			float d = Vector3.Distance(from, p);
			if (d < dist) { dist = d; nearest = p; }
		}
		return nearest;
	}

	private string ViewportArrow(Vector3 viewport) {
		Vector2 dir = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
		if (viewport.z <= 0f) dir = -dir;
		float angle = (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 360f) % 360f;
		int   sector = Mathf.RoundToInt(angle / 45f) % 8;
		string[] arrows = { "→", "↗", "↑", "↖", "←", "↙", "↓", "↘" };
		return arrows[sector];
	}

	private static Texture2D Solid(Color col) {
		var t = new Texture2D(1, 1);
		t.SetPixel(0, 0, col);
		t.Apply();
		return t;
	}

	// Vertical gradient: top → bottom
	private static Texture2D GradientH(Color top, Color bottom) {
		int h = 32;
		var t = new Texture2D(1, h) { wrapMode = TextureWrapMode.Clamp };
		for (int y = 0; y < h; y++) {
			t.SetPixel(0, y, Color.Lerp(top, bottom, (float)y / (h - 1)));
		}
		t.Apply();
		return t;
	}

	// Horizontal gradient: left → right
	private static Texture2D GradientV(Color left, Color right) {
		int w = 32;
		var t = new Texture2D(w, 1) { wrapMode = TextureWrapMode.Clamp };
		for (int x = 0; x < w; x++) {
			t.SetPixel(x, 0, Color.Lerp(left, right, (float)x / (w - 1)));
		}
		t.Apply();
		return t;
	}
}
