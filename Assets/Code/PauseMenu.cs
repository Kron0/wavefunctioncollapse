using UnityEngine;

public class PauseMenu : MonoBehaviour {

	public static bool IsPaused { get; private set; }

	// ─── Textures & Styles ──────────────────────────────────────────────────
	private Texture2D white;
	private GUIStyle titleStyle;
	private GUIStyle subtitleStyle;
	private GUIStyle buttonStyle;
	private GUIStyle buttonHoverStyle;
	private GUIStyle microStyle;
	private GUIStyle sliderLabelStyle;

	// ─── Settings ───────────────────────────────────────────────────────────
	private float mouseSensitivity = 3f;
	private FourDController player;

	private const float REF_W = 1920f;
	private const float REF_H = 1080f;

	private static readonly Color AccentColor = new Color(0.00f, 0.78f, 1.00f);

	void Start() {
		IsPaused = false;
		this.white = new Texture2D(1, 1);
		this.white.SetPixel(0, 0, Color.white);
		this.white.Apply();

		this.player = this.GetComponent<FourDController>();
		if (this.player == null) {
			this.player = Object.FindObjectOfType<FourDController>();
		}
		if (this.player != null) {
			this.mouseSensitivity = this.player.MouseSensitivity;
		}
	}

	void Update() {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (this.IsPaused) {
				this.Resume();
			} else {
				this.Pause();
			}
		}
	}

	void OnGUI() {
		if (!this.IsPaused) return;

		this.EnsureStyles();

		float sw = Screen.width;
		float sh = Screen.height;
		float s  = Mathf.Min(sw / REF_W, sh / REF_H);

		// ── Full-screen dim overlay ──────────────────────────────────────────
		GUI.color = new Color(0, 0, 0, 0.65f);
		GUI.DrawTexture(new Rect(0, 0, sw, sh), this.white);
		GUI.color = Color.white;

		// ── Vertical scan-line texture effect (thin horizontal bars) ─────────
		for (int i = 0; i < sh; i += (int)(4f * s)) {
			GUI.color = new Color(0, 0, 0, 0.06f);
			GUI.DrawTexture(new Rect(0, i, sw, Mathf.Max(1f, s)), this.white);
		}
		GUI.color = Color.white;

		// ── Panel ────────────────────────────────────────────────────────────
		float pw = 320f * s;
		float ph = 340f * s;
		float px = (sw - pw) * 0.5f;
		float py = (sh - ph) * 0.5f;

		this.FillRect(px, py, pw, ph, new Color(0.04f, 0.04f, 0.06f, 0.95f));

		// Top accent bar
		this.FillRect(px, py, pw, 2f * s, AccentColor * 0.9f);
		// Bottom edge
		this.FillRect(px, py + ph - 2f*s, pw, 2f * s, new Color(1,1,1,0.06f));
		// Left edge
		this.FillRect(px, py, 2f * s, ph, new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.25f));
		// Right edge
		this.FillRect(px + pw - 2f*s, py, 2f * s, ph, new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.08f));

		// Corner brackets
		this.DrawCornerBracket(px, py, 16f*s, s, false, true);
		this.DrawCornerBracket(px + pw - 16f*s, py, 16f*s, s, true, true);
		this.DrawCornerBracket(px, py + ph - 16f*s, 16f*s, s, false, false);
		this.DrawCornerBracket(px + pw - 16f*s, py + ph - 16f*s, 16f*s, s, true, false);

		float cx = px + 30f * s;
		float cy = py + 30f * s;
		float cw = pw - 60f * s;

		// ── Title ─────────────────────────────────────────────────────────────
		this.titleStyle.fontSize = Mathf.RoundToInt(28f * s);
		GUI.Label(new Rect(cx, cy, cw, 36f * s), "PAUSED", this.titleStyle);
		cy += 34f * s;

		this.subtitleStyle.fontSize = Mathf.RoundToInt(9f * s);
		GUI.Label(new Rect(cx, cy, cw, 14f * s), "PERSONAL DIMENSION", this.subtitleStyle);
		cy += 20f * s;

		// Divider
		this.FillRect(cx, cy, cw, Mathf.Max(1f, s), new Color(1,1,1,0.1f));
		this.FillRect(cx + cw * 0.5f - 4f*s, cy - 2f*s, 8f*s, 5f*s, new Color(AccentColor.r,AccentColor.g,AccentColor.b,0.5f));
		cy += 18f * s;

		// ── Mouse Sensitivity ────────────────────────────────────────────────
		this.sliderLabelStyle.fontSize = Mathf.RoundToInt(9f * s);
		GUI.Label(new Rect(cx, cy, cw, 13f * s), $"SENSITIVITY  {this.mouseSensitivity:F1}", this.sliderLabelStyle);
		cy += 14f * s;

		// Slider track
		this.FillRect(cx, cy + 4f*s, cw, 2f*s, new Color(1,1,1,0.12f));
		float newSens = GUI.HorizontalSlider(new Rect(cx, cy, cw, 10f*s), this.mouseSensitivity, 0.5f, 10f, GUIStyle.none, GUIStyle.none);
		if (Mathf.Abs(newSens - this.mouseSensitivity) > 0.01f) {
			this.mouseSensitivity = newSens;
			if (this.player != null) {
				this.player.MouseSensitivity = this.mouseSensitivity;
			}
		}
		// Slider fill
		float fillFrac = (this.mouseSensitivity - 0.5f) / 9.5f;
		this.FillRect(cx, cy + 3f*s, cw * fillFrac, 4f*s, new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.7f));
		// Slider thumb
		this.FillRect(cx + cw * fillFrac - 3f*s, cy + 1f*s, 6f*s, 8f*s, AccentColor);
		cy += 26f * s;

		// Divider
		this.FillRect(cx, cy, cw, Mathf.Max(1f, s), new Color(1,1,1,0.08f));
		cy += 16f * s;

		// ── Buttons ───────────────────────────────────────────────────────────
		float bh = 38f * s;
		float bGap = 10f * s;

		if (this.DrawButton(cx, cy, cw, bh, "RESUME", s)) {
			this.Resume();
		}
		cy += bh + bGap;

		if (this.DrawButton(cx, cy, cw, bh, "QUIT TO DESKTOP", s, danger: true)) {
			Application.Quit();
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#endif
		}
	}

	private bool DrawButton(float x, float y, float w, float h, string label, float s, bool danger = false) {
		Rect r = new Rect(x, y, w, h);
		bool hover = r.Contains(Event.current.mousePosition);

		Color bg   = danger
			? new Color(0.5f, 0.08f, 0.08f, hover ? 0.85f : 0.6f)
			: new Color(0.06f, 0.12f, 0.18f, hover ? 0.95f : 0.75f);
		Color border = danger
			? new Color(1f, 0.25f, 0.25f, hover ? 0.9f : 0.5f)
			: new Color(AccentColor.r, AccentColor.g, AccentColor.b, hover ? 0.9f : 0.4f);

		this.FillRect(x, y, w, h, bg);
		// Border
		this.FillRect(x, y, w, Mathf.Max(1f, s), border);
		this.FillRect(x, y + h - Mathf.Max(1f, s), w, Mathf.Max(1f, s), border * 0.5f);
		this.FillRect(x, y, Mathf.Max(1f, s), h, border * 0.7f);
		this.FillRect(x + w - Mathf.Max(1f, s), y, Mathf.Max(1f, s), h, border * 0.4f);

		// Label
		this.buttonStyle.fontSize = Mathf.RoundToInt(11f * s);
		this.buttonStyle.normal.textColor = hover ? Color.white : new Color(1,1,1,0.8f);
		GUI.Label(new Rect(x, y, w, h), label, this.buttonStyle);

		// Hover accent tick
		if (hover) {
			this.FillRect(x + 8f*s, y + h * 0.5f - 4f*s, 2f*s, 8f*s, border);
		}

		return GUI.Button(r, GUIContent.none, GUIStyle.none);
	}

	private void FillRect(float x, float y, float w, float h, Color col) {
		GUI.color = col;
		GUI.DrawTexture(new Rect(x, y, w, h), this.white);
		GUI.color = Color.white;
	}

	private void DrawCornerBracket(float x, float y, float size, float s, bool flipX, bool flipY) {
		float thick = Mathf.Max(1f, s);
		Color c = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.5f);
		this.FillRect(flipX ? x : x - size + thick, flipY ? y : y + size - thick, size, thick, c);
		this.FillRect(flipX ? x + size - thick : x - thick, flipY ? y : y, thick, size, c);
	}

	private void EnsureStyles() {
		if (this.titleStyle != null) return;

		this.titleStyle = new GUIStyle {
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleLeft,
			wordWrap  = false,
		};
		this.titleStyle.normal.textColor = Color.white;

		this.subtitleStyle = new GUIStyle {
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleLeft,
			wordWrap  = false,
		};
		this.subtitleStyle.normal.textColor = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.7f);

		this.buttonStyle = new GUIStyle {
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleCenter,
			wordWrap  = false,
		};
		this.buttonStyle.normal.textColor = Color.white;

		this.microStyle = new GUIStyle {
			alignment = TextAnchor.MiddleLeft,
			wordWrap  = false,
		};
		this.microStyle.normal.textColor = new Color(1,1,1,0.45f);

		this.sliderLabelStyle = new GUIStyle {
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleLeft,
			wordWrap  = false,
		};
		this.sliderLabelStyle.normal.textColor = new Color(1,1,1,0.5f);
	}

	private void Pause() {
		this.IsPaused = true;
		Time.timeScale = 0f;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	private void Resume() {
		this.IsPaused = false;
		Time.timeScale = 1f;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}
}
