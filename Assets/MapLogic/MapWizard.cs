//Debug.LogFormat("path find err: {0}, {1} =", X, Y);

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

struct UnitType {
	int Num,Value;
	MapUnit UnitExample;
}

class MapWizard
{
// !!! предпосылки:
//	размеры карты не больше 256*256
//	карта по краям ограничена для всех непроходимыми клетками

	//**//**//**//
	System.Diagnostics.Stopwatch _StopWatch = System.Diagnostics.Stopwatch.StartNew();
	int _ErrCount = 0;
	int _TimeCount = 0;
	int _RunCount = 0;
	long _TickCount = 0;
	long _LastTick = 0;

	const int _CostBad = 0;
	const int _Cost0 = _CostBad+1;
	const int _CostMax = 0xFFFFFF;
	const int LowX = 0;
	const int LowY = 0;
	private int HighX,HighY;
	private byte[,] CellCost = new byte[1,0x10000];
	private int[] TargetCost = new int[0x10000];

	//path searching
	private float TargetRadius2;
	Vector2 TargetCenter;
	private int StartAddr;
	private ushort[] Queue = new ushort[0x10000];
	private ushort Qin,Qout;
	//

	public MapWizard(){
		//**//**//**//
		_StopWatch.Start();
	}

	public void Unload() {
		//**//**//**//
	}

	private bool CheckWalkable(int x, int y)
	{
		MapNode node = MapLogic.Instance.Nodes[x, y];
		uint tile = node.Tile;
		MapNodeFlags flags = node.Flags;
		if ((!flags.HasFlag(MapNodeFlags.Unblocked) && (tile >= 0x1C0 && tile <= 0x2FF)) // blocked by ground terrain
			|| flags.HasFlag(MapNodeFlags.BlockedGround)) // or blocked explicitly
			return false; // not walkable
	        return true; // walkable
	}

	/*private UnitType[] unitTypes;
	public GetUnitType(MapUnit unit){
		int Num=
		найти среди UnitType по типу
		return Num;
	}*/

	/*public UnitMovingType(MapUnit unit){
        for (int ly = y; ly < y + Unit.Height; ly++)
        {
            for (int lx = x; lx < x + Unit.Width; lx++)
		if (unit.IsWalking && (flags & MapNodeFlags.Unblocked) == 0 && (tile >= 0x1C0 && tile <= 0x2FF))
			 return false;
		MapNodeFlags bAir = staticOnly ? MapNodeFlags.BlockedAir : MapNodeFlags.BlockedAir | MapNodeFlags.DynamicAir;
		MapNodeFlags bGround = staticOnly ? MapNodeFlags.BlockedGround : MapNodeFlags.BlockedGround | MapNodeFlags.DynamicGround;
		if (Unit.IsFlying && (flags & bAir) != 0) return false;
			else if (!Unit.IsFlying && (flags & bGround) != 0)
		if unit.
	};
	
    public bool CheckWalkableForUnit(int x, int y, bool staticOnly)
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

	*/

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

	public void LoadMap(MapLogic Logic) {
	int X,Y;
	int AddrY;
		//обеспечение непроходимой границы
		//LowX = 0; 
		HighX = Logic.Width-1;
		//if (HighX<254) { LowX++; HighX+=2; }
		//LowY = 0; 
		HighY = Logic.Height-1;
		//if (HighY<254) { LowY++; HighY+=2; }
		//загрузка проходимости
		//пока только один слой
		//**//**//**//
		AddrY = 0;//CellAddr(0,0);
		for (Y = 0; Y < Logic.Height; Y++){
			for (X = 0; X < Logic.Width; X++){
			if (CheckWalkable(X,Y))
				CellCost[0,AddrY+X] = (byte)_Cost0;
			else	CellCost[0,AddrY+X] = (byte)_CostBad;
			//**//**//**//
			//if (X==17 && Y==15)
			//	Debug.LogFormat("cell cost: {0},{1} = {2}", X,Y, CellCost[0,CellAddr(X,Y)]);
			}
			AddrY += 0x100;
		}
		//заполнение непроходимой границы
		AddrY = 0;
		for (Y = 0; Y <= HighY; Y++) {
			CellCost[0,AddrY+0    ] = (byte)_CostBad;
			CellCost[0,AddrY+HighX] = (byte)_CostBad;
			TargetCost[AddrY+0    ] = _CostBad;
			TargetCost[AddrY+HighX] = _CostBad;
			AddrY += 0x100;
		}
		AddrY = HighY << 8;
		for (X = 0; X <= HighX; X++) {
			CellCost[0,00000+X] = (byte)_CostBad;
			CellCost[0,AddrY+X] = (byte)_CostBad;
			TargetCost[00000+X] = _CostBad;
			TargetCost[AddrY+X] = _CostBad;
		}
	}


	///////////////////////////////
	// Path finding 0
	// Dumb version (DROD uses this for default bugs moving)

	private List<Vector2i> PathFind_Dumb(MapUnit Unit, int StartX, int StartY, int TargetX, int TargetY, int TargetMaxX, int TargetMaxY, float Distance, bool StaticOnly)
	{
		int DX = TargetX - StartX;
		int DY = TargetY - StartY;
		if (DX < 0) DX = -1; else if (DX > 0) DX = +1;
		if (DY < 0) DY = -1; else if (DY > 0) DY = +1;
		if (Unit.Interaction.CheckWalkableForUnit(StartX+DX, StartY+DY, StaticOnly))
		{
			List<Vector2i> result = new List<Vector2i>();
			result.Insert(0,new Vector2i(StartX+DX, StartY+DY));
	                return result;
		}
		return null;
	}


	///////////////////////////////
	// Path finding 1
	// Flood version = Fastest path to target
	// debugging //**//

	private float sqr(float Value) {
		return Value*Value;
	}

	private int abs(int A) {
		if (A < 0) return -A;
		return A;
	}

	private int max(int X, int Y) {
		if (X < Y) return Y;
		return X;
	}

	private int DistToPoint(int X1, int Y1, int X2, int Y2) {
		return abs(X1-X2) + abs(Y1-Y2);
	}

	private int DistToRange(int X, int Y, int MinX, int MinY, int MaxX, int MaxY) {
		if      (X < MinX) X = MinX - X;
		else if (X > MaxX) X = X - MaxX;
		else               X = 0;

		if      (Y < MinY) Y = MinY - Y;
		else if (Y > MaxY) Y = Y - MaxY;
		else               Y = 0;

		return max( X, Y );
	}

	private void Path_AddCell(int Addr, int Cost) {
		if (TargetCost[Addr] <= Cost) return;
		TargetCost[Addr] = Cost;
		Queue[Qin++] = (ushort)Addr;
	}

	private void Path_FillCost() {
	int Addr;
	int Cost;
		while (Qin != Qout) {
			Addr = Queue[Qout++];
			Cost = TargetCost[Addr]+CellCost[0,Addr];
			if (StartAddr == Addr) break;//**//**//**// //can found not fastest way if use different move-costs
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
	}

	private void Path_TakePoint(int Addr, ref int BestAddr, ref int BestCost) {
		int Cost = TargetCost[Addr];
		if ( Cost != _CostBad  &&  BestCost > Cost) {
			BestCost = Cost;
			BestAddr = Addr;
		}
	}

	private List<Vector2i> Path_TakeBestPath(int StartX, int StartY) {
		int Addr = CellAddr(StartX,StartY);
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
		//if (result.Count>1000) {
		//	Debug.LogFormat("path find bad take {0},{1}  cost={2}", StartX,StartY, TargetCost[BestAddr]);
		//	return null;
		//}
		}

		return result;
	}


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
	}

	private List<Vector2i> PathFind_Flood(MapUnit Unit, int StartX, int StartY, int TMinX, int TMinY, int TMaxX, int TMaxY, float Distance, bool StaticOnly)
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


		//**//**//**//
		//if (StartX==18 && StartY==16)
		//	Debug.LogFormat("find cell cost: {0},{1} = {2}", 17,15, CellCost[0,CellAddr(17,15)]);
		AddrY = CellAddr(LowX,LowY);
		for (Y = LowY; Y <= HighY; Y++) {
			AddrX = AddrY;
			for (X = LowX; X <= HighX; X++) {
				//TargetCost[Addr+X] = CellCost[0,Addr+X];
				if (Unit.Interaction.CheckWalkableForUnit(X,Y, StaticOnly))
					{ TargetCost[AddrX] = _CostMax; CellCost[0,AddrX] = _Cost0; }
				else	{ TargetCost[AddrX] = _CostBad; CellCost[0,AddrX] = _CostBad; }
				/*if (CellCost[0,Addr+X] != (byte)_CostBad)
					TargetCost[Addr+X] = _CostMax;
				else	TargetCost[Addr+X] = _CostBad;*/
				//**//**//**//
				//if (Addr+X == CellAddr(TargetX,TargetY))
				//	Debug.LogFormat("path find init target: {0}, {1} = {2} (cell={3})", TargetX,TargetY, TargetCost[CellAddr(TargetX,TargetY)], CellCost[0,CellAddr(TargetX,TargetY)]);

				//уточнить по виду Unit
				//**//**//**//
				AddrX++;
			}
			AddrY += 0x100;
		}
		long _Last1 = _StopWatch.ElapsedTicks;

		//**//**//**//
		//if (StartX==18 && StartY==16)
		//	Debug.LogFormat("find cell cost: {0},{1} = {2} walkable={3}", 17,15, CellCost[0,CellAddr(17,15)], Unit.Interaction.CheckWalkableForUnit(17,15, true));

		Qin=0; Qout=0;

		//**//**//**//
		//Debug.LogFormat("path find target: {0}, {1} = {2} (cell={3})", TargetX,TargetY, TargetCost[CellAddr(TargetX,TargetY)], CellCost[0,CellAddr(TargetX,TargetY)]);
		//int TargetMaxX = TargetX + TargetWidth-1;
		//int TargetMaxY = TargetY + TargetHeight-1;

		//Distance -= 1f;
		//if (Distance < 0) Distance = 0;

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
		//bool _DoDebug = (TargetX >= 22 && TargetX <= 22 && TargetY >= 20 && TargetY <= 20);
		//if (_DoDebug ) {
		//Debug.LogFormat("start finding" );
		//Dump(TMinX+1,TMinY+1);
		//}

		Path_FillCost();//StartX==15 && StartY==17 && TMinX==16 && TMinY==17 && TMaxX==18 && TMaxY==19
		long _Last2 = _StopWatch.ElapsedTicks;

		List<Vector2i> BestPath = Path_TakeBestPath(StartX,StartY);
		//**//**//**//
		//if ((BestPoint.x==StartX)&&(BestPoint.y==StartY))
		//	Debug.LogFormat("path find stupid");

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

		//**//**//**//
		long _Last3 = _StopWatch.ElapsedTicks;
		_Last3 = _Last3-_Last2;
		_Last2 = _Last2-_Last1;
		_Last1 = _Last1-_LastTick;
		_LastTick = _StopWatch.ElapsedTicks - _LastTick;
		_TickCount += _LastTick;
		//Debug.LogFormat("runs: count= {0}  ticks={1} > {2}  freq={3},  1st={4}  2st={5}  3st={6}",
		//		_RunCount, _LastTick, _TickCount, System.Diagnostics.Stopwatch.Frequency,
		//		_Last1, _Last2, _Last3
		//		);

		if (BestPath != null)
		{
		//**//**//**//
		//Debug.LogFormat("find path for ID {0}  to {1},{2}  dist={3}  static={4}   result={5},{6}",
		//		Unit.ID,  TargetX,TargetY, Distance,StaticOnly, BestPoint.x,BestPoint.y
		//		);
		//Dump(TargetX,TargetY);
		//**//**//**//
		if (!Unit.Interaction.CheckWalkableForUnit(BestPath[0].x,BestPath[0].y, StaticOnly)) {
		//Debug.LogFormat("path find bad: start={0},{1}  target={2},{3}  result={4},{5} Dist={6} static={7}",
		//		StartX,StartY, TMinX,TMinY, BestPath[0].x,BestPath[0].y, Distance,StaticOnly
		//		);
		//Dump(StartX,StartY);
		//Dump(TMinX+1,TMinY+1);
		}

	                return BestPath;
		}
		//**//**//**//
		//Debug.LogFormat("path find start: {0}, {1} = {2}", StartX,StartY, TargetCost[CellAddr(StartX,StartY)]);
		//Debug.LogFormat("path find target : {0}, {1} = {2}", TargetX,TargetY, TargetCost[CellAddr(TargetX,TargetY)]);
		//Debug.LogFormat("path find result: {0}, {1}", BestPoint.x,BestPoint.y);
		//Debug.LogFormat("ID {0} BESTPOINT FROM {1},{2} IS NULL", Unit.ID, StartX, StartY);
		//Dump(StartX,StartY);
		//Dump(TMinX+1,TMinY+1);

		return PathFind_Dumb(Unit, StartX, StartY, TMinX, TMinY, TMaxX, TMaxY, Distance, StaticOnly);//**//**//**//
		//return null;
	}


	///////////////////////////////


	public List<Vector2i> GetShortestPath(MapUnit Unit, bool StaticOnly, float Distance, int StartX, int StartY, int TargetX, int TargetY, int TargetWidth, int TargetHeight)
	{

	//return PathFind_Dumb(Unit, StartX, StartY, TargetX, TargetY, TargetWidth, TargetHeight, Distance, StaticOnly);

	return PathFind_Flood(Unit, StartX, StartY, TargetX, TargetY, TargetWidth, TargetHeight, Distance, StaticOnly);

	}
	
}
