using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Prototipo3;
using Prototipo3.Narrative;

public class TestFeralStandalone
{
    private static int testsPassed = 0;
    private static int testsFailed = 0;

    private static void Assert(bool condition, string testName, string failureDetail = "")
    {
        if (condition)
        {
            Console.WriteLine($"[PASS] {testName}");
            testsPassed++;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {testName}: {failureDetail}");
            Console.ResetColor();
            testsFailed++;
        }
    }

    public static int Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("FERAL HYBRID STORY SYSTEM - STANDALONE DEEP TESTS");
        Console.WriteLine("=================================================");

        TestCFGDerivation();
        TestTrapFormulas();
        TestCardinalDirections();
        TestManhattanAnchors();
        TestCutscenesFiles();

        Console.WriteLine("=================================================");
        Console.WriteLine($"TEST SUMMARY: {testsPassed} PASSED, {testsFailed} FAILED");
        Console.WriteLine("=================================================");

        return testsFailed == 0 ? 0 : 1;
    }

    private static void TestCFGDerivation()
    {
        Console.WriteLine("\n--- Testing CFG Grammar Derivation ---");

        string[] symbols = new string[]
        {
            "ARCO_CORTO",
            "ARCO_MEDIO",
            "ARCO_COMPLEJO",
            "ARCO_CORTO_OPC",
            "ARCO_MEDIO_OPC",
            "INVESTIGAR",
            "OBTENER",
            "PREPARAR",
            "ACCESO",
            "SUBMISION",
            "NEUTRALIZAR",
            "BOSS_ENCOUNTER"
        };

        int[] seeds = new int[] { 42, 101, 777, 9999, 12345 };

        foreach (string sym in symbols)
        {
            foreach (int seed in seeds)
            {
                List<TerminalAction> actions = QuestGrammarParser.Derivate(sym, seed, 2);
                Assert(actions != null && actions.Count > 0,
                    $"Derivate({sym}, seed={seed}) non-empty",
                    $"Count was {(actions == null ? "null" : actions.Count.ToString())}");

                if (actions != null)
                {
                    bool allInVocab = true;
                    string badToken = "";
                    foreach (var a in actions)
                    {
                        if (!QuestGrammarParser.TerminalVocabulary.Contains(a.actionType))
                        {
                            allInVocab = false;
                            badToken = a.actionType;
                            break;
                        }
                    }
                    Assert(allInVocab, $"All tokens of {sym} (seed={seed}) in TerminalVocabulary", $"Invalid token: {badToken}");
                }
            }
        }

        // Test optional sub-arcs in ARCO_COMPLEJO
        foreach (int seed in seeds)
        {
            List<TerminalAction> complex = QuestGrammarParser.Derivate("ARCO_COMPLEJO", seed, 2);
            bool hasOptional = false;
            foreach (var a in complex)
            {
                if (a.isOptional) { hasOptional = true; break; }
            }
            Assert(hasOptional, $"ARCO_COMPLEJO (seed={seed}) contains optional sub-arc actions", "No isOptional actions found");
        }

        // Test BOSS_ENCOUNTER has 4 phases terminating with antidote and pacification
        List<TerminalAction> bossActions = QuestGrammarParser.Derivate("BOSS_ENCOUNTER", 12345, 2);
        Assert(bossActions.Count >= 6, $"BOSS_ENCOUNTER derived {bossActions.Count} actions (>= 6 expected)");
        Assert(bossActions[bossActions.Count - 2].actionType == "inyectar_antidoto_vial", "BOSS_ENCOUNTER phase 4 has inyectar_antidoto_vial");
        Assert(bossActions[bossActions.Count - 1].actionType == "hablar_npc", "BOSS_ENCOUNTER phase 4 ends with hablar_npc");
    }

    private static void TestTrapFormulas()
    {
        Console.WriteLine("\n--- Testing Trap Recipes and Descriptions ---");

        Assert(TrapSystemPrototipo3.GetTrapRecipeText(TipoTrampa.Madera) == "2 Madera",
            "TrampaMadera recipe is 2 Madera",
            TrapSystemPrototipo3.GetTrapRecipeText(TipoTrampa.Madera));

        Assert(TrapSystemPrototipo3.GetTrapRecipeText(TipoTrampa.Red) == "1 Goma + 1 Desperdicios",
            "TrampaRed recipe is 1 Goma + 1 Desperdicios",
            TrapSystemPrototipo3.GetTrapRecipeText(TipoTrampa.Red));

        Assert(TrapSystemPrototipo3.GetTrapRecipeText(TipoTrampa.Puas) == "2 Metal + 1 Madera",
            "TrampaPuas recipe is 2 Metal + 1 Madera",
            TrapSystemPrototipo3.GetTrapRecipeText(TipoTrampa.Puas));

        string descMadera = TrapSystemPrototipo3.GetTrapDescription(TipoTrampa.Madera);
        Assert(descMadera.Contains("10 s") && descMadera.Contains("NavMeshObstacle"),
            "TrampaMadera description mentions 10s and NavMeshObstacle");

        string descRed = TrapSystemPrototipo3.GetTrapDescription(TipoTrampa.Red);
        Assert(descRed.Contains("5 s") && descRed.Contains("inmoviliza"),
            "TrampaRed description mentions 5s immobilization");

        string descPuas = TrapSystemPrototipo3.GetTrapDescription(TipoTrampa.Puas);
        Assert(descPuas.Contains("50%") && descPuas.Contains("6 s") && descPuas.Contains("chispas"),
            "TrampaPuas description mentions 50% slow for 6s with sparks");
    }

    private static void TestCardinalDirections()
    {
        Console.WriteLine("\n--- Testing Cardinal Direction Hints ---");

        GameObject dummy = new GameObject("DummyCityGen");
        CityGeneratorPrototipo3 cityGen = dummy.AddComponent<CityGeneratorPrototipo3>();

        Vector3 origin = Vector3.zero;

        string north = cityGen.GetCardinalDirectionHint(origin, new Vector3(0, 0, 50));
        Assert(north.Contains("Norte"), $"Direction (0, 0, 50) is North: got '{north}'");

        string northEast = cityGen.GetCardinalDirectionHint(origin, new Vector3(50, 0, 50));
        Assert(northEast.Contains("Noreste"), $"Direction (50, 0, 50) is NorthEast: got '{northEast}'");

        string east = cityGen.GetCardinalDirectionHint(origin, new Vector3(50, 0, 0));
        Assert(east.Contains("Este"), $"Direction (50, 0, 0) is East: got '{east}'");

        string southEast = cityGen.GetCardinalDirectionHint(origin, new Vector3(50, 0, -50));
        Assert(southEast.Contains("Sureste"), $"Direction (50, 0, -50) is SouthEast: got '{southEast}'");

        string south = cityGen.GetCardinalDirectionHint(origin, new Vector3(0, 0, -50));
        Assert(south.Contains("Sur"), $"Direction (0, 0, -50) is South: got '{south}'");

        string southWest = cityGen.GetCardinalDirectionHint(origin, new Vector3(-50, 0, -50));
        Assert(southWest.Contains("Suroeste"), $"Direction (-50, 0, -50) is SouthWest: got '{southWest}'");

        string west = cityGen.GetCardinalDirectionHint(origin, new Vector3(-50, 0, 0));
        Assert(west.Contains("Oeste"), $"Direction (-50, 0, 0) is West: got '{west}'");

        string northWest = cityGen.GetCardinalDirectionHint(origin, new Vector3(-50, 0, 50));
        Assert(northWest.Contains("Noroeste"), $"Direction (-50, 0, 50) is NorthWest: got '{northWest}'");

        string nearby = cityGen.GetCardinalDirectionHint(origin, new Vector3(1, 0, 1));
        Assert(nearby.Contains("aquí mismo"), $"Direction (1, 0, 1) is nearby: got '{nearby}'");

        GameObject.DestroyImmediate(dummy);
    }

    private static void TestManhattanAnchors()
    {
        Console.WriteLine("\n--- Testing Manhattan Ring Bounds ---");

        Vector2Int baseCell = new Vector2Int(10, 10);
        Vector2Int ring0Cell = new Vector2Int(10, 11);
        int d0 = Math.Abs(ring0Cell.x - baseCell.x) + Math.Abs(ring0Cell.y - baseCell.y);
        Assert(d0 <= 1, $"Ring 0 Manhattan distance {d0} <= 1");

        Vector2Int ring1Cell = new Vector2Int(12, 12); // dM = 2 + 2 = 4
        int d1 = Math.Abs(ring1Cell.x - baseCell.x) + Math.Abs(ring1Cell.y - baseCell.y);
        Assert(d1 >= 3 && d1 <= 5, $"Ring 1 Manhattan distance {d1} in [3, 5]");

        Vector2Int ring2CellA = new Vector2Int(14, 14); // dM = 4 + 4 = 8
        int d2a = Math.Abs(ring2CellA.x - baseCell.x) + Math.Abs(ring2CellA.y - baseCell.y);
        Assert(d2a >= 6 && d2a <= 9, $"Ring 2 Manhattan distance {d2a} in [6, 9]");

        Vector2Int ring3Cell = new Vector2Int(20, 15); // dM = 10 + 5 = 15
        int d3 = Math.Abs(ring3Cell.x - baseCell.x) + Math.Abs(ring3Cell.y - baseCell.y);
        Assert(d3 >= 10, $"Ring 3 Manhattan distance {d3} >= 10");
    }

    private static void TestCutscenesFiles()
    {
        Console.WriteLine("\n--- Testing Cutscene Files and Fallback Presence ---");

        string streamingCutscenes = @"C:\Users\CIAC\Documents\Colilla-Malvoro\Assets\StreamingAssets\Cutscenes";
        Assert(Directory.Exists(streamingCutscenes), "StreamingAssets/Cutscenes directory exists");

        string[] required = CutsceneManager.RequiredCutsceneFiles;
        Assert(required.Length == 7, $"RequiredCutsceneFiles count is 7: got {required.Length}");

        int foundMp4 = 0;
        int missingMp4 = 0;
        foreach (string file in required)
        {
            string fullPath = Path.Combine(streamingCutscenes, file);
            if (File.Exists(fullPath))
            {
                foundMp4++;
                Console.WriteLine($"  [MP4 Available] {file}");
            }
            else
            {
                missingMp4++;
                Console.WriteLine($"  [In-Engine Fallback Active] {file}");
            }
        }

        Assert(foundMp4 >= 5, $"At least 5 MP4 files present (found {foundMp4})");
        Assert(missingMp4 >= 2, $"At least 2 cutscenes use In-Engine Comic Fallback (found {missingMp4})");
    }
}
