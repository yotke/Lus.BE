using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace Lus.Infrastructure.ValuesConverters
{
    public class ListToStringConverter : ValueConverter<ICollection<string>, string>
    {
        private static readonly Expression<Func<ICollection<string>, string>> EncryptExpr = v => string.Join(",", v);

        private static readonly Expression<Func<string, ICollection<string>>> DecryptExpr = v =>
            v.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        public ListToStringConverter(ConverterMappingHints mappingHints = default)
            : base(EncryptExpr, DecryptExpr, mappingHints)
        {
        }
    }
}