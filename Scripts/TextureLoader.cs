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

	public Texture GetTexture(string textureName)
	{
		if (Textures.ContainsKey(textureName))
			return Textures[textureName];

		Debug.Log("TextureLoader: No texture \"" + textureName + "\"");
		return illegal;
	}

	public static void LoadJPGTextures(List<BSPTexture> mapTextures)
	{
		foreach (BSPTexture tex in mapTextures)
		{
			string path = tex.Name.ToUpper() + ".JPG";
			if (PakManager.ZipFiles.ContainsKey(path))
			{
				string FileName = PakManager.ZipFiles[path];
				FileStream stream = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				ZipFile zip = ZipFile.Read(stream);

				byte[] jpgBytes = PakManager.ZipToByteArray(path, ref zip);
				stream.Close();

				Texture2D readyTex = new Texture2D(4, 4);
				readyTex.LoadImage(jpgBytes);

				readyTex.name = tex.Name;
				readyTex.filterMode = FilterMode.Trilinear;
				CompressTextureNearestPowerOfTwo(ref readyTex);

				if (Textures.ContainsKey(tex.Name))
				{
					Debug.Log("Updating texture with name " + tex.Name + ".jpg");
					Textures[tex.Name] = readyTex;
				}
				else
					Textures.Add(tex.Name, readyTex);
			}
		}
	}

	public static void LoadTGATextures(List<BSPTexture> mapTextures)
	{
		foreach (BSPTexture tex in mapTextures)
		{
			string path = tex.Name.ToUpper() + ".TGA";
			if (PakManager.ZipFiles.ContainsKey(path))
			{
				string FileName = PakManager.ZipFiles[path];
				FileStream stream = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				ZipFile zip = ZipFile.Read(stream);

				byte[] tgaBytes = PakManager.ZipToByteArray(path, ref zip);
				stream.Close();

				Texture2D readyTex = LoadTGA(tgaBytes);

				readyTex.name = tex.Name;
				readyTex.filterMode = FilterMode.Trilinear;
				readyTex.Compress(true);

				if (Textures.ContainsKey(tex.Name))
				{
					Debug.Log("Updating texture with name " + tex.Name + ".tga");
					Textures[tex.Name] = readyTex;
				}
				else
					Textures.Add(tex.Name, readyTex);
			}
		}
	}
	public static Texture2D LoadTGA(byte[] TGABytes)
	{
		// Skip some header info we don't care about.
		// Even if we did care, we have to move the stream seek point to the beginning,
		// as the previous method in the workflow left it at the end.
		int p = 12;

		int width = ByteReader.ReadInt16(TGABytes, ref p);
		int height = ByteReader.ReadInt16(TGABytes, ref p);
		int bitDepth = TGABytes[p++];

		// Skip a byte of header information we don't care about.
		p++;

		Texture2D tex = new Texture2D(width, height);
		Color32[] pulledColors = new Color32[width * height];

		if (bitDepth == 32)
		{
			for (int i = 0; i < width * height; i++)
			{
				byte red = TGABytes[p++];
				byte green = TGABytes[p++];
				byte blue = TGABytes[p++];
				byte alpha = TGABytes[p++];

				pulledColors[i] = new Color32(blue, green, red, alpha);
			}
		}
		else if (bitDepth == 24)
		{
			for (int i = 0; i < width * height; i++)
			{
				byte red = TGABytes[p++];
				byte green = TGABytes[p++];
				byte blue = TGABytes[p++];

				pulledColors[i] = new Color32(blue, green, red, 1);
			}
		}
		else
			throw new Exception("TGA texture had non 32/24 bit depth.");

		tex.SetPixels32(pulledColors);
		tex.Apply();
		return tex;
	}
	public static Texture2D CreateLightmapTexture(byte[] rgb)
	{
		Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
		Color32[] colors = new Color32[128 * 128];
		int j = 0;
		for (int i = 0; i < 128 * 128; i++)
			colors[i] = new Color32(ChangeGamma(rgb[j++]), ChangeGamma(rgb[j++]), ChangeGamma(rgb[j++]), (byte)1f);
		tex.SetPixels32(colors);
		tex.wrapMode = TextureWrapMode.Clamp;
		tex.Apply();
		return tex;
	}
	public static byte ChangeGamma(byte color)
	{
		float scale = 1.0f, temp = 0.0f;
		float icolor = color *  GameManager.Instance.gamma / 255f;

		if (icolor > 1.0f && (temp = (1.0f / icolor)) < scale) 
			scale = temp;
		
		scale *= 255f;
		icolor *= scale;

		return (byte)icolor;
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
			texture.Compress(false);
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
		texture.Compress(false);

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
