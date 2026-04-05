using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Wildcard search
    /// </summary>
    public class UnitTestWqlWildcardSearchA(UnitTestIndexFixtureWqlA fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlA>
    {
        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; } = output;

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureWqlA Fixture { get; set; } = fixture;

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql1()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'?elena'");
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql2()
        {
            // act
            var wql = Fixture.ExecuteWql("text='*elena'");
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql3()
        {
            // act
            var wql = Fixture.ExecuteWql("text='Helen?'");
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql4()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helen*'");
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql5()
        {
            // act
            var wql = Fixture.ExecuteWql("text='*elen*'");
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql6()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'?elen?' ~90");
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql7()
        {
            // act
            var wql = Fixture.ExecuteWql("text='?elen*' ORDER BY text");
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void SingleCharacterFirst()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'?elena'");
            var res = Fixture.IndexManager.Retrieve(wql);

            // validation
            Assert.NotNull(res);
            Assert.Equal(4, res.Count());
            Assert.Contains("b2e8a5c3-1f6d-4e7b-9e1f-8c1a9d0f2b4a", res.Select(x => x.Id.ToString()));
            Assert.Contains("c7d8f9e0-3a2b-4c5d-8e6f-9a1b0c2d4e5f", res.Select(x => x.Id.ToString()));
            Assert.Contains("a8901fac-aaef-483b-aba8-dba74e36e7fc", res.Select(x => x.Id.ToString()));
            Assert.Contains("3f3d7066-a925-42ac-90f7-ef100afb8460", res.Select(x => x.Id.ToString()));
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void SingleCharacterMiddle()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'He?ena'");
            var res = Fixture.IndexManager.Retrieve(wql);

            // validation
            Assert.NotNull(res);
            Assert.Equal(4, res.Count());
            Assert.Contains("b2e8a5c3-1f6d-4e7b-9e1f-8c1a9d0f2b4a", res.Select(x => x.Id.ToString()));
            Assert.Contains("c7d8f9e0-3a2b-4c5d-8e6f-9a1b0c2d4e5f", res.Select(x => x.Id.ToString()));
            Assert.Contains("a8901fac-aaef-483b-aba8-dba74e36e7fc", res.Select(x => x.Id.ToString()));
            Assert.Contains("3f3d7066-a925-42ac-90f7-ef100afb8460", res.Select(x => x.Id.ToString()));
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void SingleCharacterEnd()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helen?'");
            var res = Fixture.IndexManager.Retrieve(wql);

            // validation
            Assert.NotNull(res);
            Assert.Equal(4, res.Count());
            Assert.Contains("b2e8a5c3-1f6d-4e7b-9e1f-8c1a9d0f2b4a", res.Select(x => x.Id.ToString()));
            Assert.Contains("c7d8f9e0-3a2b-4c5d-8e6f-9a1b0c2d4e5f", res.Select(x => x.Id.ToString()));
            Assert.Contains("a8901fac-aaef-483b-aba8-dba74e36e7fc", res.Select(x => x.Id.ToString()));
            Assert.Contains("3f3d7066-a925-42ac-90f7-ef100afb8460", res.Select(x => x.Id.ToString()));
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void MultipleCharactersFirst()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'*ena'");
            var res = Fixture.IndexManager.Retrieve(wql);

            // validation
            Assert.NotNull(res);
            Assert.Equal(4, res.Count());
            Assert.Contains("b2e8a5c3-1f6d-4e7b-9e1f-8c1a9d0f2b4a", res.Select(x => x.Id.ToString()));
            Assert.Contains("c7d8f9e0-3a2b-4c5d-8e6f-9a1b0c2d4e5f", res.Select(x => x.Id.ToString()));
            Assert.Contains("a8901fac-aaef-483b-aba8-dba74e36e7fc", res.Select(x => x.Id.ToString()));
            Assert.Contains("3f3d7066-a925-42ac-90f7-ef100afb8460", res.Select(x => x.Id.ToString()));
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void MultipleCharactersMiddle()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'He*a'");
            var res = Fixture.IndexManager.Retrieve(wql);

            // validation
            Assert.NotNull(res);
            Assert.Equal(4, res.Count());
            Assert.Contains("b2e8a5c3-1f6d-4e7b-9e1f-8c1a9d0f2b4a", res.Select(x => x.Id.ToString()));
            Assert.Contains("c7d8f9e0-3a2b-4c5d-8e6f-9a1b0c2d4e5f", res.Select(x => x.Id.ToString()));
            Assert.Contains("a8901fac-aaef-483b-aba8-dba74e36e7fc", res.Select(x => x.Id.ToString()));
            Assert.Contains("3f3d7066-a925-42ac-90f7-ef100afb8460", res.Select(x => x.Id.ToString()));
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void MultipleCharactersEnd()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Hel*'");
            var res = Fixture.IndexManager.Retrieve(wql);

            // validation
            Assert.NotNull(res);
            Assert.Equal(4, res.Count());
            Assert.Contains("b2e8a5c3-1f6d-4e7b-9e1f-8c1a9d0f2b4a", res.Select(x => x.Id.ToString()));
            Assert.Contains("c7d8f9e0-3a2b-4c5d-8e6f-9a1b0c2d4e5f", res.Select(x => x.Id.ToString()));
            Assert.Contains("a8901fac-aaef-483b-aba8-dba74e36e7fc", res.Select(x => x.Id.ToString()));
            Assert.Contains("3f3d7066-a925-42ac-90f7-ef100afb8460", res.Select(x => x.Id.ToString()));
        }
    }
}
