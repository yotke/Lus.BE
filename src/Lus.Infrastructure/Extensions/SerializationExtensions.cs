using Newtonsoft.Json;

namespace Lus.Infrastructure.Extensions
{
    public static class SerializationExtensions
    {
        public static string ToModelString<TModel>(this TModel model)
        {
            if (Equals(model, default))
            {
                throw new ArgumentNullException(nameof(model));
            }

            var result = JsonConvert.SerializeObject(model);

            return result;
        }

        public static TModel AsModel<TModel>(this string stringModel)
        {
            if (string.IsNullOrWhiteSpace(stringModel))
            {
                return default;
            }

            try
            {
                return JsonConvert.DeserializeObject<TModel>(stringModel);
            }
            catch (Exception ex)
            {
                return default;
            }
        }
    }
}
