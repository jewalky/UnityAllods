using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ItemPack : IEnumerable<Item>
{
    private List<Item> ItemList = new List<Item>();
    public bool Passive { get; private set; }
    public MapUnit Parent { get; private set; }
    public ServerCommands.ItemMoveLocation LocationHint = ServerCommands.ItemMoveLocation.Undefined;

    public ItemPack()
    {
        Passive = false;
        Parent = null;
    }

    public ItemPack(bool passive, MapUnit parent)
    {
        Passive = passive;
        Parent = parent;
    }

    public ItemPack(ItemPack other, bool passive, MapUnit parent)
    {
        Passive = passive;
        Parent = parent;
        for (int i = 0; i < other.ItemList.Count; i++)
            ItemList.Add(new Item(other.ItemList[i], other.ItemList[i].Count));
        Money = other.Money;
    }

    public int Count
    {
        get
        {
            return ItemList.Count;
        }
    }

    private bool _AutoCompact = true;
    public bool AutoCompact
    {
        get
        {
            return _AutoCompact;
        }
        set
        {
            if (_AutoCompact != value)
            {
                _AutoCompact = value;
                if (_AutoCompact) Compact();
            }
        }
    }

    public long Money = 0;

    public long Price
    {
        get
        {
            long mOut = 0;
            for (int i = 0; i < ItemList.Count; i++)
                mOut += ItemList[i].Price * ItemList[i].Count;
            mOut += Money;
            return mOut;
        }
    }

    private void UpdateParent()
    {
        if (Parent != null)
            Parent.RenderInfoVersion++;
    }

    public void Clear()
    {
        ItemList.Clear();
        UpdateParent();
    }

    public bool Contains(Item item)
    {
        for (int i = 0; i < ItemList.Count; i++)
            if (ItemList[i].ExtendedEquals(item))
                return true;
        return false;
    }

    public int IndexOf(Item item)
    {
        for (int i = 0; i < ItemList.Count; i++)
            if (ItemList[i].ExtendedEquals(item))
                return i;
        return -1;
    }

    public Item FindItemBySlot(MapUnit.BodySlot slot)
    {
        for (int i = 0; i < ItemList.Count; i++)
            if (ItemList[i] != null && ItemList[i].Class != null && ItemList[i].Class.Option.Slot == (int)slot)
                return ItemList[i];
        return null;
    }

    public Item TakeItem(Item item, int count)
    {
        for (int i = 0; i < ItemList.Count; i++)
            if (ItemList[i] != null && ItemList[i].ExtendedEquals(item)) return TakeItem(i, count);
        return null;
    }

    // take item from pack
    public Item TakeItem(int position, int count)
    {
        if (position < 0 || position >= ItemList.Count)
            return null;

        Item sourceItem = ItemList[position];
        if (sourceItem == null)
            return null;

        if (count >= sourceItem.Count)
        {
            if (_AutoCompact)
                ItemList.RemoveAt(position);
            else ItemList[position] = null;
            UpdateParent();
            return sourceItem;
        }

        Item newItem = new Item(sourceItem, count);
        sourceItem.Count -= count;
        UpdateParent();
        return newItem;
    }

    // insert item into pack.
    public Item PutItem(int position, Item item)
    {
        if (item == null)
        {
            if (!_AutoCompact)
            {
                position = Math.Min(ItemList.Count, Math.Max(0, position));
                ItemList.Insert(position, null);
            }
            return null;
        }

        if (!Passive)
        {
            item.Parent = this;
            if (!NetworkManager.IsClient)
                item.NetParent = LocationHint;
        }

        if (item.Count <= 0)
            return null; // don't put anything if item count is zero. this can happen if item was removed from pack after drag started.

        // check for already present count
        for (int i = 0; i < ItemList.Count; i++)
        {
            if (ItemList[i] != null && ItemList[i].ExtendedEquals(item))
            {
                ItemList[i].Count += item.Count;
                if (!Passive)
                    ItemList[i].Index = i;
                UpdateParent();
                return ItemList[i];
            }
        }

        position = Math.Min(ItemList.Count, Math.Max(0, position));

        if (position < ItemList.Count && ItemList[position] == null)
            ItemList.RemoveAt(position);

        ItemList.Insert(position, item);

        if (!Passive)
            item.Index = position;

        UpdateParent();
        return item;
    }

    public IEnumerator<Item> GetEnumerator()
    {
        return ItemList.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ItemList.GetEnumerator();
    }

    public Item this[int index]
    {
        get
        {
            if (index < 0 || index >= ItemList.Count)
                return null;
            return ItemList[index];
        }
    }

    public override string ToString()
    {
        string output = "";
        for (int i = 0; i < ItemList.Count; i++)
        {
            if (i > 0)
                output += "; ";
            if (ItemList[i] != null)
                output += string.Format("[{0}] {1}", ItemList[i].Count, ItemList[i].ToStringWithEffects(false));
            else output += "<null>";
        }
        return "ItemPack[" + output + "]";
    }

    // deletes null items (empty spaces). this is for shops
    public void Compact()
    {
        ItemList.RemoveAll(x => x == null);
        if (!Passive)
        {
            for (int i = 0; i < ItemList.Count; i++)
                ItemList[i].Index = i;
        }
    }
}