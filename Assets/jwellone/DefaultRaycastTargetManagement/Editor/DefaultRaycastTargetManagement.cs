using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace jwellone.UI.Editor
{
	[InitializeOnLoadAttribute]
	public static class DefaultRaycastTargetManagement
	{
		private const float PREFAB_EDIT_MODE_INTERVAL = 0.15f;
		private static long s_hashSum = 0L;
		private static float s_prefabEditModeUpdateInterval;
		private static HashSet<Graphic> s_graphics = new HashSet<Graphic>();

		static DefaultRaycastTargetManagement()
		{
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			EditorApplication.update += OnPrefabStageUpdate;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnHierarchyChanged()
		{
			if (Application.isPlaying)
			{
				return;
			}

			if (PrefabStageUtility.GetCurrentPrefabStage() != null)
			{
				return;
			}

			var validScenes = new List<Scene>(SceneManager.sceneCount);
			for (var i = 0; i < SceneManager.sceneCount; ++i)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (scene.isLoaded && scene.IsValid())
				{
					validScenes.Add(scene);
				}
			}

			var sum = 0L;
			foreach (var scene in validScenes)
			{
				sum += scene.GetHashCode();
			}

			if (sum != s_hashSum)
			{
				s_hashSum = sum;
				s_graphics.Clear();
				foreach (var scene in validScenes)
				{
					foreach (var obj in scene.GetRootGameObjects())
					{
						var graphics = obj.GetComponentsInChildren<Graphic>();
						foreach (var graphic in graphics)
						{
							s_graphics.Add(graphic);
						}
					}
				}
				return;
			}

			foreach (var scene in validScenes)
			{
				foreach (var obj in scene.GetRootGameObjects())
				{
					AddNewGraphics(obj.GetComponentsInChildren<Graphic>());
				}
			}
		}

		private static void OnPrefabStageUpdate()
		{
			if (Application.isPlaying)
			{
				return;
			}

			s_prefabEditModeUpdateInterval -= Time.fixedUnscaledDeltaTime;
			if (s_prefabEditModeUpdateInterval > 0f)
			{
				return;
			}
			s_prefabEditModeUpdateInterval = PREFAB_EDIT_MODE_INTERVAL;

			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage == null)
			{
				return;
			}

			var hashCode = prefabStage.GetHashCode();
			if (hashCode != s_hashSum)
			{
				s_hashSum = hashCode;
				s_graphics.Clear();

				var graphics = prefabStage.prefabContentsRoot.GetComponentsInChildren<Graphic>();
				foreach (var graphic in graphics)
				{
					s_graphics.Add(graphic);
				}
				return;
			}

			AddNewGraphics(prefabStage.prefabContentsRoot.GetComponentsInChildren<Graphic>());
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.EnteredPlayMode)
			{
				Clear();
			}
		}

		private static void Clear()
		{
			s_graphics.Clear();
			s_hashSum = 0L;
			s_prefabEditModeUpdateInterval = 0f;
		}

		private static void AddNewGraphics(in Graphic[] graphics)
		{
			foreach (var graphic in graphics)
			{
				if (s_graphics.Contains(graphic))
				{
					continue;
				}

				if (PrefabUtility.GetPrefabInstanceHandle(graphic)==null)
				{
					graphic.raycastTarget = GetRaycastTarget(graphic);
				}
				s_graphics.Add(graphic);
			}
		}

		private static bool GetRaycastTarget(in Graphic graphic)
		{
			if (graphic.GetComponent<IEventSystemHandler>() != null)
			{
				return true;
			}

			var parent = graphic.transform.parent?.gameObject;
			if (parent == null)
			{
				return false;
			}

			if (parent.GetComponent<Slider>() != null || parent.GetComponent<Toggle>() != null)
			{
				return true;
			}

			return false;
		}
	}
}
