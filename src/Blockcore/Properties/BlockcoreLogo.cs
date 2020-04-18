namespace Blockcore.Properties
{
    public static class BlockcoreLogo
    {
        static BlockcoreLogo()
        {
            LogoTemplate = Resources.Logo;
            SetTitle("Full Consensus Validating Node");
        }

        public static void SetTitle(string title)
        {
            Logo = LogoTemplate.Replace("{title}", title);
        }

        public static string LogoTemplate { get; set; }

        public static string Logo { get; set; }
    }
}