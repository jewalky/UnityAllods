using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum ClientIdentifiers
{
    // login
    SwitchMap,
    Error,
    DownloadStart,
    DownloadContinue,

    // map
    AddPlayer,
    DelPlayer,
    ChatMessage,
    SpeedChanged,

    AddUnit, // also update unit if ID already exists
    DelUnit,
    AddUnitActions,
    IdleUnit,
    DamageUnit,

    UnitPack,
    UnitStats,
    UnitStatsShort,
    UnitSpells,
    UnitFlags,
    UnitPosition,

    UnitItemPickup,
    SackAt,
    NoSackAt,

    // projectile directional, homing, simple
    AddProjectileDirectional,
    AddProjectileHoming,
    AddProjectileSimple,
    AddProjectileLightning,
    AddProjectileEOT,

    // kill static object (i.e. trees)
    StaticObjectDead
}