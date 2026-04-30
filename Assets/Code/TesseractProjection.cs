using UnityEngine;

public class TesseractProjection : MonoBehaviour {
	[Range(1f, 100f)]
	public float ViewDistance = 10f;

	[Range(0.1f, 10f)]
	public float WScale = 2f;

	[Range(1, 10)]
	public int WRenderRange = 3;

	[Range(0f, 1f)]
	public float MinAlpha = 0.1f;

	private Vector3 playerPosition;
	private float playerW;

	public void UpdatePlayerPosition(Vector3 position, float w) {
		this.playerPosition = position;
		this.playerW = w;
	}

	public Vector3 ProjectPosition(Vector4Int slotPosition) {
		float relativeW = (slotPosition.w - this.playerW) * this.WScale;
		float scale = this.ViewDistance / (this.ViewDistance - relativeW);

		if (scale <= 0) {
			scale = 0.001f;
		}

		float x = (slotPosition.x * AbstractMap4D.BLOCK_SIZE - this.playerPosition.x) * scale + this.playerPosition.x;
		float y = slotPosition.y * AbstractMap4D.BLOCK_SIZE * scale;
		float z = (slotPosition.z * AbstractMap4D.BLOCK_SIZE - this.playerPosition.z) * scale + this.playerPosition.z;

		return new Vector3(x, y + AbstractMap4D.BLOCK_SIZE / 2f, z);
	}

	public float GetAlpha(int slotW) {
		float distance = Mathf.Abs(slotW - this.playerW);
		if (distance > this.WRenderRange) {
			return 0f;
		}
		return Mathf.Lerp(1f, this.MinAlpha, distance / this.WRenderRange);
	}

	public float GetScale(int slotW) {
		float relativeW = (slotW - this.playerW) * this.WScale;
		float scale = this.ViewDistance / (this.ViewDistance - relativeW);
		return Mathf.Max(scale, 0.001f);
	}

	public bool IsInRenderRange(int slotW) {
		return Mathf.Abs(slotW - this.playerW) <= this.WRenderRange;
	}
}
