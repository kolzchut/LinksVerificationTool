using System;
using System.Collections.Generic;
using System.Text;

namespace KolZchutLinksVerification
{
	public static class StringExtension
	{
		public static bool IsNullOrWhiteSpace(this String s)
		{
			return (s == null || String.IsNullOrEmpty(s.TrimStart()));
		}
	}
}
