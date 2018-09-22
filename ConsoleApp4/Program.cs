﻿using System;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace NowPlayingMain
{
    public class MainProgram
    {
        public static void Main()
        {
                        if (CSGOProcessChecker.csgoprocess() == "Ok")
                        {
                            TextShit.CmdText();
                        }
                        else
                        {
                            Console.WriteLine("no csgo no work");
                        } 
            while (CSGOProcessChecker.csgoprocess() == "Ok")
            {
                string currenttrack = Spotify.CurrentTrack();
                string currenttrackformatted = TrackFormatter.nameToWrite();
                TxtWorker.Write();
                Console.WriteLine("original: " + currenttrack);
                Console.WriteLine("formatted: " + currenttrackformatted);
                Thread.Sleep(1000);
                Console.Clear();
            }
        }
    }

    public class Spotify
    {
        public static string CurrentTrack()
        {
            var proc = Process.GetProcessesByName("Spotify")
                              .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.MainWindowTitle));

            if (proc == null)
                return "Spotify is not running!";

            if (string.Equals(proc.MainWindowTitle, "Spotify", StringComparison.InvariantCultureIgnoreCase))
                return "Paused";

            return proc.MainWindowTitle;
        }
    }

    public static class Slovar
    {
        public static readonly Dictionary<string, string> Ru2En = new Dictionary<string, string>
        {
            { "ё", "yo"},
            { "ж", "zh"},
            { "ч", "ch"},
            { "ш", "sh"},
            { "щ", "sch"},
            { "ю", "yu"},
            { "я", "ya"},
            { "Ё", "Yo"},
            { "Ж", "Zh"},
            { "Ч", "Ch"},
            { "Ш", "Sh"},
            { "Щ", "Sch"},
            { "Ю", "Yu"},
            { "Я", "Ya"},
            { "а", "a"},
            { "б", "b"},
            { "в", "v"},
            { "г", "g"},
            { "д", "d"},
            { "е", "e"},
            { "з", "z"},
            { "и", "i"},
            { "й", "j"},
            { "к", "k"},
            { "л", "l"},
            { "м", "m"},
            { "н", "n"},
            { "о", "o"},
            { "п", "p"},
            { "р", "r"},
            { "с", "s"},
            { "т", "t"},
            { "у", "u"},
            { "ф", "f"},
            { "х", "h"},
            { "ц", "c"},
            { "ъ", "j"},
            { "ы", "i"},
            { "ь", "j"},
            { "э", "e"},
            { "А", "A"},
            { "Б", "B"},
            { "В", "V"},
            { "Г", "G"},
            { "Д", "D"},
            { "Е", "E"},
            { "З", "Z"},
            { "И", "I"},
            { "Й", "J"},
            { "К", "K"},
            { "Л", "L"},
            { "М", "M"},
            { "Н", "N"},
            { "О", "O"},
            { "П", "P"},
            { "Р", "R"},
            { "С", "S"},
            { "Т", "T"},
            { "У", "U"},
            { "Ф", "F"},
            { "Х", "H"},
            { "Ц", "C"},
            { "Ъ", "J"},
            { "Ы", "I"},
            { "Ь", "J"},
            {"Э", "E"}
        };
    }

    public class TrackFormatter
    {
        public static string nameToWrite()
        {
            var OutputTrackName = Slovar.Ru2En.Aggregate(Spotify.CurrentTrack(), (current, value) => current.Replace(value.Key, value.Value));
            return OutputTrackName.ToString();
        }

    }

    public class TextShit
    {
        public static string chat_button;
        public static void CmdText()
        {
            Console.WriteLine(CSGOProcessChecker.csgoprocess());
            Console.WriteLine("кнопка exec: ");
            string exec_button = Console.ReadLine();
            Console.WriteLine($"bind \"{exec_button}\" \"exec audio\"");
            Console.WriteLine("кнопка чата: ");
            chat_button = Console.ReadLine();
            Console.WriteLine("Выбери бойца: ");
            Console.WriteLine("1. veselv2010");
            Console.WriteLine("2. scoutpan");
            Console.WriteLine("3. smurf acc by vasilvs");
            Console.WriteLine("4. jigido");
        }
    }

    public class TxtWorker
    {
        public static string WritePath = Selector();
        public static string Selector()
        {
            string temp = Console.ReadLine();
            if (temp == "1")
            {
                return @"C:\Program Files (x86)\Steam\userdata\73467495\730\local\cfg\audio.cfg";
            }
            if (temp == "2")
            {
                return @"D:\Steam\userdata\88427647\730\local\cfg\audio.cfg";
            }
            if (temp == "3")
            {
                return @"C:\Program Files (x86)\Steam\userdata\223309473\730\local\cfg\audio.cfg";
            }
            if (temp == "4")
            {
                return @"C:\Program Files (x86)\Steam\userdata\899887627\730\local\cfg\audio.cfg";
            }
            return "0";
        }
        private const string ToWrite = "bind \"{1}\" \"say Now Playing: {0}\"";
        public static void Write()
        {
            using (StreamWriter sw = new StreamWriter(WritePath, false, Encoding.GetEncoding(28591)))
                sw.WriteLine(string.Format(ToWrite, TrackFormatter.nameToWrite(), TextShit.chat_button));
        }
    }
}

public class CSGOProcessChecker
{
    public static string csgoprocess()
    {
        var CSGOprocess = Process.GetProcessesByName("csgo")
                                        .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.MainWindowTitle));
        if (CSGOprocess == null)
        {
            return "fuck no";
        }
        else
        {
            return "Ok";
        }
    }
}

//=_-icon by SCOUTPAN-_=