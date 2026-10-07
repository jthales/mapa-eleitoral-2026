using System.Text.Json;
using System.Text.RegularExpressions;
using static Election.Infrastructure.Tse.TseJson;

namespace Election.Infrastructure.Tse;

public sealed record TseElectionConfiguration(string DirectoryTemplate, string Cycle, string ElectionId, string Pleito, string? PhotoDirectoryTemplate = null);

public sealed class TseConfigurationParser
{
    public TseElectionConfiguration Parse(string json, ElectionOptions options)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (Text(root, "f") != "o") throw new InvalidDataException("Configuração não oficial.");
        var dirs = Array(root, "arq").Where(a => Text(a, "tp") == "u").ToArray();
        if (dirs.Length != 1) throw new InvalidDataException("Diretório EA20 ausente ou duplicado.");
        foreach (var pleito in Array(root, "pl"))
        {
            if (Text(pleito, "c") != options.Cycle) continue;
            foreach (var election in Array(pleito, "e"))
            {
                var direct = Text(election, "cd") == options.ElectionId && Integer(election, "t") == options.Round;
                var second = options.Round == 2 && Integer(election, "t") == 1 &&
                    election.TryGetProperty("cdt2", out var next) && next.ToString() == options.ElectionId;
                var president = Array(election, "abr").Any(a => Array(a, "cp").Any(c => Integer(c, "cd") == 1));
                if ((direct || second) && president)
                    return new(Text(dirs[0], "dir"), options.Cycle, options.ElectionId, Text(pleito, "cd"), Array(root, "arq").Where(a => Text(a, "tp") == "ft").Select(a => Text(a, "dir")).SingleOrDefault());
            }
        }
        throw new InvalidDataException("Eleição/turno de Presidente não encontrado no EA11.");
    }

    public Uri? PhotoUri(TseElectionConfiguration config, ElectionOptions options, string candidateId)
    {
        if (config.PhotoDirectoryTemplate is null) return null;
        if (!Regex.IsMatch(candidateId, "^[0-9]{1,20}$")) throw new InvalidDataException("Identificador de foto inválido.");
        var result = ResultUri(config with { DirectoryTemplate = config.PhotoDirectoryTemplate }, options, "BR");
        return new Uri(result, candidateId + ".jpeg");
    }

    public Uri ResultUri(TseElectionConfiguration config, ElectionOptions options, string scope)
    {
        if (scope != "BR" && !Domain.BrazilStates.All.ContainsKey(scope)) throw new ArgumentException("UF inválida.");
        var directory = config.DirectoryTemplate.Replace("<base>", options.BaseUrl.TrimEnd('/'))
            .Replace("<ambiente>", options.Environment).Replace("<ciclo>", config.Cycle)
            .Replace("<cd_eleicao>", config.ElectionId).Replace("<cd_pleito>", config.Pleito)
            .Replace("<uf>", scope.ToLowerInvariant());
        if (directory.Contains('<') || !Regex.IsMatch(config.ElectionId, "^[0-9]{1,6}$"))
            throw new InvalidDataException("Template ou código de eleição inválido.");
        var uri = new Uri($"{directory.TrimEnd('/')}/{scope.ToLowerInvariant()}-c0001-e{config.ElectionId.PadLeft(6, '0')}-u.json");
        if (uri.Scheme != "https" || uri.Host != new Uri(options.BaseUrl).Host || !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo))
            throw new InvalidDataException("Destino externo inesperado no EA11.");
        return uri;
    }
}
