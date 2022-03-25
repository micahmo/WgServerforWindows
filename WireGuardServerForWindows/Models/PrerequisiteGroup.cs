namespace WireGuardServerForWindows.Models
{
    public abstract class PrerequisiteGroup : PrerequisiteItem
    {
        protected PrerequisiteGroup(params PrerequisiteItem[] prerequisiteItems) : base
        (
            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty
        )
        {
            foreach (PrerequisiteItem prerequisiteItem in prerequisiteItems)
            {
                Children.Add(prerequisiteItem);
            }
        }
    }
}
