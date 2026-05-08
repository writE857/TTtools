namespace Ade_Framework
{
	public class Single<T>
		where T : class, new()
	{
		private static T instance;
		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new T();
				}
				return instance;
			}
		}
		protected Single()
		{ }
	}
}
