using System;
using System.Threading.Tasks;
using Alba;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using WebApp;
using Xunit;

namespace AlbaSamples
{
    public class WebApp : IDisposable
    {
        public readonly SystemUnderTest SystemUnderTest = SystemUnderTest.ForStartup<Startup>();

        public void Dispose()
        {
            SystemUnderTest?.Dispose();
        }
    }

    public class ContractTestWithAlba : IClassFixture<WebApp>
    {
        public ContractTestWithAlba(WebApp app)
        {
            _system = app.SystemUnderTest;
        }

        private readonly SystemUnderTest _system;

        [Fact]
        public Task happy_path()
        {
            return _system.Scenario(_ =>
            {
                _.Get.Url("/fake/okay");
                _.StatusCodeShouldBeOk();
            });
        }


        [Fact]
        public Task sad_path()
        {
            return _system.Scenario(_ =>
            {
                _.Get.Url("/fake/bad");
                _.StatusCodeShouldBe(500);
            });
        }

        [Fact]
        public async Task with_validation_errors()
        {
            var result = await _system.Scenario(_ =>
            {
                _.Get.Url("/fake/invalid");
                _.ContentTypeShouldBe("application/problem+json; charset=utf-8");
                _.StatusCodeShouldBe(400);
            });

            var problems = result.ResponseBody.ReadAsJson<ProblemDetails>();
            problems.Title.ShouldBe("This stinks!");
        }
    }
}