using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExtensionMethods
{
	public static class DictionaryExtensions
	{
		public static bool TryGetNumValue(this Dictionary<string, string> entityData, string key, out float num)
		{
			int inum = 0;
			num = 0;
			string strWord;
			if (entityData.TryGetValue(key, out strWord))
			{
				if (int.TryParse(strWord, out inum))
					num = inum;
				else
					num = float.Parse(strWord);
				return true;
			}
			return false;
		}
	}
}
