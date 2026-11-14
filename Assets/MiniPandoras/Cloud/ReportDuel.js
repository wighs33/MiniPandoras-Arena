/*
 * 온라인 한 판의 결과 보고 (Cloud Code 스크립트 「ReportDuel」).
 *
 * 두 사람이 판이 끝날 때마다 각자 「이 판에서 누구와 싸워 이겼는지·졌는지」를 보낸다. 보고는 Cloud Save의
 * 서버 전용 칸(플레이어는 쓰지 못한다)에 적어 두고, 두 보고가 모두 모이고 서로 맞을 때만(한 사람은 이기고
 * 한 사람은 졌다) 두 사람의 Elo 점수를 순위표에 적는다. 플레이어는 순위표에 직접 쓰지 못한다(NoClientScores.ac).
 * 그래서 한쪽 클라이언트를 고쳐 「이겼다」고 보내도, 상대가 같은 결과를 보내지 않으면 점수가 바뀌지 않는다.
 *
 * 두 보고가 거의 같은 때 오면 두 호출이 모두 「상대 보고가 있다」를 볼 수 있다. 점수는 한 번만 바꿔야 하므로
 * 플레이어 ID가 앞서는 쪽의 보고를 「정산했다」로 바꾸는 쪽만 정산한다. 바꿀 때 Cloud Save의 writeLock을 쓰므로
 * 둘 가운데 하나만 성공한다.
 */

const LEADERBOARD_ID = "duel-elo";
const START_RATING = 1000;
// 한 판에 오르내릴 수 있는 가장 큰 점수. 비슷한 상대끼리면 그 절반쯤이 오간다.
const K_FACTOR = 32;

module.exports = async ({ params, context, logger }) => {
  // 서비스 SDK는 Cloud Code에만 있으므로 여기서 불러온다(아래 계산 함수는 로컬 시험에서 따로 부른다).
  const { DataApi } = require("@unity-services/cloud-save-1.4");
  const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");
  const cloudSave = new DataApi(context);
  const leaderboards = new LeaderboardsApi(context);
  const { projectId, playerId } = context;
  const { matchId, opponentId } = params;
  const reports = reportsOf(matchId);

  const mine = await readReport(cloudSave, projectId, reports, playerId);
  if (mine && mine.report.settled) {
    return { status: "settled" };
  }

  const report = { opponentId, won: params.won, nickname: params.nickname, pandora: params.pandora };
  await cloudSave.setCustomItem(projectId, reports, { key: playerId, value: report });

  const theirs = await readReport(cloudSave, projectId, reports, opponentId);
  if (!theirs) {
    return { status: "waiting" };
  }

  // 정산할 권리를 얻는다. 상대가 먼저 얻었으면 여기서 멈춘다.
  const claimant = firstOf(playerId, opponentId);
  const claim = await readReport(cloudSave, projectId, reports, claimant);
  if (claim.report.settled) {
    return { status: "settled" };
  }

  const agreed = reportsAgree(report, playerId, theirs.report, opponentId);
  try {
    await cloudSave.setCustomItem(projectId, reports,
      { key: claimant, value: { ...claim.report, settled: true, disputed: !agreed }, writeLock: claim.writeLock });
  } catch (error) {
    if (error.response && error.response.status === 409) {
      return { status: "settled" };
    }
    throw error;
  }

  if (!agreed) {
    logger.warning(`보고가 서로 맞지 않는다: 판 ${matchId}, ${playerId}와 ${opponentId}`);
    return { status: "disputed" };
  }

  const winner = report.won ? { id: playerId, report } : { id: opponentId, report: theirs.report };
  const loser = report.won ? { id: opponentId, report: theirs.report } : { id: playerId, report };
  const before = {
    winner: await readRating(leaderboards, projectId, winner.id),
    loser: await readRating(leaderboards, projectId, loser.id),
  };
  const after = ratingsAfterDuel(before.winner, before.loser);
  await writeRating(leaderboards, projectId, winner, after.winner);
  await writeRating(leaderboards, projectId, loser, after.loser);

  const myBefore = report.won ? before.winner : before.loser;
  const myAfter = report.won ? after.winner : after.loser;
  return { status: "rated", rating: myAfter, change: myAfter - myBefore };
};

module.exports.params = {
  matchId: { type: "String", required: true },
  opponentId: { type: "String", required: true },
  won: { type: "Boolean", required: true },
  nickname: { type: "String", required: true },
  pandora: { type: "String", required: true },
};

// ── 계산 (서비스를 부르지 않는다) ─────────────────────────────

/**
 * 한 판의 보고를 적어 두는 Cloud Save의 서버 전용 칸(custom item) ID. 키는 두 사람의 플레이어 ID다.
 * 칸 하나에는 키를 2000개까지만 둘 수 있고 칸 수에는 제한이 없어서, 판마다 칸을 따로 쓴다.
 */
function reportsOf(matchId) {
  return `duel-${matchId}`;
}

function firstOf(a, b) {
  return a < b ? a : b;
}

/** 두 보고가 같은 판을 말하는지: 서로를 상대로 적었고, 한 사람만 이겼다. */
function reportsAgree(mine, myId, theirs, theirId) {
  return mine.opponentId === theirId && theirs.opponentId === myId && mine.won !== theirs.won;
}

/** Elo: 이길 것으로 보였던 정도만큼 덜 오른다. 오른 만큼 진 사람이 잃는다. */
function ratingsAfterDuel(winnerRating, loserRating) {
  const expected = 1 / (1 + Math.pow(10, (loserRating - winnerRating) / 400));
  const change = Math.round(K_FACTOR * (1 - expected));
  return { winner: winnerRating + change, loser: loserRating - change };
}

// ── 서비스 ────────────────────────────────────────────

async function readReport(cloudSave, projectId, reports, playerId) {
  const response = await cloudSave.getCustomItems(projectId, reports, [playerId]);
  const item = response.data.results[0];
  return item ? { report: item.value, writeLock: item.writeLock } : null;
}

/** 순위표에 아직 없으면(처음 순위에 드는 판) 시작 점수다. */
async function readRating(leaderboards, projectId, playerId) {
  try {
    const response = await leaderboards.getLeaderboardPlayerScore(projectId, LEADERBOARD_ID, playerId);
    return Math.round(response.data.score);
  } catch (error) {
    if (error.response && error.response.status === 404) {
      return START_RATING;
    }
    throw error;
  }
}

async function writeRating(leaderboards, projectId, player, rating) {
  const metadata = { nickname: player.report.nickname, pandora: player.report.pandora };
  await leaderboards.addLeaderboardPlayerScore(projectId, LEADERBOARD_ID, player.id, { score: rating, metadata });
}

module.exports.reportsAgree = reportsAgree;
module.exports.ratingsAfterDuel = ratingsAfterDuel;
module.exports.firstOf = firstOf;
