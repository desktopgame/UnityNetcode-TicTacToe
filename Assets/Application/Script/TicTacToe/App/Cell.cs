using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TicTacToe.App
{
    public enum CellType
    {
        Empty = 0,
        Maru = 1,
        Batu = 2,
    }
    [CreateAssetMenu]
    public class Cell : Tile
    {
    }
}