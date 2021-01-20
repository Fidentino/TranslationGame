using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class Main : MonoBehaviour
{
    public Tilemap grid;
    public Tile white;
    public Tile black;
    public Button button;
    public GameObject[] letters;
    int N, cellsize, width, levelsPlayed, points, lives;
    NeuralNetwork net = new NeuralNetwork();

    string textFile = "words.csv";

    string word, translation;
    string[] lines;

    void Fill(Tile tile, int X1, int Y1, int X2, int Y2)
    {
        grid.BoxFill(new Vector3Int(X1, Y1, 0), tile, X1, Y1, X2, Y2);
    }

    double[,] CellMap(int leftBorder)
    {
        bool[,] matrix = new bool[cellsize + 1, cellsize + 1];
        int l = -1, r = -1, t = -1, b = -1;
        for (int i = 0; i < cellsize + 1; i++)
            for (int j = 0; j < cellsize + 1; j++)
                if (grid.GetTile(new Vector3Int(leftBorder + j, -cellsize / 2 + i, 0)) == black)
                {
                    matrix[i, j] = true;
                    if (l == -1 || l > j) l = j;
                    if (t == -1 || t > i) t = i;
                    if (r < j) r = j;
                    if (b < i) b = i;
                }
                else
                    matrix[i, j] = false;
        double[,] map = new double[16, 16];
        for (int i = 0; i < 16; i++)
            for (int j = 0; j < 16; j++)
                map[i, j] = 0;
        double dw = (double)(r - l + 1) / 16;
        double dh = (double)(b - t + 1) / 16;
        for (int i = 0; i < b - t + 1; i++)
            for (int j = 0; j < r - l + 1; j++)
                if (grid.GetTile(new Vector3Int(leftBorder + l + j, -cellsize / 2 + t + i, 0)) == black)
                {
                    int c = (int)Math.Floor(i / dh);
                    int d = (int)Math.Floor(j / dw);
                    map[c, d] += (Math.Min(i + 1, (c + 1) * dh) - i) * (Math.Min(j + 1, (d + 1) * dw) - j);
                    if (i + 1 > (c + 1) * dh)
                        map[c + 1, d] += ((i + 1) - (c + 1) * dh) * (Math.Min(j + 1, (d + 1) * dw) - j);
                    if (j + 1 > (d + 1) * dw)
                        map[c, d + 1] += (Math.Min(i + 1, (c + 1) * dh) - i) * ((j + 1) - (d + 1) * dw);
                    if (i + 1 > (c + 1) * dh && j + 1 > (d + 1) * dw)
                        map[c + 1, d + 1] += ((i + 1) - (c + 1) * dh) * ((j + 1) - (d + 1) * dw);
                }
        double max = 0;
        for (int i = 0; i < 16; i++)
            for (int j = 0; j < 16; j++)
                if (map[i, j] > max)
                    max = map[i, j];
        max /= 2;
        if (max != 0)
            for (int i = 0; i < 16; i++)
                for (int j = 0; j < 16; j++)
                    map[i, j] /= max;

        return map;
    }

    Vector3Int ScreenToCell(Vector3 position)
    {
        return grid.WorldToCell(Camera.main.ScreenToWorldPoint(position));
    }

    void Start()
    {
        lines = File.ReadAllLines(textFile);
        button.onClick.AddListener(delegate {
            ButtonClicked(button);
        });
        levelsPlayed = 0;
        points = 100;
        lives = 3;
        GameObject.Find("PointsNumber").GetComponent<Text>().text = points.ToString();
        GameObject.Find("LivesNumber").GetComponent<Text>().text = lives.ToString();
        NewLevel();
    }

    void NewLevel()
    {
        System.Random rnd = new System.Random();
        int line;
        do
        {
            line = rnd.Next(lines.Length);
        } while (lines[line].Length == 0); 
        string[] pair = lines[line].Split(';');
        word = pair[0];
        translation = pair[1];
        GameObject.Find("Word").GetComponent<Text>().text = word;
        lines[line] = "";
        levelsPlayed++;
        GameObject.Find("LevelNumber").GetComponent<Text>().text = levelsPlayed.ToString();
        grid.ClearAllTiles();
        N = translation.Length;
        width = ScreenToCell(new Vector3(Screen.width, Screen.height, 0)).x * 2 - 40;
        cellsize = (width - 20 * (N - 1)) / N;
        grid.size = new Vector3Int(width, cellsize, 0);
        Font arial;
        arial = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
        letters = new GameObject[N];
        for (int i = 0; i < N; i++)
        {
            Fill(white, -width / 2 + (20 + cellsize) * i, -cellsize / 2, -width / 2 + (20 + cellsize) * i + cellsize, cellsize / 2);
            letters[i] = new GameObject("letter" + i);
            letters[i].transform.parent = GameObject.Find("Canvas").transform;
            letters[i].AddComponent<RectTransform>();
            Vector3 letterCoords = grid.CellToWorld(new Vector3Int((-width / 2 + (20 + cellsize) * i + cellsize / 2) * 3, -cellsize * 2, 0));
            RectTransform RT = letters[i].GetComponent<RectTransform>();
            RT.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            RT.anchoredPosition = letterCoords;
            RT.sizeDelta = new Vector2(30, 30);
            letters[i].AddComponent<Text>();
            Text T = letters[i].GetComponent<Text>();
            T.font = arial;
            T.fontSize = 20;
            T.fontStyle = FontStyle.Bold;
            T.color = Color.white;
            T.alignment = TextAnchor.MiddleCenter;

        }
            
    }

    // Update is called once per frame
    void Update()
    {
        int x = ScreenToCell(Input.mousePosition).x;
        int y = ScreenToCell(Input.mousePosition).y;
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
            double[,] map = CellMap(-width / 2 + (20 + cellsize) * i);
            double[] results = net.Calculate(map);
            int max = 0;
            for (int j = 1; j < 26; j++)
                if (results[j] > results[max]) max = j;
            if (results[max] != 0)
                GameObject.Find("letter" + i).GetComponent<Text>().text = ('A' + max).ToString();
            else
                GameObject.Find("letter" + i).GetComponent<Text>().text = "";
        }
    }

    void ButtonClicked(Button button)
    {
        string result = "";
        for (int i = 0; i < N; i++)
        { 
            result += GameObject.Find("letter" + i).GetComponent<Text>().text;
            int left = -width / 2 + (20 + cellsize) * i;
            int right = left + cellsize;
            for (int j = left; j <= right; j++)
                for (int k = -cellsize / 2; k <= cellsize / 2; k++)
                    grid.SetTile(new Vector3Int(j, k, 0), white);
        }
        if (String.Compare(result.ToUpper(), translation.ToUpper()) == 0)
        {
            lives++;
            points += 100;
            GameObject.Find("PointsNumber").GetComponent<Text>().text = points.ToString();
            GameObject.Find("LivesNumber").GetComponent<Text>().text = lives.ToString();
            for (int i = 0; i < N; i++)
                UnityEngine.Object.Destroy(GameObject.Find("letter" + i));
            if (levelsPlayed < lines.Length)
                NewLevel();
            else
                GameOver(true);
        }
        else
        {
            lives--;
            points -= 50;
            GameObject.Find("PointsNumber").GetComponent<Text>().text = points.ToString();
            GameObject.Find("LivesNumber").GetComponent<Text>().text = lives.ToString();
            if (lives == 0)
                GameOver(false);
        }
    }

    void GameOver(bool value)
    {
        grid.ClearAllTiles();
            for (int i = 0; i < N; i++)
                UnityEngine.Object.Destroy(GameObject.Find("letter" + i));
        GameObject.Find("LivesLabel").GetComponent<Text>().text = "";
        GameObject.Find("LivesNumber").GetComponent<Text>().text = "";
        GameObject.Find("LevelLabel").GetComponent<Text>().text = "";
        GameObject.Find("LevelNumber").GetComponent<Text>().text = "";
        UnityEngine.Object.Destroy(GameObject.Find("Button"));
        GameObject.Find("Word").GetComponent<Text>().text = (value ? "VICTORY" : "GAME OVER");
    }
}
