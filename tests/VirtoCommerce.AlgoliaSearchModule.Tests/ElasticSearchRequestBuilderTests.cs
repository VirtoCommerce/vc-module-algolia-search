//using System;
//using System.Collections.Generic;
//using AutoFixture;
//using FluentAssertions;
//using Moq;
//using VirtoCommerce.SearchModule.Core.Model;
//using Xunit;

//namespace VirtoCommerce.AlgoliaSearchModule.Tests
//{
//    [Trait("Category", "CI")]
//    public class AlgoliaSearchRequestBuilderTests
//    {
//        private readonly AlgoliaSearchRequestBuilderTestProxy _testClass = new AlgoliaSearchRequestBuilderTestProxy();
//        private readonly Fixture _fixture = new Fixture();

//        [Theory]
//        [InlineData("0", "false")]
//        [InlineData("1", "true")]
//        [InlineData("true", "true")]
//        [InlineData("false", "false")]
//        [InlineData("tRuE", "true")]
//        [InlineData("FaLsE", "false")]
//        public void CreateTermFilter_BooleanAggregate_ShouldCreateCorrectValues(string value, string convertedValue)
//        {
//            // Arrange
//            var fieldName = _fixture.Create<string>();

//            var termFilter = new TermFilter
//            {
//                Values = new[] { value },
//                FieldName = fieldName
//            };

//            var booleanPropertyMock = new Mock<IProperty>();
//            booleanPropertyMock
//                .SetupGet(x => x.Type)
//                .Returns("boolean");

//            var availableFields = new Properties<IProperties>(new Dictionary<PropertyName, IProperty>
//            {
//                { fieldName, booleanPropertyMock.Object }
//            });

//            // Act
//            var result = _testClass.CreateTermFilterProxy(termFilter, availableFields) as IQueryContainer;

//            // Assert
//            result.Terms.Terms.Should().Contain(convertedValue);
//        }
//    }

//    public class AlgoliaSearchRequestBuilderTestProxy : AlgoliaSearchRequestBuilder
//    {
//        public QueryContainer CreateTermFilterProxy(TermFilter termFilter, Properties<IProperties> availableFields)
//        {
//            return base.CreateTermFilter(termFilter, availableFields);
//        }
//    }
//}
