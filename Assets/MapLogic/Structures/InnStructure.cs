using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class InnStructure : StructureLogic
{
    private AllodsMap.AlmInnInfo Rules;

    public InnStructure(MapStructure s, AllodsMap.AlmInnInfo info) : base(s)
    {
        Rules = info;
    }

    public override bool OnEnter(MapUnit unit)
    {
        if (!base.OnEnter(unit))
            return false;
        if (!NetworkManager.IsServer)
        {
            ShopScreen screen = Utils.CreateObjectWithScript<ShopScreen>();
            screen.Shop = Structure;
            screen.Unit = unit;
        }
        Server.NotifyEnterInn(unit, Structure);
        return true;
    }

    public override void OnLeave(MapUnit unit)
    {
        base.OnLeave(unit);
        Server.NotifyLeaveStructure(unit);
        if (!NetworkManager.IsServer)
            UiManager.Instance.ClearWindows();
    }
}