using UnityEngine;

namespace Ade_Framework
{
	public class SingleMono<T> : MonoBehaviour
		where T : SingleMono<T>
	{
		static T instance;
		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					GameObject go = new GameObject("Singleton" + typeof(T));
					DontDestroyOnLoad(go);
					instance = go.AddComponent<T>();
				}
				return instance;
			}
		}
	}
}
