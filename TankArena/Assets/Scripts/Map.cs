using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class Map : NetworkBehaviour
{
    public GameObject[] treePrefabs;
    public GameObject[] rockPrefabs;
    public GameObject[] plantPrefabs;
    public GameObject[] powerPrefabs;
    public GameObject[] grassPrefabs;
    public GameObject[] mobCampPrefabs;
    [SyncVar]
    private int mainSeed = 0;

    Tile[,] tiles;
    public float scale = 0.1f;
    public float waterNoise = 0.3f;
    public float treeNoise = 0.1f;
    public float rockRate = 0.01f;
    public float treeRate = 0.4f;
    public float plantRate = 0.1f;
    public float powerRate = 0.2f;
    public float grassRate = 0.7f;
    public float tileSize = 0.5f;
    public int campSize = 5;
    public int size = 60;
    public int biome;
    public Material topMaterial;
    public Material sideMaterial;
    public Material borderMaterial;
    public NavMeshSurface navmesh;

    [Server]
    public override void OnStartServer()
    {
        base.OnStartServer();
        mainSeed = Random.Range(-1000000, 1000000);
    }

    [Server]
    private void CreatePowerUp(int x, int y) {
        int powerType = Random.Range(0, 4);
        GameObject powerPrefab = powerPrefabs[powerType];
        GameObject power = Instantiate(powerPrefab, new Vector3(x, 0.22f, y), Quaternion.Euler(0, Random.Range(0, 360f), 0));
        power.transform.position = new Vector3(x, 0.22f, y);
        power.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        power.transform.localScale = Vector3.one * 0.3f;
        NetworkServer.Spawn(power);
    }

    [Server]
    private void CreateMonster(int x, int y, int campNumber) {
        GameObject campPrefab = mobCampPrefabs[campNumber];
        GameObject camp = Instantiate(campPrefab, transform);
        camp.transform.position = new Vector3(x, 0, y);
        camp.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        camp.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
        NetworkServer.Spawn(camp);
    }

    [Server]
    public bool IsPlayerFalling(int x, int y) {
        if (x < 0 || y < 0 || x > size -1 || y > size -1)
            return true;
        return !tiles[x, y].walkable;
    }

    public override void OnStopClient() {
        if (!this.isServer)
            Application.LoadLevel(Application.loadedLevel);
    }

    void Start() {
        Random.InitState(mainSeed);
        float[,] randomMap = new float[size, size];
        float xSeed = Random.Range(-10000f, 10000f);
        float ySeed = Random.Range(-10000f, 10000f);
        for (int y = 0; y < size; y++){
            for (int x = 0; x < size; x++) {
                float randomCell = Mathf.PerlinNoise(x * scale+ xSeed, y * scale + ySeed);
                randomMap[x, y] = randomCell;
            }
        }
        tiles = new Tile[size, size];
            for(int y = 0; y < size; y++) {
                for(int x = 0; x < size; x++) {
                    Tile tile = new Tile();
                    float randomCell = randomMap[x, y];
                    tile.walkable = randomCell > waterNoise;
                    tiles[x, y] = tile;
                }
            }
        tiles[0, 0].walkable = true;
        biome = Random.Range(0, 4);
        CreateTopMeshes(tiles);
        CreateTexture(tiles, biome);
        CreateProps(tiles, biome);
        navmesh.BuildNavMesh();
    }

    private void CreateTree(int x, int y, int treeType) {
        tiles[x, y].isForest = true;
        GameObject treePrefab = treePrefabs[treeType];
        GameObject tree = Instantiate(treePrefab, transform);
        tree.transform.position = new Vector3(x, 0, y);
        tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        tree.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
        tree.AddComponent(typeof(BoxCollider));
        tree.AddComponent<Rigidbody>();
        Rigidbody rb = tree.GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void CreateRock(int x, int y) {
        int rockType = Random.Range(0, 4);
        GameObject rockPrefab = rockPrefabs[rockType];
        GameObject rock = Instantiate(rockPrefab, transform);
        rock.transform.position = new Vector3(x, 0, y);
        rock.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        rock.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
        rock.AddComponent(typeof(BoxCollider));
        rock.AddComponent<Rigidbody>();
        Rigidbody rb = rock.GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void CreatePlant(int x, int y) {
        int plantType = Random.Range(0, 8);
        GameObject plantPrefab = plantPrefabs[plantType];
        GameObject plant = Instantiate(plantPrefab, transform);
        plant.transform.position = new Vector3(x, 0, y);
        plant.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        plant.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
    }

    void CreateGrass(int x, int y) {
        int grassType = Random.Range(0, 1);
        GameObject grassPrefab = grassPrefabs[0];
        GameObject grass = Instantiate(grassPrefab, transform);
        grass.transform.position = new Vector3(x, 0, y);
        grass.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        grass.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
    }

    void CreateProps(Tile[,] tiles, int treeType) {
        float[,] randomTreeMap = new float[size, size];
        float xSeed = Random.Range(-10000f, 10000f);
        float ySeed = Random.Range(-10000f, 10000f);
        for (int y = 0; y < size; y++){
            for (int x = 0; x < size; x++) {
                float randomTreeCell = Mathf.PerlinNoise(x * treeNoise + xSeed, y * treeNoise + ySeed);
                randomTreeMap[x, y] = randomTreeCell;
            }
        }
        int campNumber = 0;
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
            Tile tile = tiles[x, y];
                if (tile.walkable) {
                    float isTree = Random.Range(0f, treeRate);
                    if (randomTreeMap[x, y] > grassRate) {
                        CreateGrass(x, y);
                    }                   
                    if (randomTreeMap[x, y] < isTree) {
                        CreateTree(x, y, treeType);
                    } else if (Random.Range(0f, 2f) < plantRate) {
                        CreatePlant(x, y);
                    } else if (Random.Range(0f, 1f) < rockRate) {
                        CreateRock(x, y);
                    } else if (Random.Range(0f, 100) < powerRate) {
                        CreatePowerUp(x,y);
                    }
                    if (isSpawnable(x, y, tiles, campSize, size) && campNumber < mobCampPrefabs.Length) {
                        CreateMonster(x, y, campNumber);
                        CreateCamp(x, y, tiles, campSize);
                        campNumber++;
                    }
                }
            }
        }
    }

    bool isSpawnable(int x, int y, Tile[,] tiles, int campSize, int mapSize) {
        bool spawnable = true;
        if (x > campSize && y > campSize && x < mapSize - campSize - 1 && y < mapSize - campSize - 1) {
            for (int i = 0; i < campSize; i++) {
                if (!tiles[x - i, y].walkable || tiles[x - i, y].isForest || tiles[x - i, y].isCamp) {
                    spawnable = false;
                } else if (!tiles[x + i, y].walkable || tiles[x + i, y].isForest || tiles[x + i, y].isCamp) {
                    spawnable = false;
                } else if (!tiles[x, y + i].walkable || tiles[x, y + i].isForest || tiles[x, y + i].isCamp) {
                    spawnable = false;
                } else if (!tiles[x, y - i].walkable || tiles[x, y - i].isForest || tiles[x, y - i].isCamp) {
                    spawnable = false;
                } else if (!tiles[x - i, y + i].walkable || tiles[x - i, y + i].isForest || tiles[x - i, y + i].isCamp) {
                    spawnable = false;
                } else if (!tiles[x + i, y + i].walkable || tiles[x + i, y + i].isForest || tiles[x + i, y + i].isCamp) {
                    spawnable = false;
                } else if (!tiles[x - i, y - i].walkable || tiles[x - i, y - i].isForest || tiles[x - i, y - i].isCamp) {
                    spawnable = false;
                } else if (!tiles[x + i, y - i].walkable || tiles[x + i , y - i].isForest || tiles[x + i, y - i].isCamp) {
                    spawnable = false;
                }
            }
            return(spawnable);
        } else {
            return false;
        }
    }

    void CreatePrefab(int x, int y, GameObject prefab) {
        GameObject newPrefab = Instantiate(prefab, transform);
        newPrefab.transform.position = new Vector3(x, 0, y);
        newPrefab.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        newPrefab.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
    }

    void CreateCamp(int x, int y, Tile[,] tiles, int campSize) {
        for (int i = 0; i < campSize; i++) {
            tiles[x - i, y].isCamp = true;
            tiles[x + i, y].isCamp = true;
            tiles[x, y - i].isCamp = true;
            tiles[x, y + i].isCamp = true;
            tiles[x - i, y + i].isCamp = true;
            tiles[x + i, y - i].isCamp = true;
            tiles[x - i, y - i].isCamp = true;
            tiles[x + i, y + i].isCamp = true;
        }
    }

    void CreateTopMeshes(Tile[,] tiles) {
        Mesh mesh = new Mesh();
        List<Vector3> vao =  new List<Vector3>();
        List<int> cube = new List<int>();
        for(int y = 0; y < size; y++) {
            for (int x = 0; x < size ; x++) {
                Tile tile = tiles[x, y];
                if (tile.walkable) {
                    Vector3 at = new Vector3(x - tileSize, 0, y + tileSize);
                    Vector3 bt = new Vector3(x + tileSize, 0, y + tileSize);
                    Vector3 ct = new Vector3(x - tileSize, 0, y - tileSize);
                    Vector3 dt = new Vector3(x + tileSize, 0, y - tileSize);
                    Vector3[] cornersTop = new Vector3[] {at, bt, ct, bt, dt , ct};
                    for (int i = 0; i < 6; i++) {
                        vao.Add(cornersTop[i]);
                        cube.Add(cube.Count);
                    }
                    if (x > 0 && !tiles[x - 1, y].walkable) {
                        Vector3 a = new Vector3(x - tileSize, 0, y + tileSize);
                        Vector3 b = new Vector3(x - tileSize, 0, y - tileSize);
                        Vector3 c = new Vector3(x - tileSize, -1, y + tileSize);
                        Vector3 d = new Vector3(x - tileSize, -1, y - tileSize);
                        Vector3[] corners = new Vector3[] {a, b, c, b, d , c};
                        for (int i = 0; i < 6; i++) {
                            vao.Add(corners[i]);
                            cube.Add(cube.Count);
                        }
                    }
                    if (x < size - 1 && !tiles[x + 1, y].walkable) {
                        Vector3 a = new Vector3(x + tileSize, 0, y - tileSize);
                        Vector3 b = new Vector3(x + tileSize, 0, y + tileSize);
                        Vector3 c = new Vector3(x + tileSize, -1, y - tileSize);
                        Vector3 d = new Vector3(x + tileSize, -1, y + tileSize);
                        Vector3[] corners = new Vector3[] {a, b, c, b, d , c};
                        for (int i = 0; i < 6; i++) {
                            vao.Add(corners[i]);
                            cube.Add(cube.Count);
                        }
                    }
                    if (y > 0 && !tiles[x , y - 1].walkable) {
                        Vector3 a = new Vector3(x - tileSize, 0, y - tileSize);
                        Vector3 b = new Vector3(x + tileSize, 0, y - tileSize);
                        Vector3 c = new Vector3(x - tileSize, -1, y - tileSize);
                        Vector3 d = new Vector3(x + tileSize, -1, y - tileSize);
                        Vector3[] corners = new Vector3[] {a, b, c, b, d , c};
                        for (int i = 0; i < 6; i++) {
                            vao.Add(corners[i]);
                            cube.Add(cube.Count);
                        }
                    }
                    if (y < size - 1 && !tiles[x , y + 1].walkable) {
                        Vector3 a = new Vector3(x + tileSize, 0, y + tileSize);
                        Vector3 b = new Vector3(x - tileSize, 0, y + tileSize);
                        Vector3 c = new Vector3(x + tileSize, -1, y + tileSize);
                        Vector3 d = new Vector3(x - tileSize, -1, y + tileSize);
                        Vector3[] corners = new Vector3[] {a, b, c, b, d , c};
                        for (int i = 0; i < 6; i++) {
                            vao.Add(corners[i]);
                            cube.Add(cube.Count);
                        }
                    }
                }
            }
        }
        mesh.vertices = vao.ToArray();
        mesh.triangles = cube.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
    }

    void CreateTexture(Tile[,] tiles, int biome) {
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                Tile tile = tiles[x, y];
                switch (biome) {
                    case 0:
                        colors[y * size + x] = new Color(79f/255f, 121f/255f, 66f/255f);
                        break;
                    case 1:
                        colors[y * size + x] = new Color(34f/255f, 139f/255f, 34f/255f);
                        break;
                    case 2:
                        colors[y * size + x] = new Color(124f/255f, 252f/255f, 0f);
                        break;
                    case 3:
                        colors[y * size + x] = new Color(53f/255f, 94f/255f, 59f/255f);
                        break;
                    default:
                        colors[y * size + x] = Color.green;
                        break;
                }
            }
        }
        texture.filterMode = FilterMode.Point;
        texture.SetPixels(colors);
        texture.Apply();
        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshRenderer.material = topMaterial;
        meshRenderer.material.mainTexture = texture;
    }
}
