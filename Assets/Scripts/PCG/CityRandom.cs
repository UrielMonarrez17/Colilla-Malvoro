using UnityEngine;

// Envuelve MathPRNG para usarlo como Random de Unity.
public class CityRandom
{
    private readonly MathPRNG prng;

    public CityRandom(MathPRNG prng) { this.prng = prng; }

    // Igual que Random.value: [0, 1)
    public float Value() => (float)prng.NextProbability(2);

    // Igual que Random.Range(0, max) con enteros (max exclusivo)
    public int Range(int maxExclusive) => Range(0, maxExclusive);

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive) return minInclusive;
        return minInclusive + Mathf.FloorToInt(Value() * (maxExclusive - minInclusive));
    }

    // Igual que Random.value < probability
    public bool Chance(float probability) => Value() < probability;
}
