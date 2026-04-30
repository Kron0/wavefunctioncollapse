using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEditor;

[System.Serializable]
public class Module {
	public string Name;

	public ModulePrototype Prototype;
	public GameObject Prefab;

	public int Rotation;
	
	public ModuleSet[] PossibleNeighbors;
	public Module[][] PossibleNeighborsArray;

	[System.NonSerialized]
	public ModuleSet[] PossibleNeighbors4D;
	[System.NonSerialized]
	public Module[][] PossibleNeighborsArray4D;

	[HideInInspector]
	public int Index;

	// This is precomputed to make entropy calculation faster
	public float PLogP;

	public Module(GameObject prefab, int rotation, int index) {
		this.Rotation = rotation;
		this.Index = index;
		this.Prefab = prefab;
		this.Prototype = this.Prefab.GetComponent<ModulePrototype>();
		this.Name = this.Prototype.gameObject.name + " R" + rotation;
		this.PLogP = this.Prototype.Probability * Mathf.Log(this.Prototype.Probability);
	}

	public bool Fits(int direction, Module module) {
		int otherDirection = (direction + 3) % 6;

		if (Orientations.IsHorizontal(direction)) {
			var f1 = this.Prototype.Faces[Orientations.Rotate(direction, this.Rotation)] as ModulePrototype.HorizontalFaceDetails;
			var f2 = module.Prototype.Faces[Orientations.Rotate(otherDirection, module.Rotation)] as ModulePrototype.HorizontalFaceDetails;
			return f1.Connector == f2.Connector && (f1.Symmetric || f1.Flipped != f2.Flipped);
		} else {
			var f1 = this.Prototype.Faces[direction] as ModulePrototype.VerticalFaceDetails;
			var f2 = module.Prototype.Faces[otherDirection] as ModulePrototype.VerticalFaceDetails;
			return f1.Connector == f2.Connector && (f1.Invariant || (f1.Rotation + this.Rotation) % 4 == (f2.Rotation + module.Rotation) % 4);
		}
	}

	public bool Fits(int direction, int connector) {
		if (Orientations.IsHorizontal(direction)) {
			var f = this.GetFace(direction) as ModulePrototype.HorizontalFaceDetails;
			return f.Connector == connector;
		} else {
			var f = this.Prototype.Faces[direction] as ModulePrototype.VerticalFaceDetails;
			return f.Connector == connector;
		}
	}

	public ModulePrototype.FaceDetails GetFace(int direction) {
		return this.Prototype.Faces[Orientations.Rotate(direction, this.Rotation)];
	}

	public bool Fits4D(int direction4D, Module other) {
		int mapped = MapDirection4DTo3D(direction4D);
		int otherMapped = MapDirection4DTo3D(Orientations4D.Opposite(direction4D));
		return this.Fits3DFace(mapped, other, otherMapped);
	}

	private bool Fits3DFace(int dir3D, Module other, int otherDir3D) {
		if (Orientations.IsHorizontal(dir3D)) {
			var f1 = this.Prototype.Faces[Orientations.Rotate(dir3D, this.Rotation)] as ModulePrototype.HorizontalFaceDetails;
			var f2 = other.Prototype.Faces[Orientations.Rotate(otherDir3D, other.Rotation)] as ModulePrototype.HorizontalFaceDetails;
			return f1.Connector == f2.Connector && (f1.Symmetric || f1.Flipped != f2.Flipped);
		} else {
			var f1 = this.Prototype.Faces[dir3D] as ModulePrototype.VerticalFaceDetails;
			var f2 = other.Prototype.Faces[otherDir3D] as ModulePrototype.VerticalFaceDetails;
			return f1.Connector == f2.Connector && (f1.Invariant || (f1.Rotation + this.Rotation) % 4 == (f2.Rotation + other.Rotation) % 4);
		}
	}

	public static int MapDirection4DTo3D(int direction4D) {
		switch (direction4D) {
			case Orientations4D.LEFT: return 0;
			case Orientations4D.DOWN: return 1;
			case Orientations4D.BACK: return 2;
			case Orientations4D.RIGHT: return 3;
			case Orientations4D.UP: return 4;
			case Orientations4D.FORWARD: return 5;
			case Orientations4D.ANA: return 2;
			case Orientations4D.KATA: return 5;
			default: throw new ArgumentException("Invalid 4D direction: " + direction4D);
		}
	}

	public static void Initialize4DNeighbors(Module[] modules) {
		foreach (var module in modules) {
			module.PossibleNeighbors4D = new ModuleSet[8];
			for (int d = 0; d < 8; d++) {
				module.PossibleNeighbors4D[d] = new ModuleSet(modules
					.Where(neighbor => module.Fits4D(d, neighbor)));
			}
			module.PossibleNeighborsArray4D = module.PossibleNeighbors4D.Select(ms => ms.ToArray()).ToArray();
		}
	}

	public override string ToString() {
		return this.Name;
	}
}
