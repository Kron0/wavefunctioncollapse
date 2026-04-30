using System;
using UnityEngine;

public class Orientations4D {
	public const int LEFT = 0;
	public const int DOWN = 1;
	public const int BACK = 2;
	public const int ANA = 3;
	public const int RIGHT = 4;
	public const int UP = 5;
	public const int FORWARD = 6;
	public const int KATA = 7;

	public static readonly Vector4Int[] Direction = new Vector4Int[] {
		new Vector4Int(-1,  0,  0,  0),
		new Vector4Int( 0, -1,  0,  0),
		new Vector4Int( 0,  0, -1,  0),
		new Vector4Int( 0,  0,  0, -1),
		new Vector4Int( 1,  0,  0,  0),
		new Vector4Int( 0,  1,  0,  0),
		new Vector4Int( 0,  0,  1,  0),
		new Vector4Int( 0,  0,  0,  1),
	};

	public static readonly int[] HorizontalDirections = { LEFT, BACK, RIGHT, FORWARD };

	public static readonly string[] Names = {
		"-X (Left)", "-Y (Down)", "-Z (Back)", "-W (Ana)",
		"+X (Right)", "+Y (Up)", "+Z (Forward)", "+W (Kata)"
	};

	public static int Opposite(int direction) {
		return (direction + 4) % 8;
	}

	public static int Rotate(int direction, int amount) {
		if (!IsHorizontal(direction)) {
			return direction;
		}
		int idx = Array.IndexOf(HorizontalDirections, direction);
		return HorizontalDirections[(idx + amount) % 4];
	}

	public static bool IsHorizontal(int direction) {
		return direction == LEFT || direction == BACK || direction == RIGHT || direction == FORWARD;
	}

	public static bool IsVertical(int direction) {
		return direction == UP || direction == DOWN;
	}

	public static bool IsAnaKata(int direction) {
		return direction == ANA || direction == KATA;
	}
}
