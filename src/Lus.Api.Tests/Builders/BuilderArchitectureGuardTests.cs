using System.Reflection;
using Lus.Application.Common.Builders;
using Xunit;

namespace Lus.Api.Tests.Builders
{
    public class BuilderArchitectureGuardTests
    {
        private static readonly Assembly ApplicationAssembly = typeof(IBuilderAgentCatalog).Assembly;

        [Fact]
        public void CommonBuildersKernel_ReferencesNoEntitySpecificTypes()
        {
            var kernelTypes = ApplicationAssembly.GetTypes()
                .Where(t => t.Namespace == "Lus.Application.Common.Builders")
                .ToList();

            Assert.NotEmpty(kernelTypes);

            foreach (var type in kernelTypes)
            {
                var offenders = ReferencedTypes(type)
                    .Where(IsEntitySpecific)
                    .Select(r => $"{type.Name} → {r.FullName}")
                    .ToList();

                Assert.True(
                    offenders.Count == 0,
                    "Common/Builders is entity-agnostic BY LAW — Documents types must never leak into kernel signatures: "
                    + string.Join("; ", offenders));
            }
        }

        private static bool IsEntitySpecific(Type type) =>
            type.Namespace is { } ns &&
            (ns.StartsWith("Lus.Application.Documents", StringComparison.Ordinal)
             || ns.StartsWith("Lus.Contracts.Documents", StringComparison.Ordinal));

        private static IEnumerable<Type> ReferencedTypes(Type type)
        {
            static IEnumerable<Type> Unwrap(Type t)
            {
                if (t.IsGenericType)
                    foreach (var arg in t.GetGenericArguments())
                        foreach (var inner in Unwrap(arg))
                            yield return inner;
                yield return t.IsGenericType ? t.GetGenericTypeDefinition() : t;
            }

            const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic
                                   | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            var referenced = new List<Type>();
            if (type.BaseType is not null) referenced.AddRange(Unwrap(type.BaseType));
            referenced.AddRange(type.GetInterfaces().SelectMany(Unwrap));
            referenced.AddRange(type.GetProperties(All).SelectMany(p => Unwrap(p.PropertyType)));
            referenced.AddRange(type.GetFields(All).SelectMany(f => Unwrap(f.FieldType)));
            foreach (var method in type.GetMethods(All).Where(m => !m.IsSpecialName))
            {
                referenced.AddRange(Unwrap(method.ReturnType));
                referenced.AddRange(method.GetParameters().SelectMany(p => Unwrap(p.ParameterType)));
            }

            return referenced.Where(r => r != type);
        }

        [Fact]
        public void DocumentsBuilder_DoesNotReferenceOtherEntityBuilders()
        {
            var builderTypes = ApplicationAssembly.GetTypes()
                .Where(t => t.Namespace is { } ns && ns.StartsWith("Lus.Application.Documents.Builder", StringComparison.Ordinal))
                .ToList();

            Assert.NotEmpty(builderTypes);

            foreach (var type in builderTypes)
            {
                var offenders = ReferencedTypes(type)
                    .Where(r => r.Namespace is { } ns &&
                                (ns.StartsWith("Lus.Application.Organizations", StringComparison.Ordinal)
                                 || ns.StartsWith("Lus.Application.Rules", StringComparison.Ordinal)))
                    .Select(r => $"{type.Name} → {r.FullName}")
                    .ToList();

                Assert.True(offenders.Count == 0, string.Join("; ", offenders));
            }
        }

        [Fact]
        public void SessionSchemaVersion_IsAtLeastOne()
        {
            Assert.True(
                Lus.Application.Documents.Builder.Services.DocumentBuildSession.CurrentSchemaVersion >= 1,
                "SessionSchemaVersion may only ever increase.");
        }
    }
}
