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
					instance = FindObjectOfType<T>();
					if (instance == null)
					{
						GameObject go = new GameObject("Singleton" + typeof(T).Name);
						DontDestroyOnLoad(go);
						instance = go.AddComponent<T>();
					}
				}
				return instance;
			}
		}

		protected virtual void Awake()
		{
			if (instance == null)
			{
				instance = this as T;
			}
		}

		protected virtual void OnDestroy()
		{
			if (instance == this as T)
			{
				instance = null;
			}
		}
	}
}
