using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Funcs
{
    static public double MSE(double[] actual, double[] ideal)
    {
        double val = 0;
        for (int i = 0; i < actual.Length; i++)
            val += (actual[i] - ideal[i]) * (actual[i] - ideal[i]);
        return 0.5 * val / actual.Length;
    }

    static public double sigmoid(double x)
    {
        return 1.0 / (1.0 + Math.Exp(-x));
    }

    static public double deriv(double f)
    {
        return f * (1.0 - f);
    }
}

public class NeuronLayer
{
    public double[] inVal;
    public double[] outVal;

    public NeuronLayer(int size)
    {
        inVal = new double[size];
        outVal = new double[size];
    }
}

public class NeuralNetwork
{
    public NeuronLayer hidden;
    public NeuronLayer output;
    public double[,] inHidden;
    public double[,] hiddenOut;

    public double learningSpeed;

    static string textFile = "weights.csv";

    public NeuralNetwork()
    {
        hidden = new NeuronLayer(20);
        output = new NeuronLayer(26);
        inHidden = new double[256, 20];
        hiddenOut = new double[20, 26];

        string[] lines = File.ReadAllLines(textFile);
        for (int i = 0; i < 256; i++)
        {
            string[] numbers = lines[i].Split(';');
            for (int j = 0; j < 20; j++)
                inHidden[i, j] = Convert.ToDouble(numbers[j]);
        }
        for (int i = 0; i < 20; i++)
        {
            string[] numbers = lines[256 + i].Split(';');
            for (int j = 0; j < 26; j++)
                hiddenOut[i, j] = Convert.ToDouble(numbers[j]);
        }

        UnityEngine.Debug.Log(inHidden[25, 15]);
        learningSpeed = 0.5;
    }

    public double[] Calculate(double[,] input)
    {
        for (int i = 0; i < 20; i++)
        {
            hidden.inVal[i] = 0;
            for (int j = 0; j < 16; j++)
                for (int k = 0; k < 16; k++)
                    hidden.inVal[i] += inHidden[j * 16 + k, i] * input[j, k];
            hidden.outVal[i] = Funcs.sigmoid(hidden.inVal[i]);
        }

        for (int i = 0; i < 26; i++)
        {
            output.inVal[i] = 0;
            for (int j = 0; j < 20; j++)
                output.inVal[i] += hiddenOut[j, i] * hidden.outVal[j];
            output.inVal[i] = Funcs.sigmoid(output.outVal[i]);
        }

        //UnityEngine.Debug.Log(String.Join(";", output.outVal));
        return output.outVal;
    }
    public void BackPropagate(double[,] input, double[] ideal)
    {
        double error;

        error = Funcs.MSE(Calculate(input), ideal);

        while (error > 0.01)
        {
            UnityEngine.Debug.Log(error);
            double[] dOut = new double[26];
            double[] dHidden = new double[20];

            for (int i = 0; i < 26; i++)
                dOut[i] = (ideal[i] - output.outVal[i]) * Funcs.deriv(output.inVal[i]);

            for (int i = 0; i < 20; i++)
            {
                dHidden[i] = 0;
                for (int j = 0; j < 26; j++)
                    dHidden[i] += hiddenOut[i, j] * dOut[j];
                dHidden[i] *= Funcs.deriv(hidden.inVal[i]);
            }

            for (int i = 0; i < 20; i++)
                for (int j = 0; j < 26; j++)
                    hiddenOut[i, j] += learningSpeed * dOut[j] * hidden.outVal[i];

            for (int i = 0; i < 16; i++)
                for (int j = 0; j < 16; j++)
                    for (int k = 0; k < 20; k++)
                        inHidden[i * 16 + j, k] += learningSpeed * dHidden[k] * input[i, j];

            error = Funcs.MSE(Calculate(input), ideal);
        }

        File.Delete(textFile);
        string[] lines = new string[276];
        for (int i = 0; i < 256; i++)
        {
            string[] numbers = new string[20];
            for (int j = 0; j < 20; j++)
                numbers[j] = inHidden[i, j].ToString();
            lines[i] = String.Join(";", numbers);
        }
        for (int i = 0; i < 20; i++)
        {
            string[] numbers = new string[20];
            for (int j = 0; j < 20; j++)
                numbers[j] = hiddenOut[i, j].ToString();
            lines[256 + i] = String.Join(";", numbers);
        }
        File.WriteAllLines(textFile, lines);

    }

}

public class Training : MonoBehaviour
{
    public Tilemap grid;
    public Tile white;
    public Tile black;
    public TileBase empty;
    public InputField textField;
    public Button button;
    public Toggle toggle;
    public NeuralNetwork net;


    void Fill(Tile tile, int X1, int Y1, int X2, int Y2)
    {
        grid.BoxFill(new Vector3Int(X1, Y1, 0), tile, X1, Y1, X2, Y2);
    }

    double[,] CellMap()
    {
        bool[,] matrix = new bool[81, 81];
        int l = -1, r = -1, t = -1, b = -1;
        for (int i = 0; i < 81; i++)
            for (int j = 0; j < 81; j++)
                if (grid.GetTile(new Vector3Int(-240 + j, -40 + i, 0)) == black)
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
                if (grid.GetTile(new Vector3Int(-240 + l + j, -40 + t + i, 0)) == black)
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
        for (int i = 0; i < 16; i++)
            for (int j = 0; j < 16; j++)
            {
                if (max != 0)
                    map[i, j] /= max;
                if (map[i, j] >= 0.5)
                    grid.SetTile(new Vector3Int(-7 + j, -7 + i, 0), black);
                else
                    grid.SetTile(new Vector3Int(-7 + j, -7 + i, 0), white);
            }


        return map;
    }
    Vector3Int ScreenToCell(Vector3 position)
    {
        return grid.WorldToCell(Camera.main.ScreenToWorldPoint(position));
    }

    /*public PointerEventData toggleClick()
    {

    }*/

    // Start is called before the first frame update
    void Start()
    {
        net = new NeuralNetwork();
        toggle.onValueChanged.AddListener(delegate {
            ToggleValueChanged(toggle);
        });
        button.onClick.AddListener(delegate {
            ButtonClicked(button);
        });
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
            if (-240 <= x && x <= -160 && -40 <= y && y <= 40)
            {
                for (int i = -240; i <= -160; i++)
                    for (int j = -40; j <= 40; j++)
                        grid.SetTile(new Vector3Int(i, j, 0), white);
            }
        }
        if (!toggle.isOn)
        {
            double[] results = net.Calculate(CellMap());
            int max = 0;
            for (int i = 1; i < 26; i++)
                if (results[i] > results[max]) max = i;
            if (results[max] != 0)
                textField.textComponent.text = ('A' + max).ToString();
            else
                textField.textComponent.text = "";
        }
    }

    void ToggleValueChanged(Toggle toggle)
    {
        textField.readOnly = !textField.readOnly;
        button.interactable = !button.interactable;
        textField.textComponent.text = "";
        UnityEngine.Debug.Log("toggle");
    }

    void ButtonClicked(Button button)
    {
        UnityEngine.Debug.Log("button");
        int ch = textField.text.ToUpper()[0] - 'A';
        double[] trainingResults = new double[26];
        for (int i = 0; i < 26; i++)
            trainingResults[i] = (i == ch ? 0.99 : 0.01);
        net.BackPropagate(CellMap(), trainingResults);
        for (int i = -240; i <= -160; i++)
            for (int j = -40; j <= 40; j++)
                grid.SetTile(new Vector3Int(i, j, 0), white);
    }
}
