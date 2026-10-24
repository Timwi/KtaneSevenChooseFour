using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;

public class sevenChooseFour : MonoBehaviour
{
    enum Buttonnames { Button1, Button2, Button3, Button4 };
    public KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;
    public KMSelectable[] buttons;

    public Material[] ledColors; //B C G M R W Y
    private string[] ledColorNames = { "B", "C", "G", "M", "R", "Y", "W" };
    private string[] manColorNames = { "R", "G", "B", "M", "C", "Y", "W" };
    public Material blackled; 
    public Renderer[] leds;
    private List<int> ledindex = new List<int>();

    private int morsekeyindex;
    private char morsekeyletter;
    private bool[] morse;

    private int tapkeyindex;
    private char tapkey;
    private bool[] tapcode;

    private int numkey;
    private bool[] flashcode;

    private List<int> ledfunction = new List<int>() {0,1,2,3}; //0: morse 1: tap 2: colors 3: number
    private List<int> ledfunctionshuffled = new List<int>();

    private bool lightsready = false;

    //private List<string> keySequences = new List<string>() { "1234", "1122", "1314", "3344" };

    private readonly string[] MORSE_SYMBOLS = {
        ".-", "-...", "-.-.", "-..",  ".",   "..-.", "--.", "....", "..",   ".---", "-.-",  ".-..", "--",
        "-.", "---",  ".--.", "--.-", ".-.", "...",  "-",   "..-",  "...-", ".--",  "-..-", "-.--", "--.."
    };

    private const string SYMBOLS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private string[] gates = { "NOR", "XOR", "OR", "AND" };

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    private bool moduleReady = false;

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
        {
            KMSelectable pressedButton = button;
            button.OnInteract += delegate () { ButtonPress(pressedButton); return false; };
        }
    }

    private List<string> solution;

    // Use this for initialization
    void Start()
    {
        solution = new List<string>();
        PickLedColor();
        GenerateKeys();
        solution = FillSolution();
        moduleReady = true;
        string cursol = solution[0];
        if(solution[0] == "W")
        {
            cursol = "";
            foreach (string s in whitesolution)
            {
                cursol += s;
                cursol += " ";
            }
        }
        Debug.LogFormat("[Seven Choose Four #{0}] Stage " + (stage + 1).ToString() + " solution is " + cursol, moduleId);
    }

    void PickLedColor()
    {
        while (ledindex.Count < 4)
        {
            int ran = UnityEngine.Random.Range(0, 7);
            if (!ledindex.Contains(ran)) ledindex.Add(ran);
        }
        ledindex.Add(UnityEngine.Random.Range(0, 7));
        for (int i = 0; i < 4; i++)
        {
            leds[i].material = ledColors[ledindex[i]];
        }
    }

    private string[] keys = new string[4];



    void GenerateKeys()
    {
        morsekeyindex = UnityEngine.Random.Range(0, 26);
        morsekeyletter = SYMBOLS[morsekeyindex];
        morse = MorseToBoolArray(MORSE_SYMBOLS[morsekeyindex]);

        tapkeyindex = UnityEngine.Random.Range(0, 26);
        if (tapkeyindex == 10) tapkeyindex = 2;
        tapkey = SYMBOLS[tapkeyindex];
        tapcode = TapToBoolArray(tapkeyindex >= 11 ? tapkeyindex - 1 : tapkeyindex);

        numkey = UnityEngine.Random.Range(1, 8);
        flashcode = NumToFlashArray(numkey);

        ledfunctionshuffled = ledfunction.OrderBy(x => UnityEngine.Random.value).ToList();
        //Debug.Log("order of led functions " + ListToString(ledfunctionshuffled));
        lightsready = true;

        Debug.LogFormat("[Seven Choose Four #{0}] Morse key is " + morsekeyletter + " " + MORSE_SYMBOLS[morsekeyindex],moduleId);
        Debug.LogFormat("[Seven Choose Four #{0}] Tap code key is " + tapkey,moduleId);
        string ledcolorstring = "";
        for(int i = 0; i < ledindex.Count; i++)
        {
            ledcolorstring += ledColorNames[ledindex.ToArray()[i]];
        }
        Debug.LogFormat("[Seven Choose Four #{0}] Color seq is " + ledcolorstring,moduleId);
        Debug.LogFormat("[Seven Choose Four #{0}] Number key is " + numkey + "=" + manColorNames[numkey-1],moduleId);
        List<Buttoninfo> buttoninfolist = DefineButtons();
        //int keysoffset = ledfunctionshuffled[2];
        int keysoffset = ledfunctionshuffled.IndexOf(2);
        //Debug.Log("keyoffset " + keysoffset);
        string keyorder = "";
        for (int i = 0; i < 4; i++)
        {
            keys[i] = buttoninfolist[(i + keysoffset) % 4].buttonoutputkey;
            // Debug.Log("keys " + keys[i] + " from location " + (i + keysoffset) % 4);
            keyorder += keys[i];

        }
        Debug.LogFormat("[Seven Choose Four #{0}] The correct key order is " + keyorder, moduleId);
    }

    private class Buttoninfo
    {
        public int buttonpos { get; set; }
        public string buttonoutputkey { get; set; }
    }

    List<Buttoninfo> DefineButtons()
    {
        List<Buttoninfo> l = new List<Buttoninfo>();
        for(int i = 0; i < 4; i++)
        {
            l.Add(new Buttoninfo { buttonpos = i,
                                   buttonoutputkey = GetOutputKey(ledfunctionshuffled[i])
                                  });
        }
        return l;
    }

    string GetOutputKey(int oper)
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

    List<string> FillSolution()
    {
        List<string> solution = new List<string>();
        string sol = "";
        for (int i = 0; i < 4; i++)
        {
            switch (ledColorNames[ledindex.ToArray()[i]])
            {
                case "R":
                    sol = SolveRed(keys[i], i);                    
                    solution.Add(sol);
                    break;
                case "G":
                    sol = SolveGreen(keys[i],i);                    
                    solution.Add(sol);
                    break;
                case "B":
                    sol = SolveBlue(keys[i],i);                    
                    solution.Add(sol);
                    break;
                case "M":
                    sol = SolveMagenta(keys[i],i);                   
                    solution.Add(sol);
                    break;
                case "C":
                    sol = SolveCyan(keys[i],i);                    
                    solution.Add(sol);
                    break;
                case "Y":
                    sol = SolveYellow(keys[i],i);                  
                    solution.Add(sol);
                    break;
                case "W":
                    whitesolution = FillWhiteSolution(keys[i], i);
                    string whitesols = "";
                    int colname = 0;
                    foreach(string s in whitesolution)
                    {
                        whitesols += manColorNames[colname];
                        whitesols += ": ";
                        whitesols += s;
                        whitesols += " ";
                        colname++;
                    }
                    Debug.LogFormat("[Seven Choose Four #{0}] White solutions are: " + whitesols,moduleId);
                    solution.Add("W");
                    break;
            }
        }
        return solution;
    }

    private List<string> FillWhiteSolution(string key, int seqnum)
    {
        Debug.LogFormat("[Seven Choose Four #{0}] White Puzzle: Key is " + key,moduleId);
        List<string> whitesols = new List<string>();
        whitesols.Add(SolveRed(key, seqnum));
        whitesols.Add(SolveGreen(key, seqnum));
        whitesols.Add(SolveBlue(key, seqnum));
        whitesols.Add(SolveMagenta(key, seqnum));
        whitesols.Add(SolveCyan(key, seqnum));
        whitesols.Add(SolveYellow(key, seqnum));

        return whitesols;
    }

    private int stage = 0;
    private int numpresses = 0;
    private List<char> buttonspressed = new List<char>();

    void ButtonPress(KMSelectable button)
    {
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);
        string buttonname = button.name;
        button.AddInteractionPunch();
        Debug.LogFormat("[Seven Choose Four #{0}] Button pressed is " + getButtonValue(buttonname).ToString(),moduleId);
        
        if (moduleReady)
        {            
            buttonspressed.Add(getButtonValue(buttonname));            
            if(numpresses == 3)
            {
                string currentsolution = solution[stage];
                if (currentsolution == "W")
                {
                    if (whitesolution.Contains(ListToString(buttonspressed)))
                    {
                        UpdateStage();
                    }
                    else
                    {
                        module.HandleStrike();
                        buttons[stage].GetComponent<MeshRenderer>().material = ledColors[4];
                        numpresses = -1;
                        Debug.LogFormat("[Seven Choose Four #{0}] Incorrect, you entered " + ListToString(buttonspressed) + ", stage reset.", moduleId);
                    }
                }
                else
                {                  
                    if(ListToString(buttonspressed) == currentsolution)
                    {
                        UpdateStage();
                    }
                    else
                    {
                        module.HandleStrike();
                        buttons[stage].GetComponent<MeshRenderer>().material = ledColors[4];
                        numpresses = -1;
                        Debug.LogFormat("[Seven Choose Four #{0}] Incorrect, you entered " + ListToString(buttonspressed) + ",stage reset.", moduleId);
                    }
                }
                buttonspressed.Clear();
            }
            numpresses++;
        }
    }

    void UpdateStage()
    {
        buttons[stage].GetComponent<MeshRenderer>().material = ledColors[2];
        stage++;
        numpresses = -1;
        if (stage == 4)
        {
            lightsready = false;
            moduleReady = false;
            foreach(Renderer led in leds)
            {
                led.material = ledColors[2];
            }
            module.HandlePass();
            moduleSolved = true;
        }
        else
        {
            string stagesol = "";
            Debug.LogFormat("[Seven Choose Four #{0}] Stage Passed", moduleId);
            if (solution[stage] == "W")
            {
                foreach (string s in whitesolution)
                {
                    stagesol += s;
                    stagesol += " ";
                }
            }
            else
            {
                stagesol = solution[stage];
            }
            Debug.LogFormat("[Seven Choose Four #{0}] Next stage solution is " + stagesol, moduleId);
        }
    }

    char getButtonValue(string name)
    {
        switch (name)
        {
            case "Button1":
                return (char)((4 - stage) % 4 + 49);
            case "Button2":
                return (char)((4 - stage + 1) % 4 + 49);
            case "Button3":
                return (char)((4 - stage + 2) % 4 + 49);
            case "Button4":
                return (char)((4 - stage + 3) % 4 + 49);
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
    void Update()
    {
        if (lightsready)
        {
            timer_morse -= Time.deltaTime;
            if (timer_morse < 0)
            {
                int morseled = ledfunctionshuffled.IndexOf(0);
                leds[morseled].material = morse[morsetimer] ? ledColors[ledindex[morseled]] : blackled;
                morsetimer = (morsetimer + 1) % morse.Length;
                timer_morse = DOT_LENGTH;
            }

            timer_tap -= Time.deltaTime;
            if (timer_tap < 0)
            {
                int tapled = ledfunctionshuffled.IndexOf(1);
                leds[tapled].material = tapcode[taptimer] ? ledColors[ledindex[tapled]] : blackled;
                taptimer = (taptimer + 1) % tapcode.Length;
                timer_tap = DOT_LENGTH;
            }

            timer_color -= Time.deltaTime;
            if (timer_color < 0)
            { 
                int colorled = ledfunctionshuffled.IndexOf(2);
                
                leds[colorled].material = colortimer < ledindex.Count ? ledColors[ledindex[colortimer]] : blackled;
                colortimer = (colortimer + 1) % (ledindex.Count + 1);
                
                timer_color = DOT_LENGTH * 3;
            }

            timer_num -= Time.deltaTime;
            if (timer_num < 0)
            {
                int numled = ledfunctionshuffled.IndexOf(3);
                leds[numled].material = flashcode[numtimer] ? ledColors[ledindex[numled]] : blackled;
                numtimer = (numtimer + 1) % flashcode.Length;
                timer_num = DOT_LENGTH;
            }
        }
    }

    bool[] MorseToBoolArray(string morse)
    {
        int length = 0;
        foreach (char c in morse)
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
        bool[] result = new bool[length + 4];
        int pointer = 0;
        foreach (char c in morse)
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

    private int tapsmallgap = 3;
    private int taplargegap = 10;

    bool[] TapToBoolArray(int index)
    {
        int row = index / 5 + 1;
        int col = index % 5 + 1;
        
        int length = row * 2 + col * 2 + tapsmallgap + taplargegap;
        int rowmax = row * 2;
        int colmax = rowmax + col * 2 + tapsmallgap;
        bool[] tapcodeflashes = new bool[length];
        for (int i = 0; i < length; i++)
        {
            if (i < rowmax || (i >= rowmax + tapsmallgap && i < colmax)) tapcodeflashes[i] = i % 2 == 0;
            else if (i >= rowmax && i < rowmax + tapsmallgap) tapcodeflashes[i] = false;          
        }

        return tapcodeflashes;
    }

    bool[] NumToFlashArray(int index)
    {
        int onframes = 7;
        int offframes = 10;
        int length = index * onframes + index + offframes;
        bool[] numflashes = new bool[length];
        for (int i = 0; i < length; i++)
        {
            if (i <= length - offframes) numflashes[i] = i % (onframes + 1) == 0 ? false : true;
            else numflashes[i] = false;
        }

        return numflashes;
    }

    string Reverse(string s)
    {
        string sr = "";
        for(int i = s.Length - 1; i >= 0; i--)
        {
            sr += s.ToCharArray()[i];
        }
        return sr;
    }

    private List<string> redtrue = new List<string>() { "1423", "2314", "4123", "2341" };
    private List<string> redfalse = new List<string>() { "2313", "4231", "2233", "1231" };

    string SolveRed(string keyletter, int seqnum)
    {
        int key = SYMBOLS.IndexOf(keyletter) + 1;
        int num_rports = bomb.GetPortCount("StereoRCA") + bomb.GetPortCount("RJ45") + bomb.GetPortCount("Parallel") + bomb.GetPortCount("Serial");
        Debug.LogFormat("[Seven Choose Four #{0}] Red Puzzle: Key is " + keyletter,moduleId);
        key = key + num_rports;
        key = key % 100;
        int tensdigit = key / 10;
        int onesdigit = key % 10;
        bool tensdigitbool = tensdigit % 2 != 0;
        bool onesdigitbool = onesdigit % 2 != 0;
        bool solution = false;
        Debug.LogFormat("[Seven Choose Four #{0}] Red Puzzle: Gate used is " + gates[seqnum],moduleId);
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
        return logSolution("Red",solution ? redtrue[seqnum] : redfalse[seqnum]);
    }

    private int[,] linesgrid =
    {
        {5, 6, 3, 5 },
        {7, 6, 6, 6 },
        {5, 6, 4, 4 },
        {4, 7, 5, 7 }
    };

    private List<string> greenodd = new List<string>() { "3241", "1122", "4332", "1231" };

    string SolveGreen(string keyname, int seqnum)
    {
        int key = SYMBOLS.IndexOf(keyname) + 1;
        int num_g7 = bomb.GetSerialNumber().Count(x => x == 'G' | x == '7');
        Debug.LogFormat("[Seven Choose Four #{0}] Green Puzzle: Key is " + keyname,moduleId);
        key = (key) % 100;
        int tensdigit = key / 10;
        int onesdigit = key % 10;
        int a = (tensdigit + num_g7) % 4;
        int b = (onesdigit + num_g7) % 4;
        int sum = linesgrid[a,a] + linesgrid[a,b] + linesgrid[b,a] + linesgrid[b,b];
        Debug.LogFormat("[Seven Choose Four #{0}] Green Puzzle: Sum of the lines is " + sum,moduleId);
        if (sum % 2 == 0) return logSolution("Green",Reverse(greenodd[seqnum]));
        else return logSolution("Green",greenodd[seqnum]);
    }

    private string[,] buttonordergrid = { {"1234", "1243", "2134", "2143", "3124" },
                                          {"3142", "1324", "1342", "1423", "1432" },
                                          {"4123", "4132", "2314", "2341", "3214" },
                                          {"3241", "2413", "2431", "4213", "4231" },
                                          {"3412", "3421", "4312", "4321", "1234" }};
    
    string SolveBlue(string keyname, int seqnum)
    {
        Debug.LogFormat("[Seven Choose Four #{0}] Blue Puzzle: Key is " + keyname,moduleId);
        int key = SYMBOLS.IndexOf(keyname) + 1;
        int numaabat = bomb.GetBatteryCount(Battery.AA);
        int numdbat = bomb.GetBatteryCount(Battery.D);
        char col = 'F';
        int row = key;
        while (row > 5) row -= 5;

        row = row - 1;
        
        
        foreach (char c in bomb.GetSerialNumberLetters())
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
        col = SYMBOLS.ToCharArray()[row];
    foundchar:
        Debug.LogFormat("[Seven Choose Four #{0}] Blue Puzzle: Starting Table Position is " + "(" + col + "," + (row + 1).ToString() + ")",moduleId);
        int colnum = SYMBOLS.IndexOf(col);
        int dir = FindMaxDirection(row, colnum, 5, 5, 3);
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
        Debug.LogFormat("[Seven Choose Four #{0}] Blue Puzzle: Intermediate Table Position is " + "(" + SYMBOLS[colnum] + "," + (row+1).ToString() + ")",moduleId);
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
        Debug.LogFormat("[Seven Choose Four #{0}] Blue Puzzle: Final Table Position is " + "(" + SYMBOLS[colnum] + "," + (row + 1).ToString() + ")",moduleId);

        return logSolution("Blue",buttonordergrid[row, colnum]);
    }

    private int[,] lettergrid =
    {
        {12, 16, 18, 9, 19},
        {24, 15, 6, 2, 22 },
        {1, 25, 4, 3, 20 },
        {11, 7, 23, 17, 14 },
        {13, 5, 21, 8, 10 }
    };

    string SolveMagenta(string key, int keypos)
    {
        int keyvalue = SYMBOLS.IndexOf(key) + 1;
        if (keyvalue == 26) keyvalue = 1;
        Debug.LogFormat("[Seven Choose Four #{0}] Magenta Puzzle: Key is " + key,moduleId);

        string letters = "";
        char[] s = new char[4];
        
        for (int i = 0; i < 4; i++)
        {
            char c = bomb.GetSerialNumber().ToCharArray()[i];
            letters += c.ToString();
            if (c < 65) s[i] = (char)(c + 17);
            else s[i] = c;
        }
        Debug.LogFormat("[Seven Choose Four #{0}] Magenta Puzzle: Letters to find are " + new string(s),moduleId);
        int keyrow = -1;
        int keycol = -1;
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                if (lettergrid[row,col] == keyvalue)
                {
                    keyrow = row;
                    keycol = col;
                    goto next;
                }
            }
        }
    next:
        int[] dist = new int[4];
        for (int i = 0; i < 4; i++)
        {
            int value = SYMBOLS.IndexOf(s[i]) + 1;
            for(int row = 0; row < 5; row++)
            {
                for(int col = 0; col < 5; col++)
                {
                    if(lettergrid[row,col] == value)
                    {
                        dist[i] = Math.Abs(row - keyrow) + Math.Abs(col - keycol);
                        if (dist[i] > 3) dist[i] = dist[i] % 4;
                        if (dist[i] == 0) dist[i] = 4;
                    }
                }
            }
        }
        return logSolution("Magenta",ListToString(dist.ToList()));
    }

    string SolveCyan(string keyname, int seqnum)
    {
        Debug.LogFormat("[Seven Choose Four #{0}] Cyan Puzzle: Key is " + keyname,moduleId);
        int key = SYMBOLS.IndexOf(keyname);
        key = key % 7;
        int numprim = 0;
        int numsec = 0;
        if (key < 3 | key == 6) numprim++;
        if (key >= 3) numsec++;
        string colors = manColorNames[key];
        string solleft = "";
        string solright = "";
        for(int i = 0; i < 4; i++)
        {
            if (i != seqnum)
            {
                colors += ledColorNames[ledindex[i]];
                if (ledindex[i] % 2 == 0) numprim++;
                if (ledindex[i] % 2 != 0 | ledindex[i] == 6) numsec++;
            }
        }

        Debug.LogFormat("[Seven Choose Four #{0}] Cyan Puzzle: Colors are " + colors,moduleId);
        Debug.LogFormat("[Seven Choose Four #{0}] Cyan Puzzle: There are " + numprim + "P and " + numsec + "S",moduleId);
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
        return logSolution("Cyan",solleft + solright);
    }

    string SolveYellow(string key, int seqnum)
    {
        int pos1 = SYMBOLS.IndexOf(key);
        Debug.LogFormat("[Seven Choose Four #{0}] Yellow Puzzle: Key is " + key,moduleId);
        List<int> distances = new List<int>() { 24, 4, 11, 14, 22 };
        List<string> letters = new List<string>() { "Y", "E", "L", "L", "O", "W"};
        int index = -1;
        index = FindClosestLetter(pos1, distances);
        if (index != 11) distances.Remove(index);
        letters.Remove(SYMBOLS[index].ToString());
        pos1 = 25 - pos1;
        index = FindClosestLetter(pos1, distances);
        //letters.RemoveAt(index);
        letters.Remove(SYMBOLS[index].ToString());
        List<string> newletters = new List<string>() { "A", "A", "A", "A" };
        int shift = (seqnum + 1)%4;
        for(int i = 0; i < 4; i++)
        {
            newletters[shift] = letters[i];
            shift = (shift + 1) % 4;
        }
        Debug.LogFormat("[Seven Choose Four #{0}] Yellow Puzzle: Shifted Letters: " + ListToString(newletters),moduleId);
        var letterindexpairs = new List<Item>()
        {
            new Item {key = 0, value = SYMBOLS.IndexOf(newletters[0])},
            new Item {key = 1, value = SYMBOLS.IndexOf(newletters[1])},
            new Item {key = 2, value = SYMBOLS.IndexOf(newletters[2])},
            new Item {key = 3, value = SYMBOLS.IndexOf(newletters[3])},
        };
        var sorted = letterindexpairs.OrderBy(x => x.value).ToList();
        int digit = 1;
        foreach(Item i in sorted)
        {
            i.value = digit;
            digit++;
        }
        sorted = sorted.OrderBy(x => x.key).ToList();
        string result = "";
        foreach(Item i in sorted)
        {
            result += (i.value).ToString();
        }
        return logSolution("Yellow",result);
    }

    private class Item
    {
        public int key { get; set; }
        public int value { get; set; }
    }

    int FindClosestLetter(int input, List<int> l)
    {
        int smalld = 100000;
        int index = -1;
        List<Item> sortdist = new List<Item>();
        for(int i = 0; i < l.Count; i++)
        {
            sortdist.Add(new Item { key = i, value = l[i] });
        }
        sortdist = sortdist.OrderBy(x => x.value).ToList();
        int step = 0;
        foreach(Item i in sortdist)
        {
            int dist = Math.Abs(input - i.value);
            if(dist <= smalld)
            {
                smalld = dist;
                index = step;
            }
            step++;
        }
        return sortdist[index].value;
    }

    int FindMaxDirection(int row, int col, int maxrows, int maxcols, int bias)
    {
        int posydist = row;
        int negydist = maxrows - row - 1;
        int posxdist = maxcols - col - 1;
        int negxdist = col;

        List<int> l = new List<int>();
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

    int FirstMaxIndex(List<int> l)
    {
        int max = -10000;
        int index = -1;
        for (int i = 0; i < l.Count(); i++) 
        {
            
            if(l[i] > max)
            {
                max = l[i];
                index = i;
            }
        }
        return index;
    }

    int MoveDirection(int x, int dist, int maxpos, bool pos)
    {
        return pos ? Math.Min(x + dist, maxpos) : Math.Max(x - dist, 0);
    }

    string ListToString(List<string> l)
    {
        string str = "";
        foreach (var s in l) str += s;
        return str;
    }

    string ListToString(List<int> l)
    {
        string str = "";
        foreach (var s in l) str += s.ToString();
        return str;
    }

    string ListToString(List<char> l)
    {
        string str = "";
        foreach (var s in l) str += s.ToString();
        return str;
    }

    string ListToString(List<Item> l)
    {
        string str = "";
        foreach (var s in l) str += s.value.ToString();
        return str;
    }

    string logSolution(string name, string sol)
    {
        Debug.LogFormat("[Seven Choose Four #{0}] {1} solution is: " + sol, moduleId,name);
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
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ',',');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            for(int i = 1; i < parameters.Length; i++)
            {
                if (inputIsValid(parameters[i]))
                {
                    yield return null;
                    if (parameters[i].ToLower().Equals("1"))
                    {
                        buttons[0].OnInteract();
                    }
                    else if (parameters[i].ToLower().Equals("2"))
                    {
                        buttons[1].OnInteract();
                    }
                    else if (parameters[i].ToLower().Equals("3"))
                    {
                        buttons[2].OnInteract();
                    }
                    else if (parameters[i].ToLower().Equals("4"))
                    {
                        buttons[3].OnInteract();
                    }
                }
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
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
                module.HandlePass();
                yield break;
            }

            // Press the next correct button
            var nextBtn = possibleSolutions[validPrefix][buttonspressed.Count] - '1';
            buttons[(nextBtn + stage) % 4].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }
}
