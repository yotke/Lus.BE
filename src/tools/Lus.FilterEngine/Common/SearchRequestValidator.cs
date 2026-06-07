using Lus.FilterEngine.Builder;
using Lus.FilterEngine.Operations;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using Lus.FilterEngine.Attributes;

namespace Lus.FilterEngine.Common
{
    public static class SearchRequestValidator
    {
        private const string PropertyDoesNotExist = "Property {0} does not exist";
        private const string PropertyFilteringIsDisabled = "Filtering is disabled for property {0}";
        private const string PropertyTypeMissmatch = "Property {0} requires type {1}. Value: {2}";
        private const string IncorrectValuesCount = "Property {0} filter has incorrect values count";
        private const string IncorrectPropertyName = "Property name could not be null";
        private const string PropertyValueCannotExceedLimit = "Property value cannot exceed 1024 characters";
        private const string FieldIsNotSelected = "Field {0} is not selected";

        private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> PropertiesDescriptorsPerType =
            new ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>>();

        public static Dictionary<string, string> Validate<TDto>(SearchRequest<TDto> request) where TDto : class
        {
            var errors = new Dictionary<string, string>();

            var modelProperties = GetProperties<TDto>();

            ValidateFilters<TDto>(request.Filters ?? Array.Empty<Filtering>(), modelProperties, errors);
            ValidateFields(request.Fields ?? Array.Empty<string>(), modelProperties, errors);
            ValidateSorts(request.Sorts ?? Array.Empty<Sorting>(), modelProperties, request.Fields?.ToArray() ?? Array.Empty<string>(), errors);
            ValidateFraming(request.Framing?.Skip ?? 0, request.Framing?.Take ?? 10, errors);

            return errors;
        }

        private static void ValidateFilters<TDto>(IEnumerable<Filtering> filters, Dictionary<string, PropertyInfo> modelProperties, Dictionary<string, string> errors)
            where TDto : class
        {
            int filterIdx = -1;
            foreach (var requestFilter in filters)
            {
                filterIdx++;
                if (FilterProfile.DtoPropertyFilterExists(typeof(TDto), requestFilter.PropertyName))
                {
                    //...skip if filter profile contains custom filter for this
                    continue;
                }

                if (string.IsNullOrWhiteSpace(requestFilter.PropertyName))
                {
                    errors.Add($"Filters[{filterIdx}].PropertyName", IncorrectPropertyName);
                    continue;
                }

                if (modelProperties.TryGetValue(requestFilter.PropertyName.Trim(), out var propertyInfo))
                {
                    if (Attribute.IsDefined(propertyInfo, typeof(DisableFilteringAttribute)))
                    {
                        errors.Add($"Filters[{filterIdx}].PropertyName",
                            string.Format(PropertyFilteringIsDisabled, requestFilter.PropertyName));
                        continue;
                    }

                    ValidateFilterParameters(requestFilter, propertyInfo.PropertyType, filterIdx, errors);
                }
                else
                {
                    errors.Add($"Filters[{filterIdx}].PropertyName", string.Format(PropertyDoesNotExist, requestFilter.PropertyName));
                }
            }
        }

        private static void ValidateFilterParameters(
            Filtering requestFilter,
            Type propertyType,
            int filterIdx,
            Dictionary<string, string> errors)
        {
            var parameterIdx = -1;

            foreach (var filterParameter in requestFilter.FilterParameters)
            {
                parameterIdx++;

                // ✅ Special-case null checks: must have NO values
                if (filterParameter.Operation == FilterOperation.IsNull ||
                    filterParameter.Operation == FilterOperation.IsNotNull)
                {
                    if (filterParameter.Values != null && filterParameter.Values.Any())
                    {
                        errors.Add(
                            $"Filters[{filterIdx}].FilterParameters[{parameterIdx}].Values",
                            $"Property {requestFilter.PropertyName} {filterParameter.Operation} must not have values");
                    }
                    // Nothing else to validate for these ops
                    continue;
                }

                // ✅ Values count rules
                // In: allow 1..N values
                // Others (Eq, Gt, Lt, Sw, Ct): require exactly 1 value
                if (filterParameter.Operation == FilterOperation.In)
                {
                    if (filterParameter.Values == null || !filterParameter.Values.Any())
                    {
                        errors.Add(
                            $"Filters[{filterIdx}].FilterParameters[{parameterIdx}].Values",
                            string.Format(IncorrectValuesCount, requestFilter.PropertyName));
                        continue;
                    }
                }
                else
                {
                    if (filterParameter.Values == null || filterParameter.Values.Count() != 1)
                    {
                        errors.Add(
                            $"Filters[{filterIdx}].FilterParameters[{parameterIdx}].Values",
                            string.Format(IncorrectValuesCount, requestFilter.PropertyName));
                        continue;
                    }
                }

                // 🔎 Determine the effective member type (supports inner path)
                var effectiveType = propertyType;
                if (!string.IsNullOrWhiteSpace(filterParameter.InnerPath))
                {
                    try
                    {
                        effectiveType = ResolveInnerMemberType(propertyType, filterParameter.InnerPath!);
                    }
                    catch
                    {
                        errors.Add(
                            $"Filters[{filterIdx}].FilterParameters[{parameterIdx}].InnerPath",
                            $"Cannot resolve InnerPath '{filterParameter.InnerPath}' on '{requestFilter.PropertyName}'.");
                        continue;
                    }
                }

                // 🔤 String-only operations enforcement (Ct/Sw)
                // 🔤 String-only ops (Ct/Sw)
                if (effectiveType != typeof(string) &&
                    (filterParameter.Operation == FilterOperation.Ct ||
                     filterParameter.Operation == FilterOperation.Sw))
                {
                    // ✅ allow when user explicitly said "treat as string"
                    if (!(filterParameter.AsString ?? false))
                    {
                        var propPath = string.IsNullOrWhiteSpace(filterParameter.InnerPath)
                            ? requestFilter.PropertyName
                            : $"{requestFilter.PropertyName}.{filterParameter.InnerPath}";

                        errors.Add(
                            $"Filters[{filterIdx}].FilterParameters[{parameterIdx}].Operation",
                            $"StartsWith/Contains applicable only to string member. Member: {propPath}");
                        continue;
                    }
                }

                // 🧪 Type validation for provided values (based on effective member type)
                ValidateFilterValuesType(
                    filterParameter,
                    effectiveType,
                    requestFilter.PropertyName,
                    filterIdx,
                    parameterIdx,
                    errors);
            }
        }




        // Minimal helper to get the inner member type for a dotted InnerPath ("A.B.C")
        private static Type ResolveInnerMemberType(Type baseType, string innerPath)
        {
            var current = baseType;
            foreach (var segment in innerPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                var prop = current.GetProperty(segment);
                if (prop != null) { current = prop.PropertyType; continue; }

                var field = current.GetField(segment);
                if (field != null) { current = field.FieldType; continue; }

                throw new NotSupportedException(
                    $"Cannot resolve member '{segment}' on type '{current.FullName}'.");
            }
            return current;
        }


        private const int PropertyValueLengthLimit = 1024;
        private static void ValidateFilterValuesType(FilterParameter filterParameter, Type propertyType, string propertyName, int filterIdx, int parameterIdx, Dictionary<string, string> errors)
        {
            var valueIdx = -1;
            foreach (var filterValue in filterParameter.Values)
            {
                valueIdx++;

                if (Nullable.GetUnderlyingType(propertyType) != null && string.IsNullOrEmpty(filterValue))
                {
                    continue;
                }

                if (!String.IsNullOrEmpty(filterValue) && filterValue.Length > PropertyValueLengthLimit)
                {
                    errors.Add(
                        $"Filters[{filterIdx}].FilterParameters[{parameterIdx}].Values[{valueIdx}]",
                        PropertyValueCannotExceedLimit);
                    continue;
                }

                var value = filterValue?.Trim();
                var converter = TypeDescriptor.GetConverter(propertyType);

                try
                {
                    if (value != null) converter.ConvertFromInvariantString(value);
                }
                catch
                {
                    errors.Add(
                        $"Filters[{filterIdx}].FilterParameters[{parameterIdx}].Values[{valueIdx}]",
                        string.Format(PropertyTypeMissmatch, propertyName, propertyType, value));
                }
            }
        }

        private static void ValidateFields(IEnumerable<string> fields, Dictionary<string, PropertyInfo> modelProperties, Dictionary<string, string> errors)
        {
            var fieldIndex = 0;

            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field))
                {
                    errors.Add($"Fields[{fieldIndex}]", IncorrectPropertyName);
                    continue;
                }

                if (!modelProperties.TryGetValue(field.Trim(), out _))
                {
                    errors.Add($"Fields[{fieldIndex}]", string.Format(PropertyDoesNotExist, field));
                }

                fieldIndex++;
            }
        }

        private static void ValidateSorts(IEnumerable<Sorting> sorts, Dictionary<string, PropertyInfo> modelProperties, string[] selectedFields, Dictionary<string, string> errors)
        {
            int sortingIdx = 0;
            foreach (var sort in sorts)
            {
                if (!modelProperties.ContainsKey(sort.SortBy.Trim()))
                {
                    errors.Add($"Sorts[{sortingIdx}].SortBy", string.Format(PropertyDoesNotExist, sort.SortBy));
                }

                if (selectedFields.Any() && !selectedFields.Contains(sort.SortBy))
                {
                    errors.Add($"Sorts[{sortingIdx}].SortBy", string.Format(FieldIsNotSelected, sort.SortBy));
                }

                sortingIdx++;
            }
        }

        private static void ValidateFraming(int skip, int take, Dictionary<string, string> errors)
        {
            if (take <= 0)
            {
                errors.Add("Framing.Take", "Incorrect framing values. Take should be greater than 0");
            }

            if (skip < 0)
            {
                errors.Add("Framing.Skip", "Incorrect framing values. Skip should be greater or equal to 0");
            }
        }

        private static Dictionary<string, PropertyInfo> GetProperties<T>()
        {
            if (!PropertiesDescriptorsPerType.TryGetValue(typeof(T), out var properties))
            {
                properties = typeof(T).GetProperties().ToDictionary(p => p.Name, p => p, StringComparer.InvariantCultureIgnoreCase);
                PropertiesDescriptorsPerType.AddOrUpdate(typeof(T), properties, (t, p) => properties);
            }
            return properties;
        }
    }
}
