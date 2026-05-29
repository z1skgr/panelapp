using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace panelapp.Infrastructure.ModelBinding
{
    public class FlexibleDecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (valueResult == ValueProviderResult.None)
                return Task.CompletedTask;

            var value = valueResult.FirstValue;

            if (string.IsNullOrWhiteSpace(value))
            {
                bindingContext.Result = ModelBindingResult.Success(0m);
                return Task.CompletedTask;
            }

            value = value.Trim();

            decimal result;

            // 53,01 => 53.01
            // Αν έχει κόμμα αλλά όχι τελεία, το κόμμα είναι decimal separator.
            if (value.Contains(',') && !value.Contains('.'))
            {
                var normalized = value.Replace(',', '.');

                if (decimal.TryParse(
                        normalized,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out result))
                {
                    bindingContext.Result = ModelBindingResult.Success(result);
                    return Task.CompletedTask;
                }
            }

            // 53.01
            if (decimal.TryParse(
                    value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                bindingContext.Result = ModelBindingResult.Success(result);
                return Task.CompletedTask;
            }

            // Fallback για current culture
            if (decimal.TryParse(
                    value,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out result))
            {
                bindingContext.Result = ModelBindingResult.Success(result);
                return Task.CompletedTask;
            }

            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                "Μη έγκυρη δεκαδική τιμή.");

            return Task.CompletedTask;
        }
    }
}