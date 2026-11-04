using OficinaBrasil.Domain;
using Xunit;
namespace OficinaBrasil.Domain.Tests;

public class DomainTests
{
    [Fact]
    public void WorkflowCompleto()
    {
        var o = new ServiceOrder();
        o.Transition(ServiceOrderStatus.EM_DIAGNOSTICO);
        o.Transition(ServiceOrderStatus.AGUARDANDO_APROVACAO);
        o.Transition(ServiceOrderStatus.APROVADA);
        o.Transition(ServiceOrderStatus.EM_EXECUCAO);
        o.Transition(ServiceOrderStatus.FINALIZADA);
        o.Transition(ServiceOrderStatus.ENTREGUE);
        Assert.Equal(ServiceOrderStatus.ENTREGUE, o.Status);
        Assert.NotNull(o.FinishedAt);
    }

    [Fact]
    public void NaoPodePularDiagnostico()
    {
        var o = new ServiceOrder();
        Assert.Throws<BusinessException>(() => o.Transition(ServiceOrderStatus.EM_EXECUCAO));
    }

    [Fact]
    public void EstoqueNaoPodeFicarNegativo()
    {
        var p = new Part
        {
            Quantity = 2
        };
        Assert.Throws<BusinessException>(() => p.AdjustStock(- 3));
        Assert.Equal(2, p.Quantity);
    }

    [Fact]
    public void EstoquePermiteBaixa()
    {
        var p = new Part
        {
            Quantity = 2
        };
        p.AdjustStock(- 2);
        Assert.Equal(0, p.Quantity);
    }

    [Theory]
    [InlineData("ABC1D23")]
    [InlineData("ABC1Z23")]
    [InlineData("ABC1K23")]
    [InlineData("ABC-1234")]
    public void PlacaValida(string placa) => Assert.Equal(7, Checks.Plate(placa).Length);
    [Theory]
    [InlineData("A12")]
    [InlineData("ABC123")]
    [InlineData("AB11234")]
    public void PlacaInvalida(string placa) => Assert.Throws<BusinessException>(() => Checks.Plate(placa));
    [Fact]
    public void CpfValido() => Assert.Equal("52998224725", Checks.Document("529.982.247-25"));
    [Fact]
    public void CpfInvalido() => Assert.Throws<BusinessException>(() => Checks.Document("111.111.111-11"));
}
