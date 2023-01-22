using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Cysharp.Threading.Tasks;
using TicTacToe.Lib.Util;

namespace TicTacToe.App
{
    public class Board : SingletonMonoBehaviour<Board>
    {
        [SerializeField] Camera m_camera;
        [SerializeField] Tilemap m_tilemap;
        [SerializeField] TilemapRenderer m_tilemapRenderer;
        [SerializeField] TileBase[] m_tiles;

        public void Setup()
        {
            Clear();
            // カメラを中央に移動
            var x = (3.0f / 2.0f) * 2.0f;
            var y = (3.0f / 2.0f) * 2.0f;
            m_camera.transform.position = new Vector3(x, y, -10);
        }

        public void Clear()
        {
            foreach (int row in Enumerable.Range(0, 3))
            {
                foreach (int col in Enumerable.Range(0, 3))
                {
                    Put(new Vector2Int(col, row), CellType.Empty);
                }
            }
        }

        public void Put(Vector2Int pos, CellType type)
        {
            m_tilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), m_tiles[(int)type]);
        }

        public void Show() => m_tilemapRenderer.enabled = true;

        public void Hide() => m_tilemapRenderer.enabled = false;

        public async UniTask<Vector2Int?> WaitForInput(CellType type)
        {
            await UniTask.WaitUntil(() => Input.GetMouseButtonDown(0));
            var wp = m_camera.ScreenToWorldPoint(Input.mousePosition);
            var cp = m_tilemap.WorldToCell(wp);
            if (cp.x >= 0 && cp.x < 3 && cp.y >= 0 && cp.y < 3)
            {
                return (Vector2Int)cp;
            }
            return null;
        }
    }
}