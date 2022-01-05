using Blockcore.Networks;

namespace Blockcore.Networks.SeniorBlockCoin.Networks
{
   public static class Networks
   {
      public static NetworksSelector SeniorBlockCoin
      {
         get
         {
            return new NetworksSelector(() => new SeniorBlockCoinMain(), () => new SeniorBlockCoinTest(), () => new SeniorBlockCoinRegTest());
         }
      }
   }
}
