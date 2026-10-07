using System.Globalization;
using System.Text.Json;
using Election.Domain;
using static Election.Infrastructure.Tse.TseJson;

namespace Election.Infrastructure.Tse;

// EA20 (10/07/2026). External abbreviations stay within this adapter.
public sealed class TseResultParser
{
    public ElectionTally Parse(string json, string electionId, int round, string scope, TimeSpan sourceOffset)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (Text(root, "ele") != electionId || Integer(root, "t") != round || Text(root, "f") != "o" ||
            !Text(root, "cdabr").Equals(scope, StringComparison.OrdinalIgnoreCase) ||
            Text(root, "tpabr") != (scope == "BR" ? "br" : "uf"))
            throw new InvalidDataException("Eleição, turno, fase ou abrangência inesperada.");
        var date = Text(root, "dg") + " " + Text(root, "hg");
        if (!DateTime.TryParseExact(date, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var generated)) throw new InvalidDataException("Data de geração inválida.");
        var updated = new DateTimeOffset(DateTime.SpecifyKind(generated, DateTimeKind.Unspecified), sourceOffset);
        var status = Text(root, "and") switch
        {
            "n" => "not_started",
            "p" => "in_progress",
            "f" => "completed",
            _ => throw new InvalidDataException("Andamento não reconhecido.")
        };
        var final = Text(root, "tf") switch { "s" => true, "n" => false, _ => throw new InvalidDataException("tf inválido") };
        var released = Text(root, "dv") switch { "s" => true, "n" => false, _ => throw new InvalidDataException("dv inválido") };
        var sections = root.GetProperty("s");
        var total = Integer(sections, "ts");
        var processed = Integer(sections, "st");
        if (processed > total) throw new InvalidDataException("Seções totalizadas excedem o total.");
        var cargos = Array(root, "carg").Where(c => Integer(c, "cd") == 1).ToArray();
        if (cargos.Length != 1) throw new InvalidDataException("Cargo Presidente ausente ou duplicado.");
        var candidates = new List<CandidateResult>();
        foreach (var group in Array(cargos[0], "agr"))
            foreach (var party in Array(group, "par"))
                foreach (var candidate in Array(party, "cand"))
                {
                    var id = Text(candidate, "sqcand");
                    if (string.IsNullOrWhiteSpace(id)) throw new InvalidDataException("Identificador de candidato vazio.");
                    // e=s also means 'second round'; never convert it to a winner flag.
                    var officialStatus = final && candidate.TryGetProperty("st", out var st) ? st.GetString() : null;
                    var votes = released ? Integer(candidate, "vap") : 0;
                    var percent = released ? Percent(candidate, candidate.TryGetProperty("pvapn", out _) ? "pvapn" : "pvap") : 0;
                    candidates.Add(new(id, Text(candidate, "n"), Text(candidate, "nmu"), Text(party, "sg"), votes, percent, officialStatus));
                }
        if (candidates.Count == 0 || candidates.Select(c => c.Id).Distinct().Count() != candidates.Count)
            throw new InvalidDataException("Lista de candidatos vazia ou duplicada.");
        return new(electionId, round, scope, status, updated, total, processed, candidates.AsReadOnly(), released);
    }
}
