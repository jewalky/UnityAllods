//WB_pathfind developing
//#define NoMapWizardUsing

//#define CheckTime
//#define CheckStatistic


//кривоты:
//разливка
//после переписывания уже найденной стоимости НЕ запрещается путь из нее туда, откуда была записана переписанная.
//это ведет к построению НЕбыстрейшего пути (разница была замечена до 20% от шага)
//НЕ учитывается направление ходока, т.е. время поворота

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

	public void UpdateNode(int NodeX, int NodeY, MapNode Node) {}

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
	int [] Stat1 = new int [0x10000];


	/////////////////////////////
	// MapWizard interface
	/////////////////////////////


	const int _CostBad	= 0;
	const int _Cost0	= _CostBad+1;
	const int _CostMax	= 0xFFFFFF;
	const int LowX = 0;
	const int LowY = 0;
	private int HighX,HighY,MapWidth,MapHeight;


	/////////////////////////////
	// map cache
	
	/////////////////////////////

	//path searching. parameters
	private MapUnit FindingUnit;
	private int StartAddr;
	private bool FindingMode;
	private Vector2 TargetCenter;
	private float TargetRadius2;

	//path searching. map data
	private int[] TargetCost = new int[0x10000]; // [word]
	private int[] CellCost = new int[0x10000]; // [word]

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
		return new Vector2i((Addr & 0xFF), (Addr >> 8));
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

	private void SetFindingData(MapUnit Unit,bool StaticOnly) {
		FindingUnit = Unit;
		FindingMode = StaticOnly;
	} // SetFindingData

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

		int X = Addr & 0xFF;
		int Y = Addr >> 8;
		if (FindingUnit.Interaction.CheckWalkableForUnit(X, Y, FindingMode)) { // X,Y,StaticOnly

			CellCost[Addr] = (int)(8 * FindingUnit.Interaction.GetNodeCostFactor(X, Y, FindingMode) );
			TargetCost[Addr] = _CostMax;

		} else {

			CellCost[Addr] = _CostBad;
			TargetCost[Addr] = _CostBad;

		}
		CellFresh[Addr] = CellFreshValue;
	} // RefreshCost


	public MapWizard() {
		//**//**//**//
		_StopWatch.Start();
	}

	public void Unload() {
		HighX = -1;
		HighY = -1;
		CellFreshValue = _MaxCellFreshID;
		CellFresh = null;
		//TargetCost = null;
		//Queue = null;
	} // Unload


	///////////////////////////////
	// Path finding initializing

	public void LoadMap(MapLogic Logic) {

		MapWidth = Logic.Width;
		MapHeight = Logic.Height;
		//LowX = 0;
		HighX = MapWidth-1;
		//if (HighX<254) { LowX++; HighX+=2; }
		//LowY = 0; 
		HighY = MapHeight-1;
		//if (HighY<254) { LowY++; HighY+=2; }
	} // LoadMap


	///////////////////////////////
	// Path finding 1
	// Flood version = Fastest path to target
	// debugging //**//


	///////////////////////////////
	// cells queue for finding

	private ushort[] QueueAddr = new ushort[0x10000]; // [word]
	private int[] QueueCost = new int[0x10000]; // [word]
	private ushort Qin,Qout;
	private int QueueMinCost;

	private void ClearQueue() {
		Qin = Qout = 1;
	}

	private void PutToQueue(int Addr, int Cost) {
	int QPlace;
		QueueCost[Qout-1] = Cost;
		QPlace = Qin;
		while (QueueCost[--QPlace] > Cost) ; // cycle body is empty
		QPlace++;

		if (QPlace < Qin) {
			Array.Copy(QueueAddr,QPlace, QueueAddr,QPlace+1,Qin-QPlace);
			Array.Copy(QueueCost,QPlace, QueueCost,QPlace+1,Qin-QPlace);
		}

		QueueCost[QPlace] = Cost;
		QueueAddr[QPlace] = (ushort)Addr;
		Qin++;
	} // PutToQueue

	private int GetFromQueue() {
		//**//**//**//
		return QueueAddr[Qout++];

	} // GetFromQueue


	///////////////////////////////
	// path costs filling

	private void Path_AddCell(int Addr, int Cost, int CellCost1, int MulCostOneMove) {
		RefreshCost(Addr);
		if (TargetCost[Addr] != _CostMax ) return;

		Cost += (CellCost1 + CellCost[Addr]) * MulCostOneMove;
		TargetCost[Addr] = Cost;

		PutToQueue(Addr,Cost);

	} // Path_AddCell

	private void Path_FillCost() {
	int Addr;
		while (Qin != Qout) {
			Addr = GetFromQueue();
			if (Addr == StartAddr) break;

			int Cost = TargetCost[Addr];
			int CellCost1 = CellCost[Addr];

		//	Path_AddCell(Addr        ,Cost,CellCost1,);
			Path_AddCell(Addr-0x100  ,Cost,CellCost1,5);
			Path_AddCell(Addr+0x100  ,Cost,CellCost1,5);
			Path_AddCell(Addr      -1,Cost,CellCost1,5);
			Path_AddCell(Addr      +1,Cost,CellCost1,5);
			Path_AddCell(Addr-0x100-1,Cost,CellCost1,7);
			Path_AddCell(Addr-0x100+1,Cost,CellCost1,7);
			Path_AddCell(Addr+0x100-1,Cost,CellCost1,7);
			Path_AddCell(Addr+0x100+1,Cost,CellCost1,7);
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
		//	Path_TakePoint(Addr        ,ref BestAddr,ref BestCost); //  0, 0
			Path_TakePoint(Addr-0x100  ,ref BestAddr,ref BestCost); //  0,-1
			Path_TakePoint(Addr+0x100  ,ref BestAddr,ref BestCost); //  0,+1
			Path_TakePoint(Addr      -1,ref BestAddr,ref BestCost); // -1, 0
			Path_TakePoint(Addr      +1,ref BestAddr,ref BestCost); // +1, 0
			Path_TakePoint(Addr-0x100-1,ref BestAddr,ref BestCost); // -1,-1
			Path_TakePoint(Addr-0x100+1,ref BestAddr,ref BestCost); // +1,-1
			Path_TakePoint(Addr+0x100-1,ref BestAddr,ref BestCost); // -1,+1
			Path_TakePoint(Addr+0x100+1,ref BestAddr,ref BestCost); // +1,+1

			result.Add(AddrAsVector(BestAddr));
			Addr = BestAddr;

		//**//**//**//	
		if (result.Count() > 10000)
			return null;

		}
		if (result.Count() == 0)
			result = null;
		return result;
	} // Path_TakeBestPath


	//**//**//**//
	private void LogStat(string HeaderMsg, int[] Stat) {
		string Msg = "";
		Array.Sort(Stat);
		int Num = 0; int Val = 0; int Count = int.MinValue; int Total = 0;
		Stat[0xFFFF] = -1;
		while (Num < 0x10000) {
			if (Stat[Num] == Val)
				Count++;
			else {
				if (Count > 0) {
					Count *=Val;
					Msg += " "+Convert.ToString(Val)+">"+Convert.ToString(Count);
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

	private List<Vector2i> PathFind_Flood(MapUnit Unit, int StartX, int StartY, int TMinX, int TMinY, int TMaxX, int TMaxY, float Distance, bool StaticOnly, int Dummy)
	{
	int X,Y,AddrX,AddrY,MinX,MinY,MaxX,MaxY;
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

		SetFindingData(Unit, StaticOnly);

		StartRefreshingCost();

		ClearQueue();

		AddrY = CellAddr(MinX,MinY);
		for (Y = MinY; Y <= MaxY; Y++) {
			AddrX = AddrY;
			for (X = MinX; X <= MaxX; X++) {
				if (NeedCheckRadius && !IsInTargetRadius(X, Y))
				{
					//skip
				}
				else
				{
					Path_AddCell(AddrX, _Cost0, 0, 1);
					if (TargetCost[AddrX] != _CostBad)
						TargetCost[AddrX] = _Cost0;
				}
				AddrX++;
			}
			AddrY += 0x100;
		}
		//**//**//**//
//		long _Last1 = _StopWatch.ElapsedTicks;

		Path_FillCost();
		//**//**//**//
//		long _Last2 = _StopWatch.ElapsedTicks;

		List<Vector2i> BestPath = Path_TakeBestPath(StartX,StartY);
		//**//**//**//
/*		long _Last3 = _StopWatch.ElapsedTicks;
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
			LogStat("Stat 1(read)",Stat1); Stat1 = new int [0x10000];
		}*/
		//

		if (BestPath != null)
			return BestPath;

		return null;
	} // PathFind_Flood


	///////////////////////////////


	public List<Vector2i> GetShortestPath(MapUnit Unit, bool StaticOnly, float Distance, int StartX, int StartY, int TargetX, int TargetY, int TargetWidth, int TargetHeight, int Dummy)
	{

	//return PathFind_Dumb(Unit, StartX, StartY, TargetX, TargetY, TargetWidth, TargetHeight, Distance, StaticOnly);

	return PathFind_Flood(Unit, StartX, StartY, TargetX, TargetY, TargetWidth, TargetHeight, Distance, StaticOnly, Dummy);

	} // GetShortestPath

#endif
	
} // MapWizard
