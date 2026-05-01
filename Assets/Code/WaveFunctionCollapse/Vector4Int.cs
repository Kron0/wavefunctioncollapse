using System;
using UnityEngine;

[System.Serializable]
public struct Vector4Int : IEquatable<Vector4Int> {
	public int x;
	public int y;
	public int z;
	public int w;

	public Vector4Int(int x, int y, int z, int w) {
		this.x = x;
		this.y = y;
		this.z = z;
		this.w = w;
	}

	public float magnitude {
		get {
			return Mathf.Sqrt(x * x + y * y + z * z + w * w);
		}
	}

	public Vector4 ToVector4() {
		return new Vector4(x, y, z, w);
	}

	public Vector3 ToVector3() {
		return new Vector3(x, y, z);
	}

	public Vector3Int ToVector3Int() {
		return new Vector3Int(x, y, z);
	}

	public static Vector4Int operator +(Vector4Int a, Vector4Int b) {
		return new Vector4Int(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
	}

	public static Vector4Int operator -(Vector4Int a, Vector4Int b) {
		return new Vector4Int(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
	}

	public static Vector4Int operator *(Vector4Int a, int s) {
		return new Vector4Int(a.x * s, a.y * s, a.z * s, a.w * s);
	}

	public static Vector4Int operator *(int s, Vector4Int a) {
		return a * s;
	}

	public static bool operator ==(Vector4Int a, Vector4Int b) {
		return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
	}

	public static bool operator !=(Vector4Int a, Vector4Int b) {
		return !(a == b);
	}

	public bool Equals(Vector4Int other) {
		return this == other;
	}

	public override bool Equals(object obj) {
		return obj is Vector4Int other && this == other;
	}

	public override int GetHashCode() {
		unchecked {
			int hash = 17;
			hash = hash * 31 + x;
			hash = hash * 31 + y;
			hash = hash * 31 + z;
			hash = hash * 31 + w;
			return hash;
		}
	}

	public override string ToString() {
		return "(" + x + ", " + y + ", " + z + ", " + w + ")";
	}

	public static readonly Vector4Int zero = new Vector4Int(0, 0, 0, 0);
}
