using Lus.Application.Common.Builders;
using Lus.Application.Documents.Builder.Agents;
using Xunit;

namespace Lus.Api.Tests.Documents
{
    public class DocumentBuilderAgentCatalogTests
    {
        private readonly DocumentBuilderAgentCatalog catalog = new();

        [Fact]
        public void Every_name_is_doc_star()
        {
            Assert.All(this.catalog.All, d => Assert.StartsWith("doc.", d.Name, StringComparison.Ordinal));
        }

        [Fact]
        public void Content_wave_orders_row_extractor_before_formatter()
        {
            var content = this.catalog.Content;
            Assert.Contains(content, c => c.Name == "doc.row_extractor");
            Assert.Contains(content, c => c.Name == "doc.formatter");
            Assert.DoesNotContain(content, c => c.Name == "doc.echo");
            var names = content.Select(c => c.Name).ToList();
            Assert.True(names.IndexOf("doc.row_extractor") < names.IndexOf("doc.formatter"));
        }

        [Fact]
        public void Deterministic_agents_are_registered()
        {
            Assert.True(this.catalog.Find("doc.template_reader")!.Enabled);
            Assert.True(this.catalog.Find("doc.validator")!.Enabled);
            Assert.True(this.catalog.Find("doc.formatter")!.Enabled);
            Assert.Equal(BuilderAgentKind.Importer, this.catalog.Find("doc.template_reader")!.Kind);
            Assert.Equal(BuilderAgentKind.Validator, this.catalog.Find("doc.validator")!.Kind);
        }

        [Fact]
        public void RulesWave_is_empty_documents_are_not_org_rules()
        {
            Assert.Empty(this.catalog.RulesWave);
        }
    }
}
