using System;
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
    bool[,,] matrix;

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
        matrix = new bool[N, cellsize + 1, cellsize + 1];
        grid.size = new Vector3Int(width, cellsize, 0);
        for (int i = 0; i < N; i++)
        {
            fill(white, -width / 2 + (20 + cellsize) * i, -cellsize / 2, -width / 2 + (20 + cellsize) * i + cellsize, cellsize / 2);
            for (int j = 0; j <= cellsize; j++)
                for (int k = 0; k <= cellsize; k++)
                    matrix[i, j, k] = false;
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
            int border = -width / 2 + (20 + cellsize) * i;
            int l = -1, r = -1, t = -1, b = -1;
            for (int j = 0; j < cellsize + 1; j++)
                for (int k = 0; k < cellsize + 1; k++)
                    if (grid.GetTile(new Vector3Int(border + k, -cellsize / 2 + j, 0)) == black)
                    {
                        matrix[i, j, k] = true;
                        if (l == -1 || l < k) l = k;
                        if (t == -1 || t < j) t = j;
                        if (r < k) r = k;
                        if (b < j) b = j;
                    }
                    else
                        matrix[i, j, k] = false;
            double[,] trim = new double[16, 16];
            for (int j = 0; j < 16; j++)
                for (int k = 0; k < 16; k++)
                    trim[j, k] = 0;
            double dw = (double) (r - l + 1) / 16;
            double dh = (double) (b - t + 1) / 16;
            for (int j = 0; j < b - t + 1; j++)
                for (int k = 0; k < r - l + 1; k++)
                    if (grid.GetTile(new Vector3Int(border + l + k, -cellsize / 2 + t + j, 0)) == black)
                    {
                        int c = (int) Math.Floor(j / dh);
                        int d = (int) Math.Floor(k / dw);
                        trim[c, d] += (Math.Min(j + 1, (c + 1) * dh) - j) * (Math.Min(k + 1, (d + 1) * dw) - k);
                        if (j + 1 > (c + 1) * dh)
                            trim[c + 1, d] += ((j + 1) - (c + 1) * dh) * (Math.Min(k + 1, (d + 1) * dw) - k);
                        if (k + 1 > (d + 1) * dw)
                            trim[c, d + 1] += (Math.Min(j + 1, (c + 1) * dh) - j) * ((k + 1) - (d + 1) * dw);
                        if (j + 1 > (c + 1) * dh && k + 1 > (d + 1) * dw)
                            trim[c + 1, d + 1] += ((j + 1) - (c + 1) * dh) * ((k + 1) - (d + 1) * dw);
                    }
            double max = 0;
            for (int j = 0; j < 16; j++)
                for (int k = 0; k < 16; k++)
                    if (trim[j, k] > max)
                        max = trim[j, k];
            max /= 2;
            bool[,] map = new bool[16, 16];
            for (int j = 0; j < 16; j++)
                for (int k = 0; k < 16; k++)
                {
                    map[j, k] = (trim[j, k] >= max && max > 0);
                    if (map[j, k])
                        grid.SetTile(new Vector3Int(1000 + 20 * i + k, 1000 + j, 0), black);
                    else
                        grid.SetTile(new Vector3Int(1000 + 20 * i + k, 1000 + j, 0), white);

                }
        }
    }
}
