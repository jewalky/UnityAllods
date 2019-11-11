//Debug.LogFormat("path find err: {0}, {1} =", X, Y);

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

//подумать насчет сохранения карт доступности

class MapWizard
{
// !!! предпосылки:
//	размеры карты не больше 256*256
//	карта по краям ограничена для всех непроходимыми клетками


//////////////////////////////////////////////////////////
// возможно будет использовано когда нибудь
    public bool _CheckWalkableForUnit(MapUnit Unit, int x, int y, bool staticOnly)
    {
        if (x < 8 || x > MapLogic.Instance.Width - 8 ||
            y < 8 || y > MapLogic.Instance.Height - 8) return false;
        for (int ly = y; ly < y + Unit.Height; ly++)
        {
            for (int lx = x; lx < x + Unit.Width; lx++)
            {
                MapNode node = MapLogic.Instance.Nodes[lx, ly];
                // skip cells currently taken
                if (node.Objects.Contains(Unit))
                    continue; // if we are already on this cell, skip it as passible
                uint tile = node.Tile;
                MapNodeFlags flags = node.Flags;
                if (Unit.IsWalking && (flags & MapNodeFlags.Unblocked) == 0 && (tile >= 0x1C0 && tile <= 0x2FF))
                    return false;
                MapNodeFlags bAir = staticOnly ? MapNodeFlags.BlockedAir : MapNodeFlags.BlockedAir | MapNodeFlags.DynamicAir;
                MapNodeFlags bGround = staticOnly ? MapNodeFlags.BlockedGround : MapNodeFlags.BlockedGround | MapNodeFlags.DynamicGround;
                if (Unit.IsFlying && (flags & bAir) != 0) return false;
                else if (!Unit.IsFlying && (flags & bGround) != 0)
                    return false;
            }
        }

        return true;
    }

    const byte HZ = 0;
	private byte[] _CellMoveCostMixType = new byte[0x40]  //char a540A6[0x40] =
	{ 2, 3, 2, 4, 3, 4, 2, 2, 2, 2, 4, 4, 4, 4, HZ, HZ,
	  3, 5, 3, 3, 1, 3, 2, 4, 2, 2, 4, 2, 4, 4, HZ, HZ,
	  2, 3, 2, 4, 3, 4, 2, 4, 2, 2, 4, 2, 4, 4, HZ, HZ,
	  5, 5, 5, 5, 5, 5, 2, 2, 2, 2, 4, 4, 4, 4, HZ, HZ };
	private byte[][] _CellMoveCostMixValues = new byte[0x10][]  //char a54126[0x10][2] =
	{ {2, 1}, {5, 1}, {4, 1}, {7, 1}, {6, 1}, {5, 6}, {3, 7}, {8, 6},
	  {HZ,HZ},{HZ,HZ},{HZ,HZ},{HZ,HZ},{HZ,HZ},{HZ,HZ},{HZ,HZ},{HZ,HZ}
	};	
	private byte[] _CellCostBase = new byte[0x10]  //must be reloaded from map.reg !!!
	{
	0,	//dummy
	0x8,	//1: land
	0x8,	//2: grass
	0x9,	//3: flowers
	0xE,	//4: sand
	0x6,    //5: cracked
	0xC,	//6: stones
	0xB,	//7: savanna
	0x10,	//8: mountain
	0x8,	//9: water
	0x6,	//A: road
	0,	//dummy
	0,	//dummy
	0,	//dummy
	0	//dummy
	};

	public int CellMoveCost(int tile) { // 0 == unwalkable tile
		if (tile >= 0x1C0 && tile <= 0x2FF)
			return 0;
		int MixType = _CellMoveCostMixType[tile & 0x3F];
		int Mix = (tile >> 6) & 0x3;
		if      (MixType == 1)
			return (CellMoveCostMixValues[Mix][1] * 4 + _CellMoveCostMixValues[Mix][0] * 0 ) >> 2;
		else if (MixType == 2)
			return (CellMoveCostMixValues[Mix][1] * 3 + _CellMoveCostMixValues[Mix][0] * 1 ) >> 2;
		else if (MixType == 3)
			return (CellMoveCostMixValues[Mix][1] * 2 + _CellMoveCostMixValues[Mix][0] * 2 ) >> 2;
		else if (MixType == 4)
			return (CellMoveCostMixValues[Mix][1] * 1 + _CellMoveCostMixValues[Mix][0] * 3 ) >> 2;
		else if (MixType == 5)
			return (CellMoveCostMixValues[Mix][1] * 0 + _CellMoveCostMixValues[Mix][0] * 4 ) >> 2;
		else
			return 0;
	}

class TFindTask {
	public MapUnit Unit;
	public int TargetX, TargetY, TargetMaxX, TargetMaxY;
	public float Distance;
	public bool StaticOnly;
	public int[] TargetCost;

	public TFindTask() {
		TargetCost = new int[0x10000];
	}

	~TFindTask() {
		TargetCost = null;
	}

} // TFindTask

//////////////////////////////////////////////////////////


    //**//**//**//
    // Math
	private float sqr(float Value) {
		return Value*Value;
	} // sqr

	private int abs(int A) {
		if (A < 0) return -A;
		return A;
	} // abs

	private int min(int X, int Y) {
		if (X > Y) return Y;
		return X;
	} // max

	private int max(int X, int Y) {
		if (X < Y) return Y;
		return X;
	} // max

	private int DistToPoint(int X1, int Y1, int X2, int Y2) {
		return abs(X1-X2) + abs(Y1-Y2);
	} // DistToPoint

	private int DistToRange(int X, int Y, int MinX, int MinY, int MaxX, int MaxY) {
		if      (X < MinX) X = MinX - X;
		else if (X > MaxX) X = X - MaxX;
		else               X = 0;

		if      (Y < MinY) Y = MinY - Y;
		else if (Y > MaxY) Y = Y - MaxY;
		else               Y = 0;

		return max( X, Y );
	} // DistToRange
	// end Math


	//**//**//**//
	System.Diagnostics.Stopwatch _StopWatch = System.Diagnostics.Stopwatch.StartNew();
	int _ErrCount = 0;
	int _TimeCount = 0;
	int _RunCount = 0;
	long _TickCount = 0;
	long _LastTick = 0;
	long _CheckTime1 = 0;
	long _CheckTime2 = 0;
	long _CheckTime3 = 0;
	DateTime _TimeToLog = DateTime.Now;


	/////////////////////////////
	// MapWizard interface
	/////////////////////////////


struct TUnitData {
	public MapUnit Unit;
	public int TypeNum;
	public int UpdatedX,UpdatedY;
	public int UpdateNum;
} // TUnitData
struct TUnitType {
	public int Width,Height;
	public bool IsWalking,IsFlying;
	public MapUnit ExampleUnit;
} // TUnitType


	const int _CostBad      = 0;
	const int _Cost0        = _CostBad+1;
	const int _CostMax      = 0xFFFFFF;
	const int LowX = 0;
	const int LowY = 0;
	private int HighX,HighY,MapWidth,MapHeight;


	/////////////////////////////
	// map cache
	int LastUnitType = -1;
	private TUnitType[] UnitTypes;
	private byte[][][] CellCosts; // [range(UnitTypeNum)][bool][word]


	/////////////////////////////
	// finds cache
	private TFindTask[] FindTasks;

	//path searching. parameters
	private MapUnit FindingUnit;
	private int FindingUnitTypeNum;
	private int CheckWalkable_Width, CheckWalkable_Height;
	private bool CheckWalkable_IsWalking, CheckWalkable_IsFlying;
	private int StartAddr;
	private Vector2 TargetCenter;
	private float TargetRadius2;

	//path searching. map data
	private ushort[] QueueAddr = new ushort[0x10000]; // [word]
	private int[] QueueCost = new int[0x10000]; // [word]
	private ushort Qin,Qout;
	private int[] TargetCost = new int[0x10000]; // [word]
	private byte[] CellCost; // [word]
	//

	///////////////////////////////
	// service

	private ushort CellAddr(int X, int Y) {
		//return (ushort) (((Y+LowY) << 8) + X+LowX);
		return (ushort) ((Y << 8) + X);
	}

	private bool IsInTargetRadius(int X, int Y) {
		return (sqr(TargetCenter.x-X)+sqr(TargetCenter.y-Y) <= TargetRadius2);
	}

	private Vector2i AddrAsVector(int Addr)
	{
		return new Vector2i((Addr & 0xFF) - LowX , (Addr >> 8) - LowY);
	}

	private List<Vector2i> PointAsPath(int X, int Y)
	{
		List<Vector2i> result = new List<Vector2i>();
		result.Add(new Vector2i(X,Y));
		return result;
	}

	//speed-up starting cost filling
	const uint _MaxCellFreshID = uint.MaxValue-1;
	private uint CellFreshValue = _MaxCellFreshID;
	private uint[] CellFresh;

	private void StartRefreshingCost() {
		if (CellFreshValue == _MaxCellFreshID) {
			CellFreshValue = 0;
			CellFresh = new uint[0x10000]; // fills by 0
			InitTargetCost();
		}
		CellFreshValue++;
	}

	private void RefreshCost(int Addr) {
		if (CellFresh[Addr] == CellFreshValue) return;
		if (CellFresh[Addr] > _MaxCellFreshID) return;
		//if (FindingUnit.Interaction.CheckWalkableForUnit(Addr & 0xFF, Addr >> 8, FindingMode)) { // X,Y,StaticOnly
		if (CellCost[Addr] != _CostBad) {

			//CellCost[Addr] = (byte)_Cost0;
			TargetCost[Addr] = _CostMax;

		} else {

			//CellCost[Addr] = (byte)_CostBad;
			TargetCost[Addr] = _CostBad;

		}
		CellFresh[Addr] = CellFreshValue;
	} // RefreshCost

	private void SetCheckWalkableForUnit(MapUnit Unit) {
		FindingUnit = Unit;
		FindingUnitTypeNum = FindUnitTypeNum(Unit);
		CheckWalkable_Width		= Unit.Width;
		CheckWalkable_Height	= Unit.Height;
		CheckWalkable_IsWalking	= Unit.IsWalking;
		CheckWalkable_IsFlying	= Unit.IsFlying;
	} // SetCheckWalkableForUnit
	private void SetCheckWalkableForUnitTypeNum(int UnitTypeNum) {
		FindingUnit = UnitTypes[UnitTypeNum].ExampleUnit;
		FindingUnitTypeNum = UnitTypeNum;
		CheckWalkable_Width		= UnitTypes[UnitTypeNum].Width;
		CheckWalkable_Height	= UnitTypes[UnitTypeNum].Height;
		CheckWalkable_IsWalking	= UnitTypes[UnitTypeNum].IsWalking;
		CheckWalkable_IsFlying	= UnitTypes[UnitTypeNum].IsFlying;
	} // SetCheckWalkableForUnitTypeNum

	private bool CheckWalkable(int x, int y, bool staticOnly, MapUnit ExcludeUnit) {
		//Unit.Interaction.CheckWalkableForUnit(X, Y, false);
//		if (x < 8 || x > MapLogic.Instance.Width - 8 ||
//		y < 8 || y > MapLogic.Instance.Height - 8) return false;
		for (int ly = y; ly < y + CheckWalkable_Height; ly++) {
			for (int lx = x; lx < x + CheckWalkable_Width; lx++) {
				MapNode node = MapLogic.Instance.Nodes[lx, ly];
				uint tile = node.Tile;
				// skip cells currently taken
				if (ExcludeUnit != null && node.Objects.Contains(ExcludeUnit))
					continue; // if we are already on this cell, skip it as passible
				MapNodeFlags flags = node.Flags;
				if (CheckWalkable_IsWalking && (flags & MapNodeFlags.Unblocked) == 0 && (tile >= 0x1C0 && tile <= 0x2FF))
					return false;
				MapNodeFlags bAir = staticOnly ? MapNodeFlags.BlockedAir : MapNodeFlags.BlockedAir | MapNodeFlags.DynamicAir;
				MapNodeFlags bGround = staticOnly ? MapNodeFlags.BlockedGround : MapNodeFlags.BlockedGround | MapNodeFlags.DynamicGround;
				if (CheckWalkable_IsFlying && (flags & bAir) != 0)
					return false;
				else if (!CheckWalkable_IsFlying && (flags & bGround) != 0)
					return false;
			}
		}
		return true;
	} // CheckWalkable


	public MapWizard() {
		//**//**//**//
		_StopWatch.Start();
	}

	/////////////////////////////
	// units cache
	private int LastUnit = -1;
	private TUnitData[] UnitsData;
	private int LastUnitToUpdate = -1;
	private int[] UnitsToUpdate;

	private void AddUnitUpdate(int WizardID) {
		UnitsData[WizardID].UpdateNum = ++LastUnitToUpdate;
		if (UnitsToUpdate == null)
			UnitsToUpdate = new int[64];
		int Size = UnitsToUpdate.Count();
		if (Size <= LastUnitToUpdate) {
			Array.Resize(ref UnitsToUpdate, Size + (Size >> 1) );
		}
		UnitsToUpdate[LastUnitToUpdate] = WizardID;
	} // AddUnitUpdate

	public void UpdateUnit(MapUnit Unit, ref int WizardID) {
		if (Unit == null && WizardID < 0)
			return;

		if (WizardID < 0) {
			WizardID = ++LastUnit;
			if (UnitsData == null)
				UnitsData = new TUnitData[256];
			int Size = UnitsData.Count();
			if (Size <= LastUnit) {
				Array.Resize(ref UnitsData, Size + (Size >> 1) );
			}
			UnitsData[WizardID].Unit = Unit;
			UnitsData[WizardID].TypeNum = -1;
			UnitsData[WizardID].UpdatedX = -1;
			UnitsData[WizardID].UpdatedY = -1;
			UnitsData[WizardID].UpdateNum = -1;
		}
		if (UnitsData[WizardID].UpdateNum < 0) AddUnitUpdate(WizardID);
			
		if (Unit == null) {
			UnitsData[WizardID].Unit = null;
		}

		return;

	} // UpdateUnit

	private int FindUnitTypeNum(MapUnit Unit) {
		int result = UnitsData[Unit.WizardID].TypeNum;
		if (result >= 0)
			return result;

		for (result = 0; result <= LastUnitType; result++) {
			if (
					UnitTypes[result].Width		== Unit.Width
				&&	UnitTypes[result].Height	== Unit.Height
				&&	UnitTypes[result].IsWalking	== Unit.IsWalking
				&&	UnitTypes[result].IsFlying	== Unit.IsFlying
			)
				goto FoundType;
		}
		result = ++LastUnitType;
		if (UnitTypes == null)
			UnitTypes = new TUnitType[1];
		else
			Array.Resize(ref UnitTypes, LastUnitType+1);
		UnitTypes[result].Width			= Unit.Width;
		UnitTypes[result].Height		= Unit.Height;
		UnitTypes[result].IsWalking		= Unit.IsWalking;
		UnitTypes[result].IsFlying		= Unit.IsFlying;
		UnitTypes[result].ExampleUnit	= Unit;
	FoundType:
		UnitsData[Unit.WizardID].TypeNum = result;

		return result;

	} // FindUnitTypeNum

	private void UpdateCellCostByUnit(MapUnit Unit, int UpdateX, int UpdateY, ref byte[] CellCostWithUnits, ref byte[] CellCostMapOnly, bool ExcludeFindingUnit) {

		if (UpdateX < 0) return;

		MapUnit ExcludeUnit;
		if (ExcludeFindingUnit)
			ExcludeUnit = FindingUnit;
		else
			ExcludeUnit = null;

		int MinX,MinY,MaxX,MaxY;

		MinX = max(UpdateX + 1 - CheckWalkable_Width , LowX);
		MinY = max(UpdateY + 1 - CheckWalkable_Height, LowY);
		MaxX = min(UpdateX + Unit.Width  - 1, HighX - CheckWalkable_Width );
		MaxY = min(UpdateY + Unit.Height - 1, HighY - CheckWalkable_Height);

		for (int Y = MinY; Y <= MaxY; Y++) {
			int Addr = CellAddr(MinX,Y);
			for (int X = MinX; X <= MaxX; X++) {
				if (CellCostMapOnly[Addr] != (byte)_CostBad
					&& CheckWalkable(X, Y, false, ExcludeUnit)
					)
					CellCostWithUnits[Addr] = (byte)_Cost0;
				else
					CellCostWithUnits[Addr] = (byte)_CostBad;
				Addr++;
			}
		}
	} // UpdateCellCostByUnit

	private void ApplyUpdatingUnitData(ref TUnitData UnitData, MapUnit Unit) {
		if (UnitData.TypeNum < 0)
			FindUnitTypeNum(Unit);

		for (int UnitTypeNum = UnitTypes.Count(); --UnitTypeNum >= 0; ) {
			SetCheckWalkableForUnitTypeNum(UnitTypeNum);

			UpdateCellCostByUnit(Unit, UnitData.UpdatedX, UnitData.UpdatedY, ref CellCosts[UnitTypeNum][1], ref CellCosts[UnitTypeNum][0], false );

			UpdateCellCostByUnit(Unit, Unit.X, Unit.Y, ref CellCosts[UnitTypeNum][1], ref CellCosts[UnitTypeNum][0], false);
		}

		UnitData.UpdatedX = Unit.X;
		UnitData.UpdatedY = Unit.Y;
		UnitData.UpdateNum = -1;
	} // ApplyUpdatingUnitData

	private void ApplyUpdatingUnits() {
		int SaveUnitTypeNum = FindingUnitTypeNum;

		for (int UpdateNum = 0; UpdateNum <= LastUnitToUpdate; UpdateNum++) {
			ApplyUpdatingUnitData(ref UnitsData[UnitsToUpdate[UpdateNum]], UnitsData[UnitsToUpdate[UpdateNum]].Unit);
		}
		LastUnitToUpdate = -1;

		SetCheckWalkableForUnitTypeNum(SaveUnitTypeNum);
	} // ApplyUpdatingUnits

	private void CreateUnitTypes() {
		for (int UnitNum = 0; UnitNum <= LastUnit; UnitNum++) {
			FindUnitTypeNum(UnitsData[UnitNum].Unit);
		}
	} // CreateUnitTypes
 	//////

	public void Unload() {
		LastUnitType = -1;
		LastUnit = -1;
		UnitsData = null;
		LastUnitToUpdate = -1;
		UnitsToUpdate = null;
		HighX = -1;
		HighY = -1;
		if (UnitTypes != null) {
			for (int NType=0; NType < UnitTypes.Count(); NType++) {
				UnitTypes[NType].ExampleUnit = null;
			}
		}
		UnitTypes = null;
		if (FindTasks != null) {
			for (int NTask=0; NTask < FindTasks.Count(); NTask++) {
				FindTasks[NTask] = null;
			}
		}
		CellFreshValue = _MaxCellFreshID;
		CellFresh = null;
		FindTasks = null;
		CellCosts = null;
		//TargetCost = null;
		//Queue = null;
	} // Unload


	///////////////////////////////
	// Path finding initializing

	public void InitTargetCost() {
	int AddrY = 0;
	int X,Y;
		//заполнение непроходимой границы
		for (Y = 0; Y <= HighY; Y++) {
			TargetCost[AddrY+0    ] = _CostBad; CellFresh[AddrY+0    ] = _MaxCellFreshID+1;
			TargetCost[AddrY+HighX] = _CostBad; CellFresh[AddrY+HighX] = _MaxCellFreshID+1;
			AddrY += 0x100;
		}
		AddrY = HighY << 8;
		for (X = 0; X <= HighX; X++) {
			TargetCost[00000+X] = _CostBad; CellFresh[00000+X] = _MaxCellFreshID+1;
			TargetCost[AddrY+X] = _CostBad; CellFresh[AddrY+X] = _MaxCellFreshID+1;
		}
	} // InitTargetCost

	public void CreateCellCost(ref byte[][] _CellCosts2) {
		_CellCosts2 = new byte[2][];
		for (int SeeMapOnlyNum = 0; SeeMapOnlyNum <= 1; SeeMapOnlyNum++) {
			_CellCosts2[SeeMapOnlyNum] = new byte[0x10000];
		}
	} // CreateCellCost

	public void LoadCellCost(ref byte [] CellCost, MapUnit Unit, bool SeeMapOnly) {
	int X,Y,MaxX,MaxY;
	int AddrX,AddrY;
		//загрузка проходимости

		SetCheckWalkableForUnit(Unit);

		MaxX = MapWidth  - CheckWalkable_Width;
		MaxY = MapHeight - CheckWalkable_Height;
		AddrY = CellAddr(1,1);
		for (Y = 1; Y <= MaxY; Y++) {
			AddrX = AddrY;
			for (X = 1; X <= MaxX; X++) {
				if (CheckWalkable(X, Y, SeeMapOnly, null))
					CellCost[AddrX] = (byte)_Cost0;
				else
					CellCost[AddrX] = (byte)_CostBad;
				AddrX++;
			}
			AddrY += 0x100;
		}
		//заполнение непроходимой границы
		for (Y = 0; Y <= HighY; Y++) {
			for (X = 0; X < CheckWalkable_Width; X++) {
				CellCost[CellAddr(X         , Y)] = (byte)_CostBad;
				CellCost[CellAddr(HighX - X , Y)] = (byte)_CostBad;
			}
		}
		for (Y = 0; Y < CheckWalkable_Height; Y++) {
			for (X = 0; X <= HighX; X++) {
				CellCost[CellAddr(X ,         Y)] = (byte)_CostBad;
				CellCost[CellAddr(X , HighY - Y)] = (byte)_CostBad;
			}
		}
	} // LoadCellCost

	public void LoadCellCostByTypeNum(int UnitTypeNum) {

		if (UnitTypeNum>=CellCosts.Count()) {
			Array.Resize(ref CellCosts,UnitTypeNum+1);
			CreateCellCost(ref CellCosts[UnitTypeNum]);
		}

		LoadCellCost(ref CellCosts[UnitTypeNum][0], UnitTypes[UnitTypeNum].ExampleUnit, true); // parallel
		LoadCellCost(ref CellCosts[UnitTypeNum][1], UnitTypes[UnitTypeNum].ExampleUnit, false);

	} // LoadCellCostByTypeNum

	public void LoadMap(MapLogic Logic) {
		//**//**//**//
		_LastTick = _StopWatch.ElapsedTicks;

		MapWidth = Logic.Width;
		MapHeight = Logic.Height;
		//LowX = 0;
		HighX = MapWidth-1;
		//if (HighX<254) { LowX++; HighX+=2; }
		//LowY = 0; 
		HighY = MapHeight-1;
		//if (HighY<254) { LowY++; HighY+=2; }

		CreateUnitTypes();

		CellCosts = new byte[UnitTypes.Count()][][];
		for (int UnitTypeNum = 0; UnitTypeNum < UnitTypes.Count(); UnitTypeNum++) {
			CreateCellCost(ref CellCosts[UnitTypeNum]);
			LoadCellCostByTypeNum(UnitTypeNum); // parallel
		}

		//**//**//**//
		_LastTick = _StopWatch.ElapsedTicks - _LastTick;
		_TickCount += _LastTick;
		Debug.LogFormat("WB:Time:Load map: {0}*{1}   ticks={2}, freq={3}",
				Logic.Width, Logic.Height, _LastTick, System.Diagnostics.Stopwatch.Frequency
				);
	} // LoadMap


	///////////////////////////////
	// Path finding 0
	// Dumb version (DROD uses this for default bugs moving)

	private List<Vector2i> PathFind_Dumb(MapUnit Unit, int StartX, int StartY, int TargetX, int TargetY, int TargetMaxX, int TargetMaxY, float Distance, bool StaticOnly)
	{
		int DX = TargetX - StartX;
		int DY = TargetY - StartY;
		if (DX < 0) DX = -1; else if (DX > 0) DX = +1;
		if (DY < 0) DY = -1; else if (DY > 0) DY = +1;
		if                 (Unit.Interaction.CheckWalkableForUnit(StartX+DX, StartY+DY, StaticOnly))
			;//ok
		else if	(DX != 0 && Unit.Interaction.CheckWalkableForUnit(StartX+DX, StartY   , StaticOnly))
			DY = 0;
		else if (DY != 0 && Unit.Interaction.CheckWalkableForUnit(StartX   , StartY+DY, StaticOnly))
			DX = 0;
		else if (DX == 0 && Unit.Interaction.CheckWalkableForUnit(StartX-1 , StartY+DY, StaticOnly))
			DX = -1;
		else if (DX == 0 && Unit.Interaction.CheckWalkableForUnit(StartX+1 , StartY+DY, StaticOnly))
			DX = +1;
		else if	(DY == 0 && Unit.Interaction.CheckWalkableForUnit(StartX+DX, StartY-1 , StaticOnly))
			DY = -1;
		else if (DY == 0 && Unit.Interaction.CheckWalkableForUnit(StartX+DX, StartY+1 , StaticOnly))
			DY = +1;
		else
			return null;
		List<Vector2i> result = new List<Vector2i>();
		result.Insert(0,new Vector2i(StartX+DX, StartY+DY));
		return result;
	} // PathFind_Dumb


	///////////////////////////////
	// Path finding 1
	// Flood version = Fastest path to target
	// debugging //**//

	private void FreeCellCostForUnit(MapUnit Unit) {
		UpdateCellCostByUnit(Unit, Unit.X, Unit.Y, ref CellCosts[FindingUnitTypeNum][1], ref CellCosts[FindingUnitTypeNum][0], true );
	} // FreeCellCostForUnit

	private void Path_AddCell(int Addr, int Cost) {
	//int Qbest;
		RefreshCost(Addr);
		if (TargetCost[Addr] <= Cost) return;
		TargetCost[Addr] = Cost;
		//while
		QueueAddr[Qin] = (ushort)Addr;
		QueueCost[Qin] = Cost;
		Qin++;
		return;
	} // Path_AddCell

	private void Path_FillCost() {
	int Addr;
	int Cost;
		while (Qin != Qout) {
			Addr = QueueAddr[Qout];
			if (Addr == StartAddr) break;
			QueueCost[Qout] = 0;
			Qout++;

			Cost = TargetCost[Addr] + CellCost[Addr];
			Path_AddCell(Addr-0x100-1,Cost);
			Path_AddCell(Addr-0x100  ,Cost);
			Path_AddCell(Addr-0x100+1,Cost);
			Path_AddCell(Addr      -1,Cost);
		//	Path_AddCell(Addr        ,Cost);
			Path_AddCell(Addr      +1,Cost);
			Path_AddCell(Addr+0x100-1,Cost);
			Path_AddCell(Addr+0x100  ,Cost);
			Path_AddCell(Addr+0x100+1,Cost);
		}
	} // Path_FillCost

	private void Path_TakePoint(int Addr, ref int BestAddr, ref int BestCost) {
		RefreshCost(Addr);
		int Cost = TargetCost[Addr];
		if ( Cost != _CostBad  &&  BestCost > Cost) {
			BestCost = Cost;
			BestAddr = Addr;
		}
	} // Path_TakePoint

	private List<Vector2i> Path_TakeBestPath(int StartX, int StartY) {
		int Addr = CellAddr(StartX,StartY);
		RefreshCost(Addr);
		if ( TargetCost[Addr] == _CostBad  ||  TargetCost[Addr] == _CostMax )
			return null;
		int BestCost;
		int BestAddr=0;
		List<Vector2i> result = new List<Vector2i>();

		while (TargetCost[Addr] != _Cost0) {
			BestCost = _CostMax;
			Path_TakePoint(Addr-0x100-1,ref BestAddr,ref BestCost); // -1,-1
			Path_TakePoint(Addr-0x100  ,ref BestAddr,ref BestCost); //  0,-1
			Path_TakePoint(Addr-0x100+1,ref BestAddr,ref BestCost); // +1,-1
			Path_TakePoint(Addr      -1,ref BestAddr,ref BestCost); // -1, 0
		//	Path_TakePoint(Addr        ,ref BestAddr,ref BestCost); //  0, 0
			Path_TakePoint(Addr      +1,ref BestAddr,ref BestCost); // +1, 0
			Path_TakePoint(Addr+0x100-1,ref BestAddr,ref BestCost); // -1,+1
			Path_TakePoint(Addr+0x100  ,ref BestAddr,ref BestCost); //  0,+1
			Path_TakePoint(Addr+0x100+1,ref BestAddr,ref BestCost); // +1,+1

			result.Add(AddrAsVector(BestAddr));
			Addr = BestAddr;

		}
		if (result.Count() == 0)
			result = null;
		return result;
	} // Path_TakeBestPath


	private void Dump(int X,int Y) {
		int Addr = CellAddr(X,Y);
		Debug.LogFormat("around point {25},{26}  costs: {0},{1},{2},{3},{4}  {5},{6},{7},{8},{9}  {10},{11},{12},{13},{14}   {15},{16},{17},{18},{19}  {20},{21},{22},{23},{24}",
				TargetCost[Addr-0x200-2],TargetCost[Addr-0x200-1],TargetCost[Addr-0x200  ],TargetCost[Addr-0x200+1],TargetCost[Addr-0x200+2],
				TargetCost[Addr-0x100-2],TargetCost[Addr-0x100-1],TargetCost[Addr-0x100  ],TargetCost[Addr-0x100+1],TargetCost[Addr-0x100+2],
				TargetCost[Addr-0x000-2],TargetCost[Addr-0x000-1],TargetCost[Addr-0x000  ],TargetCost[Addr-0x000+1],TargetCost[Addr-0x000+2],
				TargetCost[Addr+0x100-2],TargetCost[Addr+0x100-1],TargetCost[Addr+0x100  ],TargetCost[Addr+0x100+1],TargetCost[Addr+0x100+2],
				TargetCost[Addr+0x200-2],TargetCost[Addr+0x200-1],TargetCost[Addr+0x200  ],TargetCost[Addr+0x200+1],TargetCost[Addr+0x200+2],
				X,Y
				);
	} // Dump

	private List<Vector2i> PathFind_Flood(MapUnit Unit, int StartX, int StartY, int TMinX, int TMinY, int TMaxX, int TMaxY, float Distance, bool StaticOnly)
	{
	int X,Y,AddrX,AddrY,MinX,MinY,MaxX,MaxY;
		//**//**//**//
		if (Unit.WizardID < 0 || Unit.WizardID >= UnitsData.Count())
			throw new Exception("MapWizard: bad unit WizardID");
		//**//**//**//
		_RunCount++;
		_LastTick = _StopWatch.ElapsedTicks;

		StartAddr = CellAddr(StartX,StartY);

		TargetCenter = new Vector2((TMinX+TMaxX)/2, (TMinY+TMaxY)/2);
		if (Distance < 2) Distance = 0;
		TargetRadius2 = Distance * Distance;
		MinX = (int)(TargetCenter.x - Distance);		if (MinX > TMinX) MinX = TMinX;		if (MinX < LowX ) MinX = LowX;
		MaxX = (int)(TargetCenter.x + Distance);		if (MaxX < TMaxX) MaxX = TMaxX;		if (MaxX > HighX) MaxX = HighX;
		MinY = (int)(TargetCenter.y - Distance);		if (MinY > TMinY) MinY = TMinY;		if (MinY < LowY ) MinY = LowY;
		MaxY = (int)(TargetCenter.y + Distance);		if (MaxY < TMaxY) MaxY = TMaxY;		if (MaxY > HighY) MaxY = HighY;

		bool NeedCheckRadius =	TargetRadius2 > 1 &&
					(	!IsInTargetRadius(MinX,MinY)
					 ||	!IsInTargetRadius(MaxX,MaxY)
					);
		if (
			StartX >= MinX && StartX <= MaxX && StartY >= MinY && StartY <= MaxY // already in Rectangle
			&& (!NeedCheckRadius || IsInTargetRadius(StartX,StartY)) // already in Distance
		)
			return PointAsPath(StartX,StartY);

		ApplyUpdatingUnits();

		SetCheckWalkableForUnit(Unit);

		//написать поиск недавних задач поиска
		//private TFindTask[] FindTasks = new TFindTask[2];
		//0/0
		//Array.Copy(UnitTypes[FindingUnitTypeNum],TargetCost);

		if (StaticOnly)
			CellCost = CellCosts[FindingUnitTypeNum][0];
		else	{
			CellCost = CellCosts[FindingUnitTypeNum][1];
			FreeCellCostForUnit(Unit);
		}

		StartRefreshingCost();

		Qin = Qout = 0;

		AddrY = CellAddr(MinX,MinY);
		for (Y = MinY; Y <= MaxY; Y++) {
			AddrX = AddrY;
			for (X = MinX; X <= MaxX; X++) {
				if (NeedCheckRadius && !IsInTargetRadius(X,Y))
					;//skip
				else
				{
					//TargetCost[Addr+X] = CellCost[0,Addr+X];
					Path_AddCell(AddrX,_Cost0);
				}
				AddrX++;
			}
			AddrY += 0x100;
		}
		//**//**//**//
		long _Last1 = _StopWatch.ElapsedTicks;

		Path_FillCost();
		long _Last2 = _StopWatch.ElapsedTicks;

		List<Vector2i> BestPath = Path_TakeBestPath(StartX,StartY);

		{
		int DummyWizardID = Unit.WizardID;
		UpdateUnit(Unit, ref DummyWizardID);
		}

		//**//**//**//
		long _Last3 = _StopWatch.ElapsedTicks;
		_Last3 = _Last3-_Last2;
		_Last2 = _Last2-_Last1;
		_Last1 = _Last1-_LastTick;
		_LastTick = _StopWatch.ElapsedTicks - _LastTick;
		_TickCount += _LastTick;
		_CheckTime1 += _Last1;
		_CheckTime2 += _Last2;
		_CheckTime3 += _Last3;
		if (_TimeToLog <= DateTime.Now ) {
			Debug.LogFormat("runs: count= {0}  ticks={1} > {2}  freq={3},  1st={4}  2st={5}  3st={6}",
				_RunCount, _LastTick, _TickCount, System.Diagnostics.Stopwatch.Frequency,
				_CheckTime1/_RunCount, _CheckTime2/_RunCount, _CheckTime3/_RunCount
			);
			_RunCount = 0;
			_CheckTime1 = 0;
			_CheckTime2 = 0;
			_CheckTime3 = 0;
			_TimeToLog = DateTime.Now;
			_TimeToLog = _TimeToLog.Add(new TimeSpan(0,0,1));
		}
		//

		if (BestPath != null)
		{
		//**//**//**//
		//Debug.LogFormat("find path for ID {0}  to {1},{2}  dist={3}  static={4}   result={5},{6}",
		//		Unit.ID,  TargetX,TargetY, Distance,StaticOnly, BestPoint.x,BestPoint.y
		//		);
		//Dump(TargetX,TargetY);
		//**//**//**//
		//if (!Unit.Interaction.CheckWalkableForUnit(BestPath[0].x,BestPath[0].y, StaticOnly)) {
		//Debug.LogFormat("path find bad: start={0},{1}  target={2},{3}  result={4},{5} Dist={6} static={7}",
		//		StartX,StartY, TMinX,TMinY, BestPath[0].x,BestPath[0].y, Distance,StaticOnly
		//		);
		//Dump(StartX,StartY);
		//Dump(TMinX+1,TMinY+1);
		//}

	                return BestPath;
		}

		//**//**//**//
		//Debug.LogFormat("path find: start={0},{1}  target={2},{3}  result={4},{5} Dist={6} static={7}",
		//		StartX,StartY, TargetX,TargetY, BestPoint.x,BestPoint.y, Distance,StaticOnly
		//		);
		//Dump(TargetX,TargetY);
		//**//**//**//
		//bool _DoDebug = (StartX >= 18 && StartX <= 20 && StartY >= 16 && StartY <= 18);
		//if (_DoDebug && BestPoint == null) {
		//Debug.LogFormat("path find: start={0},{1}  target={2},{3}  result={4},{5} Dist={6} static={7}",
		//		StartX,StartY, TargetX,TargetY, "NULL","NULL", Distance,StaticOnly
		//		);
		//Dump(TargetX,TargetY);
		//}
		//if (_DoDebug && BestPoint != null) {
		//Debug.LogFormat("path find: start={0},{1}  target={2},{3}  result={4},{5} Dist={6} static={7}",
		//		StartX,StartY, TargetX,TargetY, BestPoint.x,BestPoint.y, Distance,StaticOnly
		//		);
		//Dump(TargetX,TargetY);
		//}

		return PathFind_Dumb(Unit, StartX, StartY, TMinX, TMinY, TMaxX, TMaxY, Distance, StaticOnly);//**//**//**//
		//return null;
	} // PathFind_Flood


	///////////////////////////////


	public List<Vector2i> GetShortestPath(MapUnit Unit, bool StaticOnly, float Distance, int StartX, int StartY, int TargetX, int TargetY, int TargetWidth, int TargetHeight)
	{

	//return PathFind_Dumb(Unit, StartX, StartY, TargetX, TargetY, TargetWidth, TargetHeight, Distance, StaticOnly);

	return PathFind_Flood(Unit, StartX, StartY, TargetX, TargetY, TargetWidth, TargetHeight, Distance, StaticOnly);

	} // GetShortestPath
	
} // MapWizard
