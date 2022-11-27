using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
public struct EntityLump
{
	public EntityLump(string lump)
	{
		EntityString = lump;
	}

	public string EntityString { get; }

	public override string ToString()
	{
		return EntityString;
	}
}
public class BSPHeader
{
	private const int LumpCount = 17;

	private readonly BinaryReader BSP;

	public BSPHeader(BinaryReader BSP)
	{
		this.BSP = BSP;

		ReadMagic();
		ReadVersion();
		ReadLumps();
	}

	public BSPDirectoryEntry[] Directory { get; set; }

	public string Magic { get; private set; }

	public uint Version { get; private set; }

	public string PrintInfo()
	{
		string blob = "\r\n=== BSP Header =====\r\n";
		blob += "Magic Number: " + Magic + "\r\n";
		blob += "BSP Version: " + Version + "\r\n";
		blob += "Header Directory:\r\n";
		int count = 0;
		foreach (BSPDirectoryEntry entry in Directory)
		{
			blob += "Lump " + count + ": " + entry.Name + " Offset: " + entry.Offset + " Length: " + entry.Length +
					"\r\n";
			count++;
		}

		return blob;
	}

	private void ReadLumps()
	{
		Directory = new BSPDirectoryEntry[LumpCount];
		for (int i = 0; i < 17; i++) 
			Directory[i] = new BSPDirectoryEntry(BSP.ReadInt32(), BSP.ReadInt32());

		Directory[LumpType.Entities].Name = "Entities";
		Directory[LumpType.Textures].Name = "Textures";
		Directory[LumpType.Planes].Name = "Planes";
		Directory[LumpType.Nodes].Name = "Nodes";
		Directory[LumpType.Leafs].Name = "Leafs";
		Directory[LumpType.LeafFaces].Name = "Leaf Faces";
		Directory[LumpType.LeafBushes].Name = "Leaf brushes";
		Directory[LumpType.Models].Name = "Models";
		Directory[LumpType.Brushes].Name = "Brushes";
		Directory[LumpType.BrushSides].Name = "Brush Sides";
		Directory[LumpType.Vertexes].Name = "Vertexes";
		Directory[LumpType.MeshVerts].Name = "Mesh Vertexes";
		Directory[LumpType.Effects].Name = "Effects";
		Directory[LumpType.Faces].Name = "Faces";
		Directory[LumpType.LightMaps].Name = "Light Maps";
		Directory[LumpType.LightVols].Name = "Light volumes";
		Directory[LumpType.VisData].Name = "Vis data";
	}

	private void ReadMagic()
	{
		BSP.BaseStream.Seek(0, SeekOrigin.Begin);
		Magic = new string(BSP.ReadChars(4));
	}

	private void ReadVersion()
	{
		BSP.BaseStream.Seek(4, SeekOrigin.Begin);
		Version = BSP.ReadUInt32();
	}
}
public class BSPDirectoryEntry
{
	public BSPDirectoryEntry(int offset, int length)
	{
		Offset = offset;
		Length = length;
	}

	public int Offset { get; }

	public int Length { get; }

	public string Name { get; set; }

	public bool Validate()
	{
		if (Length % 4 == 0)
			return true;
		return false;
	}
}
public class BSPTexture
{
	public BSPTexture(string rawName, int flags, int contents)
	{
		//The string is read as 64 characters, which includes a bunch of null bytes.  We strip them to avoid oddness when printing and using the texture names.
		Name = rawName.Replace("\0", string.Empty);
		Flags = flags;
		Contents = contents;

		// Remove some common shader modifiers to get normal
		// textures instead. This is kind of a hack, and could
		// bit you if a texture just happens to have any of these
		// in its name but isn't actually a shader texture.
		Name = Name.Replace("_hell", string.Empty);
		Name = Name.Replace("_trans", string.Empty);
		Name = Name.Replace("flat_400", string.Empty);
		Name = Name.Replace("_750", string.Empty);
	}

	public string Name { get; }

	public int Flags { get; }

	public int Contents { get; }
}