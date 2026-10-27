using System;
using System.Collections.Generic;

public class MathPRNG
{
    private double currentSeed;
    private Queue<int> digitQueue = new Queue<int>();

    public MathPRNG(int initialSeed)
    {
        currentSeed = initialSeed <= 0 ? 12345 : initialSeed;
    }

    public int ConsumeDigit()
    {
        if (digitQueue.Count == 0) ExtractDigits();
        return digitQueue.Dequeue();
    }

    // NUEVO: Retorna un valor entre 0.0 y 1.0 usando tu nueva función NextProbability


    private void ExtractDigits()
    {
        double rawValue = Math.Log(currentSeed + Math.E) * Math.Log(335958629508241L); // podria ser tambien 999999999999989L si se necesita un valor mas alto
        double fraction = rawValue - Math.Floor(rawValue);
        int digits = (int)(fraction * 100000000);
        
        if (digits < 1000000)
        {
            double rescue = 1.0 / (fraction + 0.00001);
            double rescueFrac = rescue - Math.Floor(rescue);
            digits = (int)(rescueFrac * 100000000);
        }

        currentSeed = digits == 0 ? 99999999 : digits;

        string digitString = digits.ToString("D8"); 
        foreach (char c in digitString)
        {
            digitQueue.Enqueue(c - '0');
        }
    }

    public double NextProbability(uint digits = 0U)
    {
        double probability = ConsumeDigit() * 10 + ConsumeDigit();
        for (int i = 1; i <= digits; i++)
        {
            probability += ConsumeDigit() * Math.Pow(10, -i);
        }        
        return probability/100;
    }
}