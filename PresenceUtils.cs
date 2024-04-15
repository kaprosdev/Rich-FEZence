using FezEngine.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace RichFEZence
{
	internal class PresenceUtils
	{

		readonly static char[] GLITCH_CHARS = "░▒▓┤╡╢╖╕╣║╗╝╜╛┐└┴┬├┼╞╟╚╔╩╦╠╬╧╨╤╥╙╘╒╓╫╪┘┌█▄▌▐▀■".ToCharArray();

		const float GLITCH_PROBABILITY = 0.2f;

		public static string GlitchString(string baseStr)
		{
			char[] fullstr = baseStr.ToCharArray();
			for (int i = 0; i < fullstr.Length; i++)
			{
				if (RandomHelper.Probability(GLITCH_PROBABILITY))
				{
					fullstr[i] = GetGlitchChar();
				}
			}
			return string.Join("", fullstr);
		}

		public static char GetGlitchChar()
		{
			return RandomHelper.InList(GLITCH_CHARS);
		}

		// https://stackoverflow.com/questions/6198744/convert-string-utf-16-to-utf-8-in-c-sharp saved my LIFE
		public static string Utf16ToUtf8(string utf16String)
		{
			// Get UTF16 bytes and convert UTF16 bytes to UTF8 bytes
			byte[] utf16Bytes = Encoding.Unicode.GetBytes(utf16String);
			byte[] utf8Bytes = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, utf16Bytes);

			// Return UTF8 bytes as ANSI string
			return Encoding.Default.GetString(utf8Bytes);
		}
	}
}
