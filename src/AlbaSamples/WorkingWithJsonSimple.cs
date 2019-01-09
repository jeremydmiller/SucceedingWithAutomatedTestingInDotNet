using System.Threading.Tasks;
using Alba;
using Shouldly;
using WebApp;
using WebApp.Controllers;
using Xunit;

namespace AlbaSamples
{
    public class WorkingWithJsonSimple
    {
        [Fact]
        public async Task get_happy_path()
        {
            using (var system = SystemUnderTest.ForStartup<Startup>())
            {
                var result = await system.GetAsJson<OperationResult>("/math/add/3/4");
                
                result.Answer.ShouldBe(7);
            }
        }

        [Fact]
        public async Task post_and_expect_response()
        {
            var systemUnderTest = SystemUnderTest.ForStartup<Startup>();

            
            using (var system = systemUnderTest)
            {
                var request = new OperationRequest
                {
                    Type = OperationType.Multiply,
                    One = 3,
                    Two = 4
                };

                var result = await system.PostJson(request, "/math")
                    .Receive<OperationResult>();
                
                result.Answer.ShouldBe(12);
                result.Method.ShouldBe("POST");
            }
        }
        
        [Fact]
        public async Task put_and_expect_response()
        {
            using (var system = SystemUnderTest.ForStartup<Startup>())
            {
                var request = new OperationRequest
                {
                    Type = OperationType.Subtract,
                    One = 3,
                    Two = 4
                };

                var result = await system.PutJson(request, "/math")
                    .Receive<OperationResult>();
                
                result.Answer.ShouldBe(-1);
                result.Method.ShouldBe("PUT");
            }
        }
    }
}