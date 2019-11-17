//WB_pathfind developing
//#define NoMapWizardUsing

//кривоты:
//разливка
//после переписывания уже найденной стоимости НЕ запрещается путь из нее туда, откуда была записана переписанная.
//это ведет к построению НЕбыстрейшего пути

//Debug.LogFormat("path find err: {0}, {1} =", X, Y);

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

//подумать насчет сохранения карт доступности

class MapWizard
{

#if (NoMapWizardUsing)
	public void Unload() {}
	public void LoadMap(MapLogic Logic) {}
	public void UpdateNode(int NodeX, int NodeY, MapNode Node) {}
	public List<Vector2i> GetShortestPath(MapUnit Unit, bool StaticOnly, float Distance, int StartX, int StartY, int TargetX, int TargetY, int TargetWidth, int TargetHeight, int Limit) {return null;}
#else

// !!! предпосылки:
//	размеры карты не больше 256*256
//	карта по краям ограничена для всех непроходимыми клетками
//  сейчас это 8 слева-сверху и 7 справа-снизу


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
	private byte[,] _CellMoveCostMixValues = new byte[0x10,2]  //char a54126[0x10][2] =
	{ {2, 1}, {5, 1}, {4, 1}, {7, 1}, {6, 1}, {5, 6}, {3, 7}, {8, 6},
	  {HZ,HZ},{HZ,HZ},{HZ,HZ},{HZ,HZ},{10,1 },{HZ,HZ},{HZ,HZ},{HZ,HZ}
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
	0,	//dummy
	0	//dummy
	};

	public int TileMoveCost(int tile) { // 0 == unwalkable tile
		if (tile >= 0x1C0 && tile <= 0x2FF)
			return 0;
		int Mix = (tile >> 6) & 0x3;
		int Cost1 = _CellCostBase[_CellMoveCostMixValues[Mix,1]];
		int Cost2 = _CellCostBase[_CellMoveCostMixValues[Mix,0]];
		int MixType = _CellMoveCostMixType[tile & 0x3F];
		if      (MixType == 1)
			return (Cost1 * 4 + Cost2 * 0 ) >> 2;
		else if (MixType == 2)
			return (Cost1 * 3 + Cost2 * 1 ) >> 2;
		else if (MixType == 3)
			return (Cost1 * 2 + Cost2 * 2 ) >> 2;
		else if (MixType == 4)
			return (Cost1 * 1 + Cost2 * 3 ) >> 2;
		else if (MixType == 5)
			return (Cost1 * 0 + Cost2 * 4 ) >> 2;
		else
			return 0;
	}

/*class TFindTask {
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

} // TFindTask*/

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
	long _CheckTimeSort = 0;
	long _CheckWorkSort = 0;
	long _CheckMaxWorkSort = 0;
	long _CheckLenSort = 0;
	long _CheckMaxLenSort = 0;
	DateTime _TimeToLog = DateTime.Now;
	int [] Stat1 = new int [0x10000];
	int [] Stat2 = new int [0x10000];
	int [] Stat3 = new int [0x10000];
	int [] Stat4 = new int [0x10000];
	int [] Stat5 = new int [0x10000];


	/////////////////////////////
	// MapWizard interface
	/////////////////////////////

	const int _CostBad      = 0;
	const int _Cost0        = _CostBad+1;
	const int _CostMax      = 0xFFFFFF;
	private int[,] _CostOneMove1,_CostOneMoveSqrt2; // [range(BaseTileWalkCost),range(BaseTileWalkCost)]


	/////////////////////////////
	// map

	private int MapWidth,MapHeight;
	const int LowX = 8;
	const int LowY = 8;
	private int HighX,HighY;

	public void InitBorders() {

        //if (x < 8 || x > MapLogic.Instance.Width - 8 ||
        //    y < 8 || y > MapLogic.Instance.Height - 8) return false;

		//
		HighX = MapWidth-8;
		//if (HighX<254) { LowX++; HighX+=2; }
		//LowY = 0; 
		HighY = MapHeight-8;
		//if (HighY<254) { LowY++; HighY+=2; }

		MapCellsPass = new byte [0x10000];
		//заполнение непроходимой границы
		int Addr = 0;
		for (int Y = 0; Y < MapHeight; Y++) {
			Addr = CellAddr(0,Y);
			for (int X = 0; X < MapWidth; X++) {
				if (X < LowX || X > HighX
				 || Y < LowY || Y > HighY) {
					MapCellsPass[Addr] = _CellPass_No;
				}
				Addr++;
			}
		}
	} // InitBorders


	/////////////////////////////
	// map cache
	int LastUnitType = -1;
	private TUnitType[] UnitTypes = new TUnitType[0];
	private byte[] MapCellsCost = new byte[0x10000];
	private byte[] MapCellsPass = new byte[0x10000];
	private byte[][] AllCellsCost; // [range(UnitTypeNum)][word]
	private byte[][] AllCellsPass; // [range(UnitTypeNum)][word]

	private const byte _CellPass_Walking		= 0x01;
	private const byte _CellPass_Swiming		= 0x02;
	private const byte _CellPass_Flying			= 0x04;
    private const byte _CellPass_WalkingMapOnly = 0x20;
    private const byte _CellPass_SwimingMapOnly = 0x40;
    private const byte _CellPass_FlyingMapOnly  = 0x80;
    private const byte _CellPass_No             = 0xFF;
//unchecked{
	private const byte NOT_CellPass_Walking			= unchecked((byte)~_CellPass_Walking);
	private const byte NOT_CellPass_Swiming			= unchecked((byte)~_CellPass_Swiming);
	private const byte NOT_CellPass_Flying			= unchecked((byte)~_CellPass_Flying);
	private const byte NOT_CellPass_WalkingMapOnly	= unchecked((byte)~_CellPass_WalkingMapOnly);
	private const byte NOT_CellPass_SwimingMapOnly	= unchecked((byte)~_CellPass_SwimingMapOnly);
	private const byte NOT_CellPass_FlyingMapOnly	= unchecked((byte)~_CellPass_FlyingMapOnly);
//}	

struct TUnitType {
	public int Width,Height;
	public bool IsWalking,IsFlying;
	public byte CellPass;
	public byte CellPassMapOnly;
//	public MapUnit ExampleUnit;
} // TUnitType


	/////////////////////////////
	// finds cache
	//private TFindTask[] FindTasks;

	//path searching. parameters
	private MapUnit FindingUnit;
	private TUnitType FindingUnitType;
	private int FindingUnitTypeNum;
	private byte FindingCellPass;
	private int StartAddr;
	private Vector2 TargetCenter;
	private float TargetRadius2;

	//path searching. map data
	private ushort[] QueueAddr = new ushort[0x10000]; // [word]
	private int[] QueueCost = new int[0x10000]; // [word]
	private ushort Qin,Qout;
	private int[] TargetCost = new int[0x10000]; // [word]
	private byte[] FindingCellsCost; // [word]
	private byte[] FindingCellsPass; // [word]
	//

	///////////////////////////////
	// service

	public MapWizard() {
		//**//**//**//
		_StopWatch.Start();
		_CostOneMove1 = new int [0x100,0x100];
		_CostOneMoveSqrt2 = new int [0x100,0x100];

		for (int C1 = 1; C1 < 0x100; C1++) {
		for (int C2 = 1; C2 < 0x100; C2++) {
			int Cost = (C1+C2);
			_CostOneMove1[C1,C2] = Cost * 10/2;		// для уменьшения округления
			_CostOneMoveSqrt2[C1,C2] = Cost * 14/2;	// sqrt(2) * множитель1
		}}

	}

	public void Unload() {
		LastUnitType = -1;
		HighX = -1;
		HighY = -1;
		UnitTypes = new TUnitType[0];
		//if (FindTasks != null) {
		//	for (int NTask=0; NTask < FindTasks.Count(); NTask++) {
		//		FindTasks[NTask] = null;
		//	}
		//}
		//FindTasks = null;
		CellFreshValue = _MaxCellFreshID;
		CellFresh = null;
		AllCellsCost = null;
		FindingCellsCost = null;
		AllCellsPass = null;
		FindingCellsPass = null;
		//TargetCost = null;
		//Queue = null;
	} // Unload

	private ushort CellAddr(int X, int Y) {
		//return (ushort) (((Y+LowY) << 8) + X+LowX);
		return (ushort) ((Y << 8) + X);
	}

	private bool IsInTargetRadius(int X, int Y) {
		return (sqr(TargetCenter.x-X)+sqr(TargetCenter.y-Y) <= TargetRadius2);
	}

	private Vector2i AddrAsVector(int Addr)
	{
		return new Vector2i(Addr & 0xFF , Addr >> 8);
	}

	private List<Vector2i> PointAsPath(int X, int Y)
	{
		List<Vector2i> result = new List<Vector2i>();
		result.Add(new Vector2i(X,Y));
		return result;
	}

	private void SetFindingUnit(MapUnit Unit, bool SeeMapOnly) {
		FindingUnit = Unit;
		FindingUnitTypeNum = FindUnitTypeNum(Unit);
		FindingUnitType = UnitTypes[FindingUnitTypeNum];
		FindingCellsCost = AllCellsCost[FindingUnitTypeNum];
		FindingCellsPass = AllCellsPass[FindingUnitTypeNum];
 		if (SeeMapOnly) {
			FindingCellPass = UnitTypes[FindingUnitTypeNum].CellPassMapOnly;
        } else {
			FindingCellPass = (byte)(UnitTypes[FindingUnitTypeNum].CellPass | UnitTypes[FindingUnitTypeNum].CellPassMapOnly);
			FreeCellsUnderFindingUnit();
		}	
	} // SetFindingUnit

	private int FindUnitTypeNum(MapUnit Unit) {
	int result;
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
		Array.Resize(ref UnitTypes, LastUnitType+1);
		UnitTypes[result].Width			= Unit.Width;
		UnitTypes[result].Height		= Unit.Height;
		UnitTypes[result].IsWalking		= Unit.IsWalking;
		UnitTypes[result].IsFlying		= Unit.IsFlying;
		//UnitTypes[result].ExampleUnit	= Unit;
		if      (UnitTypes[result].IsWalking) {
        	UnitTypes[result].CellPass			= _CellPass_Walking;
        	UnitTypes[result].CellPassMapOnly	= _CellPass_WalkingMapOnly;
        } else if (UnitTypes[result].IsFlying) {
        	UnitTypes[result].CellPass			= _CellPass_Flying;
        	UnitTypes[result].CellPassMapOnly	= _CellPass_FlyingMapOnly;
        } else {
        	UnitTypes[result].CellPass			= _CellPass_Swiming;
        	UnitTypes[result].CellPassMapOnly	= _CellPass_SwimingMapOnly;
        }

        LoadCellsCostPassForTypeNum(result);
        
	FoundType:

		return result;

	} // FindUnitTypeNum


	///////////////////////////////
	// Path finding initializing

	public void LoadMap(MapLogic Logic) {
		_LastTick = _StopWatch.ElapsedTicks;
		_CheckMaxLenSort = 0;
		_CheckMaxWorkSort = 0;

		MapWidth = Logic.Width;
		MapHeight = Logic.Height;
		InitBorders();

		AllCellsCost = new byte[0][];
		AllCellsPass = new byte[0][];

		StartRefreshingCost();

		LoadMapCellsData();

		for (int UnitTypeNum = 0; UnitTypeNum < UnitTypes.Count(); UnitTypeNum++) {
			LoadCellsCostPassForTypeNum(UnitTypeNum);
		}	

		//**//**//**//
		_LastTick = _StopWatch.ElapsedTicks - _LastTick;
		_TickCount += _LastTick;
		Debug.LogFormat("WB:Time:Load map: {0}*{1}   ticks={2}, freq={3}",
				Logic.Width, Logic.Height, _LastTick, System.Diagnostics.Stopwatch.Frequency
				);
	} // LoadMap

	private void LoadMapCellsData() {
	int X,Y;
	int Addr;
	byte CellPass;
	MapNode[,] Nodes = MapLogic.Instance.Nodes;
		//загрузка проходимости
		MapCellsPass = new byte[0x10000];

		for (Y = LowY; Y <= HighY; Y++) {
			Addr = CellAddr(LowX,Y);
			for (X = LowX; X <= HighX; X++) {
				//  constant node flags only

				MapNode Node = Nodes[X,Y];

				MapCellsCost[Addr] = (byte)max(Node.BaseWalkCost,1);

				MapCellsPass[Addr] = CalcNodeCellPass(Node,0);

				Addr++;

			}
		}
	} // LoadMapCellsData

	public void LoadCellsCostPassForTypeNum(int UnitTypeNum) {

		if (AllCellsCost == null) return;

		if (UnitTypeNum >= AllCellsCost.Count()) {
			Array.Resize(ref AllCellsCost,UnitTypeNum+1);
			AllCellsCost[UnitTypeNum] = new byte[0x10000];
			Array.Resize(ref AllCellsPass,UnitTypeNum+1);
			AllCellsPass[UnitTypeNum] = new byte[0x10000];
		}

		LoadCellsCostPass(ref AllCellsCost[UnitTypeNum], ref AllCellsPass[UnitTypeNum], UnitTypeNum);

	} // LoadCellCostsPassesForTypeNum

	public void LoadCellsCostPass(ref byte[] CellsCost, ref byte[] CellsPass, int UnitTypeNum) {
	int X,Y,MaxX,MaxY;
	int Width,Height;
	int Addr;
		//загрузка проходимости для типа юнита


		Width  = UnitTypes[UnitTypeNum].Width;
		Height = UnitTypes[UnitTypeNum].Height;

		if (Width <= 1 && Height <= 1) {
			MapCellsCost.CopyTo(CellsCost,0);
			MapCellsPass.CopyTo(CellsPass,0);
		} else {
			MaxX = HighX - Width +1;
			MaxY = HighY - Height+1;
			for (Y = LowY; Y <= MaxY; Y++) {
				for (X = LowX; X <= MaxX; X++) {
					Addr = CellAddr(X,Y);
					CalcCellCostPass(X, Y, Width, Height, ref CellsCost[Addr], ref CellsPass[Addr], UnitTypeNum);
				}
			}
		}
	} // LoadCellCostPass

	private void CalcCellCostPass(int X, int Y, int Width, int Height, ref byte CellCostVal, ref byte CellPassVal, int UnitTypeNum) {

		int CellCostInt = 0;
		CellPassVal = 0;

		for (int UnitY = Height; --UnitY >= 0; ) {
			for (int UnitX = Width; --UnitX >= 0; ) {
				int Addr = CellAddr(X+UnitX,Y+UnitY);
				CellCostInt += MapCellsCost[Addr];
				CellPassVal |= MapCellsPass[Addr];
			}
		}
		if (UnitTypes[UnitTypeNum].IsWalking)
			CellCostVal = (byte)(CellCostInt / (Width*Height) + _Cost0);
		else
			CellCostVal = (byte)5;
	} // CalcCellCostPass


	///////////////////////////////
	// updating last cell costs

	public byte CalcNodeCellPass(MapNode Node, byte result) {

		ushort Tile = Node.Tile;

		if ((Node.Flags & MapNodeFlags.BlockedAir) != 0)
			result |= _CellPass_FlyingMapOnly;
		else	
			result &= NOT_CellPass_FlyingMapOnly;

		if (	(Node.Flags & MapNodeFlags.BlockedGround) != 0
			|| 
				Tile >= 0x1C0 && Tile <= 0x2FF
				&& (Node.Flags & MapNodeFlags.Unblocked) == 0
		)
			result |= _CellPass_WalkingMapOnly | _CellPass_SwimingMapOnly;
		else
			result &= NOT_CellPass_WalkingMapOnly & NOT_CellPass_SwimingMapOnly;
			
		if ((Node.Flags & MapNodeFlags.DynamicAir) != 0)
			result |= _CellPass_Flying;
		else
			result &= NOT_CellPass_Flying;

		if ((Node.Flags & MapNodeFlags.DynamicGround) != 0)
			result |= _CellPass_Walking | _CellPass_Swiming;
		else
			result &= NOT_CellPass_Walking & NOT_CellPass_Swiming;

		return result;
	} // CalcNodeCellPass

	public void UpdateNode(int NodeX, int NodeY, MapNode Node) { // NEED Spells value
	//**//**//**//
	return;
		//**//**//**//
		long _Last1 = _StopWatch.ElapsedTicks;

		//map is loading
		if (AllCellsCost == null) {
			foreach (MapObject Object in Node.Objects) {
				if (Object is MapUnit) {
					FindUnitTypeNum((MapUnit)Object);
				}
			}
			return;
		}	

	int X,Y,MinX,MinY,Width,Height;
	int Addr;
	byte CellPass,ChangeCellPass,CellPassMapOnly;
	int UnitTypeNum;

		Addr = CellAddr(NodeX,NodeY);
		ushort Tile = Node.Tile;
		CellPass = CalcNodeCellPass(Node, MapCellsPass[Addr]);

		ChangeCellPass = (byte)(MapCellsPass[Addr] ^ CellPass);
		MapCellsPass[Addr] = CellPass;

		//**//**//**//
		// учесть тормозные колдовства на клетке
		// ...
		//

		// обновить по типам

		for (UnitTypeNum = 0; UnitTypeNum < UnitTypes.Count(); UnitTypeNum++) {
			CellPass = (byte)(UnitTypes[UnitTypeNum].CellPass | UnitTypes[UnitTypeNum].CellPassMapOnly);
			CellPassMapOnly = UnitTypes[UnitTypeNum].CellPassMapOnly;
			if (((CellPass | CellPassMapOnly) & ChangeCellPass) != 0) {
				Width = UnitTypes[UnitTypeNum].Width;
				Height = UnitTypes[UnitTypeNum].Height;
	
				MinX = max(NodeX + 1 - Width, LowX);
				MinY = max(NodeY + 1 - Height, LowY);
		
				for (Y = MinY; Y <= NodeY; Y++) {
					for (X = MinX; X <= NodeX; X++) {
						Addr = CellAddr(X,Y);
						CalcCellCostPass(X, Y, Width, Height, ref AllCellsCost[UnitTypeNum][Addr], ref AllCellsPass[UnitTypeNum][Addr], UnitTypeNum);
					}
				}
			}
		}
		//**//**//**//
		_Last1 = _StopWatch.ElapsedTicks - _Last1;
		_CheckTime1 += _Last1;
	} // UpdateNode


	///////////////////////////////
	//speed-up starting filling

	const uint _MaxCellFreshID = uint.MaxValue-1;
	private uint CellFreshValue = _MaxCellFreshID;
	private uint[] CellFresh;

	private void StartRefreshingCost() {
		if (CellFreshValue == _MaxCellFreshID) {
			CellFreshValue = 0;
			CellFresh = new uint[0x10000]; // fills by 0

			//заполнение непроходимой границы
			int Addr = 0;
			for (int Y = 0; Y < MapHeight; Y++) {
				Addr = CellAddr(0, Y);
				for (int X = 0; X < MapWidth; X++) {
					if (MapCellsPass[Addr] == _CellPass_No) {
						TargetCost[Addr] = _CostBad;
						CellFresh[Addr] = _MaxCellFreshID+1;
					}	
					Addr++;
				}
			}	
		}
		CellFreshValue++;
	}

	private void RefreshCost(int Addr) {
		if (CellFresh[Addr] == CellFreshValue) return;
		if (CellFresh[Addr] > _MaxCellFreshID) return;

		//if (FindingUnit.Interaction.CheckWalkableForUnit(Addr & 0xFF, Addr >> 8, FindingMode)) { // X,Y,StaticOnly
		if ((FindingCellsPass[Addr] & FindingCellPass) == 0) {

			//CellCost[Addr] = (byte)_Cost0;
			TargetCost[Addr] = _CostMax;

		} else {

			//CellCost[Addr] = (byte)_CostBad;
			TargetCost[Addr] = _CostBad;

		}
		CellFresh[Addr] = CellFreshValue;
	} // RefreshCost


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

	private void FreeCellsUnderFindingUnit() {
		int Addr;
		int Width = UnitTypes[FindingUnitTypeNum].Width;
		int Height = UnitTypes[FindingUnitTypeNum].Height;
		int MinX = max(FindingUnit.X + 1 - Width , LowX);
		int MinY = max(FindingUnit.Y + 1 - Height, LowY);
		int X,Y;
		for (Y = Height; --Y >= 0; ) {
			for (X = Width; --X >= 0; ) {
				Addr = CellAddr(FindingUnit.X+X, FindingUnit.Y+Y);
				MapCellsPass[Addr] = 0;
			}
		}
		for (Y = FindingUnit.Y+Height; --Y >= MinY; ) {
			for (X = FindingUnit.X+Width; --X >= MinX; ) {
				Addr = CellAddr(X, Y);
				byte Dummy=0;
				CalcCellCostPass(X, Y, Width, Height, ref Dummy, ref FindingCellsPass[Addr], FindingUnitTypeNum);
				RefreshCost(Addr);
			}
		}
	} // FreeCellsUnderFindingUnit

	private void RestoreCellsUnderFindingUnit() {
		int Addr;
		int Width = UnitTypes[FindingUnitTypeNum].Width;
		int Height = UnitTypes[FindingUnitTypeNum].Height;
		int MinX = max(FindingUnit.X + 1 - Width , LowX);
		int MinY = max(FindingUnit.Y + 1 - Height, LowY);
		for (int Y = FindingUnit.Y+Height; --Y >= MinY; ) {
			for (int X = FindingUnit.X+Width; --X >= MinX; ) {
				UpdateNode(X, Y, MapLogic.Instance.Nodes[X,Y]);
			}
		}
	} // RestoreCellsUnderFindingUnit

	private void Path_AddCell(int Addr, int Cost, byte CellCost1, int [,] CostOneMove) {
		RefreshCost(Addr);
		if (TargetCost[Addr] != _CostMax ) return;
		Cost += CostOneMove[CellCost1,FindingCellsCost[Addr]];
			//**//**//**//
			//if (TargetCost[Addr] == _CostBad ) Stat2[Addr]++;
		if (TargetCost[Addr] <= Cost) return;
			//**//**//**//
			//if (TargetCost[Addr] < _CostMax) Stat3[Addr]++;
		TargetCost[Addr] = Cost;

		//QueueAddr[Qin] = (ushort)Addr;
		//QueueCost[Qin] = Cost;
		//Qin++;

		QueueCost[Qout-1] = Cost;
		//**//**//**//
		long _LastTick = _StopWatch.ElapsedTicks;
		long Len;
		
		int QPlace = Qin;
		while (QueueCost[--QPlace] > Cost) ; // all made in condition
		if (++QPlace < Qin) {
			Array.Copy(QueueAddr,QPlace, QueueAddr,QPlace+1,Qin-QPlace);
			Array.Copy(QueueCost,QPlace, QueueCost,QPlace+1,Qin-QPlace);
			//**//**//**//
			Len = Qin-QPlace;
			_CheckWorkSort += Len;
			if (_CheckMaxWorkSort < Len) _CheckMaxWorkSort = Len;
		}
		//**//**//**//
		_CheckTimeSort += _StopWatch.ElapsedTicks - _LastTick;
		Len = Qin-Qout;
		_CheckLenSort += Len;
		if (_CheckMaxLenSort < Len) _CheckMaxLenSort = Len;

		QueueCost[QPlace] = Cost;
		QueueAddr[QPlace] = (ushort)Addr;
		Qin++;

		return;
	} // Path_AddCell

	private void Path_FillCost() {
	int Addr;
		while (Qin != Qout) {
			Addr = QueueAddr[Qout];
			//**//**//**//
			//Stat1[Addr]++;

			if (Addr == StartAddr) break;
			QueueCost[Qout] = 0;
			Qout++;

			int Cost = TargetCost[Addr];
			byte SrcCellCost = FindingCellsCost[Addr];
		//	Path_AddCell(Addr        ,Cost,SrcCellCost,);
			Path_AddCell(Addr-0x100  ,Cost,SrcCellCost,_CostOneMove1);
			Path_AddCell(Addr+0x100  ,Cost,SrcCellCost,_CostOneMove1);
			Path_AddCell(Addr      -1,Cost,SrcCellCost,_CostOneMove1);
			Path_AddCell(Addr      +1,Cost,SrcCellCost,_CostOneMove1);
			Path_AddCell(Addr-0x100-1,Cost,SrcCellCost,_CostOneMoveSqrt2);
			Path_AddCell(Addr-0x100+1,Cost,SrcCellCost,_CostOneMoveSqrt2);
			Path_AddCell(Addr+0x100-1,Cost,SrcCellCost,_CostOneMoveSqrt2);
			Path_AddCell(Addr+0x100+1,Cost,SrcCellCost,_CostOneMoveSqrt2);
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
			//**//**//**//
			if (result.Count()>1000) {
				result = new List<Vector2i>();
				result.Add(AddrAsVector(StartAddr));
				return result;
			}	

		}
		if (result.Count() == 0)
			result = null;
		return result;
	} // Path_TakeBestPath


	//**//**//**//
	private void LogDump(int X,int Y) {
		int Addr = CellAddr(X,Y);
		Debug.LogFormat("around point {25},{26}  costs: {0},{1},{2},{3},{4}  {5},{6},{7},{8},{9}  {10},{11},{12},{13},{14}   {15},{16},{17},{18},{19}  {20},{21},{22},{23},{24}",
				TargetCost[Addr-0x200-2],TargetCost[Addr-0x200-1],TargetCost[Addr-0x200  ],TargetCost[Addr-0x200+1],TargetCost[Addr-0x200+2],
				TargetCost[Addr-0x100-2],TargetCost[Addr-0x100-1],TargetCost[Addr-0x100  ],TargetCost[Addr-0x100+1],TargetCost[Addr-0x100+2],
				TargetCost[Addr-0x000-2],TargetCost[Addr-0x000-1],TargetCost[Addr-0x000  ],TargetCost[Addr-0x000+1],TargetCost[Addr-0x000+2],
				TargetCost[Addr+0x100-2],TargetCost[Addr+0x100-1],TargetCost[Addr+0x100  ],TargetCost[Addr+0x100+1],TargetCost[Addr+0x100+2],
				TargetCost[Addr+0x200-2],TargetCost[Addr+0x200-1],TargetCost[Addr+0x200  ],TargetCost[Addr+0x200+1],TargetCost[Addr+0x200+2],
				X,Y
				);
	} // LogDump

	//**//**//**//
	private void LogStat(string HeaderMsg, int[] Stat) {
		int X,Y;
		string Msg = "";
		Array.Sort(Stat);
		int Num = 0; int Val = 0; int Count = int.MinValue; int Total = 0;
		Stat[0xFFFF] = -1;
		while (Num < 0x10000) {
			if (Stat[Num] == Val)
				Count++;
			else {
				if (Count > 0) {
					Msg += " "+Convert.ToString(Val)+">"+Convert.ToString(Val*Count);
					Total += Count;
				}
				Count = 1;
				Val = Stat[Num];
			}
			Num++;
		}
		Msg = HeaderMsg + " " + Convert.ToString(Total) + ":" + Msg;
		Debug.Log(Msg);
	} // LogStat

	private List<Vector2i> PathFind_Flood(MapUnit Unit, int StartX, int StartY, int TMinX, int TMinY, int TMaxX, int TMaxY, float Distance, bool StaticOnly)
	{
	int X,Y,Addr,MinX,MinY,MaxX,MaxY;
		//**//**//**//
		_RunCount++;
		_LastTick = _StopWatch.ElapsedTicks;
		//**//**//**//

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

		//написать поиск недавних задач поиска
		//private TFindTask[] FindTasks = new TFindTask[2];
		//0/0
		//Array.Copy(UnitTypes[FindingUnitTypeNum],TargetCost);

		StartRefreshingCost();

		SetFindingUnit(Unit, StaticOnly);

		Qin = Qout = 1;

		for (Y = MinY; Y <= MaxY; Y++) {
			Addr = CellAddr(MinX,Y);
			for (X = MinX; X <= MaxX; X++) {
				if (NeedCheckRadius && !IsInTargetRadius(X,Y))
					;//skip
				else
				{
					//TargetCost[Addr+X] = CellCost[0,Addr+X];
					Path_AddCell(Addr,_Cost0, 0, _CostOneMove1);
				}
				Addr++;
			}
		}

		long _Last1 = _StopWatch.ElapsedTicks;

		Path_FillCost();
		//**//**//**//
		long _Last2 = _StopWatch.ElapsedTicks;

		List<Vector2i> BestPath = Path_TakeBestPath(StartX,StartY);

		if (!StaticOnly) {
			RestoreCellsUnderFindingUnit();
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
			Debug.LogFormat("runs: count= {0}  ticks={1} > {2}  sort={3},  1st={4}  2st={5}  3st={6},   Lsort={7}, MaxLsort={8},   Wsort={9}, MaxWsort={10}",
				_RunCount, _LastTick, _TickCount, _CheckTimeSort/_RunCount,
				//System.Diagnostics.Stopwatch.Frequency,
				_CheckTime1/_RunCount, _CheckTime2/_RunCount, _CheckTime3/_RunCount,

				_CheckLenSort/_RunCount, _CheckMaxLenSort,  _CheckWorkSort/_RunCount, _CheckMaxWorkSort

			);
			_RunCount = 0;
			_CheckTime1 = 0;
			_CheckTime2 = 0;
			_CheckTime3 = 0;
			_CheckTimeSort = 0;
			_CheckLenSort = 0;
			_CheckWorkSort = 0;
			_TimeToLog = DateTime.Now;
			_TimeToLog = _TimeToLog.Add(new TimeSpan(0,0,1));
			//LogStat("Stat 1(read)",Stat1); Stat1 = new int [0x10000];
			//LogStat("Stat 2(0-check)",Stat2); Stat2 = new int [0x10000];
			//LogStat("Stat 3(rewrite)",Stat3); Stat3 = new int [0x10000];
			//Debug.Break();
			//LogStat("Star 4",Stat4); Stat4 = new int [0x10000];
			//LogStat("Stat 5",Stat5); Stat5 = new int [0x10000];
		}
		//

		if (BestPath != null)
		{
		//**//**//**//
		//Debug.LogFormat("find path for ID {0}  to {1},{2}  dist={3}  static={4}   result={5},{6}",
		//		Unit.ID,  TargetX,TargetY, Distance,StaticOnly, BestPoint.x,BestPoint.y
		//		);
		//LogDump(TargetX,TargetY);
		//**//**//**//
		//if (!Unit.Interaction.CheckWalkableForUnit(BestPath[0].x,BestPath[0].y, StaticOnly)) {
		//Debug.LogFormat("path find bad: start={0},{1}  target={2},{3}  result={4},{5} Dist={6} static={7}",
		//		StartX,StartY, TMinX,TMinY, BestPath[0].x,BestPath[0].y, Distance,StaticOnly
		//		);
		//LogDump(StartX,StartY);
		//LogDump(TMinX+1,TMinY+1);
		//}

	                return BestPath;
		}

		//**//**//**//
		//Debug.LogFormat("path find: start={0},{1}  target={2},{3}  result={4},{5} Dist={6} static={7}",
		//		StartX,StartY, TargetX,TargetY, BestPoint.x,BestPoint.y, Distance,StaticOnly
		//		);
		//LogDump(TargetX,TargetY);
		//**//**//**//
		//bool _DoDebug = (StartX >= 18 && StartX <= 20 && StartY >= 16 && StartY <= 18);
		//if (_DoDebug && BestPoint == null) {
		//Debug.LogFormat("path find: start={0},{1}  target={2},{3}  result={4},{5} Dist={6} static={7}",
		//		StartX,StartY, TargetX,TargetY, "NULL","NULL", Distance,StaticOnly
		//		);
		//LogDump(TargetX,TargetY);
		//}
		//if (_DoDebug && BestPoint != null) {
		//Debug.LogFormat("path find: start={0},{1}  target={2},{3}  result={4},{5} Dist={6} static={7}",
		//		StartX,StartY, TargetX,TargetY, BestPoint.x,BestPoint.y, Distance,StaticOnly
		//		);
		//LogDump(TargetX,TargetY);
		//}

		return PathFind_Dumb(Unit, StartX, StartY, TMinX, TMinY, TMaxX, TMaxY, Distance, StaticOnly);//**//**//**//
		//return null;
	} // PathFind_Flood


	///////////////////////////////


	public List<Vector2i> GetShortestPath(MapUnit Unit, bool StaticOnly, float Distance, int StartX, int StartY, int TargetX, int TargetY, int TargetWidth, int TargetHeight, int Limit)
	{

	//return PathFind_Dumb(Unit, StartX, StartY, TargetX, TargetY, TargetWidth, TargetHeight, Distance, StaticOnly);

	return PathFind_Flood(Unit, StartX, StartY, TargetX, TargetY, TargetWidth, TargetHeight, Distance, StaticOnly);

	} // GetShortestPath

#endif
	
} // MapWizard
