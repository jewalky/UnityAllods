using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

class MapWizard
{
	public int X = 0;
	public int Y = 0;
	public int Width = 0;
	public int Height = 0;
	public GameObject GameObject = null;
	public MonoBehaviour GameScript = null;
	public readonly int ID = MapLogic.Instance.TopObjectID;
	public bool DoUpdateView = false;
	public bool DoUpdateInfo = false;
	public bool DoUpdateSpells = false;
	public bool IsLinked { get; private set; }

	public void LoadMap(MapLogic Logic) {
		//**//**//**//
	}

	public void UnLoad() {
		//**//**//**//
	}


	public List<Vector2i> GetShortestPath(MapUnit unit, bool staticOnly, float distance, Vector2i start, Vector2i target)
	{

	int DX=target.x-start.x;
	int DY=target.y-start.y;
	if (DX<0) DX=-1; else if (DX>0) DX=+1;
	if (DY<0) DY=-1; else if (DY>0) DY=+1;
	if (unit.Interaction.CheckWalkableForUnit(start.x+DX, start.y+DY, staticOnly))
	{
		List<Vector2i> list = new List<Vector2i>();
		list.Insert(0,new Vector2i(start.x+DX, start.y+DY));
                return list;
	}

        return null;

	}
	
	/*private List<Action> BuildSolution(SearchNode<State,Action> seachNode){
		List<Action> list = new List<Action>();
		while (seachNode != null){
			if ((seachNode.action != null ) && (!seachNode.action.Equals(default(Action)))){
				list.Insert(0,seachNode.action);
			}
			seachNode = seachNode.parent;
		}
		return list;
	}*/
}
