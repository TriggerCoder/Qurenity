using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Pathfinding.Ionic.Zip;

public class TextureLoader : MonoBehaviour
{
	public static TextureLoader Instance;
	public Texture illegal;
	public static readonly string FlareTexture = "GFX/MISC/FLARE";
	public enum ImageFilterMode
	{
		Nearest,
		Biliner,
		Average
	}

	public static Dictionary<string, Texture> Textures = new Dictionary<string, Texture>();
	public static Dictionary<string, Texture> ColorizeTextures = new Dictionary<string, Texture>();
	void Awake()
	{
		Instance = this;

	}

	public static void AddNewTexture(string textureName, bool forceSkinAlpha)
	{
		List<QShader> list = new List<QShader>();
		list.Add(new QShader(textureName, 0, 0, forceSkinAlpha));
		LoadJPGTextures(list);
		LoadTGATextures(list);
	}

	public static bool HasTexture(string textureName)
	{
		if (Textures.ContainsKey(textureName))
			return true;

		return false;
	}

	public Texture GetTexture(string textureName)
	{
		if (Textures.ContainsKey(textureName))
			return Textures[textureName];

		Debug.Log("TextureLoader: No texture \"" + textureName + "\"");
		return illegal;
	}

	public static void LoadJPGTextures(List<QShader> mapTextures)
	{
		foreach (QShader tex in mapTextures)
		{
			string path = tex.name.ToUpper() + ".JPG";
			if (PakManager.ZipFiles.ContainsKey(path))
			{
				string FileName = PakManager.ZipFiles[path];
				FileStream stream = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				ZipFile zip = ZipFile.Read(stream);

				byte[] jpgBytes = PakManager.ZipToByteArray(path, ref zip);
				stream.Close();

				Texture2D readyTex = new Texture2D(4, 4);
				readyTex.LoadImage(jpgBytes);
				if (tex.addAlpha)
				{
					Color32[] pulledColors = readyTex.GetPixels32();
					for (int i = 0; i < pulledColors.GetLength(0); i++)
					{
						int gray = (pulledColors[i].r + pulledColors[i].g + pulledColors[i].b) / 2;
						gray = Mathf.Clamp(gray, 0, 255);
						pulledColors[i].a = (byte)gray;
					}
					readyTex.Reinitialize(readyTex.width, readyTex.height, TextureFormat.RGBA32, false);
					readyTex.SetPixels32(pulledColors);
#if UNITY_EDITOR
					readyTex.alphaIsTransparency = true;
#endif
					readyTex.Apply();
				}
				readyTex.name = tex.name;
				readyTex.filterMode = FilterMode.Bilinear;
				CompressTextureNearestPowerOfTwo(ref readyTex);

				if (Textures.ContainsKey(tex.name))
				{
					Debug.Log("Updating texture with name " + tex.name + ".jpg");
					Textures[tex.name] = readyTex;
				}
				else
					Textures.Add(tex.name, readyTex);
			}
		}
	}

	public static void LoadTGATextures(List<QShader> mapTextures)
	{
		foreach (QShader tex in mapTextures)
		{
			string path = tex.name.ToUpper() + ".TGA";
			if (PakManager.ZipFiles.ContainsKey(path))
			{
				string FileName = PakManager.ZipFiles[path];
				FileStream stream = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				ZipFile zip = ZipFile.Read(stream);

				byte[] tgaBytes = PakManager.ZipToByteArray(path, ref zip);
				stream.Close();

				Texture2D readyTex = LoadTGA(path, tgaBytes, tex.addAlpha);

				readyTex.name = tex.name;
				readyTex.filterMode = FilterMode.Bilinear;
				readyTex.Compress(true);

				if (Textures.ContainsKey(tex.name))
				{
					Debug.Log("Updating texture with name " + tex.name + ".tga");
					Textures[tex.name] = readyTex;
				}
				else
					Textures.Add(tex.name, readyTex);
			}
		}
	}
	public static Texture2D LoadTGA(string FileName, byte[] TGABytes, bool forceTransparency)
	{
		// Create a new Texture2D object to hold the parsed TGA data
		Texture2D texture;
		int p = 0;

		// Read the TGA or TARGA header data
		byte idLength = TGABytes[p++];
		byte colorMapType = TGABytes[p++];
		byte imageType = TGABytes[p++];
		ushort colorMapFirstEntryIndex = (ushort)ByteReader.ReadShort(TGABytes, ref p);
		ushort colorMapLength = (ushort)ByteReader.ReadShort(TGABytes, ref p);
		byte colorMapEntrySize = TGABytes[p++];
		ushort xOrigin = (ushort)ByteReader.ReadShort(TGABytes, ref p);
		ushort yOrigin = (ushort)ByteReader.ReadShort(TGABytes, ref p);
		ushort width = (ushort)ByteReader.ReadShort(TGABytes, ref p);
		ushort height = (ushort)ByteReader.ReadShort(TGABytes, ref p);
		byte pixelDepth = TGABytes[p++];
		byte imageDescriptor = TGABytes[p++];

		// Skip the TGA or TARGA ID field
		p += idLength;

		if ((forceTransparency) || (pixelDepth == 32))
			texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
		else
			texture = new Texture2D(width, height);

		Color[] colors = new Color[width * height];
		int currentPixel = 0;

		if (imageType == 10) // RLE compressed
		{
			while (currentPixel < colors.Length)
			{
				// Get the RLE packet header byte
				byte header = TGABytes[p++];

				int packetLength = header & 0x7F;

				// Check if the RLE packet header is a RLE packet
				if ((header & 0x80) != 0)
				{
					// Read the repeated color data
					byte r = TGABytes[p++];
					byte g = TGABytes[p++];
					byte b = TGABytes[p++];
					byte a = (pixelDepth == 32) ? TGABytes[p++] : (byte)0xFF;

					Color color = new Color32(b, g, r, a);

					// Copy the repeated color into the Color array
					for (int i = 0; i <= packetLength; i++)
					{
						colors[currentPixel] = color;
						currentPixel++;
					}
				}
				else
				{
					for (int i = 0; i <= packetLength; i++, currentPixel++)
					{
						// Read the raw color data
						byte r = TGABytes[p++];
						byte g = TGABytes[p++];
						byte b = TGABytes[p++];
						byte a = (pixelDepth == 32) ? TGABytes[p++] : (byte)0xFF;
						if ((forceTransparency) && (pixelDepth != 32))
						{
							int gray = (r + g + b) / 2;
							gray = Mathf.Clamp(gray, 0, 255);
							a = (byte)gray;
						}

						colors[currentPixel] = new Color32(b, g, r, a);
					}
				}
				
			}
		}
		else if (imageType == 2) //Uncompressed
		{
			for (currentPixel = 0; currentPixel < colors.Length; currentPixel++)
			{
				// Read the color data
				byte r = TGABytes[p++];
				byte g = TGABytes[p++];
				byte b = TGABytes[p++];
				byte a = (pixelDepth == 32) ? TGABytes[p++] : (byte)0xFF;

				colors[currentPixel] = new Color32(b, g, r, a);
			}
		}
		else
			Debug.LogError("TGA texture: " + FileName + " unknown type.");

#if UNITY_EDITOR
		texture.alphaIsTransparency = true;
#endif
		texture.SetPixels(colors);
		texture.Apply();

		return texture;
    }

	public static Texture2D CreateLightmapTexture(byte[] rgb)
	{
		Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
		Color32[] colors = new Color32[128 * 128];
		int j = 0;
		for (int i = 0; i < 128 * 128; i++)
			colors[i] = ChangeGamma(rgb[j++], rgb[j++] , rgb[j++]);
		tex.SetPixels32(colors);
		tex.wrapMode = TextureWrapMode.Clamp;
		tex.Apply();
		return tex;
	}
	public static Color32 ChangeGamma(byte r, byte g, byte b)
	{
		float scale = 1.0f, temp;
		float R, G, B;

		R = r * GameManager.Instance.gamma / 255.0f;
		G = g * GameManager.Instance.gamma / 255.0f;
		B = b * GameManager.Instance.gamma / 255.0f;

		if (R > 1.0f && (temp = (1.0f / R)) < scale)
			scale = temp;
		if (G > 1.0f && (temp = (1.0f / G)) < scale)
			scale = temp;
		if (B > 1.0f && (temp = (1.0f / B)) < scale)
			scale = temp;

		scale *= 255f;
		R *= scale;
		G *= scale;
		B *= scale;
		return new Color32((byte)R,(byte)G,(byte)B, 1);
	}

	public static Color ChangeGamma(Color icolor)
	{
		float scale = 1.0f, temp;
		float R, G, B;

		R = icolor.r * GameManager.Instance.gamma;
		G = icolor.g * GameManager.Instance.gamma;
		B = icolor.b * GameManager.Instance.gamma;

		if (R > 1.0f && (temp = (1.0f / R)) < scale)
			scale = temp;
		if (G > 1.0f && (temp = (1.0f / G)) < scale)
			scale = temp;
		if (B > 1.0f && (temp = (1.0f / B)) < scale)
			scale = temp;

		R *= scale;
		G *= scale;
		B *= scale;

		return new Color(R, G, B, 1f);
	}
	public static void CompressTextureNearestPowerOfTwo(ref Texture2D texture)
	{
		int xWidth, xHeight;

		if (Mathf.IsPowerOfTwo(texture.width))
			xWidth = texture.width;
		else
			xWidth = Mathf.ClosestPowerOfTwo(texture.width);

		if (Mathf.IsPowerOfTwo(texture.height))
			xHeight = texture.height;
		else
			xHeight = Mathf.ClosestPowerOfTwo(texture.height);

		if ((xWidth == texture.width) && (xHeight == texture.height))
		{
			texture.Compress(true);
			return;
		}

		CompressResizeTexture(ref texture, ImageFilterMode.Average, xWidth, xHeight);
		return;
	}
	public static void CompressResizeTexture(ref Texture2D texture, ImageFilterMode pFilterMode, int xWidth, int xHeight)
	{
		Color[] aSourceColor = texture.GetPixels(0);
		Vector2 vSourceSize = new Vector2(texture.width, texture.height);

		int xLength = xWidth * xHeight;
		Color[] aColor = new Color[xLength];

		Vector2 vPixelSize = new Vector2(vSourceSize.x / xWidth, vSourceSize.y / xHeight);
		Vector2 vCenter = new Vector2();

		texture.Reinitialize(xWidth, xHeight);

		for (int i = 0; i < xLength; i++)
		{
			float xX = (float)i % xWidth;
			float xY = Mathf.Floor((float)i / xWidth);

			vCenter.x = (xX / xWidth) * vSourceSize.x;
			vCenter.y = (xY / xHeight) * vSourceSize.y;

			switch (pFilterMode)
			{
				case ImageFilterMode.Nearest:
					{
						vCenter.x = Mathf.Round(vCenter.x);
						vCenter.y = Mathf.Round(vCenter.y);

						int xSourceIndex = (int)((vCenter.y * vSourceSize.x) + vCenter.x);

						aColor[i] = aSourceColor[xSourceIndex];
					}
					break;

				case ImageFilterMode.Biliner:
					{
						float xRatioX = vCenter.x - Mathf.Floor(vCenter.x);
						float xRatioY = vCenter.y - Mathf.Floor(vCenter.y);

						int xIndexTL = (int)((Mathf.Floor(vCenter.y) * vSourceSize.x) + Mathf.Floor(vCenter.x));
						int xIndexTR = (int)((Mathf.Floor(vCenter.y) * vSourceSize.x) + Mathf.Ceil(vCenter.x));
						int xIndexBL = (int)((Mathf.Ceil(vCenter.y) * vSourceSize.x) + Mathf.Floor(vCenter.x));
						int xIndexBR = (int)((Mathf.Ceil(vCenter.y) * vSourceSize.x) + Mathf.Ceil(vCenter.x));

						aColor[i] = Color.Lerp(
							Color.Lerp(aSourceColor[xIndexTL], aSourceColor[xIndexTR], xRatioX),
							Color.Lerp(aSourceColor[xIndexBL], aSourceColor[xIndexBR], xRatioX),
							xRatioY
						);
					}
					break;

				case ImageFilterMode.Average:
					{
						int xXFrom = (int)Mathf.Max(Mathf.Floor(vCenter.x - (vPixelSize.x * 0.5f)), 0);
						int xXTo = (int)Mathf.Min(Mathf.Ceil(vCenter.x + (vPixelSize.x * 0.5f)), vSourceSize.x);
						int xYFrom = (int)Mathf.Max(Mathf.Floor(vCenter.y - (vPixelSize.y * 0.5f)), 0);
						int xYTo = (int)Mathf.Min(Mathf.Ceil(vCenter.y + (vPixelSize.y * 0.5f)), vSourceSize.y);

						Color oColorTemp = new Color();
						float xGridCount = 0;
						for (int iy = xYFrom; iy < xYTo; iy++)
						{
							for (int ix = xXFrom; ix < xXTo; ix++)
							{
								oColorTemp += aSourceColor[(int)(((float)iy * vSourceSize.x) + ix)];
								xGridCount++;
							}
						}
						aColor[i] = oColorTemp / (float)xGridCount;
					}
					break;
			}
		}

		texture.SetPixels(aColor);
		texture.Apply();
		texture.Compress(true);

		return;
	}

	public static void ResizeTexture(ref Texture2D texture, int xWidth, int xHeight)
	{
		Color[] aSourceColor = texture.GetPixels(0);
		Vector2 vSourceSize = new Vector2(texture.width, texture.height);

		int xLength = xWidth * xHeight;
		Color[] aColor = new Color[xLength];

		Vector2 vPixelSize = new Vector2(vSourceSize.x / xWidth, vSourceSize.y / xHeight);
		Vector2 vCenter = new Vector2();

		texture.Reinitialize(xWidth, xHeight);

		for (int i = 0; i < xLength; i++)
		{
			float xX = (float)i % xWidth;
			float xY = Mathf.Floor((float)i / xWidth);

			vCenter.x = (xX / xWidth) * vSourceSize.x;
			vCenter.y = (xY / xHeight) * vSourceSize.y;

			int xXFrom = (int)Mathf.Max(Mathf.Floor(vCenter.x - (vPixelSize.x * 0.5f)), 0);
			int xXTo = (int)Mathf.Min(Mathf.Ceil(vCenter.x + (vPixelSize.x * 0.5f)), vSourceSize.x);
			int xYFrom = (int)Mathf.Max(Mathf.Floor(vCenter.y - (vPixelSize.y * 0.5f)), 0);
			int xYTo = (int)Mathf.Min(Mathf.Ceil(vCenter.y + (vPixelSize.y * 0.5f)), vSourceSize.y);

			Color oColorTemp = new Color();
			float xGridCount = 0;
			for (int iy = xYFrom; iy < xYTo; iy++)
			{
				for (int ix = xXFrom; ix < xXTo; ix++)
				{
					oColorTemp += aSourceColor[(int)(((float)iy * vSourceSize.x) + ix)];
					xGridCount++;
				}
			}
			aColor[i] = oColorTemp / (float)xGridCount;
		}

		texture.SetPixels(aColor);
		texture.Apply();

		return;
	}

	public static Texture2D FlipTexture(ref Texture2D texture)
	{
		Texture2D flipped = new Texture2D(texture.width, texture.height);

		int width = texture.width;
		int height = texture.height;

		for (int i = 0; i < width; i++)
			for (int j = 0; j < height; j++)
				flipped.SetPixel(width - i - 1, j, texture.GetPixel(i, j));

		flipped.Apply();

		return flipped;
	}

	public Texture2D GrayScaleTexture(Texture2D texture)
	{
		Texture2D grayscale = new Texture2D(texture.width, texture.height);
		int width = texture.width;
		int height = texture.height;

		for (int i = 0; i < width; i++)
			for (int j = 0; j < height; j++)
			{
				Color rgb = texture.GetPixel(i, j);
				float color = rgb.grayscale;
				grayscale.SetPixel(i, j, new Color(color, color, color, rgb.a));
			}
		grayscale.Apply();

		return grayscale;
	}

	public Texture GetColorizeTexture(string textureName)
	{
		if (ColorizeTextures.ContainsKey(textureName))
			return ColorizeTextures[textureName];

		return illegal;
	}
	public Texture2D ColorizeTexture(Texture2D texture, string textureName, Color sourcecolor)
	{

		Texture2D colorize = GetColorizeTexture(textureName + sourcecolor.ToString()) as Texture2D;
		if (colorize != illegal)
			return colorize;

		if (texture == null)
			texture = GetTexture(textureName) as Texture2D;

		colorize = new Texture2D(texture.width, texture.height);
		int width = texture.width;
		int height = texture.height;

		Color hsl = RGBToHSL(sourcecolor);
		for (int i = 0; i < width; i++)
			for (int j = 0; j < height; j++)
			{
				Color rgb = texture.GetPixel(i, j);
				hsl.b = rgb.grayscale;
				Color color = HSLToRGB(hsl);
				colorize.SetPixel(i, j, new Color(color.r, color.g, color.b, rgb.a));
			}
		colorize.Apply();
		colorize.wrapMode = TextureWrapMode.Clamp;
		colorize.filterMode = FilterMode.Point;

		ColorizeTextures.Add(textureName + sourcecolor.ToString(), colorize);
		return colorize;
	}

	public static int DrawTextureToTexture2D(ref Texture2D destTex, Texture2D srcTex, int offsetX = 0, int offsetY = 0)
	{
		int width = srcTex.width;
		int height = srcTex.height;

		Color[] srcdata = srcTex.GetPixels();
		Color[] destdata = destTex.GetPixels();

		for (int y = 0; y < height; y++)
			for (int x = 0; x < width; x++)
				destdata[y * destTex.width + x + offsetX] = srcdata[y * width + x];

		destTex.SetPixels(destdata);
		destTex.Apply();
		return width;
	}

	public static Color LerpHSV(Color a, Color b, float t)
	{
		// Hue interpolation
		float h = 0;
		float d = b.r - a.r;
		if (a.r > b.r)
		{
			// Swap (a.r, b.r)
			var h3 = b.r;
			b.r = a.r;
			a.r = h3;
			d = -d;
			t = 1 - t;
		}
		if (d > 0.5) // 180deg
		{
			a.r = a.r + 1; // 360deg
			h = (a.r + t * (b.r - a.r)) % 1; // 360deg
		}
		if (d <= 0.5) // 180deg
		{
			h = a.r + t * d;
	
		}
		return new Color(h, a.g + t * (b.g - a.g), a.b + t * (b.b - a.b), a.a + t * (b.a - a.a));
	}
	public static Color RGBToHSL(Color color)
	{
		float r = color.r, g = color.g, b = color.b;
		float max = Math.Max(r, g);
		max = Math.Max(max, b);

		float min = Math.Min(r, g);
		min = Math.Min(min, b);

		float h, s, l = (max + min) / 2;

		if (max == min)
		{
			h = s = 0; // achromatic
		}
		else
		{
			float d = max - min;
			s = l > 0.5f ? d / (2 - max - min) : d / (max + min);
			if (max == r)
				h = (g - b) / d + (g < b ? 6 : 0);
			else if (max == g)
				h = (b - r) / d + 2;
			else
				h = (r - g) / d + 4;
			h /= 6.0f;
		}

		return new Color(h, s, l);
	}
	public static Color HSLToRGB(Color color)
	{
		float h = color.r, s = color.g, l = color.b;
		float r, g, b;
		const float onethird = (1.0f / 3.0f);

		if (s == 0)
		{
			r = g = b = l; // achromatic
		}
		else
		{
			float q = l < 0.5f ? l * (1 + s) : l + s - l * s;
			float p = 2 * l - q;
			r = HUEToRGB(p, q, h + onethird);
			g = HUEToRGB(p, q, h);
			b = HUEToRGB(p, q, h - onethird);
		}

		return new Color(r, g, b);
	}
	public static float HUEToRGB(float p, float q, float t)
	{
		const float onesixth = (1.0f / 6.0f);
		const float twothird = (2.0f / 3.0f);

		if (t < 0)
			t += 1;
		if (t > 1)
			t -= 1;
		if (t < onesixth)
			return p + (q - p) * 6 * t;
		if (t < 0.5f)
			return q;
		if (t < twothird)
			return p + (q - p) * (twothird - t) * 6;
		return p;
	}
}
