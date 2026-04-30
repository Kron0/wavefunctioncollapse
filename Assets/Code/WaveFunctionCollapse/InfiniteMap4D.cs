using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InfiniteMap4D : AbstractMap4D {
	private Dictionary<Vector4Int, Slot4D> slots;
	private readonly object slotsLock = new object();

	public readonly int Height;

	public Vector4Int rangeLimitCenter;
	public int RangeLimit = 80;

	private TilingMap4D defaultColumn;

	public InfiniteMap4D(int height) : base() {
		this.Height = height;
		this.slots = new Dictionary<Vector4Int, Slot4D>();
		this.defaultColumn = new TilingMap4D(1, height, 1, 1);

		if (ModuleData.Current == null || ModuleData.Current.Length == 0) {
			throw new InvalidOperationException("Module data was not available, please create module data first.");
		}
	}

	public override Slot4D GetSlot(Vector4Int position) {
		if (position.y >= this.Height || position.y < 0) {
			return null;
		}

		lock (this.slotsLock) {
			if (this.slots.ContainsKey(position)) {
				return this.slots[position];
			}

			if (this.IsOutsideOfRangeLimit(position)) {
				return null;
			}

			this.slots[position] = new Slot4D(position, this, this.defaultColumn.GetSlot(new Vector4Int(0, position.y, 0, 0)));
			return this.slots[position];
		}
	}

	public bool IsOutsideOfRangeLimit(Vector4Int position) {
		return (position - this.rangeLimitCenter).magnitude > this.RangeLimit;
	}

	public override void ApplyBoundaryConstraints(IEnumerable<BoundaryConstraint> constraints) {
		foreach (var constraint in constraints) {
			int y = constraint.RelativeY;
			if (y < 0) {
				y += this.Height;
			}
			int[] directions = null;
			switch (constraint.Direction) {
				case BoundaryConstraint.ConstraintDirection.Up:
					directions = new int[] { Orientations4D.UP }; break;
				case BoundaryConstraint.ConstraintDirection.Down:
					directions = new int[] { Orientations4D.DOWN }; break;
				case BoundaryConstraint.ConstraintDirection.Horizontal:
					directions = Orientations4D.HorizontalDirections; break;
			}

			foreach (int d in directions) {
				switch (constraint.Mode) {
					case BoundaryConstraint.ConstraintMode.EnforceConnector:
						this.defaultColumn.GetSlot(new Vector4Int(0, y, 0, 0)).EnforceConnector(d, constraint.Connector);
						break;
					case BoundaryConstraint.ConstraintMode.ExcludeConnector:
						this.defaultColumn.GetSlot(new Vector4Int(0, y, 0, 0)).ExcludeConnector(d, constraint.Connector);
						break;
				}
			}
		}
	}

	public override IEnumerable<Slot4D> GetAllSlots() {
		lock (this.slotsLock) {
			return this.slots.Values.ToArray();
		}
	}

	public Slot4D GetDefaultSlot(int y) {
		return this.defaultColumn.GetSlot(new Vector4Int(0, y, 0, 0));
	}

	public bool IsSlotInitialized(Vector4Int position) {
		return this.slots.ContainsKey(position);
	}
}
