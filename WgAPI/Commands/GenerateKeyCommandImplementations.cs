namespace WgAPI.Commands
{
    public class GeneratePrivateKeyCommand : GenerateKeyCommand
    {
        public GeneratePrivateKeyCommand() : base(KeyType.PrivateKey) { }
    }

    public class GeneratePresharedKeyCommand : GenerateKeyCommand
    {
        public GeneratePresharedKeyCommand() : base(KeyType.PresharedKey) { }
    }

    public class GeneratePublicKeyCommand : GenerateKeyCommand
    {
        public GeneratePublicKeyCommand(string privateKey) : base(KeyType.PublicKey, seedKey: privateKey) { }
    }
}
