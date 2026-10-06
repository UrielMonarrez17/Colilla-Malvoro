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
        if (digitQueue.Count == 0)
        {
            ExtractDigits();
        }
        return digitQueue.Dequeue();
    }

    private void ExtractDigits()
    {
        // 1. Aplicamos Logaritmo Natural. Sumamos Euler para asegurar que no haya Log(0) o negativos.
        double rawValue = Math.Log10(currentSeed + 10);
        
        // 2. Extraer la parte fraccionaria pura (ej. 0.141516)
        double fraction = rawValue - Math.Floor(rawValue);
        
        // 3. Truncar para obtener un entero de 6 dígitos
        int sixDigits = (int)(fraction * 1000000);
        
        // 4. Si tiende a cero, aplicamos 1/x
        if (sixDigits < 100000)
        {
            double rescue = 1.0 / (fraction + 0.0001);
            double rescueFrac = rescue - Math.Floor(rescue);
            sixDigits = (int)(rescueFrac * 1000000);
        }

        // 5. Preparar la semilla para la siguiente generación
        currentSeed = sixDigits == 0 ? 999999 : sixDigits;

        // 6. Convertir el número en sus dígitos individuales y encolarlos
        string digitString = sixDigits.ToString("D6"); 
        foreach (char c in digitString)
        {
            digitQueue.Enqueue(c - '0');
        }
    }
}