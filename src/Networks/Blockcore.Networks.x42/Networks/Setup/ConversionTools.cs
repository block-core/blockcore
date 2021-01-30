using System;
using System.Linq;

namespace Blockcore.Networks.x42.Networks.Setup
{
    public class ConversionTools
   {
      public static uint ConvertToUInt32(string magicText, bool reverse = false)
      {
         byte[] number = magicText.Split('-').Select(b => Convert.ToByte(b, 16)).ToArray();

         if (reverse)
         {
            Array.Reverse(number);
         }

         return BitConverter.ToUInt32(number);
      }
   }
}
