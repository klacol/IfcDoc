// Name:        IfcGloballyUniqueId.cs
// Description: Formats and parses GUIDs in compact format.
// Author:      Tim Chipman
// Origination: This is based on prior work of Constructivity donated to BuildingSmart at no charge.
// Copyright:   (c) 2012 BuildingSmart International Ltd., (c) 2006-2012 Constructivity.com LLC.
// Note:        This specific file has dual copyright such that both organizations maintain all rights to its use.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.Text;

namespace IfcDoc
{
	public class IfcGloballyUniqueId
	{
		private static string cConversionTable = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_$";

		public static Guid Parse(string format64)
		{
			int i, j, m;
			uint[] num = new uint[6];

			if (format64 == null)
			{
				throw new ArgumentNullException("format64");
			}

			format64 = format64.Trim('\'');

			if (format64.Length != 22)
			{
				throw new ArgumentException("Invalid Global ID Length: " + format64);
			}

			j = 0;
			m = 2;

			for (i = 0; i < 6; i++)
			{
				string temp = format64.Substring(j, m);
				j = j + m;
				m = 4;

				num[i] = cv_from_64(temp);
			}

			uint Data1 = (uint)(num[0] * 16777216 + num[1]);                 // 16-13. bytes
			ushort Data2 = (ushort)(num[2] / 256);                              // 12-11. bytes
			ushort Data3 = (ushort)((num[2] % 256) * 256 + num[3] / 65536);     // 10-09. bytes

			byte Data4_0 = (byte)((num[3] / 256) % 256);                   //    08. byte
			byte Data4_1 = (byte)(num[3] % 256);                           //    07. byte
			byte Data4_2 = (byte)(num[4] / 65536);                         //    06. byte
			byte Data4_3 = (byte)((num[4] / 256) % 256);                   //    05. byte
			byte Data4_4 = (byte)(num[4] % 256);                           //    04. byte
			byte Data4_5 = (byte)(num[5] / 65536);                         //    03. byte
			byte Data4_6 = (byte)((num[5] / 256) % 256);                   //    02. byte
			byte Data4_7 = (byte)(num[5] % 256);                           //    01. byte

			return new Guid(Data1, Data2, Data3, Data4_0, Data4_1, Data4_2, Data4_3, Data4_4, Data4_5, Data4_6, Data4_7);
		}

		public static string Format(Guid pGuid)
		{
			uint[] num = new uint[6];
			int i, n;

			byte[] comp = pGuid.ToByteArray();
			uint Data1 = BitConverter.ToUInt32(comp, 0);
			ushort Data2 = BitConverter.ToUInt16(comp, 4);
			ushort Data3 = BitConverter.ToUInt16(comp, 6);
			byte Data4_0 = comp[8];
			byte Data4_1 = comp[9];
			byte Data4_2 = comp[10];
			byte Data4_3 = comp[11];
			byte Data4_4 = comp[12];
			byte Data4_5 = comp[13];
			byte Data4_6 = comp[14];
			byte Data4_7 = comp[15];

			//
			// Creation of six 32 Bit integers from the components of the GUID structure
			//
			num[0] = (uint)(Data1 / 16777216);                                                 //    16. byte  (pGuid->Data1 / 16777216) is the same as (pGuid->Data1 >> 24)
			num[1] = (uint)(Data1 % 16777216);                                                 // 15-13. bytes (pGuid->Data1 % 16777216) is the same as (pGuid->Data1 & 0xFFFFFF)
			num[2] = (uint)(Data2 * 256 + Data3 / 256);                                 // 12-10. bytes
			num[3] = (uint)((Data3 % 256) * 65536 + Data4_0 * 256 + Data4_1);  // 09-07. bytes
			num[4] = (uint)(Data4_2 * 65536 + Data4_3 * 256 + Data4_4);       // 06-04. bytes
			num[5] = (uint)(Data4_5 * 65536 + Data4_6 * 256 + Data4_7);       // 03-01. bytes

			StringBuilder buf = new StringBuilder();

			//
			// Conversion of the numbers into a system using a base of 64
			//
			n = 2;
			for (i = 0; i < 6; i++)
			{
				string temp = cv_to_64(num[i], n);
				buf.Append(temp);
				n = 4;
			}

			return buf.ToString();
		}

		//
		// Conversion of an integer into a number with base 64
		// using the coside table cConveronTable
		//
		private static string cv_to_64(uint number, int nDigits)
		{
			uint act;
			int iDigit;
			char[] result = new char[nDigits];

			act = number;

			for (iDigit = 0; iDigit < nDigits; iDigit++)
			{
				result[nDigits - iDigit - 1] = cConversionTable[(int)(act % 64)];
				act /= 64;
			}

			if (act != 0)
			{
				throw new ArgumentException("Number out of range");
			}

			return new string(result);
		}

		//
		// The reverse function to calculate the number from the code
		//
		private static uint cv_from_64(string str)
		{
			int len, i, j, index;

			len = str.Length;
			if (len > 4)
			{
				throw new ArgumentException("Invalid Global ID Format");
			}

			uint pRes = 0;

			for (i = 0; i < len; i++)
			{
				index = -1;
				for (j = 0; j < 64; j++)
				{
					if (cConversionTable[j] == str[i])
					{
						index = j;
						break;
					}
				}

				if (index == -1)
				{
					throw new ArgumentException("Invalid Global ID Format");
				}

				pRes = (uint)(pRes * 64 + index);
			}

			return pRes;
		}

	}
}
