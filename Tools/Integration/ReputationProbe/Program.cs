using PocketCaravan;
using System.Text.Json;
using System.Text.Json.Serialization;
var passed = new List<string>();
var json = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
string Encode<T>(T value) => JsonSerializer.Serialize(value, json);
T Roundtrip<T>(T value) => JsonSerializer.Deserialize<T>(Encode(value), json);
void Check(string name, bool ok) { if (!ok) throw new Exception(name); passed.Add(name); }
SaveData Legacy(int version = 2) => new() { version = version, money = 77, credit = -2, stamina = 1, day = 3, minute = 480,
    cargoItems = new() { "salt" }, cargoCounts = new() { 2 }, cargoCosts = new() { 17 } };
var origin = new OriginData { id = "goryeo", homeRegion = new() { id = "goryeo" } };
var state = new ReputationState();
state.Initialize(ReputationState.HomeOf(origin));
Check("new home civilization starts friendly", state.Get(ReputationScope.Civilization, "goryeo").standing == ReputationStanding.Friendly);
foreach (string id in new[] { "arabia", "venice", "viking" })
    Check("other civilization starts neutral: " + id, state.Get(ReputationScope.Civilization, id).standing == ReputationStanding.Neutral);
Check("central village starts neutral", state.Get(ReputationScope.Town, "east_gate").standing == ReputationStanding.Neutral);
state.Set(ReputationScope.Town, "east_gate", ReputationStanding.Trusted, 7);
Check("central villages independent", state.Get(ReputationScope.Town, "other_port").standing == ReputationStanding.Neutral);
state.Set(ReputationScope.Town, "goryeo", ReputationStanding.Wary, 3);
Check("same ID in town and civilization are distinct", state.Get(ReputationScope.Civilization, "goryeo").standing == ReputationStanding.Friendly);
var saved = Legacy();
state.ExportTo(saved);
SaveSystem.Validate(saved);
Check("export promotes to save version 3", saved.version == 3 && saved.reputation.schemaVersion == 1);
Check("existing cash credit cargo unchanged", saved.money == 77 && saved.credit == -2 && saved.cargoCounts.Single() == 2 && saved.cargoCosts.Single() == 17);
var loaded = Roundtrip(saved);
var restored = new ReputationState();
restored.ImportFrom(loaded, new OriginData { id = "arabia" });
Check("home baseline survives configured origin change", restored.HomeCivilizationId == "goryeo");
Check("town value and progress survives DTO JSON roundtrip", restored.Get(ReputationScope.Town, "east_gate").standing == ReputationStanding.Trusted && restored.Get(ReputationScope.Town, "east_gate").progress == 7);
state.Set(ReputationScope.Civilization, "goryeo", ReputationStanding.Hostile, 12);
state.ExportTo(saved);
restored.ImportFrom(Roundtrip(saved), origin);
Check("loading does not regrant friendly after home reputation drops", restored.Get(ReputationScope.Civilization, "goryeo").standing == ReputationStanding.Hostile);
foreach (int version in new[] { 1, 2 }) {
    var old = Legacy(version); string before = Encode(old);
    restored.ImportFrom(old, origin);
    Check("legacy initializes home once: v" + version, restored.Get(ReputationScope.Civilization, "goryeo").standing == ReputationStanding.Friendly);
    Check("legacy migration does not mutate payload: v" + version, Encode(old) == before);
    restored.Set(ReputationScope.Town, "port", ReputationStanding.Wary, 4);restored.ExportTo(old);
    restored.ImportFrom(Roundtrip(old), origin);
    Check("second load preserves migrated values: v" + version, restored.Get(ReputationScope.Town, "port").progress == 4);
}
var materializedLegacy = Legacy(); materializedLegacy.reputation = new ReputationState.Save();
restored.ImportFrom(materializedLegacy, origin);
Check("Unity-style absent inline legacy block is accepted", restored.HomeCivilizationId == "goryeo");
var noHome = new ReputationState();noHome.Initialize(null);
Check("test or no-origin session stays neutral", noHome.Get(ReputationScope.Civilization, "goryeo").standing == ReputationStanding.Neutral);
Check("unintegrated origin keeps its existing ID", ReputationState.HomeOf(new OriginData { id = "viking" }) == "viking");
foreach (ReputationStanding stage in Enum.GetValues<ReputationStanding>()) {
    state.Set(ReputationScope.Town, "stages", stage, 11);
    restored.Import(Roundtrip(state.Export()));
    Check("standing roundtrip: " + stage, restored.Get(ReputationScope.Town, "stages").standing == stage);
}
state.Set(ReputationScope.Town, "future_team_town", ReputationStanding.Respected, int.MaxValue);
restored.Import(Roundtrip(state.Export()));
Check("unknown future town retained without balancing clamp", restored.Get(ReputationScope.Town, "future_team_town").progress == int.MaxValue);
var copy = restored.Get(ReputationScope.Town, "future_team_town");copy.progress = 0;
Check("returned entry cannot mutate state", restored.Get(ReputationScope.Town, "future_team_town").progress == int.MaxValue);
var exported = restored.Export();exported.entries.Clear();
Check("export does not alias live records", restored.Get(ReputationScope.Town, "future_team_town").progress == int.MaxValue);
var source = state.Export();restored.Import(source);source.entries.Clear();
Check("import does not alias incoming records", restored.Get(ReputationScope.Town, "future_team_town").progress == int.MaxValue);
void Reject(string name, Action<ReputationState.Save> mutate) {
    var invalid = state.Export();mutate(invalid);string before = Encode(restored.Export());bool refused = false;
    try { restored.Import(invalid); } catch (InvalidDataException) { refused = true; }
    Check(name, refused && before == Encode(restored.Export()));
}
Reject("future reputation schema rejected without mutation", s => s.schemaVersion = 2);
Reject("null entries rejected without mutation", s => s.entries = null);
Reject("null row rejected without mutation", s => s.entries.Add(null));
Reject("duplicate target rejected without mutation", s => s.entries.Add(s.entries[0]));
Reject("negative progress rejected without mutation", s => s.entries[0].progress = -1);
Reject("invalid standing rejected without mutation", s => s.entries[0].standing = (ReputationStanding)99);
Reject("invalid scope rejected without mutation", s => s.entries[0].scope = (ReputationScope)99);
Reject("blank target rejected without mutation", s => s.entries[0].targetId = " ");
Reject("missing home record rejected without mutation", s => s.entries.RemoveAll(e => e.scope == ReputationScope.Civilization && e.targetId == s.homeCivilizationId));
foreach (var data in new[] { new SaveData { version = 3 }, new SaveData { version = 4 }, new SaveData { version = 3, reputation = new ReputationState.Save() }, new SaveData { version = 2, reputation = state.Export() } }) {
    bool refused = false;try { SaveSystem.Validate(data); } catch (InvalidDataException) { refused = true; }
    Check("inconsistent save header rejected: v" + data.version, refused);
}
state.Initialize("goryeo");
Check("new game resets previous town changes", state.Get(ReputationScope.Town, "future_team_town").standing == ReputationStanding.Neutral);
Console.WriteLine(Encode(new { environment = "Production C# linked into .NET 10; platform stubs; System.Text.Json DTO roundtrip", unityJsonTested = false, productionSavesTouched = false, passed }));
