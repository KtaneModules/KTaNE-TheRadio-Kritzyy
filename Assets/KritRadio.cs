using KModkit;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Random = UnityEngine.Random;
using System;

public class KritRadio : MonoBehaviour
{
    public KMSelectable OnBtn;
    public KMSelectable FreqUpBtn, FreqDownBtn;
    public KMSelectable Switch;
    public KMSelectable ResetKey;

    public TextMesh FrequencyText, TransmissionText;
    public TextMesh BarcodeText;

    public Transform SwitchTransform;
    public Transform FrequencyMarker;

    public KMBombInfo BombInfo;

    public KMAudio FrequencyChange, StaticNoise;

    public KMAudio DutchSongs, EnglishSongs, GermanSongs, FrenchSongs;


    Vector3 StartingMarkerPosition;

    List<string> NumberSerial = new List<string>
    {
        "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0"
    };
    List<string> LetterSerial = new List<string>
    {
        "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "W", "X", "Y", "Z"
    };
    List<string> CountryChoice = new List<string>
    {
        "Dutch", "English", "German", "French"
    };


    bool Transmitting = false;
    bool UsingTwitchPlays = false;
    bool TPSubmittng;
    bool Active = true;

    static int moduleIdCounter = 1;
    int moduleId;
    int FrequencyGen;
    int FrequencyInt;
    int SerialNum1Gen;
    int SerialNum2Gen;
    int ChannelNr;
    int FirstChannelNr;
    int DesiredChannelNr;
    int BatteryCount;
    int TPMultiplier;
    int BombTimeInt;
    int Timer;

    float Frequency;
    float FirstFrequency;
    float DesiredFrequency;

    string Barcode;
    string CurrentTransmission;
    string Country;
    string ChannelName;
    string StartingTransmission;
    string DesiredTransmission;
    string ErrorMessage;
    string FrequencyString;
    string DesiredCountry;

    string serialChar1;
    string serialChar2;
    string serialChar3;
    string serialChar4;
    string serialChar5;

    //TP related
    string CommandString;
    int CommandInt;
    int TPBombTimer;
    //TP related

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "Type '!{0} channel up/down 5' to move the radio 5 channels and '!{0} switch' to switch transmission. Then type '!{0} transmit at 00' to turn the radio on at XX:00 and submit your answer. To reset to the Factory Settings, type '!{0} reset'";
#pragma warning restore 0414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        Command = Command.ToLowerInvariant().Trim();

        KMSelectable buttonSelectable = null;
         
        if (Regex.IsMatch(Command, @"^transmit at +\d\d$"))
        {
            CommandString = Regex.Replace(Command, "[^0-9.]", "");
            CommandInt = int.Parse(CommandString);
            StartCoroutine("TimerHandler");
            while (CommandInt != TPBombTimer)
            {
                yield return "trycancel";
            }
            buttonSelectable = OnBtn;
            StopCoroutine("TimerHandler");
        }
        else if (Command.Equals("switch"))
        {
            buttonSelectable = Switch;
        }
        else if (Command.Equals("reset"))
        {
            buttonSelectable = ResetKey;
        }
        else if (Regex.IsMatch(Command, @"^channel up +\d$"))
        {
            UsingTwitchPlays = true;
            Command = Command.Remove(0, 11);
            int.TryParse(Command, out TPMultiplier);
            buttonSelectable = FreqUpBtn;
        }
        else if (Regex.IsMatch(Command, @"^channel down +\d$"))
        {
            UsingTwitchPlays = true;
            Command = Command.Remove(0, 13);
            int.TryParse(Command, out TPMultiplier);
            buttonSelectable = FreqDownBtn;
        }
        else if (Regex.IsMatch(Command, @"^channel up +\d\d$"))
        {
            UsingTwitchPlays = true;
            Command = Command.Remove(0, 11);
            int.TryParse(Command, out TPMultiplier);
            if (TPMultiplier + ChannelNr > 20)
            {
                yield return "sendtochaterror That command is not possible as the radio only has 20 channels";
            }
            else
            {
                buttonSelectable = FreqUpBtn;
            }
        }
        else if (Regex.IsMatch(Command, @"^channel down +\d\d$"))
        {
            UsingTwitchPlays = true;
            Command = Command.Remove(0, 13);
            int.TryParse(Command, out TPMultiplier);
            if (ChannelNr - TPMultiplier < 1)
            {
                yield return "sendtochaterror That command is not possible as it would put the channel at 0 or lower, which don't exist.";
            }
            else
            {
                buttonSelectable = FreqDownBtn;
            }
        }
        else if (Regex.IsMatch(Command, @"^channel down") || Regex.IsMatch(Command, @"^channel up"))
        {
            yield return "sendtochaterror Please specify with how many channels you wanna go up/down.";
        }
        else
        {
            yield return "sendtochaterror The command \"" + Command + "\" does not exist in the current context.";
        }
        yield return buttonSelectable;
        yield return new WaitForSeconds(0.1f);
        yield return buttonSelectable;
    }

    IEnumerator TimerHandler()
    {
        while (true)
        {
            TPBombTimer = ((int)BombInfo.GetTime()) % 60;
            yield return new WaitForSecondsRealtime(0.1f);
        }
    }


    void Awake()
    {
        moduleId = moduleIdCounter++;
        OnBtn.OnInteract += SubmitBtn;
        FreqUpBtn.OnInteract += FrequencyUp;
        FreqDownBtn.OnInteract += FrequencyDown;
        Switch.OnInteract += SwitchTransmission;
        ResetKey.OnInteract += Reset;
    }

    void Start()
    {
        Init();
    }

    void Init()
    {
        int TransmissionGen = Random.Range(0,2);
        if (TransmissionGen == 0)
        {
            StartingTransmission = "FM";
            CurrentTransmission = "FM";
            SwitchTransform.transform.Rotate(45, 0, 0);
        }
        else
        {
            StartingTransmission = "AM";
            SwitchTransform.transform.Rotate(-45, 0, 0);
            CurrentTransmission = "AM";
        }
        TransmissionText.text = CurrentTransmission;
        BarcodeGenerator();
    }

    void BarcodeGenerator()
    {
        //Barcode Generator...
        int SerialLtr1Gen = Random.Range(0, 22);
        int SerialLtr2Gen = Random.Range(0, 22);
        int SerialLtr3Gen = Random.Range(0, 22);
        SerialNum1Gen = Random.Range(0, 10);
        SerialNum2Gen = Random.Range(0, 10);

        serialChar1 = LetterSerial[SerialLtr1Gen];
        serialChar2 = LetterSerial[SerialLtr2Gen];
        serialChar3 = LetterSerial[SerialLtr3Gen];
        serialChar4 = NumberSerial[SerialNum1Gen];
        serialChar5 = NumberSerial[SerialNum2Gen];

        //...And applying the Barcode
        Barcode = serialChar1 + serialChar2 + serialChar3 + serialChar4 + serialChar5;
        BarcodeText.text = "RA-D-IO: " + Barcode;
        Debug.LogFormat("[The Radio #{0}] The barcode is {1}.", moduleId, Barcode);
        StartingFrequency();
    }

    void StartingFrequency()
    {
        int StartingMarkChannel;
        int StartingMarkCountryPicker;

        StartingMarkCountryPicker = Random.Range(0, 4);
        Country = CountryChoice[StartingMarkCountryPicker];
        CountryChoice.Remove(Country);

        StartingMarkChannel = Random.Range(0, 5);

        if (Country == "Dutch")
        {
            if (StartingMarkChannel == 0)
            {
                FrequencyMarker.localPosition = new Vector3(0.614f, 0.35f, 0f);
                Frequency = 91.8f;
                ChannelNr = 4;
                ChannelName = "NPO1";

                FirstFrequency = 91.8f;
                FirstChannelNr = 4;
            }
            else if (StartingMarkChannel == 1)
            {
                FrequencyMarker.localPosition = new Vector3(-0.169f, 0.35f, 0f);
                Frequency = 100.7f;
                ChannelNr = 14;
                ChannelName = "Q-Music";

                FirstFrequency = 100.7f;
                FirstChannelNr = 14;
            }
            else if (StartingMarkChannel == 2)
            {
                FrequencyMarker.localPosition = new Vector3(-0.232f, 0.35f, 0f);
                Frequency = 101.9f;
                ChannelNr = 15;
                ChannelName = "Sky Radio";

                FirstFrequency = 101.9f;
                FirstChannelNr = 15;
            }
            else if (StartingMarkChannel == 3)
            {
                FrequencyMarker.localPosition = new Vector3(0.119f, 0.35f, 0f);
                Frequency = 97.7f;
                ChannelNr = 9;
                ChannelName = "Radio Veronica";

                FirstFrequency = 97.7f;
                FirstChannelNr = 9;
            }
            else
            {
                FrequencyMarker.localPosition = new Vector3(-0.313f, 0.35f, 0f);
                Frequency = 102.4f;
                ChannelNr = 16;
                ChannelName = "Radio538";

                FirstFrequency = 102.4f;
                FirstChannelNr = 16;
            }
        } 
        else if (Country == "English")
        {
            if (StartingMarkChannel == 0)
            {
                FrequencyMarker.localPosition = new Vector3(-0.0775f, 0.35f, 0f);
                Frequency = 99.9f;
                ChannelNr = 12;
                ChannelName = "Classic FM";

                FirstFrequency = 99.9f;
                FirstChannelNr = 12;
            }
            else if (StartingMarkChannel == 1)
            {
                FrequencyMarker.localPosition = new Vector3(-0.521f, 0.35f, 0f);
                Frequency = 105.4f;
                ChannelNr = 20;
                ChannelName = "Magic 105.4 FM";

                FirstFrequency = 105.4f;
                FirstChannelNr = 20;
            }
            else if (StartingMarkChannel == 2)
            {
                FrequencyMarker.localPosition = new Vector3(-0.501f, 0.35f, 0f);
                Frequency = 104.9f;
                ChannelNr = 19;
                ChannelName = "Radio X";

                FirstFrequency = 104.9f;
                FirstChannelNr = 19;
            }
            else if (StartingMarkChannel == 3)
            {
                FrequencyMarker.localPosition = new Vector3(-0.331f, 0.35f, 0f);
                Frequency = 102.7f;
                ChannelNr = 17;
                ChannelName = "Heart Radio";

                FirstFrequency = 102.7f;
                FirstChannelNr = 17;
            }
            else
            {
                FrequencyMarker.localPosition = new Vector3(0.198f, 0.35f, 0f);
                Frequency = 96.1f;
                ChannelNr = 7;
                ChannelName = "Rother FM";

                FirstFrequency = 96.1f;
                FirstChannelNr = 7;
            }
        } 
        else if (Country == "German")
        {
            if (StartingMarkChannel == 0)
            {
                FrequencyMarker.localPosition = new Vector3(0.463f, 0.35f, 0f);
                Frequency = 93.3f;
                ChannelNr = 5;
                ChannelName = "RTL Radio";

                FirstFrequency = 93.3f;
                FirstChannelNr = 5;
            }
            else if (StartingMarkChannel == 1)
            {
                FrequencyMarker.localPosition = new Vector3(0.782f, 0.35f, 0f);
                Frequency = 88.9f;
                ChannelNr = 1;
                ChannelName = "Bayern 1";

                FirstFrequency = 88.9f;
                FirstChannelNr = 1;
            }
            else if (StartingMarkChannel == 2)
            {
                FrequencyMarker.localPosition = new Vector3(-0.051f, 0.35f, 0f);
                Frequency = 99.5f;
                ChannelNr = 11;
                ChannelName = "WDR 4";

                FirstFrequency = 99.5f;
                FirstChannelNr = 11;
            }
            else if (StartingMarkChannel == 3)
            {
                FrequencyMarker.localPosition = new Vector3(-0.354f, 0.35f, 0f);
                Frequency = 103.6f;
                ChannelNr = 18;
                ChannelName = "Radio Hamburg";

                FirstFrequency = 103.6f;
                FirstChannelNr = 18;
            }
            else
            {
                FrequencyMarker.localPosition = new Vector3(0.223f, 0.35f, 0f);
                Frequency = 96.0f;
                ChannelNr = 6;
                ChannelName = "Klassik Radio";

                FirstFrequency = 96.0f;
                FirstChannelNr = 6;
            }
        } 
        else if (Country == "French")
        {
            if (StartingMarkChannel == 0)
            {
                FrequencyMarker.localPosition = new Vector3(-0.1f, 0.35f, 0f);
                Frequency = 100.0f;
                ChannelNr = 13;
                ChannelName = "Jazz Radio";

                FirstFrequency = 100.0f;
                FirstChannelNr = 13;
            }
            else if (StartingMarkChannel == 1)
            {
                FrequencyMarker.localPosition = new Vector3(0.127f, 0.35f, 0f);
                Frequency = 97.5f;
                ChannelNr = 8;
                ChannelName = "Skyrock";

                FirstFrequency = 97.5f;
                FirstChannelNr = 8;
            }
            else if (StartingMarkChannel == 2)
            {
                FrequencyMarker.localPosition = new Vector3(0.724f, 0.35f, 0f);
                Frequency = 89.8f;
                ChannelNr = 3;
                ChannelName = "Radio Nova";

                FirstFrequency = 89.8f;
                FirstChannelNr = 3;
            }
            else if (StartingMarkChannel == 3)
            {
                FrequencyMarker.localPosition = new Vector3(0.05f, 0.35f, 0f);
                Frequency = 98.2f;
                ChannelNr = 10;
                ChannelName = "Radio FG";

                FirstFrequency = 98.2f;
                FirstChannelNr = 10;
            }
            else
            {
                FrequencyMarker.localPosition = new Vector3(0.765f, 0.35f, 0f);
                Frequency = 89.4f;
                ChannelNr = 2;
                ChannelName = "Radio Libertaire";

                FirstFrequency = 89.4f;
                FirstChannelNr = 2;
            }
        }

        StartingMarkerPosition = FrequencyMarker.localPosition;

        FrequencyString = Frequency.ToString();
        FrequencyText.text = FrequencyString;

        DesiredFrequencyGenerator();
    }

    void DesiredFrequencyGenerator()
    {
        //For frequency:
        int Batteries = BombInfo.GetBatteryCount();

        if (Batteries == 3 || Batteries == 5 || Batteries == 7 || Batteries == 11 || Batteries == 13)
        {
            DesiredCountry = "Dutch";
        }
        else if (BombInfo.GetBatteryCount() == BombInfo.GetBatteryHolderCount() && Batteries > 0)
        {
            DesiredCountry = "English";
        }
        else if (Batteries % 2 == 0)
        {
            DesiredCountry = "German";
        }
        else
        {
            DesiredCountry = "French";
        }

        if (DesiredCountry == "Dutch")
        {
            if (Barcode.Contains("N") || Barcode.Contains("P") || Barcode.Contains("O") || Barcode.Contains("1"))
            {
                ChannelName = "NPO1";
                DesiredFrequency = 91.8f;
                DesiredChannelNr = 4;
            }
            else if (Barcode.Contains("Q") || Barcode.Contains("M") || Barcode.Contains("U") || Barcode.Contains("S") || Barcode.Contains("I") || Barcode.Contains("C"))
            {
                ChannelName = "Q-Music";
                DesiredFrequency = 100.7f;
                DesiredChannelNr = 14;
            }
            else if (Barcode.Contains("S") || Barcode.Contains("K") || Barcode.Contains("Y") || Barcode.Contains("R") || Barcode.Contains("A") || Barcode.Contains("D") || Barcode.Contains("I") || Barcode.Contains("O"))
            {
                ChannelName = "Sky Radio";
                DesiredFrequency = 101.9f;
                DesiredChannelNr = 15;
            }
            else if (Barcode.Contains("V") || Barcode.Contains("E") || Barcode.Contains("R") || Barcode.Contains("O") || Barcode.Contains("N") || Barcode.Contains("I") || Barcode.Contains("C") || Barcode.Contains("A"))
            {
                ChannelName = "Radio Veronica";
                DesiredFrequency = 97.7f;
                DesiredChannelNr = 9;
            }
            else
            {             
                ChannelName = "Radio538";
                DesiredFrequency = 102.4f;
                DesiredChannelNr = 16;
            }
        } //Dutch radio channels
        else if (DesiredCountry == "English")
        {
            if (Barcode.Contains("C") || Barcode.Contains("L") || Barcode.Contains("A") || Barcode.Contains("S") || Barcode.Contains("I") || Barcode.Contains("F") || Barcode.Contains("M"))
            {
                ChannelName = "Classic FM";
                DesiredFrequency = 99.9f;
                DesiredChannelNr = 12;
            }
            else if (Barcode.Contains("M") || Barcode.Contains("A") || Barcode.Contains("G") || Barcode.Contains("I") || Barcode.Contains("C") || Barcode.Contains("1") || Barcode.Contains("0") || Barcode.Contains("5"))
            {
                ChannelName = "Magic 105.4 FM";
                DesiredFrequency = 105.4f;
                DesiredChannelNr = 20;
            }
            else if (Barcode.Contains("R") || Barcode.Contains("A") || Barcode.Contains("D") || Barcode.Contains("I") || Barcode.Contains("O") || Barcode.Contains("X"))
            {
                ChannelName = "Radio X";
                DesiredFrequency = 104.9f;
                DesiredChannelNr = 19;
            }
            else if (Barcode.Contains("H") || Barcode.Contains("E") || Barcode.Contains("A") || Barcode.Contains("R") || Barcode.Contains("T"))
            {
                ChannelName = "Heart Radio";
                DesiredFrequency = 102.7f;
                DesiredChannelNr = 17;
            }
            else
            {
                ChannelName = "Rother FM";
                DesiredFrequency = 96.1f;
                DesiredChannelNr = 7;
            }
        } //English radio channels
        else if (DesiredCountry == "German")
        {
            if (Barcode.Contains("R") || Barcode.Contains("T") || Barcode.Contains("L"))
            {
                ChannelName = "RTL Radio";
                DesiredFrequency = 93.3f;
                DesiredChannelNr = 5;
            }
            else if (Barcode.Contains("B") || Barcode.Contains("A") || Barcode.Contains("Y") || Barcode.Contains("E") || Barcode.Contains("R") || Barcode.Contains("N") || Barcode.Contains("1"))
            {
                ChannelName = "Bayern 1";
                DesiredFrequency = 88.9f;
                DesiredChannelNr = 1;
            }
            else if (Barcode.Contains("W") || Barcode.Contains("D") || Barcode.Contains("R") || Barcode.Contains("4"))
            {
                ChannelName = "WDR 4";
                DesiredFrequency = 99.5f;
                DesiredChannelNr = 11;
            }
            else if (Barcode.Contains("H") || Barcode.Contains("A") || Barcode.Contains("M") || Barcode.Contains("B") || Barcode.Contains("U") || Barcode.Contains("R") || Barcode.Contains("G"))
            {
                ChannelName = "Radio Hamburg";
                DesiredFrequency = 103.6f;
                DesiredChannelNr = 18;
            }
            else
            {
                ChannelName = "Klassik Radio";
                DesiredFrequency = 96.0f;
                DesiredChannelNr = 6;
            }
        } //German radio channels
        else if (DesiredCountry == "French")
        {
            if (Barcode.Contains("J") || Barcode.Contains("A") || Barcode.Contains("Z") || Barcode.Contains("R") || Barcode.Contains("A") || Barcode.Contains("D") || Barcode.Contains("I") || Barcode.Contains("O"))
            {
                ChannelName = "Jazz Radio";
                DesiredFrequency = 100.0f;
                DesiredChannelNr = 13;
            }
            else if (Barcode.Contains("S") || Barcode.Contains("K") || Barcode.Contains("Y") || Barcode.Contains("R") || Barcode.Contains("O") || Barcode.Contains("C") || Barcode.Contains("K"))
            {
                ChannelName = "Skyrock";
                DesiredFrequency = 97.5f;
                DesiredChannelNr = 8;
            }
            else if (Barcode.Contains("N") || Barcode.Contains("O") || Barcode.Contains("V") || Barcode.Contains("A"))
            {
                ChannelName = "Radio Nova";
                DesiredFrequency = 89.8f;
                DesiredChannelNr = 3;
            }
            else if (Barcode.Contains("F") || Barcode.Contains("G"))
            {
                ChannelName = "Radio FG";
                DesiredFrequency = 98.2f;
                DesiredChannelNr = 10;
            }
            else
            {
                ChannelName = "Radio Libertaire";
                DesiredFrequency = 89.4f;
                DesiredChannelNr = 2;
            }
        } //French radio channels

        Debug.LogFormat("[The Radio #{0}] The desired channel is the {1} {2} ({3}) with frequency {4}.", moduleId, DesiredCountry, ChannelName, DesiredChannelNr, DesiredFrequency);
        if (DesiredFrequency == FirstFrequency)
            Debug.LogFormat("[The Radio #{0}] The starting channel and the desired channel match! Look at you!", moduleId);

        DesiredTransmissionGenerator();
    }

    void DesiredTransmissionGenerator()
    {
        //For transmission:
        if (BombInfo.GetSerialNumberNumbers().First() > BombInfo.GetSerialNumberNumbers().Last() && SerialNum1Gen > SerialNum2Gen)
        {
            DesiredTransmission = "FM";
        }
        else
        {
            DesiredTransmission = "AM";
        }
        Debug.LogFormat("[The Radio #{0}] The desired transmission is {1}.", moduleId, DesiredTransmission);
        return;
    }

    void SubmitFreq()
    {
        if (Frequency == DesiredFrequency)
        {
            if (CurrentTransmission == DesiredTransmission)
            {
                Debug.LogFormat("[The Radio #{0}] Frequency and transmission correct! checking time...", moduleId, BatteryCount);
                CheckBatteries();
            }
            else
            {
                ErrorMessage = "Transmission was incorrect";
                Incorrect();
            }
        }
        else
        {
            ErrorMessage = "Frequency was incorrect";
            Incorrect();
        }
    }

    void CheckBatteries()
    {
        BatteryCount = BombInfo.GetBatteryCount() % 10;
        Debug.LogFormat("[The Radio #{0}] Battery count: {1}.", moduleId, BatteryCount);
        CheckTime();
    }

    void CheckTime()
    {
        int Time = ((int)BombInfo.GetTime()) % 60;

        int TimeSecond1 = (((int)BombInfo.GetTime()) % 60) / 10;
        int TimeSecond2 = ((int)BombInfo.GetTime()) % 10;

        string Action1 = "", Action2 = "";

        Debug.LogFormat("[The Radio #{0}] Timer: {1}.", moduleId, Time);
        Debug.LogFormat("[The Radio #{0}] 1st timer digit: {1}.", moduleId, TimeSecond1);
        Debug.LogFormat("[The Radio #{0}] 2nd timer digit: {1}.", moduleId, TimeSecond2);

        if (DesiredCountry == "Dutch")
        {
            Action1 = "the 2 timer digits combined";
            Action2 = "be " + SerialNum1Gen.ToString();
            if (TimeSecond1 + TimeSecond2 == SerialNum1Gen)
            {
                Correct();
            }
            else
            {
                ErrorMessage = "Time not correct";
                Incorrect();
            }
        }
        else if (DesiredCountry == "English")
        {
            Action1 = "the seconds digits on the timer must match, so it";
            Action2 = "either be XX:00, XX:11, XX:22, XX:33, XX:44 or XX:55";
            if (TimeSecond1 == TimeSecond2)
            {
                Correct();
            }
            else
            {
                ErrorMessage = "Time not correct";
                Incorrect();
            }
        }
        else if (DesiredCountry == "German")
        {
            Action1 = "the 2 timer digits combined";
            Action2 = "match the amount of batteries (" + BatteryCount + ")";
            if (TimeSecond1 + TimeSecond2 == BatteryCount)
            {
                Correct();
            }
            else
            {
                ErrorMessage = "Time not correct";
                Incorrect();
            }
        }
        else if (DesiredCountry == "French")
        {
            Action1 = "the 2 timer digits combined";
            Action2 = "match than the last digit of the serial number (" + BombInfo.GetSerialNumberNumbers().Last() + ")";
            if (TimeSecond1 + TimeSecond2 == BombInfo.GetSerialNumberNumbers().Last())
            {
                Correct();
            }
            else
            {
                ErrorMessage = "Time not correct";
                Incorrect();
            }
        }
        Debug.LogFormat("[The Radio #{0}] For submission, {1} should {2}", moduleId, Action1, Action2);
    }

    void Incorrect()
    {

        StaticNoise.PlaySoundAtTransform("StaticNoiseIncorrect", transform);
        StartCoroutine("StaticNoiseIncorrect");
        OnBtn.OnInteract = EmptyVoid;
        FreqUpBtn.OnInteract = EmptyVoid;
        FreqDownBtn.OnInteract = EmptyVoid;
        Switch.OnInteract = EmptyVoid;
        ResetKey.OnInteract = EmptyVoid;
    }

    void Correct()
    {
        Debug.LogFormat("[The Radio #{0}] Frequency received, radio turned on. Module passed.", moduleId);
        StaticNoise.PlaySoundAtTransform("StaticNoise", transform);
        StaticNoise.PlaySoundAtTransform("BackgroundStaticNoise", transform);
        StartCoroutine("StaticNoiseCorrect");
        Active = false;
        OnBtn.OnInteract = EmptyVoid;
        FreqUpBtn.OnInteract = EmptyVoid;
        FreqDownBtn.OnInteract = EmptyVoid;
        Switch.OnInteract = EmptyVoid;
        ResetKey.OnInteract = EmptyVoid;
    }

    IEnumerator StaticNoiseCorrect()
    {
        int Song;
        for (int i = 0; i < 6; i++)
        {
            if (i == 1)
            {
                Song = Random.Range(0, 4);
                if (DesiredCountry == "Dutch")
                {
                    switch (Song)
                    {
                        case 1:
                            DutchSongs.PlaySoundAtTransform("DutchSong1", transform);
                            break;
                        case 2:
                            DutchSongs.PlaySoundAtTransform("DutchSong2", transform);
                            break;
                        case 3:
                            DutchSongs.PlaySoundAtTransform("DutchSong3", transform);
                            break;
                        default:
                            DutchSongs.PlaySoundAtTransform("DutchSong4", transform);
                            break;
                    }
                }
                else if (DesiredCountry == "English")
                {
                    switch (Song)
                    {
                        case 1:
                            EnglishSongs.PlaySoundAtTransform("EnglishSong1", transform);
                            break;
                        case 2:
                            EnglishSongs.PlaySoundAtTransform("EnglishSong2", transform);
                            break;
                        case 3:
                            EnglishSongs.PlaySoundAtTransform("EnglishSong3", transform);
                            break;
                        default:
                            EnglishSongs.PlaySoundAtTransform("EnglishSong4", transform);
                            break;
                    }
                }
                else if (DesiredCountry == "German")
                {
                    switch (Song)
                    {
                        case 1:
                            GermanSongs.PlaySoundAtTransform("GermanSong1", transform);
                            break;
                        case 2:
                            GermanSongs.PlaySoundAtTransform("GermanSong2", transform);
                            break;
                        case 3:
                            GermanSongs.PlaySoundAtTransform("GermanSong3", transform);
                            break;
                        default:
                            GermanSongs.PlaySoundAtTransform("GermanSong4", transform);
                            break;
                    }
                }
                else if (DesiredCountry == "French")
                {
                    switch (Song)
                    {
                        case 1:
                            FrenchSongs.PlaySoundAtTransform("FrenchSong1", transform);
                            break;
                        case 2:
                            FrenchSongs.PlaySoundAtTransform("FrenchSong2", transform);
                            break;
                        case 3:
                            FrenchSongs.PlaySoundAtTransform("FrenchSong3", transform);
                            break;
                        default:
                            FrenchSongs.PlaySoundAtTransform("FrenchSong4", transform);
                            break;
                    }
                }
            }
            if (i == 3)
            {
                Debug.LogFormat("[The Radio #{0}] Frequency received, radio turned on. Module passed.", moduleId);
                GetComponent<KMBombModule>().HandlePass();
                GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                Switch.OnInteract = SwitchTransmission;
            }
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    IEnumerator StaticNoiseIncorrect()
    {
        for (int i = 0; i < 4; i++)
        {
            if (i == 3)
            {
                Debug.LogFormat("[The Radio #{0}] {1}. Strike handed.", moduleId, ErrorMessage);
                Reset();
                GetComponent<KMBombModule>().HandleStrike();
            }
            yield return new WaitForSecondsRealtime(1f);
        }
    }


    void RaiseFreq()
    {
        ChannelNr++;
        if (ChannelNr > 0 && ChannelNr <= 20)
        {
            FrequencyChange.PlaySoundAtTransform("FrequencyChange", transform);
            NewChannel();
        }
        else
        {
            Debug.LogFormat("[The Radio #{0}] You are at the last channel already.", moduleId);
            ChannelNr--;
        }

        if (TPMultiplier > 0)
        {
            TPMultiplier--;
            RaiseFreq();
        }
    }

    void LowerFreq()
    {
        ChannelNr--;
        if (ChannelNr > 0 && ChannelNr <= 20)
        {
            FrequencyChange.PlaySoundAtTransform("FrequencyChange", transform);
            NewChannel();
        }
        else
        {
            Debug.LogFormat("[The Radio #{0}] You are at the first channel already.", moduleId);
            ChannelNr++;
        }

        if (TPMultiplier > 0)
        {
            TPMultiplier--;
            LowerFreq();
        }
    }

    void NewChannel()
    {
        if (ChannelNr == 1)
        {
            Frequency = 88.9f;
            FrequencyMarker.localPosition = new Vector3(0.782f, 0.35f, 0f);
        }
        else if (ChannelNr == 2)
        {
            Frequency = 89.4f;
            FrequencyMarker.localPosition = new Vector3(0.765f, 0.35f, 0f);
        }
        else if (ChannelNr == 3)
        {
            Frequency = 89.8f;
            FrequencyMarker.localPosition = new Vector3(0.724f, 0.35f, 0f);
        }
        else if (ChannelNr == 4)
        {
            Frequency = 91.8f;
            FrequencyMarker.localPosition = new Vector3(0.614f, 0.35f, 0f);
        }
        else if (ChannelNr == 5)
        {
            Frequency = 93.3f;
            FrequencyMarker.localPosition = new Vector3(0.463f, 0.35f, 0f);
        }
        else if (ChannelNr == 6)
        {
            Frequency = 96.0f;
            FrequencyMarker.localPosition = new Vector3(0.223f, 0.35f, 0f);
        }
        else if (ChannelNr == 7)
        {
            Frequency = 96.1f;
            FrequencyMarker.localPosition = new Vector3(0.198f, 0.35f, 0f);
        }
        else if (ChannelNr == 8)
        {
            Frequency = 97.5f;
            FrequencyMarker.localPosition = new Vector3(0.127f, 0.35f, 0f);
        }
        else if (ChannelNr == 9)
        {
            Frequency = 97.7f;
            FrequencyMarker.localPosition = new Vector3(0.119f, 0.35f, 0f);
        }
        else if (ChannelNr == 10)
        {
            Frequency = 98.2f;
            FrequencyMarker.localPosition = new Vector3(0.05f, 0.35f, 0f);
        }
        else if (ChannelNr == 11)
        {
            Frequency = 99.5f;
            FrequencyMarker.localPosition = new Vector3(-0.051f, 0.35f, 0f);
        }
        else if (ChannelNr == 12)
        {
            Frequency = 99.9f;
            FrequencyMarker.localPosition = new Vector3(-0.0775f, 0.35f, 0f);
        }
        else if (ChannelNr == 13)
        {
            Frequency = 100.0f;
            FrequencyMarker.localPosition = new Vector3(-0.1f, 0.35f, 0f);
        }
        else if (ChannelNr == 14)
        {
            Frequency = 100.7f;
            FrequencyMarker.localPosition = new Vector3(-0.169f, 0.35f, 0f);
        }
        else if (ChannelNr == 15)
        {
            Frequency = 101.9f;
            FrequencyMarker.localPosition = new Vector3(-0.232f, 0.35f, 0f);
        }
        else if (ChannelNr == 16)
        {
            Frequency = 102.4f;
            FrequencyMarker.localPosition = new Vector3(-0.313f, 0.35f, 0f);
        }
        else if (ChannelNr == 17)
        {
            Frequency = 102.7f;
            FrequencyMarker.localPosition = new Vector3(-0.331f, 0.35f, 0f);
        }
        else if (ChannelNr == 18)
        {
            Frequency = 103.6f;
            FrequencyMarker.localPosition = new Vector3(-0.354f, 0.35f, 0f);
        }
        else if (ChannelNr == 19)
        {
            Frequency = 104.9f;
            FrequencyMarker.localPosition = new Vector3(-0.501f, 0.35f, 0f);
        }
        else if (ChannelNr == 20)
        {
            Frequency = 105.4f;
            FrequencyMarker.localPosition = new Vector3(-0.521f, 0.35f, 0f);
        }

        FrequencyString = Frequency.ToString();
        FrequencyText.text = FrequencyString;

        if (UsingTwitchPlays && TPMultiplier < 1)
        {
            UsingTwitchPlays = false;
        }
    }

    protected bool SubmitBtn()
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();
        Debug.LogFormat("[The Radio #{0}] Turned on the radio.", moduleId);
        SubmitFreq();
        return false;
    }

    protected bool FrequencyUp()
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();
        if (UsingTwitchPlays)
        {
            TPMultiplier--;
        }
        RaiseFreq();
        return false;
    }

    protected bool FrequencyDown()
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();
        if (UsingTwitchPlays)
        {
            TPMultiplier--;
        }
        LowerFreq();
        return false;
    }

    protected bool SwitchTransmission()
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();
        if (!Transmitting)
        {
            StartCoroutine("Transmission");
        }
        else
        {
            
            Debug.LogFormat("[The Radio #{0}] Transmission already in progress!", moduleId);
        }
        return false;
    }

    IEnumerator Transmission()
    {
        for (int i = 0; i < 9; i++)
        {
            Transmitting = true;
            if (CurrentTransmission == "FM")
            {
                SwitchTransform.transform.Rotate(-12f, 0, 0);
            }
            else if (CurrentTransmission == "AM")
            {
                SwitchTransform.transform.Rotate(12f, 0, 0);
            }
            
            if (i == 1)
            {
                if (Active)
                    Debug.LogFormat("[The Radio #{0}] Switching transmission...", moduleId);
            }

            if (i == 7)
            {
                if (CurrentTransmission == "FM")
                {
                    CurrentTransmission = "AM";
                }
                else if (CurrentTransmission == "AM")
                {
                    CurrentTransmission = "FM";
                }
                TransmissionText.text = CurrentTransmission;
            }

            if (i == 8)
            {
                Transmitting = false;
                if (Active)
                    Debug.LogFormat("[The Radio #{0}] The current transmission is '{1}'.", moduleId, CurrentTransmission);
            }
            yield return new WaitForSecondsRealtime(0.01f);
        }
    }

    protected bool Reset()
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();
        Debug.LogFormat("[The Radio #{0}] Resetting radio to Factory Setting...", moduleId);
        if (CurrentTransmission != StartingTransmission)
        {
            if (CurrentTransmission == "FM")
            {
                SwitchTransform.transform.Rotate(-90, 0, 0);
            }
            else if (CurrentTransmission == "AM")
            {
                SwitchTransform.transform.Rotate(90, 0, 0);
            }
        }

        CurrentTransmission = StartingTransmission;
        Frequency = FirstFrequency;
        ChannelNr = FirstChannelNr;

        OnBtn.OnInteract = SubmitBtn;
        FreqUpBtn.OnInteract = FrequencyUp;
        FreqDownBtn.OnInteract = FrequencyDown;
        Switch.OnInteract = SwitchTransmission;
        ResetKey.OnInteract = Reset;

        FrequencyMarker.localPosition = StartingMarkerPosition;

        FrequencyString = Frequency.ToString();
        FrequencyText.text = FrequencyString;

        Debug.LogFormat("[The Radio #{0}] The transmission is now {1} again.", moduleId, CurrentTransmission);
        Debug.LogFormat("[The Radio #{0}] The channel number is now {1} again. Frequency is {2}", moduleId, ChannelNr, Frequency);

        return false;
    }

    protected bool EmptyVoid()
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();
        return false;
    }
}
