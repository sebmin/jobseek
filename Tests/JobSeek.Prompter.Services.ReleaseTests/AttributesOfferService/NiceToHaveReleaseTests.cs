using JobSeek.Categorizator.Contracts.Models.Contract;
using JobSeek.Prompter.Services.ReleaseTests.Assertions;
using JobSeek.Prompter.Services.ReleaseTests.Factories;

namespace JobSeek.Prompter.Services.ReleaseTests.AttributesOfferService;

[TestClass]
[TestCategory("Llm")]
[TestCategory("Release-NiceToHave")]
public sealed class NiceToHaveReleaseTests
{
    [ReleaseCriterion("NH-01", "Senior-expert wording is level 4")]
    public async Task ExtractAttributes_SeniorExpertWording_HasLevelFour()
    {
        var id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");

        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Requirements:
            - Kubernetes, senior expert level
            """,
            [],
            []);

        ReleaseSkillAssertions.AssertSchema(result, id);
        ReleaseSkillAssertions.AssertPresentWithLevel(result.RequiredSkills, "Kubernetes", 4);
    }

    [ReleaseCriterion("NH-02", "A Polish description uses English technical names")]
    public async Task ExtractAttributes_PolishDescription_UsesEnglishTechnicalNames()
    {
        var id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2");

        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Wymagania:
            - React

            Szukamy osoby komunikatywnej z doświadczeniem w projektach bankowych.
            """,
            [],
            []);

        ReleaseSkillAssertions.AssertSchema(result, id);
        ReleaseSkillAssertions.AssertPresent(result.RequiredSkills, "React");
        ReleaseSkillAssertions.AssertNoPolishDiacritics(result);
    }

    [ReleaseCriterion("NH-03", "A long description yields the required skills")]
    public async Task ExtractAttributes_FromLongtext_RecognizeSKills()
    {
        var id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");

        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Your role on the team:

            develop and support existing REST API modules,
            develop and maintain the frontend application in Angular,
            implement new features and modify existing solutions,
            work with the team on applications used in the financial markets area,
            take part in deploying modules to the OpenShift platform,
            collaborate on analyzing and solving technical problems,
            care for the quality and stability of the solutions being developed.

            Knowledge and qualifications:
            advanced knowledge of C# / .NET,
            advanced knowledge of Angular,
            experience working with REST APIs,
            basic knowledge of PostgreSQL databases,
            professional experience at mid/senior level (5–8 years),
            ability to work in a team and complete assigned tasks independently.
            """,
            [],
            []);

        ReleaseSkillAssertions.AssertSchema(result, id);
        ReleaseSkillAssertions.AssertPresentWithLevel(result.RequiredSkills, "c#", 4);
        ReleaseSkillAssertions.AssertPresentWithLevel(result.RequiredSkills, ".net", 4);
        ReleaseSkillAssertions.AssertPresentWithLevel(result.RequiredSkills, "rest apis", 3);
        ReleaseSkillAssertions.AssertPresentWithLevel(result.RequiredSkills, "PostgreSQL", 1);
        Assert.IsEmpty(result.NiceToHaveSkills);
    }

    [ReleaseCriterion("NH-04", "A plus-item stays on the nice-to-have list")]
    public async Task ExtractAttributes_FromLongtext_RecognizeNiceToHaveSKills()
    {
        var id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");

        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Your role on the team:

            develop and support existing REST API modules,
            develop and maintain the frontend application in Angular,
            implement new features and modify existing solutions,
            work with the team on applications used in the financial markets area,
            collaborate on analyzing and solving technical problems,
            care for the quality and stability of the solutions being developed.

            Knowledge and qualifications:
            advanced knowledge of C# / .NET
            
            If you heard anything about OpenShift it's a plus.
            """,
            [],
            []);

        ReleaseSkillAssertions.AssertSchema(result, id);
        Assert.HasCount(1, result.NiceToHaveSkills);
        ReleaseSkillAssertions.AssertPresentWithLevel(result.NiceToHaveSkills, "OpenShift", 1);
    }

    [ReleaseCriterion("NH-05", "Negation, levels, and the static list stay distinct")]
    public async Task ExtractAttributes_NegationScopeLevelsAndStaticList_KeepsTheHardDistinctions()
    {
        var id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1");
        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Senior Backend Developer
            Requirements and skills:
            - advanced knowledge of C#
            - basic, purely theoretical knowledge of PostgreSQL
            - JavaScript, not Java
            - Terraform
            - Git
            - lead-level mastery of Kubernetes
            - lead-level mastery of Redis
            Docker is not a requirement. Familiarity with Docker is only a plus.
            You will not work with PHP or WordPress. We do not use the Go language.
            On a previous banking project the team maintained PHP.
            We offer a paid AWS exam and a Multisport card.
            English is required.
            The rest of the team works from the Lisbon office.
            """,
            [],
            [new SkillDto("Redis", 2)]);
        ReleaseSkillAssertions.AssertSchema(result, id);
        ReleaseSkillAssertions.AssertPresentWithLevel(result.RequiredSkills, "C#", 4);
        ReleaseSkillAssertions.AssertPresentWithLevel(result.RequiredSkills, "PostgreSQL", 1);
        ReleaseSkillAssertions.AssertPresentWithLevel(result.RequiredSkills, "JavaScript", 1);
        ReleaseSkillAssertions.AssertPresentWithLevel(result.RequiredSkills, "Terraform", 1);
        ReleaseSkillAssertions.AssertPresentWithLevel(result.RequiredSkills, "Git", 1);
        ReleaseSkillAssertions.AssertPresentWithLevel(result.RequiredSkills, "Kubernetes", 5);
        var redis = ReleaseSkillAssertions.AssertPresent(result.NiceToHaveSkills, "Redis");
        Assert.IsTrue(redis.isStatic);
        Assert.AreEqual(2, redis.Level);
        ReleaseSkillAssertions.AssertAbsentFrom(result.RequiredSkills, "Redis");
        ReleaseSkillAssertions.AssertPresentWithLevel(result.NiceToHaveSkills, "Docker", 1);
        ReleaseSkillAssertions.AssertAbsentFrom(result.RequiredSkills, "Docker");
        foreach (var skill in new[] { "Java", "PHP", "WordPress", "Go", "AWS" })
        {
            ReleaseSkillAssertions.AssertAbsentFrom(result.RequiredSkills, skill);
            ReleaseSkillAssertions.AssertAbsentFrom(result.NiceToHaveSkills, skill);
        }
        ReleaseSkillAssertions.AssertTextAbsent(
            result,
            "english",
            "multisport",
            "bank",
            "aws",
            "php",
            "wordpress");
    }

    [ReleaseCriterion("NH-06", "A buried .NET offer yields at least 50 required skills")]
    public async Task ExtractAttributes_FromBuriedDotNetOffer_RecognizesAtLeastFiftyRequiredSkills()
    {
        var id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc3");

        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Senior .NET Specialist — payments ledger

            Astraforge is hiring a .NET specialist for the payments ledger. The work is C# on .NET 8. HTTP endpoints are ASP.NET Core, and since last spring those endpoints are minimal APIs; the old controller project is frozen and only gets bugfixes.

            The system of record is SQL Server. Everyday reads and writes go through LINQ against Entity Framework — the Core packages, not the EF6 assemblies still sitting in the legacy solution. When a reconciliation report has to be one round trip, that query is Dapper and hand-written T-SQL. A smaller read model, the one the ops desk refreshes, is PostgreSQL. Hot counters and short-lived locks live in Redis. Documents that do not belong in a table go to Cosmos DB.

            Public contracts are REST, described with OpenAPI and returned as JSON. Service-to-service calls inside the cluster are gRPC. The blotter pushes row updates over SignalR. Outbound clients are generated with Refit on top of HttpClient, and payloads are serialized with System.Text.Json. Do not add Newtonsoft to a new project.

            After last year's token mix-up, every new route must validate a JWT. Tokens are issued with OAuth 2.0 and OpenID Connect against Entra ID. Browser users still sign in through ASP.NET Core Identity. Connection strings and signing keys are read from Azure Key Vault with the workload's managed identity. Nothing secret is committed to Git.

            The solution is laid out as Clean Architecture. Writes and reads are split — CQRS — and a request is dispatched with MediatR. Input rules live in FluentValidation. A handful of maps still go through AutoMapper; we are not adding maps, but the existing ones are yours to change. The ubiquitous language in the ledger bounded context follows DDD, and review comments will call out SOLID violations by name. Dependencies are constructor-injected. There is no service locator left.

            The failure you will see on call is a consumer parked on RabbitMQ. Publishing goes through MassTransit. The overnight finance feed is a separate Kafka topic, not that broker. Notifications that must survive a process restart use Azure Service Bus. Retries, timeouts, and the circuit breaker are Polly policies. Work that only has to run later inside the same process is a BackgroundService. The statement job that accounting kicks at 02:00 is Hangfire.

            A service is not ready until it exports traces. Logging is Serilog, the export pipeline is OpenTelemetry, and the board the on-call lead watches is Application Insights. Liveness is the built-in health checks endpoint, not a custom page. Features that are not ready for every tenant are gated with Microsoft.FeatureManagement.

            A change is not done when the screen looks right. The xUnit suite has to be green. Test doubles are Moq, assertions are FluentAssertions, and anything that needs a real database or broker comes up with Testcontainers. Fake payloads are built with Bogus. Bugfixes start from a failing test — that test-first habit is required, not optional — and asynchronous code stays on async/await. Blocking on .Result or .Wait() fails review.

            You package the service with Docker and run it on Kubernetes. The chart is Helm. The cloud account is Azure, and two bursty endpoints are Azure Functions rather than the always-on API. The release pipeline sits in Azure DevOps and is checked in as YAML; that pipeline is the CI/CD path, there is no side door. Shared libraries are pushed to our private NuGet feed. The shared landing zone is Terraform. Application resources next to the service are Bicep. The pipeline also runs SonarQube, and new routes are reviewed against the OWASP API list before they are called done.

            One screen is still a Blazor Server page built from Razor components. You will edit that markup: HTML, a small amount of CSS, and the design-system pieces that are TypeScript. Routes that move money declare an explicit API version, and callers must send an idempotency key; duplicates are rejected, not applied twice.

            The squad runs Scrum. Written and spoken English is required — the other half of the team is in Lisbon and the backlog is in English.
            """,
            [],
            []);

        ReleaseSkillAssertions.AssertSchema(result, id);
        Assert.IsEmpty(result.NiceToHaveSkills);
        Assert.IsTrue(result.RequiredSkills.Count >= 50, $"Expected at least 50 required skills, got {result.RequiredSkills.Count}.");
        ReleaseSkillAssertions.AssertTextAbsent(result, "english");

        string[] required =
        [
            "C#",
            ".NET",
            "ASP.NET Core",
            "SQL Server",
            "LINQ",
            "Entity Framework",
            "Dapper",
            "T-SQL",
            "PostgreSQL",
            "Redis",
            "Cosmos DB",
            "REST",
            "OpenAPI",
            "JSON",
            "gRPC",
            "SignalR",
            "Refit",
            "HttpClient",
            "System.Text.Json",
            "JWT",
            "OAuth 2.0",
            "OpenID Connect",
            "Entra ID",
            "ASP.NET Core Identity",
            "Azure Key Vault",
            "Git",
            "Clean Architecture",
            "CQRS",
            "MediatR",
            "FluentValidation",
            "AutoMapper",
            "DDD",
            "SOLID",
            "RabbitMQ",
            "MassTransit",
            "Kafka",
            "Azure Service Bus",
            "Polly",
            "BackgroundService",
            "Hangfire",
            "Serilog",
            "OpenTelemetry",
            "Application Insights",
            "xUnit",
            "Moq",
            "FluentAssertions",
            "Testcontainers",
            "Bogus",
            "Docker",
            "Kubernetes",
            "Helm",
            "Azure",
            "Azure Functions",
            "Azure DevOps",
            "YAML",
            "NuGet",
            "Terraform",
            "Bicep",
            "SonarQube",
            "OWASP",
            "Blazor",
            "Razor",
            "HTML",
            "CSS",
            "TypeScript",
        ];

        foreach (var skill in required)
            ReleaseSkillAssertions.AssertPresent(result.RequiredSkills, skill);
    }
}
