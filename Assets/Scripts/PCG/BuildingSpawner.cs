using UnityEngine;

// Instancia edificios y los orienta hacia la red. No decide que construir.
public class BuildingSpawner
{
    private readonly Transform parent;
    private readonly float cellSize;
    private readonly CityGrid grid;
    private readonly CityRandom rng;

    public BuildingSpawner(Transform parent, float cellSize, CityGrid grid, CityRandom rng)
    {
        this.parent = parent;
        this.cellSize = cellSize;
        this.grid = grid;
        this.rng = rng;
    }

    public static BuildingData GetData(GameObject prefab)
    {
        BuildingData data = prefab.GetComponent<BuildingData>();
        return data != null ? data : prefab.AddComponent<BuildingData>();
    }

    // Devuelve el socket solo si el objeto lo tiene completo
    public static CitySocket GetSocket(GameObject go)
    {
        CitySocket socket = go.GetComponent<CitySocket>();
        return (socket != null && socket.frontSocket != null) ? socket : null;
    }

    public GameObject Spawn(GameObject prefab, int x, int y, BuildingData data)
    {
        Vector3 pos = data.GetCalculatedWorldPosition(x, y, cellSize, prefab.transform.position.y);
        return UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity, parent);
    }

    // Celda hacia la que apunta el frente del edificio
    public Vector2Int GetFrontCell(CitySocket socket)
    {
        Transform front = socket.frontSocket;
        Vector3 pos = front.position + front.forward * (cellSize * 0.9f);
        return new Vector2Int(Mathf.FloorToInt(pos.x / cellSize), Mathf.FloorToInt(pos.z / cellSize));
    }

    // Edificio normal: gira hasta que su frente toque una calle. Sin socket se acepta tal cual.
    public GameObject SpawnFacingRoad(GameObject prefab, int x, int y, BuildingData data)
    {
        GameObject go = Spawn(prefab, x, y, data);
        CitySocket socket = GetSocket(go);
        if (socket == null) return go;

        go.transform.Rotate(0f, rng.Range(4) * 90f, 0f);
        for (int i = 0; i < 4; i++)
        {
            if (grid.IsRoad(GetFrontCell(socket))) return go;
            go.transform.Rotate(0f, 90f, 0f);
        }

        Discard(go);
        return null;
    }

    public void Discard(GameObject go) => UnityEngine.Object.Destroy(go);
}