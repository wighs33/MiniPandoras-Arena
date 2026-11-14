// Cloud Code 「ReportDuel」의 계산(보고가 맞는지, Elo)을 로컬에서 시험한다. 서비스는 부르지 않는다.
//
//     node --test Tools/CloudCode/report_duel.test.js
//
const test = require("node:test");
const assert = require("node:assert");
const script = require("../../Assets/MiniPandoras/Cloud/ReportDuel.js");

test("한 사람이 이기고 한 사람이 졌으며 서로를 상대로 적었으면 맞는다", () => {
  const a = { opponentId: "B", won: true };
  const b = { opponentId: "A", won: false };
  assert.strictEqual(script.reportsAgree(a, "A", b, "B"), true);
});

test("둘 다 이겼다고 하면 맞지 않는다", () => {
  const a = { opponentId: "B", won: true };
  const b = { opponentId: "A", won: true };
  assert.strictEqual(script.reportsAgree(a, "A", b, "B"), false);
});

test("다른 사람과 싸웠다고 하면 맞지 않는다", () => {
  const a = { opponentId: "B", won: true };
  const b = { opponentId: "C", won: false };
  assert.strictEqual(script.reportsAgree(a, "A", b, "B"), false);
});

test("자기 자신을 상대로 적으면 맞지 않는다", () => {
  const a = { opponentId: "A", won: true };
  assert.strictEqual(script.reportsAgree(a, "A", a, "A"), false);
});

test("같은 점수끼리는 16점이 오간다", () => {
  assert.deepStrictEqual(script.ratingsAfterDuel(1000, 1000), { winner: 1016, loser: 984 });
});

test("약한 쪽이 이기면 많이 오르고, 강한 쪽이 이기면 조금 오른다", () => {
  assert.deepStrictEqual(script.ratingsAfterDuel(1000, 1200), { winner: 1024, loser: 1176 });
  assert.deepStrictEqual(script.ratingsAfterDuel(1200, 1000), { winner: 1208, loser: 992 });
});

test("오른 만큼 잃는다", () => {
  const after = script.ratingsAfterDuel(1137, 954);
  assert.strictEqual(after.winner + after.loser, 1137 + 954);
});

test("정산은 플레이어 ID가 앞서는 쪽의 보고로 한다", () => {
  assert.strictEqual(script.firstOf("b", "a"), "a");
  assert.strictEqual(script.firstOf("a", "b"), "a");
});
