using UnityEngine;

// Implemented by FourDController (Player). Declared in Map so MapBehaviour4D
// and GenerateMap4DNearPlayer can use it without a Map → Player assembly dependency.
public interface IWPositionProvider {
    float WPosition { get; }
    Transform transform { get; }
    bool SnapToGround();
    void ClampWPosition(int layer);
}
