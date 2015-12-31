using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Locale
{
    public static List<string> Main;
    public static List<string> Building;
    public static List<string> UnitName;

    public static List<string> Patch;

    public static void InitLocale()
    {
        Main = new StringFile("main/text/main.txt").Strings;
        Building = new StringFile("main/text/building.txt").Strings;
        UnitName = new StringFile("main/text/unitname.txt").Strings;

        Patch = new StringFile("patch/patch.txt").Strings;
    }
}
