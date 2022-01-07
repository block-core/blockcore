namespace Blockcore.Networks.SERF
{
   public static class Networks
   {
      public static NetworksSelector SERF
      {
         get
         {
            return new NetworksSelector(() => new SERFMain(), () => new SERFTest(), () => new SERFRegTest());
         }
      }
   }
}
