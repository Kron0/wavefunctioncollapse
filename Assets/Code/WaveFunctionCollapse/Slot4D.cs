using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Slot4D {
	public Vector4Int Position;

	public ModuleSet Modules;

	public short[][] ModuleHealth;

	private AbstractMap4D map;

	public Module Module;

	public GameObject GameObject;

	public bool Collapsed {
		get {
			return this.Module != null;
		}
	}

	public bool ConstructionComplete {
		get {
			return this.GameObject != null || (this.Collapsed && !this.Module.Prototype.Spawn);
		}
	}

	public Slot4D(Vector4Int position, AbstractMap4D map) {
		this.Position = position;
		this.map = map;
		this.ModuleHealth = map.CopyInitialModuleHealth();
		this.Modules = new ModuleSet(initializeFull: true);
	}

	public Slot4D(Vector4Int position, AbstractMap4D map, Slot4D prototype) {
		this.Position = position;
		this.map = map;
		this.ModuleHealth = prototype.ModuleHealth.Select(a => a.ToArray()).ToArray();
		this.Modules = new ModuleSet(prototype.Modules);
	}

	public Slot4D GetNeighbor(int direction) {
		return this.map.GetSlot(this.Position + Orientations4D.Direction[direction]);
	}

	public void Collapse(Module module) {
		if (this.Collapsed) {
			Debug.LogWarning("Trying to collapse already collapsed 4D slot.");
			return;
		}

		this.map.History.Push(new HistoryItem4D(this));

		this.Module = module;
		var toRemove = new ModuleSet(this.Modules);
		toRemove.Remove(module);
		this.RemoveModules(toRemove);

		this.map.NotifySlotCollapsed(this);
	}

	public void CollapseRandom() {
		if (!this.Modules.Any()) {
			throw new CollapseFailedException4D(this);
		}
		if (this.Collapsed) {
			throw new Exception("4D Slot is already collapsed.");
		}

		float max = this.Modules.Select(module => module.Prototype.Probability).Sum();
		float roll = (float)(AbstractMap4D.Random.NextDouble() * max);
		float p = 0;
		foreach (var candidate in this.Modules) {
			p += candidate.Prototype.Probability;
			if (p >= roll) {
				this.Collapse(candidate);
				return;
			}
		}
		this.Collapse(this.Modules.First());
	}

	public void RemoveModules(ModuleSet modulesToRemove, bool recursive = true) {
		modulesToRemove.Intersect(this.Modules);

		if (this.map.History != null && this.map.History.Any()) {
			var item = this.map.History.Peek();
			if (!item.RemovedModules.ContainsKey(this.Position)) {
				item.RemovedModules[this.Position] = new ModuleSet();
			}
			item.RemovedModules[this.Position].Add(modulesToRemove);
		}

		for (int d = 0; d < 8; d++) {
			int inverseDirection = Orientations4D.Opposite(d);
			var neighbor = this.GetNeighbor(d);
			if (neighbor == null || neighbor.Forgotten) {
				continue;
			}

			foreach (var module in modulesToRemove) {
				for (int i = 0; i < module.PossibleNeighborsArray4D[d].Length; i++) {
					var possibleNeighbor = module.PossibleNeighborsArray4D[d][i];
					if (neighbor.ModuleHealth[inverseDirection][possibleNeighbor.Index] == 1 && neighbor.Modules.Contains(possibleNeighbor)) {
						this.map.RemovalQueue[neighbor.Position].Add(possibleNeighbor);
					}
					neighbor.ModuleHealth[inverseDirection][possibleNeighbor.Index]--;
				}
			}
		}

		this.Modules.Remove(modulesToRemove);

		if (this.Modules.Empty) {
			throw new CollapseFailedException4D(this);
		}

		if (recursive) {
			this.map.FinishRemovalQueue();
		}
	}

	public void AddModules(ModuleSet modulesToAdd) {
		foreach (var module in modulesToAdd) {
			if (this.Modules.Contains(module) || module == this.Module) {
				continue;
			}
			for (int d = 0; d < 8; d++) {
				int inverseDirection = Orientations4D.Opposite(d);
				var neighbor = this.GetNeighbor(d);
				if (neighbor == null || neighbor.Forgotten) {
					continue;
				}

				foreach (var possibleNeighbor in module.PossibleNeighbors4D[d]) {
					neighbor.ModuleHealth[inverseDirection][possibleNeighbor.Index]++;
				}
			}
			this.Modules.Add(module);
		}

		if (this.Collapsed && !this.Modules.Empty) {
			this.Module = null;
			this.map.NotifySlotCollapseUndone(this);
		}
	}

	public void EnforceConnector(int direction, int connector) {
		int dir3D = Module.MapDirection4DTo3D(direction);
		var toRemove = this.Modules.Where(module => !module.Fits(dir3D, connector));
		this.RemoveModules(ModuleSet.FromEnumerable(toRemove));
	}

	public void ExcludeConnector(int direction, int connector) {
		int dir3D = Module.MapDirection4DTo3D(direction);
		var toRemove = this.Modules.Where(module => module.Fits(dir3D, connector));
		this.RemoveModules(ModuleSet.FromEnumerable(toRemove));
	}

	public override int GetHashCode() {
		return this.Position.GetHashCode();
	}

	public void Forget() {
		this.ModuleHealth = null;
		this.Modules = null;
	}

	public bool Forgotten {
		get {
			return this.Modules == null;
		}
	}
}
