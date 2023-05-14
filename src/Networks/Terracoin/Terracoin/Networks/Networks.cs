using Blockcore.Networks;

namespace Terracoin.Networks
{
   public static class Networks
   {
      public static NetworksSelector Terracoin
      {
         get
         {
            return new NetworksSelector(() => new TerracoinMain(), () => new TerracoinTest(), () => new TerracoinRegTest());
         }
      }
   }
}
