using System.Collections.Generic;

namespace Redbean
{
	public class Container<Key, Value> : IContainer
	{
		private static Dictionary<Key, Value> m_container;
		protected static Dictionary<Key, Value> container
		{
			get
			{
				if (m_container is not null)
					return m_container;

				ApplicationLifeCycle.OnApplicationExit += OnApplicationExit;
				m_container = new Dictionary<Key, Value>();
				
				return m_container;
			}
			
			set => m_container = value;
		}

		private static void OnApplicationExit()
		{
			ApplicationLifeCycle.OnApplicationExit -= OnApplicationExit;
			m_container.Clear();
		}
	}
}