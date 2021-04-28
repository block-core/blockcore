using Blockcore.Networks;
using NBitcoin;

namespace Blockcore.Networks.City.Networks
{
   public static class Networks
   {
      public static NetworksSelector City
      {
         get
         {
            return new NetworksSelector(() => new CityMain(), () => new CityTest(), () => new CityRegTest());
         }
      }
   }
}
