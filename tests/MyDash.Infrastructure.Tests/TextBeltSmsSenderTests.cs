using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using MyDash.Application.Options;
using MyDash.Infrastructure.Sms;

namespace MyDash.Infrastructure.Tests;

public class TextBeltSmsSenderTests
{
    [Fact]
    public async Task Posts_form_encoded_payload_to_textbelt()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { success = true, textId = "tb-1" }),
        });
        var client = new HttpClient(handler);
        var sut = new TextBeltSmsSender(client,
            Microsoft.Extensions.Options.Options.Create(new SmsOptions { ApiKey = "textbelt" }));

        var result = await sut.SendAsync("+31621114421", "Your code is 123456");

        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().Be("tb-1");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.AbsoluteUri.Should().Contain("textbelt.com/text");

        var body = await handler.LastRequest.Content!.ReadAsStringAsync();
        body.Should().Contain("phone=").And.Contain("message=").And.Contain("key=textbelt");
    }

    [Fact]
    public async Task Returns_failure_on_http_error()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { success = false, error = "quota exceeded" }),
        });
        var client = new HttpClient(handler);
        var sut = new TextBeltSmsSender(client,
            Microsoft.Extensions.Options.Options.Create(new SmsOptions { ApiKey = "textbelt" }));

        var result = await sut.SendAsync("+31621114421", "test");
        result.Success.Should().BeFalse();
        result.Error.Should().Be("quota exceeded");
    }
}

internal sealed class RecordingHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;
    public HttpRequestMessage? LastRequest { get; private set; }

    public RecordingHandler(HttpResponseMessage response) => _response = response;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        LastRequest = request;
        return Task.FromResult(_response);
    }
}
