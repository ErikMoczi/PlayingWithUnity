using System;
using Random = UnityEngine.Random;

[Serializable]
public struct FloatRange
{
    public float Min;
    public float Max;

    public float RandomInRange
    {
        get { return Random.Range(Min, Max); }
    }
}