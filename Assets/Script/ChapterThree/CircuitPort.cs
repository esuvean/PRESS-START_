using UnityEngine;

public enum CircuitPortShape
{
    Circle,
    Square,
    Triangle
}

public class CircuitPort : MonoBehaviour
{
    [Header("Port Shape")]
    public CircuitPortShape shape;

    public RectTransform RectTransform
    {
        get
        {
            return transform as RectTransform;
        }
    }
}