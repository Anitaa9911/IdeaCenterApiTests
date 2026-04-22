using NUnit.Framework;
using RestSharp;
using System.Net;
using System.Text.Json;
using System.Linq;

namespace IdeaCenterApiTests
{
    public class IdeaTests
    {
        private RestClient client;
        private string token;
        private static string? lastIdeaId;

        [OneTimeSetUp]
        public void Setup()
        {
            string baseUrl = Environment.GetEnvironmentVariable("BASE_URL")
                ?? "http://144.91.123.158:82/api";

            client = new RestClient(baseUrl);

            string uniqueUserName = "user" + DateTime.Now.Ticks;
            string email = $"{uniqueUserName}@abv.bg";
            string password = "123456";

            var registerRequest = new RestRequest("/User/Create", Method.Post);
            registerRequest.AddJsonBody(new
            {
                userName = uniqueUserName,
                email,
                password,
                rePassword = password,
                acceptedAgreement = true
            });

            client.Execute(registerRequest);

            var loginRequest = new RestRequest("/User/Authentication", Method.Post);
            loginRequest.AddJsonBody(new
            {
                email,
                password
            });

            var response = client.Execute(loginRequest);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);
            token = json.GetProperty("accessToken").GetString()!;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            client?.Dispose();
        }

        [Test, Order(1)]
        public void CreateIdea_WithRequiredFields_ShouldCreateSuccessfully()
        {
            var request = new RestRequest("/Idea/Create", Method.Post);
            request.AddHeader("Authorization", $"Bearer {token}");

            request.AddJsonBody(new
            {
                title = "Test Idea",
                description = "Test Description",
                url = ""
            });

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Successfully created"));
        }

        [Test, Order(2)]
        public void GetAllIdeas_ShouldReturnNonEmptyList()
        {
            var request = new RestRequest("/Idea/All", Method.Get);
            request.AddHeader("Authorization", $"Bearer {token}");

            var response = client.Execute(request);

            var ideas = JsonSerializer.Deserialize<List<JsonElement>>(response.Content!);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(ideas, Is.Not.Null);
            Assert.That(ideas!.Count, Is.GreaterThan(0));

            var lastIdea = ideas.Last();

            if (lastIdea.TryGetProperty("_id", out JsonElement idElement))
            {
                lastIdeaId = idElement.GetString();
            }
            else if (lastIdea.TryGetProperty("id", out JsonElement altIdElement))
            {
                lastIdeaId = altIdElement.GetString();
            }
            else
            {
                Assert.Fail("Не намерих поле за ID. Response item: " + lastIdea.ToString());
            }

            Assert.That(lastIdeaId, Is.Not.Null.And.Not.Empty);
        }
        [Test, Order(3)]
        public void EditIdea_WithValidId_ShouldEditSuccessfully()
        {
            var request = new RestRequest($"/Idea/Edit?ideaId={lastIdeaId}", Method.Put);
            request.AddHeader("Authorization", $"Bearer {token}");

            request.AddJsonBody(new
            {
                title = "Edited Idea",
                description = "Edited Description",
                url = ""
            });

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Edited successfully"));
        }

        [Test, Order(4)]
        public void DeleteIdea_WithValidId_ShouldDeleteSuccessfully()
        {
            var request = new RestRequest($"/Idea/Delete?ideaId={lastIdeaId}", Method.Delete);
            request.AddHeader("Authorization", $"Bearer {token}");

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
        }

        [Test, Order(5)]
        public void CreateIdea_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/Idea/Create", Method.Post);
            request.AddHeader("Authorization", $"Bearer {token}");

            request.AddJsonBody(new { });

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditIdea_WithInvalidId_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/Idea/Edit?ideaId=invalid-id", Method.Put);
            request.AddHeader("Authorization", $"Bearer {token}");

            request.AddJsonBody(new
            {
                title = "Test",
                description = "Test",
                url = ""
            });

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

        [Test, Order(7)]
        public void DeleteIdea_WithInvalidId_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/Idea/Delete?ideaId=invalid-id", Method.Delete);
            request.AddHeader("Authorization", $"Bearer {token}");

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }
    }
}