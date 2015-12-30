using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class ScanrangeCalc
{
    public ScanrangeCalc()
    {
        InitializeTables();
    }

    private sbyte[,,] pTablesOffset = new sbyte[41, 41, 2];
    private int[,] pTablesCost = new int[41, 41];
    public int[,] pTablesVision = new int[41, 41];

    public byte ScanShift = 7;

    private bool SetCell(int x, int y, int height_origin, int height_cell)
    {
        int vision_previous = pTablesVision[x + pTablesOffset[x, y, 0], y + pTablesOffset[x,y,1]];
        int cost = pTablesCost[x,y];
        //if(bDiv2) cost -= 18;
        pTablesVision[x,y] = vision_previous - (height_cell - height_origin + cost);
        return (pTablesVision[x,y] >= 0);
    }

    public void InitializeTables()
    {
        uint ulScanShifted = 1u << ScanShift;

        for (int i = 0; i <= 20; ++i)
        {
            for (int j = 0; j <= 20; ++j)
            {
                if (j >= (i / 2))
                {
                    if (j <= (i * 2))
                    {
                        pTablesOffset[20 + i, 20 + j, 0] = -1;
                        pTablesOffset[20 + i, 20 + j, 1] = -1;
                        pTablesOffset[20 - i, 20 - j, 0] = 1;
                        pTablesOffset[20 - i, 20 - j, 1] = 1;
                        pTablesOffset[20 + i, 20 - j, 0] = -1;
                        pTablesOffset[20 + i, 20 - j, 1] = 1;
                        pTablesOffset[20 - i, 20 + j, 0] = 1;
                        pTablesOffset[20 - i, 20 + j, 1] = -1;
                    }
                    else
                    {
                        pTablesOffset[20 + i, 20 + j, 0] = 0;
                        pTablesOffset[20 + i, 20 + j, 1] = -1;
                        pTablesOffset[20 - i, 20 - j, 0] = 0;
                        pTablesOffset[20 - i, 20 - j, 1] = 1;
                        pTablesOffset[20 + i, 20 - j, 0] = 0;
                        pTablesOffset[20 + i, 20 - j, 1] = 1;
                        pTablesOffset[20 - i, 20 + j, 0] = 0;
                        pTablesOffset[20 - i, 20 + j, 1] = -1;
                    }
                }
                else
                {
                    pTablesOffset[20 + i, 20 + j, 0] = -1;
                    pTablesOffset[20 + i, 20 + j, 1] = 0;
                    pTablesOffset[20 - i, 20 - j, 0] = 1;
                    pTablesOffset[20 - i, 20 - j, 1] = 0;
                    pTablesOffset[20 + i, 20 - j, 0] = -1;
                    pTablesOffset[20 + i, 20 - j, 1] = 0;
                    pTablesOffset[20 - i, 20 + j, 0] = 1;
                    pTablesOffset[20 - i, 20 + j, 1] = 0;
                }

                int v4;
                if (i > j) v4 = i;
                else v4 = j;

                int v1 = (int)(Math.Sqrt((double)(j * j + i * i)) * ulScanShifted / v4);
                pTablesCost[20 + i, 20 + j] = v1;
                pTablesCost[20 + i, 20 - j] = v1;
                pTablesCost[20 - i, 20 + j] = v1;
                pTablesCost[20 - i, 20 - j] = v1;
            }
        }

        pTablesCost[20, 20] = 0;
    }

    private bool CheckValid(int x, int y)
    {
        return (x >= 8 && y >= 8 && x < MapLogic.Instance.Width - 8 && y < MapLogic.Instance.Height - 8);
    }

    private int GetHeight(int x, int y)
    {
        return MapLogic.Instance.Nodes[x, y].Height;
    }

    public void CalculateVision(int x, int y, float scanrangef)
    {
        // we need to make scanshifted scanrange from float.
        int scanrange = (int)scanrangef;
        scanrange = (scanrange << 8) | (int)((scanrangef - scanrange) * 255);

        for (int ly = 0; ly < 41; ly++)
            for (int lx = 0; lx < 41; lx++)
                pTablesVision[lx, ly] = 0;

        int vision = scanrange;
        int vision2 = (1 << (ScanShift - 1)) + (vision >> (8 - ScanShift));

        int genX = x - 20;
        int genY = y - 20;
        int ht_origin = GetHeight(x, y);

        pTablesVision[20, 20] = vision2;
        for(int i = 1; i < 20; i++)
        {
            bool fdisp = false;
            for(int j = -i; j < i+1; j++)
            {
                if(CheckValid(genX+(20+j), genY+(20-i)) &&
                   SetCell(20+j, 20-i, ht_origin, GetHeight(genX+(20+j), genY+(20-i))))
                    fdisp = true;
                if(CheckValid(genX+(20+j), genY+(20+i)) &&
                   SetCell(20+j, 20+i, ht_origin, GetHeight(genX+(20+j), genY+(20+i))))
                    fdisp = true;
                if(CheckValid(genX+(20-i), genY+(20+j)) &&
                   SetCell(20-i, 20+j, ht_origin, GetHeight(genX+(20-i), genY+(20+j))))
                    fdisp = true;
                if(CheckValid(genX+(20+i), genY+(20-j)) &&
                   SetCell(20+i, 20-j, ht_origin, GetHeight(genX+(20+i), genY+(20-j))))
                    fdisp = true;
            }

		    if(!fdisp) break;
        }
    }
}