using NBitcoin;

namespace Impleum.Networks
{
   public static class Networks
   {
      public static NetworksSelector Impleum
      {
         get
         {
            return new NetworksSelector(() => new ImpleumMain(), () => new ImpleumTest(), () => new ImpleumRegTest());
         }
      }
   }
}
