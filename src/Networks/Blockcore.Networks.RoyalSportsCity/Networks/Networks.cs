using Blockcore.Networks;

namespace Blockcore.Networks.RoyalSportsCity.Networks
{
   public static class Networks
   {
      public static NetworksSelector RoyalSportsCity
      {
         get
         {
            return new NetworksSelector(() => new RoyalSportsCityMain(), () => new RoyalSportsCityTest(), () => new RoyalSportsCityRegTest());
         }
      }
   }
}
