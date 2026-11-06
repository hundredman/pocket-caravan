using PocketCaravan;
using System.Text.Json;

// R3 흥정·R4 평판 회복 규칙 진단. 게임 원본 규칙 파일을 그대로 연결하고 통과 항목을 JSON으로 출력한다.
var passed = new List<string>();
var json = new JsonSerializerOptions { IncludeFields = true };
T Roundtrip<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, json), json);
void Check(string name, bool ok) { if (!ok) throw new Exception("FAILED: " + name); passed.Add(name); }

// ---------- R3 흥정 ----------
float neutral = .03f, respected = .10f;
Check("haggle strength is a third, two thirds, all of the D10 limit",
    Math.Abs(HaggleRules.Discount(neutral, HaggleStrength.Light) - .01f) < 1e-6 && Math.Abs(HaggleRules.Discount(neutral, HaggleStrength.Fair) - .02f) < 1e-6
    && Math.Abs(HaggleRules.Discount(neutral, HaggleStrength.Hard) - .03f) < 1e-6);
Check("respected hard haggle is 10%", Math.Abs(HaggleRules.Discount(respected, HaggleStrength.Hard) - .10f) < 1e-6);
Check("no limit means no discount and zero chance",
    HaggleRules.Discount(0, HaggleStrength.Hard) == 0 && HaggleRules.Chance(0, ReputationStanding.Wary, HaggleStrength.Light, "naru", 1) == 0);
foreach (var town in new[] { "naru", "gaegyeong", "dakmok", "goryeo_songak", "goryeo_jangdan" })
{
    float l = HaggleRules.Chance(neutral, ReputationStanding.Neutral, HaggleStrength.Light, town, 1);
    float f = HaggleRules.Chance(neutral, ReputationStanding.Neutral, HaggleStrength.Fair, town, 1);
    float h = HaggleRules.Chance(neutral, ReputationStanding.Neutral, HaggleStrength.Hard, town, 1);
    Check("harder haggle is less likely: " + town, l > f && f > h && h >= .05f && l <= .95f);
    Check("higher standing raises chance: " + town,
        HaggleRules.Chance(respected, ReputationStanding.Respected, HaggleStrength.Hard, town, 1) > h);
    Check("bulk trade raises chance: " + town,
        HaggleRules.Chance(neutral, ReputationStanding.Neutral, HaggleStrength.Hard, town, HaggleRules.BulkCount) > h);
    Check("temperament is stable per town: " + town, HaggleRules.Temperament(town) == HaggleRules.Temperament(town) && Math.Abs(HaggleRules.Temperament(town)) <= 1);
}
Check("roll is deterministic", Enumerable.Range(1, 50).All(d =>
    HaggleRules.Roll(.5f, d, "naru", "salt", false, HaggleStrength.Fair) == HaggleRules.Roll(.5f, d, "naru", "salt", false, HaggleStrength.Fair)));
int hits = Enumerable.Range(1, 2000).Count(d => HaggleRules.Roll(.35f, d, "naru", "salt", false, HaggleStrength.Hard));
Check("roll frequency follows chance (35% ± 5)", hits > 600 && hits < 800);

var rep = new ReputationState(); rep.Initialize("goryeo");
var activity = new TradeActivity();
int day = 1; float now = 10;
Check("wary and hostile cannot haggle", Haggle.Denial(activity, "naru", day, now, 0) != null
    && Haggle.Try(activity, "naru", "salt", false, 1, ReputationStanding.Wary, 0, HaggleStrength.Light, day, now).outcome == HaggleOutcome.Unavailable);

// Find a day where a fair haggle fails and one where it succeeds.
int failDay = Enumerable.Range(1, 500).First(d => !HaggleRules.Roll(HaggleRules.Chance(neutral, ReputationStanding.Friendly, HaggleStrength.Fair, "naru", 1), d, "naru", "salt", false, HaggleStrength.Fair));
int winDay = Enumerable.Range(1, 500).First(d => HaggleRules.Roll(HaggleRules.Chance(neutral, ReputationStanding.Friendly, HaggleStrength.Fair, "naru", 1), d, "naru", "salt", false, HaggleStrength.Fair));
int scoreBefore = ReputationState.Score(rep.Get(ReputationScope.Civilization, "goryeo"));
var win = Haggle.Try(activity, "naru", "salt", false, 1, ReputationStanding.Friendly, .05f, HaggleStrength.Fair, winDay, now);
Check("success yields a one-trade deal for that item, side and day", win.outcome == HaggleOutcome.Success
    && win.deal.AppliesTo("naru", "salt", false, winDay, 1) && !win.deal.AppliesTo("naru", "salt", true, winDay, 1)
    && !win.deal.AppliesTo("naru", "grain", false, winDay, 1) && !win.deal.AppliesTo("naru", "salt", false, winDay + 1, 1));
Check("success does not lock further haggling", !activity.HaggleLocked("naru", winDay));
var fail = Haggle.Try(activity, "naru", "salt", false, 1, ReputationStanding.Friendly, .05f, HaggleStrength.Fair, failDay, now);
Check("normal failure locks haggling with that merchant for the day", fail.outcome == HaggleOutcome.Refused && activity.HaggleLocked("naru", failDay)
    && Haggle.Try(activity, "naru", "grain", true, 1, ReputationStanding.Friendly, .05f, HaggleStrength.Light, failDay, now).outcome == HaggleOutcome.Unavailable);
Check("normal failure does not ban trading", activity.BanRemaining("naru", now) == 0);
Check("lock ends next day and is per town", !activity.HaggleLocked("naru", failDay + 1) && !activity.HaggleLocked("dakmok", failDay));

int hardFail = Enumerable.Range(1, 500).First(d => !HaggleRules.Roll(HaggleRules.Chance(neutral, ReputationStanding.Neutral, HaggleStrength.Hard, "dakmok", 1), d, "dakmok", "paper", false, HaggleStrength.Hard));
var hard = Haggle.Try(activity, "dakmok", "paper", false, 1, ReputationStanding.Neutral, neutral, HaggleStrength.Hard, hardFail, now);
Check("hard failure cancels the deal and bans trade for a few hours", hard.outcome == HaggleOutcome.Cancelled && hard.deal == null
    && Math.Abs(activity.BanRemaining("dakmok", now) - HaggleRules.BanHours) < 1e-4 && activity.BanRemaining("dakmok", now + HaggleRules.BanHours) == 0);
Check("ban blocks haggling while active", Haggle.Denial(activity, "dakmok", hardFail + 1, now + 1, neutral) != null);
Check("haggling never changes reputation", ReputationState.Score(rep.Get(ReputationScope.Civilization, "goryeo")) == scoreBefore);
var reloaded = new TradeActivity(); reloaded.Import(Roundtrip(activity.Export()));
Check("haggle lock and ban survive save", reloaded.HaggleLocked("naru", failDay) && reloaded.BanRemaining("dakmok", now) > 0);
var reloadTry = Haggle.Try(reloaded, "naru", "salt", false, 1, ReputationStanding.Friendly, .05f, HaggleStrength.Fair, failDay, now);
Check("reloading cannot reroll a failed haggle", reloadTry.outcome == HaggleOutcome.Unavailable);

// Saves written before R3/R4 have no new lists.
var oldSave = new TradeActivity.Save { schemaVersion = 1, haggleLocks = null, bans = null, chores = null, atonementReopen = null };
var fromOld = new TradeActivity(); fromOld.Import(oldSave);
Check("old trade activity save loads with empty haggle and recovery records", fromOld.ActiveChore == null && !fromOld.HaggleLocked("naru", 1));
bool Rejects(TradeActivity.Save s) { try { TradeActivity.Validate(s); return false; } catch (InvalidDataException) { return true; } }
Check("invalid records are rejected", Rejects(new TradeActivity.Save { schemaVersion = 1, bans = new() { new() { town = "naru", until = float.NaN } } })
    && Rejects(new TradeActivity.Save { schemaVersion = 1, chores = new() { new() { town = "a" }, new() { town = "b" } } })
    && Rejects(new TradeActivity.Save { schemaVersion = 1, haggleLocks = new() { new() { town = "naru", day = 1 }, new() { town = "naru", day = 2 } } }));

// ---------- R4 회복 ----------
var civ = ReputationScope.Civilization; var townScope = ReputationScope.Town;
var r = new ReputationState(); r.Initialize("goryeo");
r.Add(civ, "goryeo", -3000 - 150); // Friendly 3000 → -150 (경계)
Check("negative standing is wary", r.Get(civ, "goryeo").standing == ReputationStanding.Wary && StandingRecovery.Missing(r, civ, "goryeo") == 150);
var a = new TradeActivity();
Check("chore needs no money, cargo or credit", StandingRecovery.ChoreDenial(a, r, civ, "goryeo") == null);
a.StartChore("naru", 0, 5);
Check("only one chore at a time", StandingRecovery.ChoreDenial(a, r, civ, "goryeo") != null);
Check("chore finishes once at its town", !a.FinishChore("dakmok") && a.FinishChore("naru") && !a.FinishChore("naru"));
int g1 = StandingRecovery.Raise(r, civ, "goryeo", ReputationAwards.PointsFor(StandingRecovery.ChoreSize));
int g2 = StandingRecovery.Raise(r, civ, "goryeo", ReputationAwards.PointsFor(StandingRecovery.ChoreSize));
int g3 = StandingRecovery.Raise(r, civ, "goryeo", ReputationAwards.PointsFor(StandingRecovery.ChoreSize));
Check("chores raise and stop exactly at neutral", g1 == 75 && g2 == 75 && g3 == 0 && ReputationState.Score(r.Get(civ, "goryeo")) == 0
    && r.Get(civ, "goryeo").standing == ReputationStanding.Neutral);
Check("chore unavailable at neutral", StandingRecovery.ChoreDenial(a, r, civ, "goryeo") != null);
Check("atonement-sized raise at neutral gives nothing", StandingRecovery.Raise(r, civ, "goryeo", 250) == 0);

var hostile = new ReputationState(); hostile.Initialize("goryeo"); hostile.Add(civ, "goryeo", -9000); // floor -6000
int chores = 0; while (StandingRecovery.Negative(hostile, civ, "goryeo") && chores < 1000) { StandingRecovery.Raise(hostile, civ, "goryeo", 75); chores++; }
Check("from the floor (적대 -6000) chores alone reach neutral and never pass it", ReputationState.Score(hostile.Get(civ, "goryeo")) == 0 && chores == 80);

var d = new ReputationState(); d.Initialize("goryeo"); d.Add(townScope, "east_gate", -31);
Check("donation charges only what is needed", StandingRecovery.Donate(d, townScope, "east_gate", 200, 500) == (62, 31) && StandingRecovery.Score(d, townScope, "east_gate") == 0);
Check("donation at neutral takes nothing", StandingRecovery.Donate(d, townScope, "east_gate", 100, 500) == (0, 0));
d.Add(townScope, "east_gate", -100);
Check("donation limited by wallet", StandingRecovery.Donate(d, townScope, "east_gate", 200, 51) == (50, 25) && StandingRecovery.Score(d, townScope, "east_gate") == -75);
Check("donation with no money does nothing", StandingRecovery.Donate(d, townScope, "east_gate", 200, 0) == (0, 0));
d.Add(townScope, "port", -200);
StandingRecovery.Raise(d, townScope, "east_gate", 1000);
Check("central village A recovery leaves village B unchanged", StandingRecovery.Score(d, townScope, "east_gate") == 0 && StandingRecovery.Score(d, townScope, "port") == -200);
Check("town and civilization units are separate", StandingRecovery.Score(d, civ, "goryeo") == 3000);

Check("atonement deposit and deadline grow with route", StandingRecovery.AtonementDeposit(1) < StandingRecovery.AtonementDeposit(3)
    && StandingRecovery.AtonementHours(2) < 9 * 2 + 8);
Check("atonement is worth several chores", ReputationAwards.PointsFor(StandingRecovery.AtonementSize) >= 3 * ReputationAwards.PointsFor(StandingRecovery.ChoreSize));
a.SetAtonementReopen(0, "goryeo", 48); a.StartChore("naru", 2, 7);
var ra = new TradeActivity(); ra.Import(Roundtrip(a.Export()));
Check("chore and atonement reopen survive save", ra.ActiveChore?.town == "naru" && ra.ActiveChore.kind == 2 && ra.AtonementReopensAt(0, "goryeo") == 48);
Check("reloaded chore still completes only once", ra.FinishChore("naru") && !ra.FinishChore("naru"));

// A quote belongs to the real executable quantity, including the bulk threshold.
Check("successful haggle is bound to exact quantity", win.deal.count == 1
    && !win.deal.AppliesTo("naru", "salt", false, winDay, 20));
Check("zero executable quantity cannot haggle", Haggle.Try(new TradeActivity(), "naru", "grain", false, 0,
    ReputationStanding.Neutral, neutral, HaggleStrength.Fair, 1, 8).outcome == HaggleOutcome.Unavailable);
foreach(int count in new[]{1,19,20,100}) {
    var quantityDeal=new HaggleDeal("naru","grain",false,1,.01f,HaggleStrength.Fair,count);
    Check("quantity binding " + count, quantityDeal.AppliesTo("naru","grain",false,1,count)
        && !quantityDeal.AppliesTo("naru","grain",false,1,count+1));
}
Console.WriteLine(JsonSerializer.Serialize(new { passed, count = passed.Count }, new JsonSerializerOptions { WriteIndented = true }));
