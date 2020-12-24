namespace WireGuardServerForWindows.Models
{
    public class EmptyStringValidation : ConfigurationPropertyValidation
    {
        public EmptyStringValidation(string errorMessageIfEmptyString)
        {
            Validate = obj =>
            {
                string result = default;

                if (string.IsNullOrEmpty(obj.Value))
                {
                    result = errorMessageIfEmptyString;
                }

                return result;
            };
        }
    }
}
