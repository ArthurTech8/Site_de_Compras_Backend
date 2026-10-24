using SiteDeCompras.Application.Enderecos;
using SiteDeCompras.Domain.Enderecos;

namespace SiteDeCompras.Tests;

/// <summary>Dados de exemplo reaproveitados pelos testes.</summary>
public static class Exemplos
{
    public static EnderecoRequest EnderecoRequest(string cep = "30130-010") => new()
    {
        Destinatario = "Maria Souza",
        Cep = cep,
        Logradouro = "Av. Afonso Pena",
        Numero = "1500",
        Complemento = "ap 32",
        Bairro = "Centro",
        Cidade = "Belo Horizonte",
        Uf = "MG",
    };

    public static Endereco Endereco(string cep = "30130-010") => EnderecoRequest(cep).ParaDominio();
}
