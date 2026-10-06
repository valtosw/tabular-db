using System.Globalization;
using System.Text;
using TabularDB.Core.Constants;
using TabularDB.Core.Domain;

namespace TabularDB.Core.Validation
{
    public static class ValueValidator
    {
        public const decimal MaximumMoneyValue = 10_000_000_000_000.00m;

        public static string Validate(string value, TabularDataType dataType, string columnName)
        {
            var providedValue = DomainValidation.Require(value, DomainValidationMessages.MissingValue);
            var trimmedValue = providedValue.Trim();

            return dataType switch
            {
                TabularDataType.Integer => ValidateInteger(trimmedValue, columnName),
                TabularDataType.Real => ValidateReal(trimmedValue, columnName),
                TabularDataType.Character => ValidateCharacter(providedValue, columnName),
                TabularDataType.String => providedValue,
                TabularDataType.Money => ValidateMoney(trimmedValue, columnName),
                TabularDataType.MoneyInterval => ValidateMoneyInterval(trimmedValue, columnName),
                _ => throw new DomainValidationException(DomainValidationMessages.Format(DomainValidationMessages.UnsupportedDataType, columnName))
            };
        }

        private static string ValidateInteger(string value, string columnName)
        {
            var isInteger = long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsedValue);
            DomainValidation.Ensure(isInteger, DomainValidationMessages.Format(DomainValidationMessages.IntegerRequired, columnName));
            return parsedValue.ToString(CultureInfo.InvariantCulture);
        }

        private static string ValidateReal(string value, string columnName)
        {
            var isReal = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue);
            DomainValidation.Ensure(isReal && double.IsFinite(parsedValue), DomainValidationMessages.Format(DomainValidationMessages.RealRequired, columnName));
            return parsedValue.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string ValidateCharacter(string value, string columnName)
        {
            DomainValidation.Ensure(value.EnumerateRunes().Count() == 1, DomainValidationMessages.Format(DomainValidationMessages.CharacterRequired, columnName));
            return value;
        }

        private static string ValidateMoney(string value, string columnName)
        {
            var isMoney = decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var parsedValue);
            var isWithinLimits = isMoney
                && decimal.Round(parsedValue, 2) == parsedValue
                && parsedValue <= MaximumMoneyValue
                && parsedValue >= -MaximumMoneyValue;
            var maximumValue = MaximumMoneyValue.ToString("0.00", CultureInfo.InvariantCulture);

            DomainValidation.Ensure(isWithinLimits,
                DomainValidationMessages.Format(DomainValidationMessages.MoneyRequired, columnName, maximumValue));
            return parsedValue.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string ValidateMoneyInterval(string value, string columnName)
        {
            var bounds = value.Split("..", StringSplitOptions.TrimEntries);
            DomainValidation.Ensure(bounds.Length == 2,
                DomainValidationMessages.Format(DomainValidationMessages.MoneyIntervalRequired, columnName));

            var lowerBound = ValidateMoney(bounds[0], columnName);
            var upperBound = ValidateMoney(bounds[1], columnName);
            var intervalIsOrdered = decimal.Parse(lowerBound, CultureInfo.InvariantCulture)
                <= decimal.Parse(upperBound, CultureInfo.InvariantCulture);
            DomainValidation.Ensure(intervalIsOrdered,
                DomainValidationMessages.Format(DomainValidationMessages.MoneyIntervalOrder, columnName));

            return $"{lowerBound}..{upperBound}";
        }
    }
}