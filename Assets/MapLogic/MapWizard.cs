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
//	ВСЕМ юнитам доступна область не более 254*254.

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
	private int LowX,LowY,HighX,HighY;
	private byte[,] CellCost = new byte[1,0x10000];
	private int[] TargetCost = new int[0x10000];

	//path searching
	private int TargetX, TargetY, TargetWidth, TargetHeight;
	private ushort TargetAddr;
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

	private ushort CellAddr(int X,int Y) {
		return (ushort) (((Y+LowY) << 8) + X+LowX);
	}

	private Vector2i AddrAsVector(int Addr)
	{
		return new Vector2i((Addr & 0xFF) - LowX , (Addr >> 8) - LowY);
	}

	public void LoadMap(MapLogic Logic) {
	int X,Y;
	int Addr;
		//обеспечение непроходимой границы
		LowX = 0; HighX = Logic.Width-1;
		if (HighX<254) { LowX++; HighX+=2; }
		LowY = 0; HighY = Logic.Height-1;
		if (HighY<254) { LowY++; HighY+=2; }
		//загрузка проходимости
		//пока только один слой
		//**//**//**//
		Addr=CellAddr(0,0);
		for (Y=0; Y<Logic.Height; Y++){
			for (X=0; X<Logic.Width; X++){
			if (CheckWalkable(X,Y))
				CellCost[0,Addr+X] = (byte)_Cost0;
			else	CellCost[0,Addr+X] = (byte)_CostBad;
			//**//**//**//
			//if (X==17 && Y==15)
			//	Debug.LogFormat("cell cost: {0},{1} = {2}", X,Y, CellCost[0,CellAddr(X,Y)]);
			}
			Addr += 0x100;
		}
		//заполнение непроходимой границы
		Addr=0;
		for (Y=0; Y<=HighY; Y++) {
			CellCost[0,Addr+0    ] = (byte)_CostBad;
			CellCost[0,Addr+HighX] = (byte)_CostBad;
			TargetCost[Addr+0    ] = _CostBad;
			TargetCost[Addr+HighX] = _CostBad;
			Addr += 0x100;
		}
		Addr=HighY << 8;
		for (X=0; X<=HighX; X++) {
			CellCost[0,0000+X] = (byte)_CostBad;
			CellCost[0,Addr+X] = (byte)_CostBad;
			TargetCost[0000+X] = _CostBad;
			TargetCost[Addr+X] = _CostBad;
		}
	}


	///////////////////////////////
	// Path finding 0
	// Dumb version (DROD uses this for default bugs moving)

	private List<Vector2i> PathFind_Dumb(MapUnit Unit, int StartX, int StartY, int TargetX, int TargetY, int TargetWidth, int TargetHeight, float Distance, bool StaticOnly)
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
		if (X <= MinX) X = MinX - X;
		else if (X > MaxX) X = X - MaxX;
		if (Y <= MinY) Y = MinY - Y;
		else if (Y > MaxY) Y = Y - MaxY;
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
			//if (TargetAddr == Addr) break;//**//**//**//
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

	private Vector2i Path_TakeBestPoint(int StartX, int StartY, int MaxLength) {
		int Addr = CellAddr(StartX,StartY);
		if ( TargetCost[Addr] == _CostBad  ||  TargetCost[Addr] == _CostMax )
			return null;
		int BestCost = _CostMax;
		int BestAddr = 0;

		Path_TakePoint(Addr-0x100-1,ref BestAddr,ref BestCost); // -1,-1
		Path_TakePoint(Addr-0x100  ,ref BestAddr,ref BestCost); //  0,-1
		Path_TakePoint(Addr-0x100+1,ref BestAddr,ref BestCost); // +1,-1
		Path_TakePoint(Addr      -1,ref BestAddr,ref BestCost); // -1, 0
	//	Path_TakePoint(Addr        ,ref BestAddr,ref BestCost); //  0, 0
		Path_TakePoint(Addr      +1,ref BestAddr,ref BestCost); // +1, 0
		Path_TakePoint(Addr+0x100-1,ref BestAddr,ref BestCost); // -1,+1
		Path_TakePoint(Addr+0x100  ,ref BestAddr,ref BestCost); //  0,+1
		Path_TakePoint(Addr+0x100+1,ref BestAddr,ref BestCost); // +1,+1

		if (BestAddr != 0)
			return AddrAsVector(BestAddr);
		else	return null;
	}

	/*private void Path_TakePoint(int X, int Y, ref int BestX, ref int BestY, ref int BestCost) {
		int Cost = TargetCost[CellAddr(X,Y)];
		if ( Cost == _CostBad ) return;
		if ( BestCost < Cost) return;
		if ( BestCost = Cost && Dist(X,Y,BestX,BestY) > Dist(X,Y,BestX,BestY)) return;
		{
			BestCost = Cost;
			BestAddr = Addr;
		}
	}

	private Vector2i Path_TakeBestPoint(int StartX, int StartY) {
		if ( TargetCost[CellAddr(StartX,StartY)] == _CostBad  ||  TargetCost[Addr] == _CostMax )
			return null;
		int BestCost = _CostMax;
		int BestX = 0, BestY = 0;

		Path_TakePoint(Addr-0x100-1,ref BestX,ref BestCost);
		Path_TakePoint(Addr-0x100  ,ref BestAddr,ref BestCost);
		Path_TakePoint(Addr-0x100+1,ref BestAddr,ref BestCost);
		Path_TakePoint(Addr      -1,ref BestAddr,ref BestCost);
	//	Path_TakePoint(Addr        ,ref BestAddr,ref BestCost);
		Path_TakePoint(Addr      +1,ref BestAddr,ref BestCost);
		Path_TakePoint(Addr+0x100-1,ref BestAddr,ref BestCost);
		Path_TakePoint(Addr+0x100  ,ref BestAddr,ref BestCost);
		Path_TakePoint(Addr+0x100+1,ref BestAddr,ref BestCost);

		if (BestAddr != 0)
			return AddrAsVector(BestAddr);
		else	return null;
	}*/

	private void Dump(int X,int Y) {
		int Addr = CellAddr(X,Y);
		Debug.LogFormat("costs: {0},{1},{2}  {3},{4},{5}  {6},{7},{8}",
				TargetCost[Addr-0x100-1],TargetCost[Addr-0x100  ],TargetCost[Addr-0x100+1],
				TargetCost[Addr      -1],TargetCost[Addr        ],TargetCost[Addr      +1],
				TargetCost[Addr+0x100-1],TargetCost[Addr+0x100  ],TargetCost[Addr+0x100+1]
				);
	}

	private List<Vector2i> PathFind_Flood(MapUnit Unit, int StartX, int StartY, int TargetX, int TargetY, int TargetWidth, int TargetHeight, float Distance, bool StaticOnly, int MaxLength)
	{
	int X,Y,AddrX,AddrY;
		//**//**//**//
		_RunCount++;
		_LastTick = _StopWatch.ElapsedTicks;

		this.TargetX = TargetX;
		this.TargetY = TargetY;
		//**//**//**//
		//if (StartX==18 && StartY==16)
		//	Debug.LogFormat("find cell cost: {0},{1} = {2}", 17,15, CellCost[0,CellAddr(17,15)]);
		AddrY=CellAddr(LowX,LowY);
		for (Y=LowY; Y<HighY; Y++) {
			AddrX = AddrY;
			for (X=LowX; X<HighX; X++) {
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
		int TargetMaxX = TargetX; if (TargetWidth  > 0) TargetMaxX += TargetWidth-1;
		int TargetMaxY = TargetY; if (TargetHeight > 0) TargetMaxY += TargetHeight-1;
		int MinX = (int)(TargetX    - Distance); if (MinX < LowX) MinX = LowX;
		int MinY = (int)(TargetY    - Distance); if (MinY < LowY) MinY = LowY;
		int MaxX = (int)(TargetMaxX + Distance); if (MaxX > HighX) MaxX = HighX;
		int MaxY = (int)(TargetMaxY + Distance); if (MaxY > HighY) MaxY = HighY;

		AddrY = CellAddr(MinX,MinY);
		for (Y=MinY; Y<MaxY; Y++) {
			AddrX = AddrY;
			for (X=MinX; X<MaxX; X++) {
				//TargetCost[Addr+X] = CellCost[0,Addr+X];
				if (Unit.Interaction.CheckWalkableForUnit(X,Y, StaticOnly))
					Path_AddCell(CellAddr(X,Y),_Cost0 + DistToRange(X,Y, TargetX, TargetY, TargetMaxX, TargetMaxY));
				AddrX++;
			}
			AddrY += 0x100;
		}

		Path_FillCost();
		long _Last2 = _StopWatch.ElapsedTicks;

		Vector2i BestPoint = Path_TakeBestPoint(StartX,StartY,MaxLength);
		//**//**//**//
		//if ((BestPoint.x==StartX)&&(BestPoint.y==StartY))
		//	Debug.LogFormat("path find stupid");

		//**//**//**//
		//Debug.LogFormat("path find: start={0},{1}  target={2},{3}  result={4},{5} Dist={6} static={7}",
		//		StartX,StartY, TargetX,TargetY, BestPoint.x,BestPoint.y, Distance,StaticOnly
		//		);
		//Dump(TargetX,TargetY);
		//**//**//**//
		bool _DoDebug = (StartX >= 18 && StartX <= 20 && StartY >= 16 && StartY <= 18);
		if (_DoDebug && BestPoint == null) {
		Debug.LogFormat("path find: start={0},{1}  target={2},{3}  result={4},{5} Dist={6} static={7}",
				StartX,StartY, TargetX,TargetY, "NULL","NULL", Distance,StaticOnly
				);
		Dump(TargetX,TargetY);
		}
		if (_DoDebug && BestPoint != null) {
		Debug.LogFormat("path find: start={0},{1}  target={2},{3}  result={4},{5} Dist={6} static={7}",
				StartX,StartY, TargetX,TargetY, BestPoint.x,BestPoint.y, Distance,StaticOnly
				);
		Dump(TargetX,TargetY);
		}

		//**//**//**//
		long _Last3 = _StopWatch.ElapsedTicks;
		_Last3 = _Last3-_Last2;
		_Last2 = _Last2-_Last1;
		_Last1 = _Last1-_LastTick;
		_LastTick = _StopWatch.ElapsedTicks - _LastTick;
		_TickCount += _LastTick;
		//Debug.LogFormat("runs: count= {0}  ticks={1} > {2}  freq={3}", _RunCount, _LastTick, _TickCount, System.Diagnostics.Stopwatch.Frequency );
		//Debug.LogFormat("divide: {0}   {1}    {2}", _Last1, _Last2, _Last3 );

		if (BestPoint != null)
		{
			List<Vector2i> result = new List<Vector2i>();
			result.Insert(0,BestPoint);
		    //**//**//**//
		    if (!Unit.Interaction.CheckWalkableForUnit(BestPoint.x,BestPoint.y, StaticOnly)) {
		        Debug.LogFormat("path find bad: start={0},{1}  target={2},{3}  result={4},{5} Dist={6} static={7}",
				        StartX,StartY, TargetX,TargetY, BestPoint.x,BestPoint.y, Distance,StaticOnly
				        );
		        Dump(TargetX,TargetY);
		    }

	        return result;
		}

        Debug.LogFormat("ID {0} BESTPOINT FROM {1},{2} IS NULL", Unit.ID, StartX, StartY);

		//**//**//**//
		//Debug.LogFormat("path find start: {0}, {1} = {2}", StartX,StartY, TargetCost[CellAddr(StartX,StartY)]);
		//Debug.LogFormat("path find target : {0}, {1} = {2}", TargetX,TargetY, TargetCost[CellAddr(TargetX,TargetY)]);
		//Debug.LogFormat("path find result: {0}, {1}", BestPoint.x,BestPoint.y);
		return PathFind_Dumb(Unit, StartX, StartY, TargetX, TargetY, TargetWidth, TargetHeight, Distance, StaticOnly);//**//**//**//
		//return null;
	}


	///////////////////////////////


	public List<Vector2i> GetShortestPath(MapUnit Unit, bool StaticOnly, float Distance, int StartX, int StartY, int TargetX, int TargetY, int TargetWidth, int TargetHeight, int MaxLength)
	{

	//return PathFind_Dumb(Unit, StartX, StartY, TargetX, TargetY, TargetWidth, TargetHeight, Distance, StaticOnly);

	return PathFind_Flood(Unit, StartX, StartY, TargetX, TargetY, TargetWidth, TargetHeight, Distance, StaticOnly, MaxLength);

	}
	
}
