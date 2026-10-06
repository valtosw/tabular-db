using TabularDB.Core.Constants;

namespace TabularDB.Core.Validation
{
    internal static class NameRules
    {
        public static string Validate(string name, string kind)
        {
            var validatedName = DomainValidation.Require(name?.Trim(),
                DomainValidationMessages.Format(DomainValidationMessages.EmptyName, kind));
            DomainValidation.Ensure(validatedName.Length <= DomainValidationMessages.MaximumNameLength,
                DomainValidationMessages.Format(DomainValidationMessages.NameTooLong, kind, DomainValidationMessages.MaximumNameLength));
            return validatedName;
        }

        public static void EnsureUnique(IEnumerable<string> names, string kind)
        {
            List<string> validatedNames = [.. names.Select(name => Validate(name, kind))];
            var namesAreUnique = validatedNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() == validatedNames.Count;
            DomainValidation.Ensure(namesAreUnique, DomainValidationMessages.Format(DomainValidationMessages.DuplicateNames, kind));
        }
    }
}
