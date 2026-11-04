using OficinaBrasil.Domain;
using Xunit;

namespace OficinaBrasil.Domain.Tests;

public class EstimateTests
{
    private const string Token = "token-exclusivo-do-cliente";
    private static (ServiceOrder Order, Estimate Estimate) Pending()
    {
        var order = new ServiceOrder();
        order.Transition(ServiceOrderStatus.EM_DIAGNOSTICO);
        order.Transition(ServiceOrderStatus.AGUARDANDO_APROVACAO);
        return (order, new Estimate { ServiceOrderId = order.Id, TokenHash = Estimate.HashToken(Token), ExpiresAt = DateTime.UtcNow.AddHours(1) });
    }

    [Theory]
    [InlineData(true, ServiceOrderStatus.APROVADA)]
    [InlineData(false, ServiceOrderStatus.CANCELADA)]
    public void DecisionChangesOrderAndRecordsAnswer(bool approve, ServiceOrderStatus expected)
    {
        var (order, estimate) = Pending();
        var version = estimate.Version;
        var now = DateTime.UtcNow;
        estimate.Decide(order, Token, approve, now);
        Assert.Equal(expected, order.Status);
        Assert.Equal(approve, estimate.Approved);
        Assert.Equal(now, estimate.DecidedAt);
        Assert.NotEqual(version, estimate.Version);
    }

    [Fact]
    public void InvalidTokenDoesNotChangeOrder()
    {
        var (order, estimate) = Pending();
        Assert.Throws<BusinessException>(() => estimate.Decide(order, "outro-token", true, DateTime.UtcNow));
        Assert.Equal(ServiceOrderStatus.AGUARDANDO_APROVACAO, order.Status);
        Assert.Null(estimate.Approved);
    }

    [Fact]
    public void ExpiredTokenDoesNotChangeOrder()
    {
        var (order, estimate) = Pending();
        Assert.Throws<BusinessException>(() => estimate.Decide(order, Token, true, estimate.ExpiresAt));
        Assert.Null(estimate.DecidedAt);
    }

    [Fact]
    public void AnswerCannotBeReusedOrReversed()
    {
        var (order, estimate) = Pending();
        estimate.Decide(order, Token, true, DateTime.UtcNow);
        Assert.Throws<BusinessException>(() => estimate.Decide(order, Token, false, DateTime.UtcNow));
        Assert.Equal(ServiceOrderStatus.APROVADA, order.Status);
    }

    [Fact]
    public void TokenCannotBeUsedForAnotherOrder()
    {
        var (_, estimate) = Pending();
        var (otherOrder, _) = Pending();
        Assert.Throws<BusinessException>(() => estimate.Decide(otherOrder, Token, true, DateTime.UtcNow));
    }

    [Fact]
    public void ReadingValidTokenDoesNotRegisterDecision()
    {
        var (order, estimate) = Pending();
        estimate.ValidateToken(Token, DateTime.UtcNow);
        Assert.Null(estimate.Approved);
        Assert.Equal(ServiceOrderStatus.AGUARDANDO_APROVACAO, order.Status);
    }
}
