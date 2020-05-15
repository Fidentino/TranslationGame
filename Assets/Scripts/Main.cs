using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class Main : MonoBehaviour
{
    public Tilemap grid;
    public Tile white;
    public Tile black;
    public TileBase empty;
    int N, cellsize, width;

    void fill(Tile tile, int X1, int Y1, int X2, int Y2)
    {
        grid.BoxFill(new Vector3Int(X1, Y1, 0), tile, X1, Y1, X2, Y2);
    }

    Vector3Int mouseToGrid()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return grid.WorldToCell(mouseWorldPos);
    }

    void Start()
    {
        Application.targetFrameRate = 300;
        N = 8;
        width = grid.WorldToCell(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0))).x * 2 - 40;
        cellsize = (width - 20 * (N - 1)) / N;
        grid.size = new Vector3Int(width, cellsize, 0);
        for (int i = 0; i < N; i++)
        {
            fill(white, -width / 2 + (20 + cellsize) * i, -cellsize / 2, -width / 2 + (20 + cellsize) * i + cellsize, cellsize / 2);
        }
    }

    // Update is called once per frame
    void Update()
    {
        int x = mouseToGrid().x;
        int y = mouseToGrid().y;
        if (Input.GetMouseButton(0))
        {
            for (int i = -5; i <= 5; i++)
                for (int j = -5; j <= 5; j++)
                    if ((i * i + j * j < 25) && grid.GetTile(new Vector3Int(x + i, y + j, 0)) == white)
                        grid.SetTile(new Vector3Int(x + i, y + j, 0), black);
        }
        else if (Input.GetMouseButton(1))
        {
            for (int i = 0; i < N; i++)
            {
                int left = -width / 2 + (20 + cellsize) * i;
                int right = left + cellsize;
                if (left <= x && x <= right && -cellsize / 2 <= y && y <= cellsize / 2)
                {
                    for (int j = left; j <= right; j++)
                        for (int k = -cellsize / 2; k <= cellsize / 2; k++)
                            grid.SetTile(new Vector3Int(j, k, 0), white);
                }
            }
        }
        for (int i = 0; i < N; i++)
        {
            int left = -width / 2 + (20 + cellsize) * i;
            bool[,] matrix = new bool[cellsize + 1, cellsize + 1];
            for (int j = 0; j < cellsize + 1; j++)
            {
                for (int k = 0; k < cellsize + 1; k++)
                {
                    matrix[j, k] = (grid.GetTile(new Vector3Int(left + k, -cellsize / 2 + j, 0)) == black);
                }
            }
        }
    }
}
