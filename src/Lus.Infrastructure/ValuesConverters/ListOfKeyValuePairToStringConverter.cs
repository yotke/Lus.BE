using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace Lus.Infrastructure.ValuesConverters
{
    public class ListOfKeyValuePairToStringConverter : ValueConverter<ICollection<KeyValuePair<string, string>>, string>
    {
        private static readonly Expression<Func<ICollection<KeyValuePair<string, string>>, string>> EncryptExpr = v => string.Join(",", v.Select(c => $"{c.Key}={c.Value}"));
        private static readonly Expression<Func<string, ICollection<KeyValuePair<string, string>>>> DecryptExpr = v => v
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries))
            .Select(c => new KeyValuePair<string, string>(c.FirstOrDefault(), c.LastOrDefault())).ToList();

        public ListOfKeyValuePairToStringConverter(ConverterMappingHints mappingHints = default)
            : base(EncryptExpr, DecryptExpr, mappingHints)
        {
        }
    }
}
