using System;

namespace WgAPI.Commands
{
    public abstract class GenerateKeyCommand : WireGuardCommand
    {
        protected GenerateKeyCommand(KeyType keyType, string seedKey = "") : base
        (
            @switch: keyType.ToCommand(),
            whichExe: WhichExe.WGExe
        )
        {
            StandardInput = seedKey;
        }
    }

    public enum KeyType
    {
        PrivateKey,
        PresharedKey,
        PublicKey
    }

    internal static class KeyTypeExtensions
    {
        public static string ToCommand(this KeyType keyType) => keyType switch
        {
            KeyType.PrivateKey => "genkey",
            KeyType.PresharedKey => "genpsk",
            KeyType.PublicKey => "pubkey",
            _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null)
        };
    }
}
