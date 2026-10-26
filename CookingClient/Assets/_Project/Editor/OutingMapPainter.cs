using System.Collections.Generic;
using Cooking.Exploration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Cooking.EditorTools
{
    public static class OutingMapPainter
    {
        const string ScenePath = "Assets/_Project/Scenes/outingScene.unity";
        const string SheetPath = "Assets/_Project/Arts/playGround/foresttiles.png";
        const string TileFolder = "Assets/_Project/Arts/playGround/Tiles";
        const float Cell = 0.16f;
        const float CameraSize = 5.4f;

        const int MinX = -60;
        const int MaxX = 59;
        const int MinY = -34;
        const int MaxY = 33;
        static readonly int[] GrassIds = { 3, 15, 7, 5 };
        static readonly int[] DirtIds = { 17, 13, 21, 16 };
        static readonly int[] BushIds = { 1, 9, 5, 8, 4, 0, 10 };
        static readonly int[] FlowerIds = { 11, 19, 23 };

        [MenuItem("Cooking/Paint Outing Ground")]
        public static void Paint()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = false;
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedHere = true;
            }

            Dictionary<int, Tile> tiles = LoadTiles();

            Tilemap ground = FindTilemap(scene, "Ground");
            Tilemap decoration = FindTilemap(scene, "Decoration");
            Tilemap collision = FindTilemap(scene, "Collision");
            if (ground == null || decoration == null)
            {
                Debug.LogError("outingScene 里没有 Ground 或 Decoration。");
                return;
            }

            ground.ClearAllTiles();
            decoration.ClearAllTiles();
            if (collision != null)
                collision.ClearAllTiles();

            int width = MaxX - MinX + 1;
            int height = MaxY - MinY + 1;
            int count = width * height;
            var cells = new Vector3Int[count];
            var groundTiles = new TileBase[count];
            var dirt = new bool[count];

            for (int y = MinY; y <= MaxY; y++)
            {
                for (int x = MinX; x <= MaxX; x++)
                {
                    int index = Index(x, y, width);
                    bool isDirt = IsDirt(x, y);
                    dirt[index] = isDirt;
                    cells[index] = new Vector3Int(x, y, 0);
                    groundTiles[index] = isDirt ? Pick(tiles, DirtIds, x, y) : Pick(tiles, GrassIds, x, y);
                }
            }

            ground.SetTiles(cells, groundTiles);

            var decoCells = new List<Vector3Int>(count / 3);
            var decoTiles = new List<TileBase>(count / 3);
            for (int y = MinY; y <= MaxY; y++)
            {
                for (int x = MinX; x <= MaxX; x++)
                {
                    int index = Index(x, y, width);
                    if (dirt[index] || KeptClear(x, y))
                        continue;

                    int near = NearDirt(dirt, x, y, width) ? 1 : 0;
                    int roll = Hash(x, y) % 100;
                    int bushChance = near == 1 ? 22 : 48;
                    if (roll < bushChance)
                    {
                        decoCells.Add(new Vector3Int(x, y, 0));
                        decoTiles.Add(Pick(tiles, BushIds, x, y + 17));
                        continue;
                    }

                    if (roll > 94)
                    {
                        decoCells.Add(new Vector3Int(x, y, 0));
                        decoTiles.Add(Pick(tiles, FlowerIds, x + 3, y));
                    }
                }
            }

            decoration.SetTiles(decoCells.ToArray(), decoTiles.ToArray());
            decoration.GetComponent<TilemapRenderer>().sortingOrder = 1;
            ground.GetComponent<TilemapRenderer>().sortingOrder = 0;

            PlaceActors(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedHere)
                EditorSceneManager.CloseScene(scene, true);
            Debug.Log("已铺夜晚地图。地面 " + count + " 格，装饰 " + decoCells.Count + " 格。");

            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        static bool IsDirt(int x, int y)
        {
            float noise = Mathf.PerlinNoise(x * 0.08f + 4f, y * 0.08f + 2f);
            float nx = x / 36f;
            float ny = (y + 2f) / 22f;
            if (nx * nx + ny * ny < 0.62f + noise * 0.28f)
            {
                if (Island(x, y, -18, 6, 9, 6))
                    return false;
                if (Island(x, y, 16, -8, 10, 6))
                    return false;
                return true;
            }

            if (Mathf.Abs(y + 2) <= 1 && x >= -42 && x <= 42)
                return true;
            if (Mathf.Abs(x - 2) <= 1 && y >= -20 && y <= 16)
                return true;
            if (DistanceToSegment(x, y, 10, 0, 36, 10) <= 1.6f)
                return true;
            if (DistanceToSegment(x, y, -8, -2, -36, -8) <= 1.6f)
                return true;
            if (Island(x, y, 36, 10, 7, 5))
                return true;
            if (Island(x, y, -36, -8, 6, 5))
                return true;
            return false;
        }

        static bool Island(int x, int y, int cx, int cy, float rx, float ry)
        {
            float dx = (x - cx) / rx;
            float dy = (y - cy) / ry;
            float noise = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
            return dx * dx + dy * dy < 0.82f + noise * 0.28f;
        }

        static bool KeptClear(int x, int y)
        {
            if (Island(x, y, 0, -2, 6, 5))
                return true;
            if (Island(x, y, 36, 10, 6, 5))
                return true;
            if (Island(x, y, -36, -8, 5, 4))
                return true;
            return false;
        }

        static bool NearDirt(bool[] dirt, int x, int y, int width)
        {
            for (int dy = -3; dy <= 3; dy++)
            {
                for (int dx = -3; dx <= 3; dx++)
                {
                    int sx = x + dx;
                    int sy = y + dy;
                    if (sx < MinX || sx > MaxX || sy < MinY || sy > MaxY)
                        continue;
                    if (dirt[Index(sx, sy, width)])
                        return true;
                }
            }

            return false;
        }

        static void PlaceActors(Scene scene)
        {
            Transform player = FindNamed(scene, "Player");
            Transform bed = FindNamed(scene, "bed");
            Transform mushroom = FindNamed(scene, "masheroom");
            Transform cameraTransform = FindNamed(scene, "Main Camera");
            Camera camera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;

            if (player != null)
            {
                player.position = CellCenter(0, -2);
                SetOrder(player, 5);
            }

            if (bed != null)
            {
                bed.position = CellCenter(36, 10);
                SetOrder(bed, 5);
            }

            if (mushroom != null)
            {
                mushroom.position = CellCenter(-36, -8);
                SetOrder(mushroom, 5);
            }

            if (camera == null || player == null)
                return;

            camera.orthographic = true;
            camera.orthographicSize = CameraSize;
            float halfX = CameraSize * (16f / 9f);
            float halfY = CameraSize;
            float mapMinX = MinX * Cell;
            float mapMaxX = (MaxX + 1) * Cell;
            float mapMinY = MinY * Cell;
            float mapMaxY = (MaxY + 1) * Cell;

            ExploreCameraFollow follow = camera.GetComponent<ExploreCameraFollow>();
            if (follow == null)
                follow = camera.gameObject.AddComponent<ExploreCameraFollow>();

            SerializedObject so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = player;
            so.FindProperty("min").vector2Value = new Vector2(mapMinX + halfX, mapMinY + halfY);
            so.FindProperty("max").vector2Value = new Vector2(mapMaxX - halfX, mapMaxY - halfY);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Vector3 CellCenter(int x, int y)
        {
            return new Vector3((x + 0.5f) * Cell, (y + 0.5f) * Cell, 0f);
        }

        static void SetOrder(Transform transform, int order)
        {
            SpriteRenderer renderer = transform.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.sortingOrder = order;
        }

        static Dictionary<int, Tile> LoadTiles()
        {
            var sprites = new Dictionary<string, Sprite>();
            foreach (Object asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(SheetPath))
            {
                if (asset is Sprite sprite)
                    sprites[sprite.name] = sprite;
            }

            var tiles = new Dictionary<int, Tile>();
            int[] ids = { 0, 1, 3, 4, 5, 7, 8, 9, 10, 11, 13, 15, 16, 17, 19, 21, 23 };
            foreach (int id in ids)
            {
                string spriteName = "foresttiles_" + id;
                if (!sprites.TryGetValue(spriteName, out Sprite sprite))
                {
                    Debug.LogError("找不到精灵 " + spriteName);
                    continue;
                }

                string path = TileFolder + "/ft_" + id.ToString("00") + ".asset";
                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = sprite;
                    tile.color = Color.white;
                    tile.colliderType = Tile.ColliderType.None;
                    AssetDatabase.CreateAsset(tile, path);
                }
                else if (tile.sprite != sprite)
                {
                    tile.sprite = sprite;
                    EditorUtility.SetDirty(tile);
                }

                tiles[id] = tile;
            }

            AssetDatabase.SaveAssets();
            return tiles;
        }

        static Tile Pick(Dictionary<int, Tile> tiles, int[] ids, int x, int y)
        {
            int id = ids[Hash(x, y) % ids.Length];
            return tiles[id];
        }

        static int Index(int x, int y, int width)
        {
            return (y - MinY) * width + (x - MinX);
        }

        static int Hash(int x, int y)
        {
            unchecked
            {
                int n = x * 374761393 + y * 668265263;
                n = (n ^ (n >> 13)) * 1274126177;
                return n & 0x7fffffff;
            }
        }

        static float DistanceToSegment(int x, int y, int x1, int y1, int x2, int y2)
        {
            float vx = x2 - x1;
            float vy = y2 - y1;
            float wx = x - x1;
            float wy = y - y1;
            float lengthSquared = vx * vx + vy * vy;
            float t = lengthSquared <= 0.001f ? 0f : Mathf.Clamp01((vx * wx + vy * wy) / lengthSquared);
            float dx = x - (x1 + t * vx);
            float dy = y - (y1 + t * vy);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        static Tilemap FindTilemap(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
                {
                    if (tilemap.name == objectName)
                        return tilemap;
                }
            }

            return null;
        }

        static Transform FindNamed(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindChild(root.transform, objectName);
                if (found != null)
                    return found;
            }

            return null;
        }

        static Transform FindChild(Transform node, string objectName)
        {
            if (node.name == objectName)
                return node;

            for (int i = 0; i < node.childCount; i++)
            {
                Transform found = FindChild(node.GetChild(i), objectName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
