using UnityEngine;

public static class ProceduralTexGen {
	public static Texture2D Brick(int w, int h, Color brickColor, Color mortarColor) {
		var tex = new Texture2D(w, h, TextureFormat.RGB24, true);
		var pixels = new Color[w * h];
		int brickH = h / 8;
		int brickW = w / 4;
		int mortarT = 2;

		for (int y = 0; y < h; y++) {
			int row = y / brickH;
			int offsetX = (row % 2 == 0) ? 0 : brickW / 2;
			bool mortarY = (y % brickH) < mortarT || (y % brickH) >= brickH - mortarT;

			for (int x = 0; x < w; x++) {
				int col = (x + offsetX) % w;
				bool mortarX = (col % brickW) < mortarT || (col % brickW) >= brickW - mortarT;

				if (mortarY || mortarX) {
					pixels[y * w + x] = mortarColor;
				} else {
					float noise = Mathf.PerlinNoise(x * 0.05f + row * 13f, y * 0.05f) * 0.10f - 0.05f;
					pixels[y * w + x] = new Color(
						Mathf.Clamp01(brickColor.r + noise),
						Mathf.Clamp01(brickColor.g + noise * 0.7f),
						Mathf.Clamp01(brickColor.b + noise * 0.5f));
				}
			}
		}
		return Finalize(tex, pixels);
	}

	public static Texture2D Concrete(int w, int h, Color baseColor) {
		var tex = new Texture2D(w, h, TextureFormat.RGB24, true);
		var pixels = new Color[w * h];
		int formH = Mathf.Max(1, h / 12);

		for (int y = 0; y < h; y++) {
			bool seam = (y % formH) < 2;
			for (int x = 0; x < w; x++) {
				float n1 = Mathf.PerlinNoise(x * 0.03f, y * 0.03f) * 0.10f;
				float n2 = Mathf.PerlinNoise(x * 0.08f + 5f, y * 0.08f + 5f) * 0.06f;
				float noise = n1 + n2 - 0.08f - (seam ? 0.06f : 0f);
				pixels[y * w + x] = new Color(
					Mathf.Clamp01(baseColor.r + noise),
					Mathf.Clamp01(baseColor.g + noise),
					Mathf.Clamp01(baseColor.b + noise));
			}
		}
		return Finalize(tex, pixels);
	}

	public static Texture2D Plaster(int w, int h, Color baseColor) {
		var tex = new Texture2D(w, h, TextureFormat.RGB24, true);
		var pixels = new Color[w * h];

		for (int y = 0; y < h; y++) {
			for (int x = 0; x < w; x++) {
				float n1 = Mathf.PerlinNoise(x * 0.02f, y * 0.02f) * 0.12f;
				float n2 = Mathf.PerlinNoise(x * 0.07f + 10f, y * 0.07f + 10f) * 0.05f;
				float n3 = Mathf.PerlinNoise(x * 0.15f + 20f, y * 0.15f + 20f) * 0.03f;
				float noise = n1 + n2 + n3 - 0.10f;
				pixels[y * w + x] = new Color(
					Mathf.Clamp01(baseColor.r + noise),
					Mathf.Clamp01(baseColor.g + noise * 0.9f),
					Mathf.Clamp01(baseColor.b + noise * 0.8f));
			}
		}
		return Finalize(tex, pixels);
	}

	public static Texture2D Metal(int w, int h, Color baseColor) {
		var tex = new Texture2D(w, h, TextureFormat.RGB24, true);
		var pixels = new Color[w * h];
		int panelH = Mathf.Max(1, h / 6);
		int panelW = Mathf.Max(1, w / 3);
		const int seam = 3;

		for (int y = 0; y < h; y++) {
			bool hSeam = (y % panelH) < seam;
			for (int x = 0; x < w; x++) {
				bool vSeam = (x % panelW) < seam;
				float n1 = Mathf.PerlinNoise(x * 0.01f, y * 0.20f) * 0.08f;
				float n2 = Mathf.PerlinNoise(x * 0.05f + 7f, y * 0.05f + 7f) * 0.04f;
				float noise = n1 + n2 - 0.06f - ((hSeam || vSeam) ? 0.08f : 0f);
				pixels[y * w + x] = new Color(
					Mathf.Clamp01(baseColor.r + noise),
					Mathf.Clamp01(baseColor.g + noise),
					Mathf.Clamp01(baseColor.b + noise));
			}
		}
		return Finalize(tex, pixels);
	}

	private static Texture2D Finalize(Texture2D tex, Color[] pixels) {
		tex.SetPixels(pixels);
		tex.Apply();
		tex.filterMode = FilterMode.Bilinear;
		tex.wrapMode = TextureWrapMode.Repeat;
		return tex;
	}
}
