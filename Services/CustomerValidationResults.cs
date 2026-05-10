namespace panelapp.Services
{
    public class CustomerValidationResult
    {
        public bool IsValid => !Errors.Any();

        public List<CustomerValidationError> Errors { get; set; } = new();
    }

    public class CustomerValidationError
    {
        public string FieldName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}