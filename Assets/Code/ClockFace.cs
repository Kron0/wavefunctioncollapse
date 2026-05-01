using UnityEngine;

// Procedural animated clock face added to spawned clock module GameObjects.
// Creates hour, minute, and second hands from thin flat cubes.
public class ClockFace : MonoBehaviour {
	private float timeOffset;

	private Transform hourHand;
	private Transform minuteHand;
	private Transform secondHand;

	public void Init(Vector3 faceCenter, Vector3 faceNormal, float clockRadius, float timeOffsetSeconds) {
		this.timeOffset = timeOffsetSeconds;

		Color handColor = new Color(0.08f, 0.06f, 0.05f);

		var pivot = new GameObject("ClockPivot");
		pivot.transform.SetParent(this.transform);
		pivot.transform.position = faceCenter;
		pivot.transform.rotation = Quaternion.LookRotation(-faceNormal, Vector3.up);

		this.hourHand   = this.MakeHand(pivot.transform, "HourHand",
			clockRadius * 0.28f, clockRadius * 0.06f, clockRadius * 0.008f, handColor);
		this.minuteHand = this.MakeHand(pivot.transform, "MinuteHand",
			clockRadius * 0.44f, clockRadius * 0.09f, clockRadius * 0.005f, handColor);
		this.secondHand = this.MakeHand(pivot.transform, "SecondHand",
			clockRadius * 0.48f, clockRadius * 0.12f, clockRadius * 0.003f,
			new Color(0.85f, 0.15f, 0.12f));

		var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		dot.name = "CentreDot";
		dot.transform.SetParent(pivot.transform);
		dot.transform.localPosition = new Vector3(0f, 0f, -0.004f);
		dot.transform.localScale = Vector3.one * clockRadius * 0.09f;
		Object.Destroy(dot.GetComponent<Collider>());
		var dotMat = new Material(Shader.Find("Standard")) { color = handColor };
		dotMat.SetFloat("_Metallic", 0.5f);
		dotMat.SetFloat("_Glossiness", 0.7f);
		dot.GetComponent<Renderer>().sharedMaterial = dotMat;
	}

	void Update() {
		float t = Time.time + this.timeOffset;

		float secondAngle = (t % 60f) / 60f * 360f;
		float minuteAngle = (t % 3600f) / 3600f * 360f;
		float hourAngle   = (t % 43200f) / 43200f * 360f;

		if (this.hourHand   != null) this.hourHand.localRotation   = Quaternion.Euler(0f, 0f, -hourAngle);
		if (this.minuteHand != null) this.minuteHand.localRotation = Quaternion.Euler(0f, 0f, -minuteAngle);
		if (this.secondHand != null) this.secondHand.localRotation = Quaternion.Euler(0f, 0f, -secondAngle);
	}

	// Wrapper-based hand: wrapper pivot is at clock centre; mesh is offset so its visual base is at the pivot.
	private Transform MakeHand(Transform clockPivot, string handName,
		float length, float baseOffset, float thickness, Color color) {

		var wrapper = new GameObject(handName);
		wrapper.transform.SetParent(clockPivot);
		wrapper.transform.localPosition = Vector3.zero;
		wrapper.transform.localRotation = Quaternion.identity;

		var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
		go.name = handName + "_Mesh";
		go.transform.SetParent(wrapper.transform);
		go.transform.localPosition = new Vector3(0f, length * 0.5f - baseOffset, -thickness * 0.5f);
		go.transform.localScale    = new Vector3(thickness * 2.5f, length, thickness);
		Object.Destroy(go.GetComponent<Collider>());

		var mat = new Material(Shader.Find("Standard")) { color = color };
		mat.SetFloat("_Metallic", 0.6f);
		mat.SetFloat("_Glossiness", 0.7f);
		go.GetComponent<Renderer>().sharedMaterial = mat;

		return wrapper.transform;
	}
}
