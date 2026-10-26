using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;

public class sevenChooseFour : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMSelectable[] Buttons;
    public Renderer[] Leds;
    public Renderer[] ProgressLeds;

    public Material[] LedColors; //B C G M R Y W
    public Material BlackLed;
    public Material OrangeLed;  // for progress LEDs only
    public Material SolvedButton;

    private readonly string[] ledColorNames = { "B", "C", "G", "M", "R", "Y", "W" };
    private readonly string[] manColorNames = { "R", "G", "B", "M", "C", "Y", "W" };
    private readonly List<int> ledindex = new List<int>();

    private int morsekeyindex;
    private char morsekeyletter;
    private bool[] morse;

    private int tapkeyindex;
    private char tapkey;
    private bool[] tapcode;

    private int numkey;
    private bool[] flashcode;

    private static readonly List<int> ledfunction = new List<int>() { 0, 1, 2, 3 }; //0: morse 1: tap 2: colors 3: number
    private List<int> ledfunctionshuffled = new List<int>();

    private bool lightsready = false;

    private readonly string[] _morseSymbols = {
        ".-", "-...", "-.-.", "-..",  ".",   "..-.", "--.", "....", "..",   ".---", "-.-",  ".-..", "--",
        "-.", "---",  ".--.", "--.-", ".-.", "...",  "-",   "..-",  "...-", ".--",  "-..-", "-.--", "--.."
    };

    private const string _alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private readonly string[] gates = { "NOR", "XOR", "OR", "AND" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleReady = false;

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (var button in Buttons)
        {
            var pressedButton = button;
            button.OnInteract += delegate () { ButtonPress(pressedButton); return false; };
        }
    }

    private List<string> solution;

    // Use this for initialization
    private void Start()
    {
        solution = new List<string>();
        PickLedColor();
        GenerateKeys();
        solution = FillSolution();
        moduleReady = true;
        var cursol = solution[0];
        if (solution[0] == "W")
        {
            cursol = "";
            foreach (var s in whitesolution)
            {
                cursol += s;
                cursol += " ";
            }
        }
        Debug.LogFormat("[Seven Choose Four #{0}] Stage {1} solution is {2}", moduleId, stage + 1, cursol);
    }

    private void PickLedColor()
    {
        while (ledindex.Count < 4)
        {
            var ran = UnityEngine.Random.Range(0, 7);
            if (!ledindex.Contains(ran)) ledindex.Add(ran);
        }
        ledindex.Add(UnityEngine.Random.Range(0, 7));
        for (var i = 0; i < 4; i++)
        {
            Leds[i].sharedMaterial = LedColors[ledindex[i]];
        }
    }

    private readonly string[] keys = new string[4];

    private void GenerateKeys()
    {
        morsekeyindex = UnityEngine.Random.Range(0, 26);
        morsekeyletter = _alphabet[morsekeyindex];
        morse = MorseToBoolArray(_morseSymbols[morsekeyindex]);

        tapkeyindex = UnityEngine.Random.Range(0, 26);
        if (tapkeyindex == 10) tapkeyindex = 2;
        tapkey = _alphabet[tapkeyindex];
        tapcode = TapToBoolArray(tapkeyindex >= 11 ? tapkeyindex - 1 : tapkeyindex);

        numkey = UnityEngine.Random.Range(1, 8);
        flashcode = NumToFlashArray(numkey);

        ledfunctionshuffled = ledfunction.OrderBy(x => UnityEngine.Random.value).ToList();
        lightsready = true;

        Debug.LogFormat("[Seven Choose Four #{0}] Morse key is {1} ({2})", moduleId, morsekeyletter, _morseSymbols[morsekeyindex]);
        Debug.LogFormat("[Seven Choose Four #{0}] Tap code key is {1}", moduleId, tapkey);
        var ledcolorstring = "";
        for (var i = 0; i < ledindex.Count; i++)
            ledcolorstring += ledColorNames[ledindex[i]];

        Debug.LogFormat("[Seven Choose Four #{0}] Color sequence is {1}", moduleId, ledcolorstring);
        Debug.LogFormat("[Seven Choose Four #{0}] Number key is {1}={2}", moduleId, numkey, manColorNames[numkey - 1]);
        var buttoninfolist = DefineButtons();
        var keysoffset = ledfunctionshuffled.IndexOf(2);
        var keyorder = "";
        for (var i = 0; i < 4; i++)
        {
            keys[i] = buttoninfolist[(i + keysoffset) % 4].buttonoutputkey;
            keyorder += keys[i];
        }
        Debug.LogFormat("[Seven Choose Four #{0}] The correct key order is: {1}", moduleId, keyorder);
    }

    private class Buttoninfo
    {
        public int buttonpos { get; set; }
        public string buttonoutputkey { get; set; }
    }

    private List<Buttoninfo> DefineButtons()
    {
        var l = new List<Buttoninfo>();
        for (var i = 0; i < 4; i++)
        {
            l.Add(new Buttoninfo
            {
                buttonpos = i,
                buttonoutputkey = GetOutputKey(ledfunctionshuffled[i])
            });
        }
        return l;
    }

    private string GetOutputKey(int oper)
    {
        switch (oper)
        {
            case 0:
                return morsekeyletter.ToString();
            case 1:
                return tapkey.ToString();
            case 2:
                return ledColorNames[ledindex[4]].ToString();
            case 3:
                return manColorNames[numkey - 1];
            default:
                return "NULL";
        }
    }

    private List<string> whitesolution;

    private List<string> FillSolution()
    {
        var solution = new List<string>();
        for (var i = 0; i < 4; i++)
        {
            switch (ledColorNames[ledindex.ToArray()[i]])
            {
                case "R":
                    solution.Add(SolveRed(keys[i], i));
                    break;
                case "G":
                    solution.Add(SolveGreen(keys[i], i));
                    break;
                case "B":
                    solution.Add(SolveBlue(keys[i]));
                    break;
                case "M":
                    solution.Add(SolveMagenta(keys[i]));
                    break;
                case "C":
                    solution.Add(SolveCyan(keys[i], i));
                    break;
                case "Y":
                    solution.Add(SolveYellow(keys[i], i));
                    break;
                case "W":
                    whitesolution = FillWhiteSolution(keys[i], i);
                    var whitesols = "";
                    var colname = 0;
                    foreach (var s in whitesolution)
                    {
                        whitesols += manColorNames[colname];
                        whitesols += ": ";
                        whitesols += s;
                        whitesols += " ";
                        colname++;
                    }
                    Debug.LogFormat("[Seven Choose Four #{0}] White solutions are: {1}", moduleId, whitesols);
                    solution.Add("W");
                    break;
            }
        }
        return solution;
    }

    private List<string> FillWhiteSolution(string key, int seqnum)
    {
        Debug.LogFormat("[Seven Choose Four #{0}] White puzzle: key is {1}", moduleId, key);
        return new List<string>
        {
            SolveRed(key, seqnum),
            SolveGreen(key, seqnum),
            SolveBlue(key),
            SolveMagenta(key),
            SolveCyan(key, seqnum),
            SolveYellow(key, seqnum)
        };
    }

    private int stage = 0;
    private readonly List<char> buttonspressed = new List<char>();

    private void ButtonPress(KMSelectable button)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        button.AddInteractionPunch();

        var buttonname = button.name;

        if (moduleReady)
        {
            buttonspressed.Add(getButtonValue(buttonname));
            if (buttonspressed.Count == 4)
            {
                var acceptableSolutions = solution[stage] == "W" ? whitesolution : new List<string> { solution[stage] };
                if (acceptableSolutions.Contains(buttonspressed.Join("")))
                    UpdateStage();
                else
                {
                    Module.HandleStrike();
                    Buttons[stage].GetComponent<MeshRenderer>().sharedMaterial = LedColors[4];
                    Debug.LogFormat("[Seven Choose Four #{0}] Incorrect, you entered {1}. Stage reset.", moduleId, buttonspressed.Join(""));
                }

                buttonspressed.Clear();
            }

            // Set progress LEDs
            for (var i = 0; i < 3; i++)
                ProgressLeds[i].sharedMaterial = buttonspressed.Count > i ? OrangeLed : BlackLed;
        }
    }

    private void UpdateStage()
    {
        Buttons[stage].GetComponent<MeshRenderer>().sharedMaterial = SolvedButton;
        stage++;
        if (stage == 4)
        {
            lightsready = false;
            moduleReady = false;
            foreach (var led in Leds)
                led.sharedMaterial = LedColors[2];  // green
            Debug.LogFormat("[Seven Choose Four #{0}] Module solved.", moduleId);
            Module.HandlePass();
        }
        else
        {
            Debug.LogFormat("[Seven Choose Four #{0}] Stage passed.", moduleId);
            Debug.LogFormat("[Seven Choose Four #{0}] Next stage solution is: {1}", moduleId, solution[stage] == "W" ? whitesolution.Join(" or ") : solution[stage]);
        }
    }

    private char getButtonValue(string name)
    {
        switch (name)
        {
            case "Button1":
                return (char) ((4 - stage) % 4 + 49);
            case "Button2":
                return (char) ((4 - stage + 1) % 4 + 49);
            case "Button3":
                return (char) ((4 - stage + 2) % 4 + 49);
            case "Button4":
                return (char) ((4 - stage + 3) % 4 + 49);
            default:
                return '5';
        }
    }

    private const float DOT_LENGTH = 0.2f;
    private float timer_morse = DOT_LENGTH;
    private int morsetimer = 0;

    private float timer_tap = DOT_LENGTH;
    private int taptimer = 0;

    private float timer_color = DOT_LENGTH * 3;
    private int colortimer = 0;

    private float timer_num = DOT_LENGTH;
    private int numtimer = 0;

    // Update is called once per frame
    private void Update()
    {
        if (lightsready)
        {
            timer_morse -= Time.deltaTime;
            if (timer_morse < 0)
            {
                var morseled = ledfunctionshuffled.IndexOf(0);
                Leds[morseled].sharedMaterial = morse[morsetimer] ? LedColors[ledindex[morseled]] : BlackLed;
                morsetimer = (morsetimer + 1) % morse.Length;
                timer_morse = DOT_LENGTH;
            }

            timer_tap -= Time.deltaTime;
            if (timer_tap < 0)
            {
                var tapled = ledfunctionshuffled.IndexOf(1);
                Leds[tapled].sharedMaterial = tapcode[taptimer] ? LedColors[ledindex[tapled]] : BlackLed;
                taptimer = (taptimer + 1) % tapcode.Length;
                timer_tap = DOT_LENGTH;
            }

            timer_color -= Time.deltaTime;
            if (timer_color < 0)
            {
                var colorled = ledfunctionshuffled.IndexOf(2);

                Leds[colorled].sharedMaterial = colortimer < ledindex.Count ? LedColors[ledindex[colortimer]] : BlackLed;
                colortimer = (colortimer + 1) % (ledindex.Count + 1);

                timer_color = DOT_LENGTH * 3;
            }

            timer_num -= Time.deltaTime;
            if (timer_num < 0)
            {
                var numled = ledfunctionshuffled.IndexOf(3);
                Leds[numled].sharedMaterial = flashcode[numtimer] ? LedColors[ledindex[numled]] : BlackLed;
                numtimer = (numtimer + 1) % flashcode.Length;
                timer_num = DOT_LENGTH;
            }
        }
    }

    private bool[] MorseToBoolArray(string morse)
    {
        var length = 0;
        foreach (var c in morse)
        {
            if (c == '.')
            {
                length += 2;
            }
            else
            {
                length += 4;
            }
        }
        var result = new bool[length + 4];
        var pointer = 0;
        foreach (var c in morse)
        {
            if (c == '.')
            {
                result[pointer] = true;
                pointer += 2;
            }
            else
            {
                result[pointer] = true;
                result[pointer + 1] = true;
                result[pointer + 2] = true;
                pointer += 4;
            }
        }
        return result;
    }

    private readonly int tapsmallgap = 3;
    private readonly int taplargegap = 10;

    private bool[] TapToBoolArray(int index)
    {
        var row = index / 5 + 1;
        var col = index % 5 + 1;

        var length = row * 2 + col * 2 + tapsmallgap + taplargegap;
        var rowmax = row * 2;
        var colmax = rowmax + col * 2 + tapsmallgap;
        var tapcodeflashes = new bool[length];
        for (var i = 0; i < length; i++)
        {
            if (i < rowmax || (i >= rowmax + tapsmallgap && i < colmax)) tapcodeflashes[i] = i % 2 == 0;
            else if (i >= rowmax && i < rowmax + tapsmallgap) tapcodeflashes[i] = false;
        }

        return tapcodeflashes;
    }

    private bool[] NumToFlashArray(int index)
    {
        var onframes = 7;
        var offframes = 10;
        var length = index * onframes + index + offframes;
        var numflashes = new bool[length];
        for (var i = 0; i < length; i++)
            numflashes[i] = i <= length - offframes && (i % (onframes + 1) != 0);
        return numflashes;
    }

    private string Reverse(string s)
    {
        var sr = "";
        for (var i = s.Length - 1; i >= 0; i--)
        {
            sr += s.ToCharArray()[i];
        }
        return sr;
    }

    private readonly List<string> redtrue = new List<string>() { "1423", "2314", "4123", "2341" };
    private readonly List<string> redfalse = new List<string>() { "2313", "4231", "2233", "1231" };

    private string SolveRed(string keyletter, int seqnum)
    {
        var key = _alphabet.IndexOf(keyletter) + 1;
        var num_rports = Bomb.GetPortCount("StereoRCA") + Bomb.GetPortCount("RJ45") + Bomb.GetPortCount("Parallel") + Bomb.GetPortCount("Serial");
        Debug.LogFormat("[Seven Choose Four #{0}] Red Puzzle: Key is {1}", moduleId, keyletter);
        key += num_rports;
        key %= 100;
        var tensdigit = key / 10;
        var onesdigit = key % 10;
        var tensdigitbool = tensdigit % 2 != 0;
        var onesdigitbool = onesdigit % 2 != 0;
        var solution = false;
        Debug.LogFormat("[Seven Choose Four #{0}] Red Puzzle: Gate used is {1}", moduleId, gates[seqnum]);
        switch (seqnum)
        {
            case 0: //NOR
                solution = !(tensdigitbool | onesdigitbool);
                break;
            case 1: //XOR
                solution = tensdigitbool ^ onesdigitbool;
                break;
            case 2: //OR
                solution = tensdigitbool | onesdigitbool;
                break;
            case 3: //AND
                solution = tensdigitbool & onesdigitbool;
                break;
        }
        return logSolution("Red", solution ? redtrue[seqnum] : redfalse[seqnum]);
    }

    private readonly int[,] linesgrid =
    {
        {5, 6, 3, 5 },
        {7, 6, 6, 6 },
        {5, 6, 4, 4 },
        {4, 7, 5, 7 }
    };

    private readonly List<string> greenodd = new List<string>() { "3241", "1122", "4332", "1231" };

    private string SolveGreen(string keyname, int seqnum)
    {
        var key = _alphabet.IndexOf(keyname) + 1;
        var num_g7 = Bomb.GetSerialNumber().Count(x => x == 'G' | x == '7');
        Debug.LogFormat("[Seven Choose Four #{0}] Green Puzzle: Key is {1}", moduleId, keyname);
        key = (key) % 100;
        var tensdigit = key / 10;
        var onesdigit = key % 10;
        var a = (tensdigit + num_g7) % 4;
        var b = (onesdigit + num_g7) % 4;
        var sum = linesgrid[a, a] + linesgrid[a, b] + linesgrid[b, a] + linesgrid[b, b];
        Debug.LogFormat("[Seven Choose Four #{0}] Green Puzzle: Sum of the lines is {1}", moduleId, sum);
        if (sum % 2 == 0) return logSolution("Green", Reverse(greenodd[seqnum]));
        else return logSolution("Green", greenodd[seqnum]);
    }

    private readonly string[,] buttonordergrid = { {"1234", "1243", "2134", "2143", "3124" },
                                          {"3142", "1324", "1342", "1423", "1432" },
                                          {"4123", "4132", "2314", "2341", "3214" },
                                          {"3241", "2413", "2431", "4213", "4231" },
                                          {"3412", "3421", "4312", "4321", "1234" }};

    private string SolveBlue(string keyname)
    {
        Debug.LogFormat("[Seven Choose Four #{0}] Blue Puzzle: Key is {1}", moduleId, keyname);
        var key = _alphabet.IndexOf(keyname) + 1;
        var numaabat = Bomb.GetBatteryCount(Battery.AA);
        var numdbat = Bomb.GetBatteryCount(Battery.D);
        var col = 'F';
        var row = key;
        while (row > 5) row -= 5;
        row--;

        foreach (var c in Bomb.GetSerialNumberLetters())
        {
            switch (c)
            {
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                    col = c;
                    goto foundchar;
                default:
                    break;
            }
        }
        col = _alphabet.ToCharArray()[row];
        foundchar:
        Debug.LogFormat("[Seven Choose Four #{0}] Blue Puzzle: Starting Table Position is ({1},{2})", moduleId, col, row + 1);
        var colnum = _alphabet.IndexOf(col);
        var dir = FindMaxDirection(row, colnum, 5, 5, 3);
        switch (dir)
        {
            case 0:
            case 2:
                colnum = MoveDirection(colnum, numaabat, 4, dir == 0);
                break;
            case 1:
            case 3:
                row = MoveDirection(row, numaabat, 4, dir == 1);
                break;
        }
        dir = FindMaxDirection(row, colnum, 5, 5, 1);
        Debug.LogFormat("[Seven Choose Four #{0}] Blue Puzzle: Intermediate Table Position is ({1},{2})", moduleId, _alphabet[colnum], row + 1);
        switch (dir)
        {
            case 0:
            case 2:
                colnum = MoveDirection(colnum, numdbat, 4, dir == 0);
                break;
            case 1:
            case 3:
                row = MoveDirection(row, numdbat, 4, dir == 1);
                break;
        }
        Debug.LogFormat("[Seven Choose Four #{0}] Blue Puzzle: Final Table Position is ({1},{2})", moduleId, _alphabet[colnum], row + 1);

        return logSolution("Blue", buttonordergrid[row, colnum]);
    }

    private readonly int[,] lettergrid =
    {
        {12, 16, 18, 9, 19},
        {24, 15, 6, 2, 22 },
        {1, 25, 4, 3, 20 },
        {11, 7, 23, 17, 14 },
        {13, 5, 21, 8, 10 }
    };

    private string SolveMagenta(string key)
    {
        var keyvalue = _alphabet.IndexOf(key) + 1;
        if (keyvalue == 26) keyvalue = 1;
        Debug.LogFormat("[Seven Choose Four #{0}] Magenta Puzzle: Key is {1}", moduleId, key);

        var letters = "";
        var s = "";
        var serialNumber = Bomb.GetSerialNumber();

        for (var i = 0; i < 4; i++)
        {
            var c = serialNumber[i] == 'Z' ? 'A' : serialNumber[i];
            letters += c;
            s += c < 'A' ? (char) (c - '0' + 'A') : c;
        }
        Debug.LogFormat("[Seven Choose Four #{0}] Magenta Puzzle: Letters to find are {1}", moduleId, s);
        var keyrow = -1;
        var keycol = -1;
        for (var row = 0; row < 5; row++)
        {
            for (var col = 0; col < 5; col++)
            {
                if (lettergrid[row, col] == keyvalue)
                {
                    keyrow = row;
                    keycol = col;
                    goto next;
                }
            }
        }
        next:
        var dist = new int[4];
        for (var i = 0; i < 4; i++)
        {
            var value = _alphabet.IndexOf(s[i]) + 1;
            for (var row = 0; row < 5; row++)
            {
                for (var col = 0; col < 5; col++)
                {
                    if (lettergrid[row, col] == value)
                    {
                        dist[i] = Math.Abs(row - keyrow) + Math.Abs(col - keycol);
                        if (dist[i] > 3) dist[i] = dist[i] % 4;
                        if (dist[i] == 0) dist[i] = 4;
                    }
                }
            }
        }
        return logSolution("Magenta", dist.Join(""));
    }

    private string SolveCyan(string keyname, int seqnum)
    {
        Debug.LogFormat("[Seven Choose Four #{0}] Cyan Puzzle: Key is " + keyname, moduleId);
        var key = _alphabet.IndexOf(keyname);
        key %= 7;
        var numprim = 0;
        var numsec = 0;
        if (key < 3 | key == 6) numprim++;
        if (key >= 3) numsec++;
        var colors = manColorNames[key];
        for (var i = 0; i < 4; i++)
        {
            if (i != seqnum)
            {
                colors += ledColorNames[ledindex[i]];
                if (ledindex[i] % 2 == 0) numprim++;
                if (ledindex[i] % 2 != 0 | ledindex[i] == 6) numsec++;
            }
        }

        Debug.LogFormat("[Seven Choose Four #{0}] Cyan Puzzle: Colors are " + colors, moduleId);
        Debug.LogFormat("[Seven Choose Four #{0}] Cyan Puzzle: There are " + numprim + "P and " + numsec + "S", moduleId);
        string solleft;
        switch (numprim)
        {
            case 0:
                solleft = "12";
                break;
            case 1:
                solleft = "13";
                break;
            case 2:
                solleft = "13";
                break;
            case 3:
                solleft = "42";
                break;
            case 4:
                solleft = "43";
                break;
            default:
                return "NULL";
        }
        string solright;
        switch (numsec)
        {
            case 0:
                solright = "34";
                break;
            case 1:
                solright = "23";
                break;
            case 2:
                solright = "31";
                break;
            case 3:
                solright = "24";
                break;
            case 4:
                solright = "14";
                break;
            default:
                return "NULL";
        }
        return logSolution("Cyan", solleft + solright);
    }

    private string SolveYellow(string key, int seqnum)
    {
        var pos1 = _alphabet.IndexOf(key);
        Debug.LogFormat("[Seven Choose Four #{0}] Yellow Puzzle: Key is {1}", moduleId, key);
        var distances = new List<int>() { 24, 4, 11, 14, 22 };
        var letters = new List<string>() { "Y", "E", "L", "L", "O", "W" };
        var index = -1;
        index = FindClosestLetter(pos1, distances);
        if (index != 11) distances.Remove(index);
        letters.Remove(_alphabet[index].ToString());
        pos1 = 25 - pos1;
        index = FindClosestLetter(pos1, distances);
        //letters.RemoveAt(index);
        letters.Remove(_alphabet[index].ToString());
        var newletters = new List<string>() { "A", "A", "A", "A" };
        var shift = (seqnum + 1) % 4;
        for (var i = 0; i < 4; i++)
        {
            newletters[shift] = letters[i];
            shift = (shift + 1) % 4;
        }
        Debug.LogFormat("[Seven Choose Four #{0}] Yellow Puzzle: Shifted Letters: {1}", moduleId, newletters.Join(""));
        var letterindexpairs = new List<Item>()
        {
            new Item {key = 0, value = _alphabet.IndexOf(newletters[0])},
            new Item {key = 1, value = _alphabet.IndexOf(newletters[1])},
            new Item {key = 2, value = _alphabet.IndexOf(newletters[2])},
            new Item {key = 3, value = _alphabet.IndexOf(newletters[3])},
        };
        var sorted = letterindexpairs.OrderBy(x => x.value).ToList();
        var digit = 1;
        foreach (var i in sorted)
        {
            i.value = digit;
            digit++;
        }
        sorted = sorted.OrderBy(x => x.key).ToList();
        var result = "";
        foreach (var i in sorted)
        {
            result += (i.value).ToString();
        }
        return logSolution("Yellow", result);
    }

    private class Item
    {
        public int key { get; set; }
        public int value { get; set; }
    }

    private int FindClosestLetter(int input, List<int> l)
    {
        var smalld = 100000;
        var index = -1;
        var sortdist = new List<Item>();
        for (var i = 0; i < l.Count; i++)
        {
            sortdist.Add(new Item { key = i, value = l[i] });
        }
        sortdist = sortdist.OrderBy(x => x.value).ToList();
        var step = 0;
        foreach (var i in sortdist)
        {
            var dist = Math.Abs(input - i.value);
            if (dist <= smalld)
            {
                smalld = dist;
                index = step;
            }
            step++;
        }
        return sortdist[index].value;
    }

    private int FindMaxDirection(int row, int col, int maxrows, int maxcols, int bias)
    {
        var posydist = row;
        var negydist = maxrows - row - 1;
        var posxdist = maxcols - col - 1;
        var negxdist = col;

        List<int> l;
        switch (bias)
        {
            case 0:
                l = new List<int>() { posxdist, negydist, negxdist, posydist };
                return l.IndexOf(l.Max());
            case 1:
                l = new List<int>() { negydist, negxdist, posydist, posxdist };
                return (l.IndexOf(l.Max()) + bias) % 4;
            case 2:
                l = new List<int>() { negxdist, posydist, posxdist, negydist };
                return (l.IndexOf(l.Max()) + bias) % 4;
            case 3:
                l = new List<int>() { posydist, posxdist, negydist, negxdist };
                return (l.IndexOf(l.Max()) + bias) % 4;
            default:
                return -1;
        }

    }

    private int FirstMaxIndex(List<int> l)
    {
        var max = -10000;
        var index = -1;
        for (var i = 0; i < l.Count(); i++)
        {

            if (l[i] > max)
            {
                max = l[i];
                index = i;
            }
        }
        return index;
    }

    private int MoveDirection(int x, int dist, int maxpos, bool pos)
    {
        return pos ? Math.Min(x + dist, maxpos) : Math.Max(x - dist, 0);
    }

    private string logSolution(string name, string sol)
    {
        Debug.LogFormat("[Seven Choose Four #{0}] {1} solution is: {2}", moduleId, name, sol);
        return sol;
    }

    //twitch plays
    private bool inputIsValid(string cmd)
    {
        string[] validstuff = { "1", "2", "3", "4" };
        if (validstuff.Contains(cmd.ToLower()))
        {
            return true;
        }
        return false;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <1/2/3/4> [Presses the specified button]. You can also string moves together i.e. press 1 3 2 4 or press 1,3,2,4.";
#pragma warning restore 414
    private IEnumerator ProcessTwitchCommand(string command)
    {
        var parameters = command.Split(' ', ',');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            for (var i = 1; i < parameters.Length; i++)
            {
                if (inputIsValid(parameters[i]))
                {
                    yield return null;
                    if (parameters[i].ToLower().Equals("1"))
                    {
                        Buttons[0].OnInteract();
                    }
                    else if (parameters[i].ToLower().Equals("2"))
                    {
                        Buttons[1].OnInteract();
                    }
                    else if (parameters[i].ToLower().Equals("3"))
                    {
                        Buttons[2].OnInteract();
                    }
                    else if (parameters[i].ToLower().Equals("4"))
                    {
                        Buttons[3].OnInteract();
                    }
                }
            }
            yield break;
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (stage < 4)
        {
            // What are the possible solutions of the current stage?
            var possibleSolutions = solution[stage] == "W" ? whitesolution : new List<string> { solution[stage] };

            // Is what we’ve entered so far a prefix of a valid solution?
            var buttonspressedStr = buttonspressed.Join("");
            var validPrefix = possibleSolutions.IndexOf(sol => sol.StartsWith(buttonspressedStr));

            // If not, the autosolver is impossible
            if (validPrefix == -1)
            {
                Debug.LogFormat("[Seven Choose Four #{0}] Autosolver: existing input is {1}, valid solutions are: {2}", moduleId, buttonspressedStr, possibleSolutions.Join(", "));
                Module.HandlePass();
                yield break;
            }

            // Press the next correct button
            var nextBtn = possibleSolutions[validPrefix][buttonspressed.Count] - '1';
            Buttons[(nextBtn + stage) % 4].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }
}
