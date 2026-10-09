using JobSeek.Prompter.Services;

namespace JobSeek.Prompter.Services.ReleaseTests.Factories;

internal static class AttributesOfferServiceFactory
{
    private static readonly object Gate = new();
    private static Services.AttributesOfferService? _service;
    private static string? _model;

    public static string ActiveModel => _service?.Model ?? Services.AttributesOfferService.ResolveModel();

    public static Services.AttributesOfferService Service
    {
        get
        {
            lock (Gate)
            {
                var model = Services.AttributesOfferService.ResolveModel();
                if (_service is not null && string.Equals(_model, model, StringComparison.Ordinal))
                    return _service;

                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_JOBSEEK_API_KEY")))
                    Assert.Fail("OPENAI_JOBSEEK_API_KEY is not set.");

                _service = Services.AttributesOfferService.CreateService();
                _model = _service.Model;
                return _service;
            }
        }
    }
}
