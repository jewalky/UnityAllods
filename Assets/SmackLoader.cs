using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// to-do: split everything in few .cs files

namespace Smacker
{
    // helper flags
    [Flags]
    public enum HeaderFlags
    {
        HasRingFrame = 0x0001,
        YInterlaced = 0x0002,
        YDoubled = 0x0004
    }

    //
    [Flags]
    public enum AudioFlags : uint
    {
        Compressed = 0x80000000,
        Present = 0x40000000,
        Is16Bit = 0x20000000,
        IsStereo = 0x10000000,
        CompressedBink = 0x0C000000
    }

    //
    [Flags]
    public enum FrameFlags
    {
        Keyframe = 0x0001,
        Unknown = 0x0002
    }

    //
    [Flags]
    public enum FrameTypeFlags
    {
        HasPalette = 0x01,
        HasAudio1 = 0x02,
        HasAudio2 = 0x04,
        HasAudio3 = 0x08,
        HasAudio4 = 0x10,
        HasAudio5 = 0x20,
        HasAudio6 = 0x40,
        HasAudio7 = 0x80
    }

    public class BitReader
    {
        public BinaryReader Br { get; private set; }
        private long LastStreamPos = -1;
        private int SubBitPos = 0;
        private byte LastByte = 0;

        public BitReader(BinaryReader br)
        {
            Br = br;
            LastStreamPos = Br.BaseStream.Position;
        }

        public uint ReadBits(int count)
        {
            uint output = 0;
            if (LastStreamPos != Br.BaseStream.Position)
                throw new Exception("Stream position modified while reading bits: this should not happen");

            int wroteBits = 0;
            int leftBits = count;
            while (leftBits > 0)
            {
                if (SubBitPos == 0)
                {
                    if (Br.BaseStream.Position < Br.BaseStream.Length)
                        LastByte = Br.ReadByte();
                    else LastByte = 0;
                    LastStreamPos = Br.BaseStream.Position;
                }

                output |= (uint)((byte)(LastByte & 1) << wroteBits);
                LastByte >>= 1;

                SubBitPos = (SubBitPos + 1) % 8;
                leftBits--;
                wroteBits++;
            }

            return output;
        }
    }

    // helper to read Huffman trees
    // individual tree
    public class HuffContext
    {
        public class HuffNode
        {
            public int BitCount = 0;
            public int Code = 0;
            public int Value = 0;
            public HuffNode[] Branch = null;
        }

        int Length;
        HuffNode Root;

        public static HuffContext FromTree(BitReader br, int length)
        {
            HuffContext hc = new HuffContext();
            hc.Length = length;
            hc.Root = new HuffNode();
            DecodeTree(br, hc, hc.Root, 0);
            return hc;
        }

        private static void DecodeTree(BitReader br, HuffContext hc, HuffNode node, int length)
        {
            length++;
            node.Branch = null;

            if (br.ReadBits(1) == 0) // leaf
            {
                if (length >= hc.Length)
                    throw new Exception(string.Format("Tree size exceeded! (Current={0}, Length={1})", length, hc.Length));
                node.Value = (int)br.ReadBits(8);
            }
            else // node
            {
                node.Branch = new HuffNode[2];
                for (int i = 0; i < 2; i++)
                {
                    node.Branch[i] = new HuffNode();
                    node.Branch[i].BitCount = node.BitCount + 1;
                    node.Branch[i].Code = (node.Code << 1) | i;
                    node.Branch[i].Value = -1;
                    DecodeTree(br, hc, node.Branch[i], length);
                }
            }
        }

        public static HuffContext FromBigTree(BitReader br, HeaderTree header, int size)
        {
            HuffContext hc = new HuffContext();
            hc.Length = ((size + 3) >> 2) + 4;
            hc.Root = new HuffNode();
            int len = DecodeBigTree(br, hc, header, hc.Root, 0);
            // fill in empty Last values, but only if allowed
            if (header.Last[0] == null)
            {
                header.Last[0] = new HuffNode();
                len++;
            }
            if (header.Last[1] == null)
            {
                header.Last[1] = new HuffNode();
                len++;
            }
            if (header.Last[2] == null)
            {
                header.Last[2] = new HuffNode();
                len++;
            }
            if (len > hc.Length)
                throw new Exception("Huffman codes out of range (invalid tree)");
            return hc;
        }

        private static int DecodeBigTree(BitReader br, HuffContext hc, HeaderTree header, HuffNode node, int length)
        {

            int returnLength = 0;

            length++;
            node.Branch = null;

            if (br.ReadBits(1) == 0) // leaf
            {
                if (length >= hc.Length)
                    throw new Exception(string.Format("Tree size exceeded! (Current={0}, Length={1})", length, hc.Length));

                int i1;
                int i2;
                if (header.LowTree != null)
                    i1 = header.LowTree.GetValue(br);
                else throw new Exception(string.Format("Invalid tree (LowTree=null)"));
                if (header.HighTree != null)
                    i2 = header.HighTree.GetValue(br);
                else throw new Exception(string.Format("Invalid tree (HighTree=null)"));
                int value = (i1 | (i2 << 8));

                // store escapes
                for (int j = 0; j < 3; j++)
                {
                    if (header.Escapes[j] == value)
                    {
                        header.Last[j] = node;
                        value = 0;
                        break;
                    }
                }

                node.Value = value;

            }
            else // node
            {
                node.Branch = new HuffNode[2];
                for (int i = 0; i < 2; i++)
                {
                    node.Branch[i] = new HuffNode();
                    node.Branch[i].BitCount = node.BitCount + 1;
                    node.Branch[i].Code = (node.Code << 1) | i;
                    node.Branch[i].Value = -1;
                    returnLength = DecodeBigTree(br, hc, header, node.Branch[i], length);
                }
            }

            return returnLength;

        }

        public int GetValue(BitReader br)
        {

            int readBits = 0;
            HuffNode node = Root;

            while (node != null)
            {
                readBits++;
                // get bit
                int bit = (int)br.ReadBits(1);
                node = node.Branch[bit];

                if (node != null && node.Branch == null)
                    return node.Value;
            }

            throw new Exception("Invalid binary sequence for GetValue!");
        }
    }

    // header tree: Low Bytes, High Bytes, Escapes*3, BigTree
    public class HeaderTree
    {
        public HuffContext LowTree { get; private set; }
        public HuffContext HighTree { get; private set; }
        public ushort[] Escapes { get; private set; }
        public HuffContext.HuffNode[] Last { get; private set; }
        public HuffContext Tree { get; private set; }

        public HeaderTree(BitReader br, int size)
        {
            // low tree
            if (br.ReadBits(1) == 1)
            {
                //long cpos1 = br.Br.BaseStream.Position;
                LowTree = HuffContext.FromTree(br, 256);
                //long cpos2 = br.Br.BaseStream.Position;
                //Debug.LogFormat("LowTree took {0} bytes", cpos2 - cpos1);
                br.ReadBits(1);
            }
            //else Debug.LogFormat("Skipped Low Tree");
            // high tree
            if (br.ReadBits(1) == 1)
            {
                //long cpos1 = br.Br.BaseStream.Position;
                HighTree = HuffContext.FromTree(br, 256);
                //long cpos2 = br.Br.BaseStream.Position;
                //Debug.LogFormat("HighTree took {0} bytes", cpos2 - cpos1);
                br.ReadBits(1);
            }
            //else Debug.LogFormat("Skipped High Tree");
            // escapes
            Escapes = new ushort[3];
            Escapes[0] = (ushort)br.ReadBits(16);
            Escapes[1] = (ushort)br.ReadBits(16);
            Escapes[2] = (ushort)br.ReadBits(16);
            Last = new HuffContext.HuffNode[3];
            Last[0] = Last[1] = Last[2] = null;
            // 
            //long dpos1 = br.Br.BaseStream.Position;
            Tree = HuffContext.FromBigTree(br, this, size);
            br.ReadBits(1);
            //long dpos2 = br.Br.BaseStream.Position;
            //Debug.LogFormat("BigTree took {0} bytes, Size = {1}", dpos2 - dpos1, size);
            //Debug.LogFormat("Escapes = [{0}, {1}, {2}]; Last = [{3}, {4}, {5}]", Escapes[0], Escapes[1], Escapes[2], Last[0], Last[1], Last[2]);
        }

        public int GetValue(BitReader br)
        {
            int val = Tree.GetValue(br);
            if (Last[0].Value != val)
            {
                Last[2].Value = Last[1].Value;
                Last[1].Value = Last[0].Value;
                Last[0].Value = val;
            }
            return val;
        }

        public void ResetLast()
        {
            Last[0].Value = Last[1].Value = Last[2].Value = 0;
        }
    }

    // this class contains data that gets reused between keyframes
    // normally, Palette and previous pixels
    public class SmackerDecodeContext
    {
        public Color32[] Palette { get; internal set; } = new Color32[256];
        public byte[] Image { get; internal set; }
    }

    public class SmackerFrame
    {
        private SmackerFile File;
        private byte[] Data;

        public FrameFlags Flags { get; private set; }
        public FrameTypeFlags TypeFlags { get; private set; }
        public AudioFlags[] AudioFlags { get; private set; }
        public uint[] AudioSampleRate { get; private set; }
        // so unlike the context, audio is unpacked right into the frame object
        public float[][] AudioData { get; private set; }
        private int[] SkipAudioData;

        private static uint[] BlockSizeTable = new uint[]
        {
             1,    2,    3,    4,    5,    6,    7,    8,
             9,   10,   11,   12,   13,   14,   15,   16,
            17,   18,   19,   20,   21,   22,   23,   24,
            25,   26,   27,   28,   29,   30,   31,   32,
            33,   34,   35,   36,   37,   38,   39,   40,
            41,   42,   43,   44,   45,   46,   47,   48,
            49,   50,   51,   52,   53,   54,   55,   56,
            57,   58,   59,  128,  256,  512, 1024, 2048
        };

        private static byte[] PaletteMap = new byte[]
        {
           0x00, 0x04, 0x08, 0x0C, 0x10, 0x14, 0x18, 0x1C,
           0x20, 0x24, 0x28, 0x2C, 0x30, 0x34, 0x38, 0x3C,
           0x41, 0x45, 0x49, 0x4D, 0x51, 0x55, 0x59, 0x5D,
           0x61, 0x65, 0x69, 0x6D, 0x71, 0x75, 0x79, 0x7D,
           0x82, 0x86, 0x8A, 0x8E, 0x92, 0x96, 0x9A, 0x9E,
           0xA2, 0xA6, 0xAA, 0xAE, 0xB2, 0xB6, 0xBA, 0xBE,
           0xC3, 0xC7, 0xCB, 0xCF, 0xD3, 0xD7, 0xDB, 0xDF,
           0xE3, 0xE7, 0xEB, 0xEF, 0xF3, 0xF7, 0xFB, 0xFF
     };

        public SmackerFrame(SmackerFile file, FrameFlags flags, FrameTypeFlags typeFlags, AudioFlags[] audioFlags, uint[] audioSampleRate, byte[] data)
        {
            File = file;
            Flags = flags;
            TypeFlags = typeFlags;
            AudioFlags = audioFlags;
            AudioSampleRate = audioSampleRate;
            Data = data;
            AudioData = new float[7][];
            SkipAudioData = new int[7];
        }

        private void UnpackAudio(SmackerDecodeContext ctx, BinaryReader binaryReader, int channel)
        {
            if (!AudioFlags[channel].HasFlag(Smacker.AudioFlags.Present))
            {
                //Debug.LogFormat("Skip UnpackAudio channel {0} (not present)", channel);
                return;
            }
            if (AudioData[channel] != null)
            {
                //Debug.LogFormat("Skip UnpackAudio channel {0} (done)", channel);
                binaryReader.BaseStream.Position += SkipAudioData[channel];
                return; // already unpacked audio
            }
            //Debug.LogFormat("UnpackAudio channel {0}", channel);
            uint a_Length = binaryReader.ReadUInt32();
            uint dataLength = a_Length - 4;
            uint a_UnpackedLength = a_Length;
            if ((AudioFlags[channel] & (Smacker.AudioFlags.Compressed | Smacker.AudioFlags.CompressedBink)) != 0)
            {
                a_UnpackedLength = binaryReader.ReadUInt32();
                dataLength -= 4;
            }
            // pad the data with some bytes to prevent underflow errors in the last sample.
            // apparently huffman compression is not very precise here...
            byte[] a_Data = new byte[dataLength + 4];
            binaryReader.BaseStream.Read(a_Data, 0, (int)dataLength);
            SkipAudioData[channel] = (int)a_Length;
            // don't do anything with this yet
            
            using (MemoryStream audioStream = new MemoryStream(a_Data))
            using (BinaryReader audioReader = new BinaryReader(audioStream))
            {
                BitReader br = new BitReader(audioReader);
                // look at compression flags
                if ((AudioFlags[channel] & Smacker.AudioFlags.CompressedBink) != 0)
                {
                    Debug.LogFormat("Bink audio compression is not supported");
                    return;
                }
                // ROM2 sound is always compressed with DPCM - nothing to test on!
                if (!AudioFlags[channel].HasFlag(Smacker.AudioFlags.Compressed))
                {
                    Debug.LogFormat("Uncompressed PCM audio is not yet supported");
                    return;
                }

                AudioData[channel] = new float[0];
                // not sure what's the point of having these flags both in header and here
                bool b_DataPresent = br.ReadBits(1) != 0;
                if (!b_DataPresent)
                {
                    Debug.LogFormat("Data not present for channel {0}", channel);
                    return;
                }

                int b_IsStereo = (int)br.ReadBits(1);
                int b_Is16Bits = (int)br.ReadBits(1);

                // validate flags. other decoders to this
                if ((b_IsStereo == 1) != AudioFlags[channel].HasFlag(Smacker.AudioFlags.IsStereo) ||
                    (b_Is16Bits == 1) != AudioFlags[channel].HasFlag(Smacker.AudioFlags.Is16Bit))
                {
                    Debug.LogFormat("Invalid audio: flags not matching: data is {0}bit and {1}, flags are {2}bit and {3}",
                        (b_Is16Bits == 1) ? "16" : "8", (b_IsStereo == 1) ? "stereo" : "mono",
                        AudioFlags[channel].HasFlag(Smacker.AudioFlags.Is16Bit) ? "16" : "8",
                        AudioFlags[channel].HasFlag(Smacker.AudioFlags.IsStereo) ? "stereo" : "mono");
                    return;
                }

                int bytesPerSample = 1 << (b_IsStereo + b_Is16Bits);

                HuffContext[] a_Trees = new HuffContext[bytesPerSample];
                for (int i = 0; i < bytesPerSample; i++)
                {
                    // junk bits
                    br.ReadBits(1);
                    a_Trees[i] = HuffContext.FromTree(br, 256);
                    // junk bits
                    br.ReadBits(1);
                }

                sbyte[] a_Bases = new sbyte[bytesPerSample];
                for (int i = 0; i < bytesPerSample; i++)
                    a_Bases[bytesPerSample-i-1] = (sbyte)br.ReadBits(8); // reverse order here

                float numSamplesF = (float)a_UnpackedLength / bytesPerSample;
                int numSamples = (int)numSamplesF;
                if (numSamplesF != numSamples)
                {
                    Debug.LogFormat("Invalid audio: fractional sample count {0}", numSamplesF);
                    return;
                }

                // now logic is slightly different for 16 and 8 bits
                // though bytes we just read through the tree
                byte[] sampleBytes = new byte[bytesPerSample];
                float[] sampleFloats = new float[1 * (1 << b_IsStereo)];
                short[] sampleShorts = new short[sampleFloats.Length];
                short[] a_ShortBases = new short[sampleFloats.Length];

                // output
                AudioData[channel] = new float[numSamples * (1 << b_IsStereo)];

                // generate some shorts from bases if we have 16bits
                if (b_Is16Bits == 1)
                {
                    for (int i = 0; i < a_ShortBases.Length; i++)
                    {
                        short ubase = (short)(a_Bases[2 * i] | (a_Bases[2 * i + 1] << 8));
                        a_ShortBases[i] = ubase;
                    }
                }

                int lastSample = 0;
                try
                {
                    for (int j = 0; j < numSamples; j++)
                    {
                        lastSample = j;
                        for (int i = 0; i < bytesPerSample; i++)
                            sampleBytes[i] = (byte)a_Trees[i].GetValue(br);

                        // if we have 16-bit:
                        // sampleBytes[0] = 00FF left
                        // sampleBytes[1] = FF00 left
                        // sampleBytes[2] = 00FF right
                        // sampleBytes[3] = FF00 right
                        // if we have 8-bit:
                        // sampleBytes[0] = left
                        // sampleBytes[1] = right
                        if (b_Is16Bits == 1)
                        {
                            for (int i = 0; i < sampleShorts.Length; i++)
                            {
                                short value = (short)(sampleBytes[2 * i] | (sampleBytes[2 * i + 1] << 8));
                                short sample = sampleShorts[i] = (short)(a_ShortBases[i] += value);
                                // convert to float
                                // to-do reduce debug variables here
                                AudioData[channel][j * sampleFloats.Length + i] = sampleFloats[i] = sample / 32768f;
                            }
                        }
                        else
                        {
                            for (int i = 0; i < sampleBytes.Length; i++)
                            {
                                sbyte value = (sbyte)sampleBytes[i];
                                sbyte sample = (a_Bases[i] += value);
                                sampleBytes[i] = (byte)sample;
                                // convert to float
                                // to-do reduce debug variables here
                                AudioData[channel][j * sampleBytes.Length + i] = sampleFloats[i] = sample / 128f;
                            }
                        }

                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    Debug.LogFormat("Last Sample Read = {0} of {1}", lastSample, numSamples);
                }
            }
        }

        private void UnpackVideo(SmackerDecodeContext ctx, BinaryReader binaryReader)
        {
            BitReader br = new BitReader(binaryReader);
            int wBlocks = File.Width / 4;
            int hBlocks = File.Height / 4;
            int countBlocks = wBlocks * hBlocks;
            int currentBlock = 0;
            while (currentBlock < countBlocks)
            {
                // 0 = mono block
                // 1 = full block
                // 2 = void block
                // 3 = solid block
                ushort typeDescriptor = (ushort)File.TYPETree.GetValue(br);
                uint blockType = (uint)(typeDescriptor & 0x0003);
                uint chainLength = BlockSizeTable[(typeDescriptor & 0xFC) >> 2];
                uint extraData = (uint)((typeDescriptor & 0xFF00) >> 8);

                if (blockType == 2) // void block - no data
                {
                    currentBlock += (int)chainLength;
                }
                else if (blockType == 3) // solid block - palette index
                {
                    for (int i = 0; i < chainLength; i++)
                    {
                        int blockX = currentBlock % wBlocks;
                        int blockY = currentBlock / wBlocks;
                        int realX = blockX * 4;
                        int realY = blockY * 4;

                        for (int y = realY; y < realY+4; y++)
                        {
                            int offsetBase = y * File.Width;
                            for (int x = realX; x < realX + 4; x++)
                                ctx.Image[offsetBase + x] = (byte)extraData;
                        }

                        currentBlock++;
                    }
                }
                else if (blockType == 0) // mono block - two colors
                {
                    for (int i = 0; i < chainLength; i++)
                    {
                        ushort twoColors = (ushort)File.MCLRTree.GetValue(br);
                        byte color1 = (byte)((twoColors & 0xFF00) >> 8);
                        byte color2 = (byte)(twoColors & 0x00FF);
                        ushort pixelsMap = (ushort)File.MMAPTree.GetValue(br);

                        int blockX = currentBlock % wBlocks;
                        int blockY = currentBlock / wBlocks;
                        int realX = blockX * 4;
                        int realY = blockY * 4;

                        for (int y = realY; y < realY+4; y++)
                        {
                            int offsetBase = y * File.Width;
                            for (int x = realX; x < realX+4; x++)
                            {
                                byte color = ((pixelsMap & 1) == 0) ? color2 : color1;
                                pixelsMap >>= 1;
                                ctx.Image[offsetBase + x] = color;
                            }
                        }

                        currentBlock++;
                    }
                }
                else if (blockType == 1) // full block - many colors
                {
                    for (int i = 0; i < chainLength; i++)
                    {
                        int blockX = currentBlock % wBlocks;
                        int blockY = currentBlock / wBlocks;
                        int realX = blockX * 4;
                        int realY = blockY * 4;

                        for (int y = realY; y < realY + 4; y++)
                        {
                            ushort px34 = (ushort)File.FULLTree.GetValue(br);
                            ushort px12 = (ushort)File.FULLTree.GetValue(br);
                            byte[] colors = new byte[]
                            {
                                (byte)(px12 & 0x00FF),
                                (byte)((px12 & 0xFF00) >> 8),
                                (byte)(px34 & 0x00FF),
                                (byte)((px34 & 0xFF00) >> 8)
                            };

                            int offsetBase = y * File.Width;
                            int ci = 0;
                            for (int x = realX; x < realX + 4; x++)
                            {
                                ctx.Image[offsetBase + x] = colors[ci];
                                ci++;
                            }
                        }

                        currentBlock++;
                    }
                }
            }
        }

        // we need partial unpacking here to support audio streaming
        public void Unpack(SmackerDecodeContext ctx, bool unpackVideo)
        {
            File.MMAPTree.ResetLast();
            File.MCLRTree.ResetLast();
            File.FULLTree.ResetLast();
            File.TYPETree.ResetLast();

            using (MemoryStream ms = new MemoryStream(Data))
            {
                // if keyframe, reset palette
                if (Flags.HasFlag(FrameFlags.Keyframe) && unpackVideo)
                {
                    ctx.Palette = new Color32[256];
                }

                // if have palette.. read it.
                // reading palette does not require binaryreader, plus we can validate data integrity by using an array here
                if (TypeFlags.HasFlag(FrameTypeFlags.HasPalette))
                {
                    int palSize = ms.ReadByte() * 4 - 1;

                    if (unpackVideo)
                    {
                        byte[] palColors = new byte[palSize];
                        ms.Read(palColors, 0, palColors.Length);

                        Color32[] nextPalette = new Color32[256];
                        for (int i = 0; i < 256; i++)
                            nextPalette[i] = ctx.Palette[i];

                        int offset = 0;
                        int palOffset = 0;
                        int prevPalOffset = 0;
                        while (offset < palColors.Length && palOffset < 256)
                        {
                            byte flagByte = palColors[offset++];
                            if ((flagByte & 0x80) != 0) // copy next (c+1) color entries from current prevPalOffset to palOffset
                            {
                                int c = (flagByte & 0x7F) + 1;
                                palOffset += c;
                            }
                            else if ((flagByte & 0xC0) == 0x40) // copy next (c+1) color entries from s to palOffset
                            {
                                int c = (flagByte & 0x3F) + 1;
                                int s = palColors[offset++];
                                prevPalOffset = s;
                                for (int i = 0; i < c; i++)
                                    nextPalette[palOffset + i] = ctx.Palette[prevPalOffset + i];
                                prevPalOffset += c;
                                palOffset += c;
                            }
                            else // rgb color, &0x3F
                            {
                                byte r = PaletteMap[flagByte & 0x3F];
                                byte g = PaletteMap[palColors[offset++] & 0x3F];
                                byte b = PaletteMap[palColors[offset++] & 0x3F];
                                nextPalette[palOffset] = new Color32(r, g, b, 255);
                                palOffset++;
                            }
                        }

                        ctx.Palette = nextPalette;
                    }
                    else
                    {
                        ms.Position += palSize;
                    }
                }

                // from now on use binaryreader
                using (BinaryReader br = new BinaryReader(ms))
                {
                    // if have audio channels, read them
                    for (int i = 0; i < 7; i++)
                    {
                        FrameTypeFlags flag = (FrameTypeFlags)(1 << (i + 1));
                        if (TypeFlags.HasFlag(flag))
                            UnpackAudio(ctx, br, i);
                    }

                    if (unpackVideo)
                    {
                        // done with the audio, the rest is a picture
                        UnpackVideo(ctx, br);
                    }
                }
            }
        }
    }

    public class SmackerFile
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int FrameInterval { get; private set; }

        public HeaderTree MMAPTree { get; private set; }
        public HeaderTree MCLRTree { get; private set; }
        public HeaderTree FULLTree { get; private set; }
        public HeaderTree TYPETree { get; private set; }

        public SmackerDecodeContext Context { get; private set; }
        public SmackerFrame[] Frames { get; private set; }

        public SmackerFile(string filename)
        {
            MemoryStream smk = ResourceManager.OpenRead(filename);

            if (smk == null)
            {
                Core.Abort("Couldn't load \"{0}\"", filename);
                return;
            }

            using (BinaryReader br = new BinaryReader(smk))
            {
                int h_Signature = br.ReadInt32();
                int h_Width = br.ReadInt32();
                int h_Height = br.ReadInt32();
                int h_Frames = br.ReadInt32();
                int h_FrameRate = br.ReadInt32();
                HeaderFlags h_Flags = (HeaderFlags)br.ReadUInt32();
                int[] h_AudioSize = new int[7];
                for (int i = 0; i < h_AudioSize.Length; i++)
                    h_AudioSize[i] = br.ReadInt32();
                int h_TreesSize = br.ReadInt32();
                int h_MMap_Size = br.ReadInt32();
                int h_MClr_Size = br.ReadInt32();
                int h_Full_Size = br.ReadInt32();
                int h_Type_Size = br.ReadInt32();
                uint[] h_AudioRate = new uint[7];
                for (int i = 0; i < h_AudioRate.Length; i++)
                    h_AudioRate[i] = br.ReadUInt32();
                int h_Dummy = br.ReadInt32();

                // is this needed?
                //if (h_Flags.HasFlag(HeaderFlags.HasRingFrame))
                //    h_Frames++;

                // default is 10 fps
                int MsFrameDelay = 100;
                if (h_FrameRate > 0) // fps is milliseconds
                    MsFrameDelay = h_FrameRate;
                else if (h_FrameRate < 0) // fps in 100*milliseconds
                    MsFrameDelay = -h_FrameRate / 100;

                // parse audio flags
                AudioFlags[] h_AudioFlags = new AudioFlags[h_AudioRate.Length];
                for (int i = 0; i < h_AudioRate.Length; i++)
                {
                    h_AudioFlags[i] = (AudioFlags)(h_AudioRate[i] & 0xFC000000);
                    h_AudioRate[i] &= 0x00FFFFFF;
                }

                //
                uint[] FrameSizes = new uint[h_Frames];
                FrameFlags[] FrameFlags = new FrameFlags[FrameSizes.Length];
                for (int i = 0; i < FrameSizes.Length; i++)
                {
                    uint size = br.ReadUInt32();
                    FrameSizes[i] = size & 0xFFFFFFFC;
                    FrameFlags[i] = (FrameFlags)(size & 0x03);
                }

                // 
                FrameTypeFlags[] FrameTypeFlags = new FrameTypeFlags[h_Frames];
                for (int i = 0; i < FrameTypeFlags.Length; i++)
                    FrameTypeFlags[i] = (FrameTypeFlags)br.ReadByte();

                Debug.LogFormat("SMK Width = {0}, Height = {1}, FrameDelay = {2}, FrameTypeFlags(0) = {3}", h_Width, h_Height, MsFrameDelay, FrameTypeFlags[0]);

                long TreesStartPos = br.BaseStream.Position;

                // read byte with regular method
                BitReader bir = new BitReader(br);

                if (bir.ReadBits(1) == 1) // have mmap
                {
                    Debug.LogFormat("Reading MMAP tree");
                    MMAPTree = new HeaderTree(bir, h_MMap_Size);
                }
                else Debug.LogFormat("Skipping MMAP tree");

                if (bir.ReadBits(1) == 1) // have mclr
                {
                    Debug.LogFormat("Reading MCLR tree");
                    MCLRTree = new HeaderTree(bir, h_MClr_Size);
                }
                else Debug.LogFormat("Skipping MCLR tree");

                if (bir.ReadBits(1) == 1) // have full
                {
                    Debug.LogFormat("Reading FULL tree");
                    FULLTree = new HeaderTree(bir, h_Full_Size);
                }
                else Debug.LogFormat("Skipping FULL tree");

                if (bir.ReadBits(1) == 1) // have type
                {
                    Debug.LogFormat("Reading TYPE tree");
                    TYPETree = new HeaderTree(bir, h_Type_Size);
                }
                else Debug.LogFormat("Skipping TYPE tree");

                br.BaseStream.Position = TreesStartPos + h_TreesSize;

                // all trees loaded!
                // load frame data...
                Frames = new SmackerFrame[h_Frames];
                for (int i = 0; i < h_Frames; i++)
                {
                    byte[] data = br.ReadBytes((int)FrameSizes[i]);
                    Frames[i] = new SmackerFrame(this, FrameFlags[i], FrameTypeFlags[i], h_AudioFlags, h_AudioRate, data);
                }

                Context = new SmackerDecodeContext();
                Context.Image = new byte[h_Width * h_Height];

                Width = h_Width;
                Height = h_Height;
                FrameInterval = MsFrameDelay;
            }
        }
    }

}

public class SmackLoader : MonoBehaviour
{
    Smacker.SmackerFile f;

    public Texture2D TestFrame;
    private int LastFrame = 0;
    private int LastMs = 0;

    private bool FirstDone = false;

    private UnityEngine.UI.Image Image = null;
    private AudioClip AClip = null;
    private AudioSource ASource = null;

    // variables for audio streaming
    private List<float> LeftPCM = new List<float>();
    int LeftPCMPosition = 0;
    int LastAudioFrame = 0;

    private float[] NormalizeArraySize(float[] a, int len)
    {
        if (a.Length == len)
            return a;
        float[] newA = new float[len];
        for (int i = 0; i < len; i++)
        {
            if (i < a.Length)
                newA[i] = a[i];
            else break;
        }
        return newA;
    }

    private void NextFrame()
    {
        // check last ms
        if (FirstDone)
        {
            int realFrameInterval = f.FrameInterval;
            LastMs += (int)(Time.unscaledDeltaTime * 1000);
            if (LastMs > realFrameInterval)
            {
                while (LastMs > realFrameInterval)
                {
                    LastFrame++;
                    LastMs -= realFrameInterval;
                }
            }
            else return;
        }

        FirstDone = true;

        bool doInit = (TestFrame == null);
        
        if (doInit)
            TestFrame = new Texture2D(f.Width, f.Height, TextureFormat.RGBA32, false);


        if (Image == null)
        {
            UnityEngine.UI.Image img = (UnityEngine.UI.Image)GameObject.FindObjectOfType<Canvas>().GetComponentInChildren<UnityEngine.UI.Image>();
            Image = img;
        }

        if (ASource == null)
        {
            ASource = Image.gameObject.GetComponent<AudioSource>();
        }

        // put some audio in it
        if (AClip == null)
        {
            bool isStereo = f.Frames[0].AudioFlags[0].HasFlag(Smacker.AudioFlags.IsStereo);
            int channels = isStereo ? 2 : 1;

            // stitch entire PCM audio together
            LeftPCM.Clear();
            int sampleRatePerFrame = (int)(f.Frames[0].AudioSampleRate[0] / (1000f / f.FrameInterval));
            for (int i = 0; i < f.Frames.Length; i++)
            {
                try
                {
                    f.Frames[i].Unpack(f.Context, false);
                    float[] samples = f.Frames[i].AudioData[0];
                    if (samples != null)
                    {
                        LeftPCM.AddRange(NormalizeArraySize(samples, sampleRatePerFrame * channels));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
                
            }

            AClip = AudioClip.Create("SMKAUD", LeftPCM.Count / channels, channels, (int)f.Frames[0].AudioSampleRate[0], false);
            Debug.LogFormat("Audio Sample Count = {0}", LeftPCM.Count / channels);
            AClip.SetData(LeftPCM.ToArray(), 0);

            ASource.clip = AClip;
            ASource.Play();

        }



        try
        {
            f.Frames[LastFrame].Unpack(f.Context, true);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        Color32[] pixels = new Color32[f.Context.Image.Length];
        for (int y = 0; y < f.Height; y++)
        {
            int sourceOffs = y * f.Width;
            int baseOffs = (f.Height - y - 1) * f.Width;
            for (int x = 0; x < f.Width; x++)
                pixels[baseOffs + x] = f.Context.Palette[f.Context.Image[sourceOffs + x]];
        }
        TestFrame.SetPixels32(pixels);
        TestFrame.filterMode = FilterMode.Point;
        TestFrame.Apply();

        // find object
        if (doInit)
        {
            Image.material.mainTexture = TestFrame;
            Image.SetNativeSize();
        }
    }

    // Start is called before the first frame update
    void Awake()
    {
        f = new Smacker.SmackerFile("01-mw.smk");
        //NextFrame();
    }

    // Update is called once per frame
    void Update()
    {
        NextFrame();
    }
}
