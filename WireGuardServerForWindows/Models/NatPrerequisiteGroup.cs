namespace WireGuardServerForWindows.Models
{
    public class NatPrerequisiteGroup : PrerequisiteGroup
    {
        public NatPrerequisiteGroup(NewNetNatPrerequisite newNetNatPrerequisite, InternetSharingPrerequisite internetSharingPrerequisite, PersistentInternetSharingPrerequisite persistentInternetSharingPrerequisite) : base
        (
            newNetNatPrerequisite, internetSharingPrerequisite, persistentInternetSharingPrerequisite
        )
        {
        }
    }
}
