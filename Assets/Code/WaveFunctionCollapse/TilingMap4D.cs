using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TilingMap4D : AbstractMap4D {
	public readonly int SizeX;
	public readonly int SizeY;
	public readonly int SizeZ;
	public readonly int SizeW;

	private readonly Slot4D[,,,] slots;

	public TilingMap4D(int sizeX, int sizeY, int sizeZ, int sizeW) : base() {
		this.SizeX = sizeX;
		this.SizeY = sizeY;
		this.SizeZ = sizeZ;
		this.SizeW = sizeW;
		this.slots = new Slot4D[sizeX, sizeY, sizeZ, sizeW];

		for (int x = 0; x < sizeX; x++) {
			for (int y = 0; y < sizeY; y++) {
				for (int z = 0; z < sizeZ; z++) {
					for (int w = 0; w < sizeW; w++) {
						this.slots[x, y, z, w] = new Slot4D(new Vector4Int(x, y, z, w), this);
					}
				}
			}
		}
	}

	private int Mod(int value, int size) {
		int r = value % size;
		return r < 0 ? r + size : r;
	}

	public override Slot4D GetSlot(Vector4Int position) {
		if (position.y < 0 || position.y >= this.SizeY) {
			return null;
		}
		return this.slots[
			Mod(position.x, this.SizeX),
			position.y,
			Mod(position.z, this.SizeZ),
			Mod(position.w, this.SizeW)];
	}

	public override IEnumerable<Slot4D> GetAllSlots() {
		for (int x = 0; x < this.SizeX; x++) {
			for (int y = 0; y < this.SizeY; y++) {
				for (int z = 0; z < this.SizeZ; z++) {
					for (int w = 0; w < this.SizeW; w++) {
						yield return this.slots[x, y, z, w];
					}
				}
			}
		}
	}

	public override void ApplyBoundaryConstraints(IEnumerable<BoundaryConstraint> constraints) {
		foreach (var constraint in constraints) {
			int y = constraint.RelativeY;
			if (y < 0) {
				y += this.SizeY;
			}
			switch (constraint.Direction) {
				case BoundaryConstraint.ConstraintDirection.Up:
					for (int x = 0; x < this.SizeX; x++) {
						for (int z = 0; z < this.SizeZ; z++) {
							for (int w = 0; w < this.SizeW; w++) {
								if (constraint.Mode == BoundaryConstraint.ConstraintMode.EnforceConnector) {
									this.GetSlot(new Vector4Int(x, this.SizeY - 1, z, w)).EnforceConnector(Orientations4D.UP, constraint.Connector);
								} else {
									this.GetSlot(new Vector4Int(x, this.SizeY - 1, z, w)).ExcludeConnector(Orientations4D.UP, constraint.Connector);
								}
							}
						}
					}
					break;
				case BoundaryConstraint.ConstraintDirection.Down:
					for (int x = 0; x < this.SizeX; x++) {
						for (int z = 0; z < this.SizeZ; z++) {
							for (int w = 0; w < this.SizeW; w++) {
								if (constraint.Mode == BoundaryConstraint.ConstraintMode.EnforceConnector) {
									this.GetSlot(new Vector4Int(x, 0, z, w)).EnforceConnector(Orientations4D.DOWN, constraint.Connector);
								} else {
									this.GetSlot(new Vector4Int(x, 0, z, w)).ExcludeConnector(Orientations4D.DOWN, constraint.Connector);
								}
							}
						}
					}
					break;
				case BoundaryConstraint.ConstraintDirection.Horizontal:
					break;
			}
		}
	}
}
