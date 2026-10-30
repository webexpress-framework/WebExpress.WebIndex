using WebExpress.WebIndex.WebAttribute;
using WebExpress.WebIndex.Wql;
using WebExpress.WebIndex.Wql.Condition;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Tests that a property marked with WqlIgnore cannot be reached from a WQL statement,
    /// while IndexIgnore keeps a property queryable.
    /// </summary>
    public class UnitTestWqlIgnore
    {
        /// <summary>
        /// Tests that every way of naming a hidden property is reported as an unknown
        /// attribute, the same error as for a property that does not exist.
        /// </summary>
        [Theory]
        [InlineData("Secret = 'x'")]
        [InlineData("secret starts with 'a'")]
        [InlineData("Secret is null")]
        [InlineData("Name ~ 'a' order by Secret")]
        [InlineData("Owner.Token = 'x'")]
        [InlineData("Vault.Code = 1")]
        [InlineData("Vault.Code is null order by Name")]
        public void HiddenAttributeIsUnknown(string wql)
        {
            var statement = Parse(wql);

            Assert.True(statement.HasErrors);
            Assert.Equal("webexpress.webindex:wql.attribute_unknown", statement.Error.Message);
        }

        /// <summary>
        /// Tests that a hidden property on the right-hand side is read as a plain word, so the
        /// condition compares with the text "Secret" and never with the property.
        /// </summary>
        [Fact]
        public void HiddenAttributeAsParameterIsLiteral()
        {
            var statement = Parse("Name = Secret");
            var binary = Assert.IsAssignableFrom<WqlExpressionNodeFilterConditionBinary<HiddenDocument>>(statement.Filter.Condition);

            Assert.False(statement.HasErrors);
            Assert.Null(binary.Parameter.Attribute);
            Assert.Equal("Secret", binary.Parameter.Value.StringValue);
        }

        /// <summary>
        /// Tests that the visible siblings of hidden properties and properties that are only
        /// excluded from the index stay queryable.
        /// </summary>
        [Theory]
        [InlineData("Name = 'x'")]
        [InlineData("Owner.Name = 'x' order by Owner.Name")]
        [InlineData("Unindexed > 3")]
        [InlineData("Id = 10000000-0000-0000-0000-000000000001")]
        public void VisibleAttributeIsKnown(string wql)
        {
            Assert.False(Parse(wql).HasErrors);
        }

        /// <summary>
        /// Tests that the property path refuses a hidden property at any depth.
        /// </summary>
        [Theory]
        [InlineData("Secret")]
        [InlineData("Owner.Token")]
        [InlineData("Vault.Code")]
        public void PropertyPathRefusesHidden(string path)
        {
            Assert.Throws<ArgumentException>(() => WqlPropertyPath<HiddenDocument>.Parse(path));
        }

        /// <summary>
        /// Parses a statement against the document with hidden properties.
        /// </summary>
        /// <param name="wql">The statement.</param>
        /// <returns>The parsed statement.</returns>
        private static IWqlStatement<HiddenDocument> Parse(string wql)
        {
            return new WqlParser<HiddenDocument>().Parse(wql);
        }

        /// <summary>
        /// A document that hides properties from WQL at the top level and below.
        /// </summary>
        public class HiddenDocument : IIndexItem
        {
            /// <summary>
            /// Returns or sets the id.
            /// </summary>
            [IndexIgnore]
            public Guid Id { get; set; }

            /// <summary>
            /// Returns or sets the name.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Returns or sets a value that is hidden from WQL.
            /// </summary>
            [WqlIgnore]
            public string Secret { get; set; }

            /// <summary>
            /// Returns or sets a value that has no reverse index but stays queryable.
            /// </summary>
            [IndexIgnore]
            public int Unindexed { get; set; }

            /// <summary>
            /// Returns or sets an object with a hidden property.
            /// </summary>
            public HiddenOwner Owner { get; set; }

            /// <summary>
            /// Returns or sets an object that is hidden as a whole.
            /// </summary>
            [WqlIgnore]
            public HiddenVault Vault { get; set; }
        }

        /// <summary>
        /// A nested object with a visible and a hidden property.
        /// </summary>
        public class HiddenOwner
        {
            /// <summary>
            /// Returns or sets the name.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Returns or sets a value that is hidden from WQL.
            /// </summary>
            [WqlIgnore]
            public string Token { get; set; }
        }

        /// <summary>
        /// A nested object whose properties are all unreachable through its hidden parent.
        /// </summary>
        public class HiddenVault
        {
            /// <summary>
            /// Returns or sets the code.
            /// </summary>
            public int Code { get; set; }
        }
    }
}
