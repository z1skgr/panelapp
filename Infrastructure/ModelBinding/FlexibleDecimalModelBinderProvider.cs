using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace panelapp.Infrastructure.ModelBinding
{
    public class FlexibleDecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(decimal) ||
                context.Metadata.ModelType == typeof(decimal?))
            {
                return new FlexibleDecimalModelBinder();
            }

            return null;
        }
    }
}