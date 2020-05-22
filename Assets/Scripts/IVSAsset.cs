
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using System.Linq;

using UnityEditor;
#endif

namespace GK {
	public class IVSAsset : ScriptableObject {
		public Vector2[] Points;

#if UNITY_EDITOR
		static Vector2[] Parse(string text) {
			return text
				.Split('\n')
				.Where(str => str.IndexOf(',') != -1)
				.Select(str => str
					.Split(',')
					.Select(s => float.Parse(s))
					.ToArray())
				.Select(p => new Vector2(p[0], p[1]))
				.ToArray();
		}

		[MenuItem("Assets/Create IVS Asset", true, 2000)]
		public static bool IsTextAsset() {
			return Selection.activeObject != null && Selection.activeObject is TextAsset;
		}

		[MenuItem("Assets/Create IVS Asset", false, 2000)]
		public static void CreateIVSAsset() {
			if (!IsTextAsset()) return;

			var asset = Selection.activeObject as TextAsset;

			var path = AssetDatabase.GetAssetPath(asset);

			if (path == null || path == "") return;
				
			var newPath = Path.ChangeExtension(path, ".asset");
			var newAsset = ScriptableObject.CreateInstance<IVSAsset>();
			newAsset.Points = Parse(asset.text);

			AssetDatabase.CreateAsset(newAsset, newPath);
		}
#endif
	}
}
