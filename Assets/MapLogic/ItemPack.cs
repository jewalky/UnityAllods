using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ItemPack
{
    private List<Item> ItemList = new List<Item>();

    public ItemPack()
    {

    }

    public ItemPack(ItemPack other)
    {
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

    public long Money = 0;

    public long Price
    {
        get
        {
            long mOut = 0;
            for (int i = 0; i < ItemList.Count; i++)
                mOut += ItemList[i].Price;
            mOut += Money;
            return mOut;
        }
    }

    // take item from pack
    public Item TakeItem(int position, int count)
    {
        if (position < 0 || position >= ItemList.Count)
            return null;

        Item sourceItem = ItemList[position];
        if (count >= sourceItem.Count)
        {
            ItemList.RemoveAt(position);
            return sourceItem;
        }

        Item newItem = new Item(sourceItem, count);
        sourceItem.Count -= count;
        return newItem;
    }

    // insert item into pack.
    public void PutItem(int position, Item item)
    {
        // check for already present count
        for (int i = 0; i < ItemList.Count; i++)
        {
            if (ItemList[i].Class == item.Class &&
                ItemList[i].MagicEffects.SequenceEqual(item.MagicEffects))
            {
                ItemList[i].Count += item.Count;
                return;
            }
        }

        position = Math.Min(ItemList.Count, Math.Max(0, position));
        ItemList.Insert(position, new Item(item, item.Count));
    }

    public Item this[int index]
    {
        get
        {
            return ItemList[index];
        }
    }
}