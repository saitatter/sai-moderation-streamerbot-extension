using Microsoft.Extensions.Logging.Abstractions;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Services;

namespace Sai.Moderation.StreamerBot.Extension.Tests;

public sealed class ManualOverrideCallbackHandlerTests
{
    [Fact]
    public async Task ReturnsNullForInvalidPayload()
    {
        var service = new FakeOverrideService();
        var handler = new ManualOverrideCallbackHandler(
            service,
            NullLogger<ManualOverrideCallbackHandler>.Instance);

        var result = await handler.HandleRawOverrideAsync("{\"messageId\":\"m-1\"}");

        Assert.Null(result);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task ParsesAndForwardsValidPayload()
    {
        var service = new FakeOverrideService();
        var handler = new ManualOverrideCallbackHandler(
            service,
            NullLogger<ManualOverrideCallbackHandler>.Instance);

        var result = await handler.HandleRawOverrideAsync("""
            {
              "messageId": "m-1",
              "action": "falsePositive",
              "operatorId": "mod-1",
              "reason": "context"
            }
            """);

        Assert.NotNull(result);
        Assert.Equal(1, service.CallCount);
        Assert.Equal(ManualOverrideAction.FalsePositive, service.LastRequest!.Action);
        Assert.Equal("mod-1", service.LastRequest.OperatorId);
    }

    private sealed class FakeOverrideService : IManualOverrideService
    {
        public int CallCount { get; private set; }
        public ManualOverrideRequest? LastRequest { get; private set; }

        public Task<ModerationResult?> ApplyOverrideAsync(
            ManualOverrideRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount += 1;
            LastRequest = request;
            return Task.FromResult<ModerationResult?>(
                new ModerationResult(request.MessageId, ModerationVerdict.Allow, 1.0, "manual-override", request.Reason, 0));
        }
    }
}
