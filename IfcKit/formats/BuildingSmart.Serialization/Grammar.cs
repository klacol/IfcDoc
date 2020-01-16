using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BuildingSmart.Serialization
{
	public abstract class Grammar
	{
		/// <summary>
		/// Reads language structure from a directory path.
		/// </summary>
		/// <param name="path">Directory path</param>
		/// <returns>Assembly of type definitions.</returns>
		public abstract Type Read(string path);

		/// <summary>
		/// Writes language structure to a directory path.
		/// </summary>
		/// <param name="path">Directory path</param>
		/// <param name="assembly">Assembly of type definitions</param>
		public abstract void Write(string path, Type typeRoot);
	}
}
