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

	private static Color[] LayerAccents => DimensionColors.LayerAccents;

	// ─── State ──────────────────────────────────────────────────────────────
	private float accentPulse;

	// Startup fade state
	public static bool StartupComplete { get; private set; }
	private float fadeAlpha = 1f;
	private bool fadeStarted = false;
	private const int MIN_BLOCKS_TO_FADE = 40;

	// Screen-edge vignette
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
		StartupComplete = false;
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

		// Startup fade: wait for first chunk then fade in over 1.2s; block movement until complete
		if (this.fadeAlpha > 0f) {
			if (!this.fadeStarted) {
				int built = this.generator != null ? this.generator.GeneratedChunks.Length : 0;
				if (built > 0 || Time.time > 6f) {
					this.fadeStarted = true;
				}
			} else {
				this.fadeAlpha = Mathf.Max(0f, this.fadeAlpha - Time.deltaTime / 1.2f);
				if (this.fadeAlpha <= 0f) {
					StartupComplete = true;
				}
			}
		}
	}

	// =========================================================================
	// ONGUI
	// =========================================================================
	void OnGUI() {
		float sw = Screen.width;
		float sh = Screen.height;
		float s  = Mathf.Min(sw / REF_W, sh / REF_H);

		// ── Startup black fade ───────────────────────────────────────────────
		if (this.fadeAlpha > 0f) {
			GUI.color = new Color(0, 0, 0, this.fadeAlpha);
			GUI.DrawTexture(new Rect(0, 0, sw, sh), this.white);
			GUI.color = Color.white;
		}

		if (this.player == null) return;

		float wPos   = this.player.WPosition;
		int   layer  = MapBehaviour4D.ActiveWLayer;
		float frac   = wPos - Mathf.Floor(wPos);
		Color accent = this.AccentForLayer(layer, this.accentPulse);
		Color accentDim = accent * 0.35f;
		accentDim.a = 0.35f;

		// ── Scale all font sizes ─────────────────────────────────────────────
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

		// ── Screen-edge vignette ─────────────────────────────────────────────
		float vigH = sh * 0.18f;
		float vigV = sw * 0.12f;
		GUI.color = Color.white;
		GUI.DrawTexture(new Rect(0, 0, sw, vigH), this.vignetteTop);
		GUI.DrawTexture(new Rect(0, sh - vigH, sw, vigH), this.vignetteBottom);
		GUI.DrawTexture(new Rect(0, 0, vigV, sh), this.vignetteLeft);
		GUI.DrawTexture(new Rect(sw - vigV, 0, vigV, sh), this.vignetteRight);

		// ── Thin accent line across top ──────────────────────────────────────
		GUI.color = new Color(accent.r, accent.g, accent.b, 0.6f);
		GUI.DrawTexture(new Rect(0, 0, sw, Mathf.Max(1f, 2f * s)), this.white);
		GUI.color = Color.white;

		// ── W Dimension Panel (top-right) ────────────────────────────────────
		this.DrawDimensionPanel(sw, sh, s, wPos, layer, frac, accent, accentDim);

		// ── Minimap + W axis (bottom-left) ───────────────────────────────────
		this.DrawMinimap(sw, sh, s, layer, wPos, accent);

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

		// Panel background
		this.FillRect(px, py, pw, ph, new Color(0, 0, 0, 0.72f));

		// Angled top-left notch: cover top-left corner with a dark triangle effect
		// (two overlapping rects to simulate a chamfered corner)
		float chamfer = 10f * s;
		this.FillRect(px, py, chamfer, chamfer, new Color(0, 0, 0, 0.0f)); // transparent, just structural

		// Top accent bar (full width except left notch)
		this.FillRect(px + chamfer, py, pw - chamfer, 2f * s, accent * 0.9f);
		// Left edge diagonal: a thin vertical bar with slight offset to suggest angle
		this.FillRect(px, py + chamfer, 2f * s, ph - chamfer, accentDim);
		// Diagonal notch line (simulate the chamfer cut)
		for (int i = 0; i < (int)(chamfer / s); i++) {
			this.FillRect(px + i * s, py + (chamfer - i * s), 2f * s, s, new Color(accent.r, accent.g, accent.b, 0.3f));
		}
		// Right edge
		this.FillRect(px + pw - 2f * s, py, 2f * s, ph, accentDim);
		// Bottom edge
		this.FillRect(px, py + ph - 2f * s, pw, 2f * s, new Color(1,1,1,0.06f));

		// Corner bracket details — top-right and bottom-left
		this.DrawCornerBracket(px + pw - 16f*s, py, 14f*s, accent, s, true, true);    // top-right
		this.DrawCornerBracket(px, py + ph - 16f*s, 14f*s, accent, s, false, false);  // bottom-left

		float cx = px + pad;
		float cy = py + pad;

		// ── "PERSONAL DIMENSION" header ──────────────────────────────────────
		// Small accent chevron before header text
		this.FillRect(cx, cy + 3f*s, 3f*s, 9f*s, new Color(accent.r, accent.g, accent.b, 0.8f));
		this.FillRect(cx + 4f*s, cy + 5f*s, 3f*s, 5f*s, new Color(accent.r, accent.g, accent.b, 0.5f));
		this.headerStyle.normal.textColor = new Color(accent.r, accent.g, accent.b, 0.7f);
		GUI.Label(new Rect(cx + 10f*s, cy, pw, 16f * s), "PERSONAL DIMENSION", this.headerStyle);
		cy += 18f * s;

		// ── Big W value ───────────────────────────────────────────────────────
		this.valueStyle.normal.textColor = Color.white;
		GUI.Label(new Rect(cx, cy, pw, 44f * s), $"W {wPos:+0.00;-0.00;0.00}", this.valueStyle);
		cy += 42f * s;

		// ── Layer dots (pill-shaped) ──────────────────────────────────────────
		float dotW   = 12f * s;
		float dotH   = 7f * s;
		float dotGap = 4f * s;
		int   numDots = Mathf.Min(LayerAccents.Length, 6);

		for (int i = 0; i < numDots; i++) {
			float dx = cx + i * (dotW + dotGap);
			float dy = cy + 1f * s;
			Color dc = this.AccentForLayer(i, this.accentPulse);

			if (i == layer) {
				// Active: bright fill + outer glow ring
				this.FillRect(dx - 2f*s, dy - 2f*s, dotW + 4f*s, dotH + 4f*s, new Color(dc.r, dc.g, dc.b, 0.20f));
				this.FillRect(dx, dy, dotW, dotH, dc);
				// Inner highlight
				this.FillRect(dx + 2f*s, dy + 1f*s, dotW - 4f*s, 2f*s, new Color(1,1,1,0.4f));
			} else {
				// Inactive: dim outline
				this.FillRect(dx, dy, dotW, dotH, new Color(1, 1, 1, 0.10f));
				this.FillRect(dx + s, dy + s, dotW - 2f*s, dotH - 2f*s, new Color(0, 0, 0, 0.5f));
			}
		}
		cy += dotH + 10f * s;

		// ── Layer fractional bar ──────────────────────────────────────────────
		this.microStyle.normal.textColor = new Color(1, 1, 1, 0.4f);
		GUI.Label(new Rect(cx, cy, pw, 14f * s), $"LAYER {layer}", this.microStyle);
		cy += 15f * s;

		float barW = pw - pad * 2;
		float barH = 3f * s;
		this.FillRect(cx, cy, barW, barH, new Color(1, 1, 1, 0.1f));
		this.FillRect(cx, cy, barW * frac, barH, new Color(accent.r, accent.g, accent.b, 0.9f));
		// Tick marks on bar
		for (int t = 1; t < 4; t++) {
			this.FillRect(cx + barW * t / 4f - s*0.5f, cy - s, s, barH + 2f*s, new Color(1,1,1,0.15f));
		}
		cy += barH + 14f * s;

		// ── Divider with accent dot ───────────────────────────────────────────
		this.FillRect(cx, cy, pw - pad * 2, 1f * s, new Color(1, 1, 1, 0.1f));
		this.FillRect(cx + (pw - pad*2) * 0.5f - 3f*s, cy - 2f*s, 6f*s, 5f*s, new Color(accent.r,accent.g,accent.b,0.4f));
		cy += 8f * s;

		// ── Collectibles ──────────────────────────────────────────────────────
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
		// Capsule end on progress
		if (pct > 0f) {
			this.FillRect(cx + artW * pct - s, cy - s*0.5f, 2f*s, artH + s, new Color(1f, 1f, 0.5f, 0.9f));
		}
		cy += artH + 10f * s;

		// ── Nearest landmark ──────────────────────────────────────────────────
		if (LandmarkPlacer.LandmarkCount > 0 && Camera.main != null) {
			Vector3 nearest  = this.NearestLandmark(this.player.transform.position, out float dist);
			Vector3 viewport = Camera.main.WorldToViewportPoint(nearest);
			bool onScreen    = viewport.z > 0f && viewport.x > 0.05f && viewport.x < 0.95f
				&& viewport.y > 0.05f && viewport.y < 0.95f;
			string bearing   = onScreen ? "•" : this.ViewportArrow(viewport);

			this.microStyle.normal.textColor = new Color(accent.r, accent.g, accent.b, 0.7f);
			GUI.Label(new Rect(cx, cy, 80f * s, 16f * s), "BEACON", this.microStyle);

			this.labelStyle.normal.textColor = Color.white;
			GUI.Label(new Rect(cx + 60f * s, cy, 80f * s, 16f * s),
				$"{bearing}  {dist:F0}m", this.labelStyle);
		}
	}

	// Draws an L-shaped corner bracket at (x,y); topRight/bottomLeft controls orientation
	private void DrawCornerBracket(float x, float y, float size, Color col, float s, bool flipX, bool flipY) {
		float thick = Mathf.Max(1f, s);
		Color c = new Color(col.r, col.g, col.b, 0.5f);
		float sx = flipX ? 1f : -1f;
		float sy = flipY ? 1f : -1f;
		// Horizontal arm
		this.FillRect(flipX ? x : x - size + thick, flipY ? y : y + size - thick, size, thick, c);
		// Vertical arm
		this.FillRect(flipX ? x + size - thick : x - thick, flipY ? y : y, thick, size, c);
	}

	// =========================================================================
	// MINIMAP + W AXIS
	// =========================================================================
	private void DrawMinimap(float sw, float sh, float s, int activeLayer, float wPos, Color accent) {
		if (this.generator == null) return;

		float mapSize = 170f * s;
		float wAxisW  = 20f * s;
		float gap     = 6f * s;
		float mx = 24f * s;
		float my = sh - mapSize - 24f * s;

		// ── Spatial map panel ────────────────────────────────────────────────
		this.FillRect(mx, my, mapSize, mapSize, new Color(0, 0, 0, 0.7f));
		this.FillRect(mx, my, mapSize, 2f * s, new Color(accent.r, accent.g, accent.b, 0.5f));
		this.FillRect(mx, my + mapSize - 2f*s, mapSize, 2f * s, new Color(1,1,1,0.06f));
		this.FillRect(mx + mapSize - 2f*s, my, 2f * s, mapSize, new Color(1,1,1,0.04f));

		// Chamfered top-right corner on map panel
		float cham = 8f * s;
		this.FillRect(mx + mapSize - cham, my, cham, cham, new Color(0,0,0,0.7f));
		for (int i = 0; i < (int)(cham / s); i++) {
			this.FillRect(mx + mapSize - cham + i*s, my + (cham - i*s), s, s, new Color(accent.r,accent.g,accent.b,0.15f));
		}
		// Corner brackets
		this.DrawCornerBracket(mx, my, 12f*s, accent, s, false, true);
		this.DrawCornerBracket(mx + mapSize - 12f*s, my + mapSize - 12f*s, 12f*s, accent, s, true, false);

		// Header
		float mapHeaderH = 18f * s;
		this.headerStyle.normal.textColor = new Color(accent.r, accent.g, accent.b, 0.6f);
		this.FillRect(mx + 8f*s, my + 4f*s, 3f*s, 9f*s, new Color(accent.r, accent.g, accent.b, 0.8f));
		GUI.Label(new Rect(mx + 14f * s, my + 4f * s, mapSize, mapHeaderH), "SPATIAL MAP", this.headerStyle);

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
		float chunkPx     = Mathf.Min(innerW, innerH) / 16f;

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

		// Player marker
		float pdotW = 8f * s;
		float pdotH = 8f * s;
		this.FillRect(centerX - pdotW * 0.5f, centerZ - pdotH * 0.5f, pdotW, pdotH,
			new Color(accent.r, accent.g, accent.b, 1f));
		// Player direction tick (small line above marker)
		this.FillRect(centerX - s, centerZ - pdotH * 0.5f - 4f*s, 2f*s, 3f*s, new Color(1,1,1,0.7f));

		// ── W Axis strip (right side of minimap) ────────────────────────────
		float wax = mx + mapSize + gap;
		float way = my;
		int   numLayers = LayerAccents.Length;
		float segH = mapSize / numLayers;

		// Background
		this.FillRect(wax, way, wAxisW, mapSize, new Color(0,0,0,0.65f));
		this.FillRect(wax, way, 2f*s, mapSize, new Color(accent.r,accent.g,accent.b,0.3f));

		// W axis label
		this.headerStyle.normal.textColor = new Color(accent.r, accent.g, accent.b, 0.6f);
		GUI.Label(new Rect(wax + 2f*s, way + 2f*s, wAxisW, 14f*s), "W", this.headerStyle);

		for (int i = 0; i < numLayers; i++) {
			// Layers drawn bottom-to-top (i=0 at bottom)
			float sy2 = way + mapSize - (i + 1) * segH;
			Color lc = LayerAccents[i];
			int   wi  = ((activeLayer) % numLayers + numLayers) % numLayers;
			bool  cur = i == wi;
			float a   = cur ? 0.55f : 0.15f;

			// Layer segment fill
			GUI.color = new Color(lc.r, lc.g, lc.b, a);
			GUI.DrawTexture(new Rect(wax + 2f*s, sy2 + 1f, wAxisW - 4f*s, segH - 2f), this.white);
			GUI.color = Color.white;

			// Separator line
			this.FillRect(wax + 2f*s, sy2, wAxisW - 4f*s, Mathf.Max(0.5f, s*0.5f), new Color(1,1,1,0.08f));
		}

		// W fractional position indicator — a bright horizontal line on the W axis
		float wFrac = Mathf.Repeat(wPos, 1f);  // 0..1 within current layer
		int wLayerIdx = ((activeLayer % numLayers) + numLayers) % numLayers;
		float indicatorY = way + mapSize - (wLayerIdx * segH) - wFrac * segH;
		this.FillRect(wax, indicatorY - s, wAxisW, Mathf.Max(2f, 2f*s),
			new Color(accent.r, accent.g, accent.b, 0.95f));
		// Tick mark extending left from W axis
		this.FillRect(wax - 5f*s, indicatorY - s, 5f*s, Mathf.Max(2f, 2f*s),
			new Color(accent.r, accent.g, accent.b, 0.6f));

		// W layer number next to active segment
		float labelY = way + mapSize - (wLayerIdx + 0.5f) * segH - 5f*s;
		this.microStyle.normal.textColor = Color.white;
		GUI.Label(new Rect(wax + 3f*s, labelY, wAxisW, 12f*s), $"{activeLayer}", this.microStyle);

		// ── W layer color legend strip below map ─────────────────────────────
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
