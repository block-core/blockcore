namespace Blockcore.Features.Wallet.Broadcasting
{
    public enum TransactionBroadcastState
    {
        CantBroadcast,
        ToBroadcast,
        Broadcasted,
        Propagated
    }
}
