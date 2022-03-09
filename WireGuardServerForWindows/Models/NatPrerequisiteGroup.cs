namespace WireGuardServerForWindows.Models
{
    public class NatPrerequisiteGroup : PrerequisiteItem
    {
        public NatPrerequisiteGroup(NewNetNatPrerequisite newNetNatPrerequisite, InternetSharingPrerequisite internetSharingPrerequisite, PersistentInternetSharingPrerequisite persistentInternetSharingPrerequisite) : base
        (
            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty
        )
        {
            Children.Add(newNetNatPrerequisite);
            Children.Add(internetSharingPrerequisite);
            Children.Add(persistentInternetSharingPrerequisite);
        }
    }
}
