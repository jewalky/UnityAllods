using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Locale
{
    public static List<string> Main;
    public static List<string> Dialogs;
    public static List<string> Building;
    public static List<string> UnitName;

    public static List<string> ItemServ;
    public static List<string> ItemName;
    public static List<string> Stats;

    public static List<string> Spell; // spells by id
    public static List<string> Spells; // spell tooltips for spellbook

    public static List<string> Patch;

    public static void InitLocale()
    {
        Main = new StringFile("main/text/main.txt").Strings;
        Dialogs = new StringFile("main/text/dialogs.txt").Strings;
        Building = new StringFile("main/text/building.txt").Strings;
        UnitName = new StringFile("main/text/unitname.txt").Strings;

        ItemServ = new StringFile("main/text/itemserv.txt").Strings;
        ItemName = new StringFile("main/text/itemname.txt").Strings;
        Stats = new StringFile("main/text/stats.txt").Strings;

        Spell = new StringFile("main/text/spell.txt").Strings;
        Spells = new StringFile("main/text/spells.txt").Strings;

        Patch = new StringFile("patch/patch.txt").Strings;
    }
}
