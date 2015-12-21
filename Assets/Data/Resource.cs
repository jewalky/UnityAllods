using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

public class ResourceManager
{
    static readonly int ResourceSignature = 0x31415926;

    static ResourceNode[] Roots = null;

    internal class ResourceNode
    {
        public string Source = "";
        public string Name = "";
        public uint Size = 0;
        public uint Offset = 0;
        public bool IsRoot = false;
        public bool IsDirectory = false;
        public ResourceNode[] Children = null;
    }

    // =================================
    //  helper methods
    // =================================
    // 
    private static bool TreeTraverse(FileStream fs, BinaryReader fsb, ResourceNode node, uint offset, uint first, uint last)
    {
        for (uint i = first; i < last; i++)
        {
            fs.Seek(offset + 0x20 * i, SeekOrigin.Begin);

            uint r_junk = fsb.ReadUInt32();
            uint r_offset = fsb.ReadUInt32();
            uint r_size = fsb.ReadUInt32();
            uint r_type = fsb.ReadUInt32();
            string r_name = Core.UnpackByteString(866, fsb.ReadBytes(16));

            ResourceNode subnode = new ResourceNode();
            subnode.IsRoot = false;
            subnode.Name = r_name;
            node.Children[i - first] = subnode;

            if (r_type == 1) // directory
            {
                subnode.IsDirectory = true;
                subnode.Offset = 0;
                subnode.Size = 0;
                subnode.Children = new ResourceNode[r_size];
                if (!TreeTraverse(fs, fsb, subnode, offset, r_offset, r_offset + r_size))
                    return false;
            }
            else if (r_type == 0) // file
            {
                subnode.IsDirectory = false;
                subnode.Offset = r_offset;
                subnode.Size = r_size;
            }
            else return false;
        }

        return true;
    }

    // =================================
    //  public methods
    // =================================
    // 
    public static bool AddResource(string filename)
    {
        try
        {
            FileStream fs = File.OpenRead(filename);
            BinaryReader fsb = new BinaryReader(fs);

            if (fsb.ReadUInt32() != ResourceSignature)
            {
                fs.Close();
                return false;
            }

            uint root_offset = fsb.ReadUInt32();
            uint root_size = fsb.ReadUInt32();
            uint res_flags = fsb.ReadUInt32();
            uint fat_offset = fsb.ReadUInt32();
            uint fat_size = fsb.ReadUInt32();

            string resname = Path.GetFileNameWithoutExtension(filename);

            ResourceNode node = new ResourceNode();
            node.Name = resname;
            node.IsRoot = true;
            node.IsDirectory = true;
            node.Source = filename;
            node.Children = new ResourceNode[root_size];
            if (!TreeTraverse(fs, fsb, node, fat_offset, root_offset, root_offset + root_size))
            {
                fs.Close();
                return false;
            }

            if (Roots == null)
                Roots = new ResourceNode[1];
            else Array.Resize<ResourceNode>(ref Roots, Roots.Length + 1);
            Roots[Roots.Length - 1] = node;

            fs.Close();
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static readonly string[] RPathSep = new string[] { "/" };
    private static ResourceNode GetResourceNode(string path)
    {
        string normpath = path.Replace("\\", "/");
        string[] apath = normpath.Split(RPathSep, StringSplitOptions.RemoveEmptyEntries);
        ResourceNode[] toplist = Roots;
        string source = null;
        for (int i = 0; i < apath.Length; i++)
        {
            bool found = false;
            if (toplist == null)
                return null;
            foreach (ResourceNode node in toplist)
            {
                if (node.Name.ToLower().Equals(apath[i].ToLower()))
                {
                    if (i == (apath.Length - 1))
                    {
                        if (node.Source.Length == 0)
                            node.Source = source;
                        return node;
                    }

                    if (!node.IsDirectory)
                        return null; // not found already

                    if (i == 0)
                        source = node.Source;

                    toplist = node.Children;
                    found = true;
                    break;
                }
            }

            if (!found)
                return null;
        }

        return null;
    }

    private static bool FilesLoaded = false;
    private static void UpdateFiles()
    {
        if (FilesLoaded) return;
        FilesLoaded = true;
        AddResource("main.res");
        AddResource("graphics.res");
        AddResource("sfx.res");
        AddResource("music.res");
        AddResource("speech.res");
        AddResource("scenario.res");
        AddResource("world.res");
        AddResource("patch.res");
    }

    // warning: this will always read the whole file into the memory
    // for typical RoM2 files this is okay, though,
    // as files are mostly small (<512kb) and are closed immediately after reading/parsing
    public static MemoryStream OpenRead(string filename)
    {
        UpdateFiles();

        if (!filename.ToLower().StartsWith("patch"))
        {
            if (!Path.IsPathRooted(filename))
            {
                MemoryStream patch_stream = OpenRead("patch\\" + filename);
                if (patch_stream != null)
                    return patch_stream;
            }
        }
            
        ResourceNode node = GetResourceNode(filename);
        if (node == null || node.IsDirectory)
        {
            if (node != null)
                return null;

            try
            {
                // theoretically, I can just return fs here
                // but for transparency we read entire file into memory as well
                FileStream fs = File.OpenRead(filename);
                byte[] allBytes = new byte[fs.Length];
                fs.Seek(0, SeekOrigin.Begin);
                fs.Read(allBytes, 0, (int)fs.Length);
                fs.Close();
                return new MemoryStream(allBytes, false);
            }
            catch (IOException)
            {
                return null;
            }
        }

        try
        {
            FileStream fs = File.OpenRead(node.Source);
            byte[] bytes = new byte[node.Size];
            fs.Seek(node.Offset, SeekOrigin.Begin);
            fs.Read(bytes, 0, (int)node.Size);
            fs.Close();
            return new MemoryStream(bytes, false);
        }
        catch (IOException)
        {
            return null;
        }
    }
}
