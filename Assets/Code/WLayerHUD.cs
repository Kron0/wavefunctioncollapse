using System.Collections.Generic;
using UnityEngine;

public class WLayerHUD : MonoBehaviour {
	public FourDController player;

	private GUIStyle bigStyle;
	private GUIStyle smallStyle;
	private GUIStyle labelStyle;
	private Texture2D barBg;
	private Texture2D barFill;
	private Texture2D panelBg;
	private Texture2D minimapChunkTex;

	private GenerateMap4DNearPlayer generator;

	// Layer colors for minimap (cycles through hues per W layer)
	private static readonly Color[] LayerColors = {
		new Color(0.4f, 0.6f, 1f),    // W=0 blue
		new Color(0.4f, 1f, 0.5f),    // W=1 green
		new Color(1f, 0.4f, 0.4f),    // W=2 red
		new Color(1f, 0.9f, 0.3f),    // W=3 yellow
		new Color(1f, 0.5f, 1f),      // W=4 magenta
		new Color(0.5f, 1f, 1f),      // W=5 cyan
	};

	void Start() {
		this.bigStyle = new GUIStyle {
			fontSize = 20,
			fontStyle = FontStyle.Bold
		};
		this.bigStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);

		this.smallStyle = new GUIStyle {
			fontSize = 13
		};
		this.smallStyle.normal.textColor = new Color(0.75f, 0.85f, 0.95f);

		this.labelStyle = new GUIStyle {
			fontSize = 11,
			alignment = TextAnchor.MiddleCenter
		};
		this.labelStyle.normal.textColor = Color.white;

		this.barBg = MakeTex(new Color(0.08f, 0.1f, 0.18f, 0.8f));
		this.barFill = MakeTex(new Color(0.2f, 0.7f, 1f, 0.95f));
		this.panelBg = MakeTex(new Color(0f, 0f, 0f, 0.5f));
		this.minimapChunkTex = MakeTex(Color.white);

		if (this.player == null) {
			var go = GameObject.FindGameObjectWithTag("Player");
			if (go != null) {
				this.player = go.GetComponent<FourDController>();
			}
		}
		this.generator = Object.FindObjectOfType<GenerateMap4DNearPlayer>();
	}

	void OnGUI() {
		if (this.player == null) {
			return;
		}

		float sw = Screen.width;
		float sh = Screen.height;

		this.DrawWIndicator(sw, sh);
		this.DrawMinimap(sw, sh);
	}

	private void DrawWIndicator(float sw, float sh) {
		float panelW = 185f;
		float panelH = 115f;
		float panelX = sw - panelW - 15f;
		float panelY = 15f;

		GUI.DrawTexture(new Rect(panelX - 8, panelY - 8, panelW + 16, panelH + 16), this.panelBg);

		float wPos = this.player.WPosition;
		int layer = MapBehaviour4D.ActiveWLayer;
		// Fractional progress between integer layers
		float fraction = wPos - Mathf.Floor(wPos);

		// W label
		GUI.Label(new Rect(panelX, panelY, panelW, 30), $"W Axis: {wPos:F2}", this.bigStyle);
		GUI.Label(new Rect(panelX, panelY + 26, panelW, 20), $"Current layer: {layer}", this.smallStyle);

		// Layer progress bar
		float barY = panelY + 50f;
		GUI.DrawTexture(new Rect(panelX, barY, 160f, 8f), this.barBg);
		GUI.DrawTexture(new Rect(panelX, barY, 160f * fraction, 8f), this.barFill);

		// Collectibles count
		GUI.Label(new Rect(panelX, barY + 14f, panelW, 20),
			$"Found: {CollectiblePlacer.TotalCollected} / {CollectiblePlacer.TotalPlaced}", this.smallStyle);

		// Nearest landmark direction
		if (LandmarkPlacer.LandmarkCount > 0 && Camera.main != null) {
			Vector3 playerPos = this.player.transform.position;
			Vector3 nearest = LandmarkPlacer.LandmarkWorldPositions[0];
			float nearestDist = float.MaxValue;
			foreach (var lp in LandmarkPlacer.LandmarkWorldPositions) {
				float d = Vector3.Distance(playerPos, lp);
				if (d < nearestDist) {
					nearestDist = d;
					nearest = lp;
				}
			}
			if (nearestDist > 4f) {
				Vector3 vp = Camera.main.WorldToViewportPoint(nearest);
				string indicator;
				if (vp.z > 0f && vp.x > 0.05f && vp.x < 0.95f && vp.y > 0.05f && vp.y < 0.95f) {
					indicator = $"Beacon  {nearestDist:F0}m";
				} else {
					Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
					if (vp.z <= 0f) dir = -dir;
					indicator = $"Beacon {DirectionArrow(dir)} {nearestDist:F0}m";
				}
				GUI.Label(new Rect(panelX, barY + 34f, panelW, 20), indicator, this.smallStyle);
			}
		}
	}

	private void DrawMinimap(float sw, float sh) {
		if (this.generator == null) {
			return;
		}

		const float mapSize = 160f;
		const float chunkPx = 12f;
		float mapX = 15f;
		float mapY = sh - mapSize - 15f;

		// Background
		GUI.DrawTexture(new Rect(mapX - 5, mapY - 5, mapSize + 10, mapSize + 10), this.panelBg);
		GUI.Label(new Rect(mapX, mapY - 18, mapSize, 16), "MAP", this.labelStyle);

		// Center on player's chunk
		int activeW = MapBehaviour4D.ActiveWLayer;
		int chunkSize = this.generator.ChunkSize;
		Vector3 playerPos = this.player.transform.position;
		float blockSize = AbstractMap4D.BLOCK_SIZE;
		float worldChunkSize = blockSize * chunkSize;

		float playerChunkX = playerPos.x / worldChunkSize;
		float playerChunkZ = playerPos.z / worldChunkSize;

		float centerX = mapX + mapSize * 0.5f;
		float centerZ = mapY + mapSize * 0.5f;

		foreach (var chunkAddr in this.generator.GeneratedChunks) {
			// Compute screen position of this chunk relative to player
			float relX = (chunkAddr.x - playerChunkX) * chunkPx;
			float relZ = (chunkAddr.z - playerChunkZ) * chunkPx;

			float screenX = centerX + relX - chunkPx * 0.5f;
			float screenZ = centerZ - relZ - chunkPx * 0.5f; // Flip Z so north is up

			// Clip to minimap bounds
			if (screenX < mapX || screenX + chunkPx > mapX + mapSize) continue;
			if (screenZ < mapY || screenZ + chunkPx > mapY + mapSize) continue;

			int wIdx = ((chunkAddr.w % LayerColors.Length) + LayerColors.Length) % LayerColors.Length;
			Color c = LayerColors[wIdx];
			bool isCurrentW = chunkAddr.w * chunkSize <= activeW && activeW < (chunkAddr.w + 1) * chunkSize;
			GUI.color = isCurrentW ? c : c * 0.4f;
			GUI.DrawTexture(new Rect(screenX, screenZ, chunkPx - 1f, chunkPx - 1f), this.minimapChunkTex);
		}
		GUI.color = Color.white;

		// Player dot
		GUI.color = Color.white;
		GUI.DrawTexture(new Rect(centerX - 3, centerZ - 3, 6f, 6f), this.minimapChunkTex);

		// W layer legend
		for (int i = 0; i < Mathf.Min(LayerColors.Length, 4); i++) {
			GUI.color = LayerColors[i];
			GUI.DrawTexture(new Rect(mapX + i * 18f, mapY + mapSize + 5f, 14f, 6f), this.minimapChunkTex);
			GUI.color = this.smallStyle.normal.textColor;
		}
		GUI.color = Color.white;
	}

	private static string DirectionArrow(Vector2 dir) {
		if (dir == Vector2.zero) return "•";
		float angle = (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 360f) % 360f;
		int sector = Mathf.RoundToInt(angle / 45f) % 8;
		string[] arrows = { "→", "↗", "↑", "↖", "←", "↙", "↓", "↘" };
		return arrows[sector];
	}

	private static Texture2D MakeTex(Color col) {
		var tex = new Texture2D(2, 2);
		var pixels = new Color[4];
		for (int i = 0; i < 4; i++) pixels[i] = col;
		tex.SetPixels(pixels);
		tex.Apply();
		return tex;
	}
}
