using System;

namespace Ade_Framework
{
	public class Single<T>
		where T : class
	{
		private static T instance;
		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					Type type = typeof(T);
					instance = Activator.CreateInstance(type,true) as T;
				}
				return instance;
			}
		}
		protected Single()
		{ }
	}
}
