using Dovetail.SDK.Bootstrap.Tests;
using Dovetail.SDK.Fubu.Swagger;
using NUnit.Framework;

namespace Dovetail.SDK.Fubu.Tests
{
    [TestFixture]
    public class relative_url
    {

        [Test]
        public void resource_relativity()
        {
            var result = "/api/resource.json".UrlRelativeTo("/api/foo/resource.json");

            result.ShouldEqual("foo/resource.json");
        }
        
        [Test]
        public void target_without_leading_slash_should_work()
        {
            var result = "/api/resource.json".UrlRelativeTo("api/foo/resource.json");

            result.ShouldEqual("foo/resource.json");
        }

        [Test]
        public void base_without_leading_slash_should_work()
        {
            var result = "api/resource.json".UrlRelativeTo("/api/foo/resource.json");

            result.ShouldEqual("foo/resource.json");
        }

        [Test]
        public void no_leading_slashs_should_work()
        {
            var result = "api/resource.json".UrlRelativeTo("api/foo/resource.json");

            result.ShouldEqual("foo/resource.json");
        }
    }
}