namespace Blockcore.Networks.x42.Networks
{
    public static class Networks
   {
      public static NetworksSelector x42
      {
         get
         {
            return new NetworksSelector(() => new x42Main(), () => new x42Test(), () => new x42RegTest());
         }
      }
   }
}
