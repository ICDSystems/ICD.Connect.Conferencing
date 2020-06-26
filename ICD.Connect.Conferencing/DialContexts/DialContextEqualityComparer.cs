using System.Collections.Generic;

namespace ICD.Connect.Conferencing.DialContexts
{
	public sealed class DialContextEqualityComparer : IEqualityComparer<IDialContext>
	{
		private static DialContextEqualityComparer s_Instance;

		public static DialContextEqualityComparer Instance
		{
			get { return s_Instance ?? (s_Instance = new DialContextEqualityComparer()); }
		}

		/// <summary>Determines whether the specified objects are equal.</summary>
		/// <param name="x">The first object of type T to compare.</param>
		/// <param name="y">The second object of type T to compare.</param>
		/// <returns>true if the specified objects are equal; otherwise, false.</returns>
		public bool Equals(IDialContext x, IDialContext y)
		{
			if (x == null && y == null)
				return true;

			if (x == null || y == null)
				return false;

			return x.CallType == y.CallType && x.Protocol == y.Protocol 
			                                && x.DialString == y.DialString 
			                                && x.Password == y.Password;

		}

		/// <summary>Returns a hash code for the specified object.</summary>
		/// <param name="obj">The <see cref="T:System.Object"></see> for which a hash code is to be returned.</param>
		/// <returns>A hash code for the specified object.</returns>
		/// <exception cref="T:System.ArgumentNullException">The type of <paramref name="obj">obj</paramref> is a reference type and <paramref name="obj">obj</paramref> is null.</exception>
		public int GetHashCode(IDialContext obj)
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + (int)obj.CallType;
				hash = hash * 23 + (int)obj.Protocol;
				hash = hash * 23 + (obj.DialString == null ? 0 : obj.DialString.GetHashCode());
				hash = hash * 23 + (obj.Password == null ? 0 : obj.Password.GetHashCode());
				return hash;
			}
		}
	}
}