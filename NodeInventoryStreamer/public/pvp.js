var highestRank = 4;
var player = function(name) {
  this.name = name;
  this.score = 0;
  this.kills = 0;
  this.deaths = 0;
}
var players = [new player("Rofle"), new player("Avery"), new player("Blanc"), new player("NiteMare"), new player("Unarmedbox"), new player("Butt"), new player("Fearoflife"), new player("Logan")];

function getPlayersOrderedByRank() {
  var orderedPlayers = [];
  var playersCount = players.length;

  for (var i = 0; i < playersCount; i++) {
    if (i > 0) {
      for (var j = orderedPlayers.length-1; j >= 0; j--) {
        if (orderedPlayers[j].rank <= players[i].rank && orderedPlayers[j].score < players[i].score) {
          if (typeof(orderedPlayers[j+1]) !== 'undefined')
            orderedPlayers[j+2] = orderedPlayers[j+1];
          orderedPlayers[j+1] = players[i];
          break;
        } else {
          orderedPlayers[j+1] = orderedPlayers[j];

          if (j === 0)
            orderedPlayers[j] = players[i];
        }
      }
    } else {
      orderedPlayers.push(players[i]);
    }
  }

 return orderedPlayers;
}

function playerKillsPlayer(killer, victim) {
  killer.kills++;
  victim.deaths++;

  var orderedPlayers = getPlayersOrderedByRank();
  highestRanker = getHighestPersonInRank(killer.rank, orderedPlayers);
  if (killer.rank-victim.rank > 1 && killer.score-victim.score < highestRanker.score/2)
    return;

  if (victim.rank-killer.rank >= 0 || killer.score-victim.score <= players.length/2) {
    killer.score++;
  }
  
  var victimScoreLoss = victim.rank-killer.rank > 1 ? victim.rank-killer.rank : 1;
  victim.score -= victimScoreLoss;

  if (victim.score < 0)
    victim.score = 0;

  var playersLength = players.length;
  for (var i = 0; i < playersLength; i++) {
    reRank(players[i]);
  }
}

function reRank(player) {
  var orderedPlayers = getPlayersOrderedByRank();
  var highestRanked = orderedPlayers[orderedPlayers.length-1];
  if (player.score === 0)
    player.rank = 0;
  else {
    for (var rank = 1; rank <= highestRank; rank++) {
      //console.log("Rank: "+rank);
        highestRanker = getHighestPersonInRank(rank, orderedPlayers);
        if (highestRanker != null && highestRanker.score-player.score <= highestRanker.score/4) {
          //console.log(player.name+" is within "+(highestRanker.score-player.score)+" away from "+highestRanker.name+"'s Score");
          player.rank = rank;
        } else if (highestRanker === null) {
          player.rank = rank;
        } else {
          break;
        }
    }
  }
}

function getHighestPersonInRank(rank, orderedPlayers) {
  for (var i = orderedPlayers.length-1; i >= 0; i--) {
    if (orderedPlayers[i].rank === rank) {
      return orderedPlayers[i];
    }
  }

  return null;
}

function randomPlay() {
  for (var turns = 0; turns < 500; turns++) {
      var randPlayer = players[Math.floor(Math.random()*players.length)];
      var randPlayer2 = players[Math.floor(Math.random()*players.length)];
      if (randPlayer.name !== randPlayer2.name) {
        playerKillsPlayer(randPlayer, randPlayer2);
      }
    }
}