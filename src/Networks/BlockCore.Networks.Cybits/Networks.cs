using Blockcore.Networks;

namespace Blockcore.Networks.Cybits
{
   public static class Networks
   {
      public static NetworksSelector Cybits
      {
         get
         {
            return new NetworksSelector(() => new CybitsMain(), () => new CybitsTest(), () => new CybitsRegTest());
         }
      }
   }
}
