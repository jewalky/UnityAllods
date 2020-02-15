using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ShopStructure : StructureLogic
{
    private AllodsMap.AlmShop Rules;

    public ShopStructure(MapStructure s, AllodsMap.AlmShop rules) : base(s)
    {
        Rules = rules;
    }

    public override bool OnEnter(MapUnit unit)
    {
        if (!base.OnEnter(unit))
            return false;
        Server.NotifyEnterShop(unit, Structure);
        return true;
    }

    public override void OnLeave(MapUnit unit)
    {
        base.OnLeave(unit);
        Server.NotifyLeaveStructure(unit.Player);
    }
}