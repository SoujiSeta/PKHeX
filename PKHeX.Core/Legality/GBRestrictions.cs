﻿using System;
using System.Collections.Generic;
using System.Linq;
using PKHeX.Core;

using static PKHeX.Core.Legal;

namespace PKHeX
{
    internal static class GBRestrictions
    {
        internal static readonly int[] G1CaterpieMoves = { 33, 81 };
        internal static readonly int[] G1WeedleMoves = { 40, 81 };
        internal static readonly int[] G1MetapodMoves = G1CaterpieMoves.Concat(new[] { 106 }).ToArray();
        internal static readonly int[] G1KakunaMoves = G1WeedleMoves.Concat(new[] { 106 }).ToArray();
        internal static readonly int[] G1Exeggcute_IncompatibleMoves = { 78, 77, 79 };

        internal static readonly int[] Stadium_CatchRate =
        {
            167, // Normal Box
            168, // Gorgeous Box
        };

        internal static readonly HashSet<int> Stadium_GiftSpecies = new HashSet<int>
        {
            001, // Bulbasaur
            004, // Charmander
            007, // Squirtle
            054, // Psyduck (Amnesia)
            106, // Hitmonlee
            107, // Hitmonchan
            133, // Eevee
            138, // Omanyte
            140, // Kabuto
        };

        internal static readonly HashSet<int> SpecialMinMoveSlots = new HashSet<int>
        {
            25, 26, 29, 30, 31, 32, 33, 34, 36, 38, 40, 59, 91, 103, 114, 121,
        };

        internal static readonly HashSet<int> Types_Gen1 = new HashSet<int>
        {
            0, 1, 2, 3, 4, 5, 7, 8, 20, 21, 22, 23, 24, 25, 26
        };

        internal static readonly HashSet<int> Species_NotAvailable_CatchRate = new HashSet<int>
        {
            12, 18, 31, 34, 38, 45, 53, 59, 62, 65, 68, 71, 78, 91, 103, 121
        };

        internal static readonly HashSet<int> Trade_Evolution1 = new HashSet<int>
        {
            064,
            067,
            075,
            093
        };

        private static int[] GetMinLevelLearnMoveG1(int species, List<int> moves)
        {
            var r = new int[moves.Count];
            for (int i = 0; i < r.Length; i++)
                r[i] = MoveLevelUp.GetIsLevelUp1(species, moves[i], 100, 0, 0).Level;
            return r;
        }

        private static int[] GetMaxLevelLearnMoveG1(int species, List<int> moves)
        {
            var r = new int[moves.Count];

            int index = PersonalTable.RB.GetFormeIndex(species, 0);
            if (index == 0)
                return r;

            var pi_rb = ((PersonalInfoG1)PersonalTable.RB[index]).Moves;
            var pi_y = ((PersonalInfoG1)PersonalTable.Y[index]).Moves;

            for (int m = 0; m < moves.Count; m++)
            {
                bool start = pi_rb.Contains(moves[m]) && pi_y.Contains(moves[m]);
                r[m] = start ? 1 : Math.Max(GetHighest(LevelUpRB), GetHighest(LevelUpY));
                int GetHighest(IReadOnlyList<Learnset> learn) => learn[index].GetLevelLearnMove(moves[m]);
            }
            return r;
        }

        private static List<int>[] GetExclusiveMovesG1(int species1, int species2, IEnumerable<int> tmhm, IEnumerable<int> moves)
        {
            // Return from two species the exclusive moves that only one could learn and also the current pokemon have it in its current moveset
            var moves1 = MoveLevelUp.GetMovesLevelUp1(species1, 0, 1, 100);
            var moves2 = MoveLevelUp.GetMovesLevelUp1(species2, 0, 1, 100);

            // Remove common moves and remove tmhm, remove not learned moves
            var common = new HashSet<int>(moves1.Intersect(moves2).Concat(tmhm));
            var hashMoves = new HashSet<int>(moves);
            moves1.RemoveAll(x => !hashMoves.Contains(x) || common.Contains(x));
            moves2.RemoveAll(x => !hashMoves.Contains(x) || common.Contains(x));
            return new[] { moves1, moves2 };
        }

        internal static void GetIncompatibleEvolutionMoves(PKM pkm, int[] moves, List<int> tmhm, out int previousspecies, out IList<int> incompatible_previous, out IList<int> incompatible_current)
        {
            switch (pkm.Species)
            {
                case 34 when moves.Contains(31) && moves.Contains(37):
                    // Nidoking learns Thrash at level 23
                    // Nidorino learns Fury Attack at level 36, Nidoran♂ at level 30
                    // Other moves are either learned by Nidoran♂ up to level 23 or by TM
                    incompatible_current = new[] { 31 };
                    incompatible_previous = new[] { 37 };
                    previousspecies = 33;
                    return;

                case 103 when moves.Contains(23) && moves.Any(m => G1Exeggcute_IncompatibleMoves.Contains(moves[m])):
                    // Exeggutor learns stomp at level 28
                    // Exeggcute learns Stun Spore at 32, PoisonPowder at 37 and Sleep Powder at 48
                    incompatible_current = new[] { 23 };
                    incompatible_previous = G1Exeggcute_IncompatibleMoves;
                    previousspecies = 103;
                    return;

                case 134:
                case 135:
                case 136:
                    incompatible_previous = new List<int>();
                    incompatible_current = new List<int>();
                    previousspecies = 133;
                    var ExclusiveMoves = GetExclusiveMovesG1(133, pkm.Species, tmhm, moves);
                    var EeveeLevels = GetMinLevelLearnMoveG1(133, ExclusiveMoves[0]);
                    var EvoLevels = GetMaxLevelLearnMoveG1(pkm.Species, ExclusiveMoves[1]);

                    for (int i = 0; i < ExclusiveMoves[0].Count; i++)
                    {
                        // There is a evolution move with a lower level that current eevee move
                        if (EvoLevels.Any(ev => ev < EeveeLevels[i]))
                            incompatible_previous.Add(ExclusiveMoves[0][i]);
                    }
                    for (int i = 0; i < ExclusiveMoves[1].Count; i++)
                    {
                        // There is a eevee move with a greather level that current evolution move
                        if (EeveeLevels.Any(ev => ev > EvoLevels[i]))
                            incompatible_current.Add(ExclusiveMoves[1][i]);
                    }
                    return;
            }
            incompatible_previous = Array.Empty<int>();
            incompatible_current = Array.Empty<int>();
            previousspecies = 0;
        }

        internal static int GetRequiredMoveCount(PKM pk, int[] moves, LegalInfo info, int[] initialmoves)
        {
            if (!pk.Gen1_NotTradeback) // No Move Deleter in Gen 1
                return 1; // Move Deleter exits, slots from 2 onwards can always be empty

            int required = GetRequiredMoveCount(pk, moves, info.EncounterMoves.LevelUpMoves, initialmoves);
            if (required >= 4)
                return 4;

            // tm, hm and tutor moves replace a free slots if the pokemon have less than 4 moves
            // Ignore tm, hm and tutor moves already in the learnset table
            var learn = info.EncounterMoves.LevelUpMoves;
            var tmhm = info.EncounterMoves.TMHMMoves;
            var tutor = info.EncounterMoves.TutorMoves;
            var union = initialmoves.Union(learn[1]);
            required += moves.Count(m => m != 0 && union.All(t => t != m) && (tmhm[1].Any(t => t == m) || tutor[1].Any(t => t == m)));

            return Math.Min(4, required);
        }

        private static int GetRequiredMoveCount(PKM pk, int[] moves, List<int>[] learn, int[] initialmoves)
        {
            if (SpecialMinMoveSlots.Contains(pk.Species))
                return GetRequiredMoveCountSpecial(pk, moves, learn);

            // A pokemon is captured with initial moves and can't forget any until have all 4 slots used
            // If it has learn a move before having 4 it will be in one of the free slots
            int required = GetRequiredMoveSlotsRegular(pk, moves, learn, initialmoves);
            return required != 0 ? required : GetRequiredMoveCountDecrement(pk, moves, learn, initialmoves);
        }

        private static int GetRequiredMoveSlotsRegular(PKM pk, int[] moves, List<int>[] learn, int[] initialmoves)
        {
            int species = pk.Species;
            int catch_rate = ((PK1)pk).Catch_Rate;
            // Caterpie and Metapod evolution lines have different count of possible slots available if captured in different evolutionary phases
            // Example: a level 7 caterpie evolved into metapod will have 3 learned moves, a captured metapod will have only 1 move
            if ((species == 011 || species == 012) && catch_rate == 120)
            {
                // Captured as Metapod without Caterpie moves
                return initialmoves.Union(learn[1]).Distinct().Count(lm => lm != 0 && !G1CaterpieMoves.Contains(lm));
                // There is no valid Butterfree encounter in generation 1 games
            }
            if ((species == 014 || species == 015) && (catch_rate == 45 || catch_rate == 120))
            {
                if (species == 15 && catch_rate == 45) // Captured as Beedril without Weedle and Kakuna moves
                    return initialmoves.Union(learn[1]).Distinct().Count(lm => lm != 0 && !G1KakunaMoves.Contains(lm));

                // Captured as Kakuna without Weedle moves
                return initialmoves.Union(learn[1]).Distinct().Count(lm => lm != 0 && !G1WeedleMoves.Contains(lm));
            }

            return IsMoveCountRequired3(species, pk.CurrentLevel, moves) ? 3 : 0; // no match
        }

        private static bool IsMoveCountRequired3(int species, int level, int[] moves)
        {
            // Species that evolve and learn the 4th move as evolved species at a greather level than base species
            // The 4th move is included in the level up table set as a preevolution move,
            // it should be removed from the used slots count if is not the learn move
            switch (species)
            {
                case 017: return level < 21 && !moves.Contains(018); // Pidgeotto without Whirlwind
                case 028: return level < 27 && !moves.Contains(040); // Sandslash without Poison Sting
                case 047: return level < 30 && !moves.Contains(147); // Parasect without Spore
                case 055: return level < 39 && !moves.Contains(093); // Golduck without Confusion
                case 087: return level < 44 && !moves.Contains(156); // Dewgong without Rest
                case 093:
                case 094: return level < 29 && !moves.Contains(095); // Haunter/Gengar without Hypnosis
                case 110: return level < 39 && !moves.Contains(108); // Weezing without Smoke Screen
            }
            return false;
        }

        private static int GetRequiredMoveCountDecrement(PKM pk, int[] moves, List<int>[] learn, int[] initialmoves)
        {
            int usedslots = initialmoves.Union(learn[1]).Where(m => m != 0).Distinct().Count();
            switch (pk.Species)
            {
                case 031: // Venonat; ignore Venomoth (by the time Venonat evolves it will always have 4 moves)
                    if (pk.CurrentLevel >= 11 && !moves.Contains(48)) // Supersonic
                        usedslots--;
                    if (pk.CurrentLevel >= 19 && !moves.Contains(93)) // Confusion
                        usedslots--;
                    break;
                case 064:
                case 065: // Abra & Kadabra
                    int catch_rate = ((PK1)pk).Catch_Rate;
                    if (catch_rate != 100)// Initial Yellow Kadabra Kinesis (move 134)
                        usedslots--;
                    if (catch_rate == 200 && pk.CurrentLevel < 20) // Kadabra Disable, not learned until 20 if captured as Abra (move 50)
                        usedslots--;
                    break;
                case 104:
                case 105: // Cubone & Marowak
                    if (!moves.Contains(39)) // Initial Yellow Tail Whip
                        usedslots--;
                    if (!moves.Contains(125)) // Initial Yellow Bone Club
                        usedslots--;
                    if (pk.Species == 105 && pk.CurrentLevel < 33 && !moves.Contains(116)) // Marowak evolved without Focus Energy
                        usedslots--;
                    break;
                case 113:
                    if (!moves.Contains(39)) // Yellow Initial Tail Whip
                        usedslots--;
                    if (!moves.Contains(3)) // Yellow Lvl 12 and Initial Red/Blue Double Slap
                        usedslots--;
                    break;
                case 056 when pk.CurrentLevel >= 9 && !moves.Contains(67): // Mankey (Low Kick)
                case 127 when pk.CurrentLevel >= 21 && !moves.Contains(20): // Pinsir (Bind)
                case 130 when pk.CurrentLevel < 32: // Gyarados
                    usedslots--;
                    break;
            }
            return usedslots;
        }

        private static int GetRequiredMoveCountSpecial(PKM pk, int[] moves, List<int>[] learn)
        {
            // Species with few mandatory slots, species with stone evolutions that could evolve at lower level and do not learn any more moves
            // and Pikachu and Nidoran family, those only have mandatory the initial moves and a few have one level up moves,
            // every other move could be avoided switching game or evolving
            var mandatory = GetRequiredMoveCountLevel(pk);
            switch (pk.Species)
            {
                case 103 when pk.CurrentLevel >= 28: // Exeggutor
                    // At level 28 learn different move if is a Exeggute or Exeggutor
                    if (moves.Contains(73))
                        mandatory.Add(73); // Leech Seed level 28 Exeggute
                    if (moves.Contains(23))
                        mandatory.Add(23); // Stomp level 28 Exeggutor
                    break;
                case 25 when pk.CurrentLevel >= 33:
                    mandatory.Add(97); // Pikachu always learns Agility
                    break;
                case 114:
                    mandatory.Add(132); // Tangela always has Constrict as Initial Move
                    break;
            }

            // Add to used slots the non-mandatory moves from the learnset table that the pokemon have learned
            return mandatory.Count + moves.Count(m => m != 0 && mandatory.All(l => l != m) && learn[1].Any(t => t == m));
        }

        private static List<int> GetRequiredMoveCountLevel(PKM pk)
        {
            int species = pk.Species;
            int basespecies = GetBaseSpecies(pk);
            int maxlevel = 1;
            int minlevel = 1;

            if (species == 114) // Tangela moves before level 32 are different in RB vs Y
            {
                minlevel = 32;
                maxlevel = pk.CurrentLevel;
            }
            else if (029 <= species && species <= 034 && pk.CurrentLevel >= 8)
            {
                maxlevel = 8; // Always learns a third move at level 8
            }

            if (minlevel > pk.CurrentLevel)
                return new List<int>();

            return MoveLevelUp.GetMovesLevelUp1(basespecies, 0, maxlevel, minlevel);
        }

        internal static IEnumerable<GameVersion> GetGen2Versions(LegalInfo Info)
        {
            if (ParseSettings.AllowGen2Crystal(Info.Korean) && Info.Game == GameVersion.C)
                yield return GameVersion.C;

            // Any encounter marked with version GSC is for pokemon with the same moves in GS and C
            // it is sufficient to check just GS's case
            yield return GameVersion.GS;
        }

        internal static IEnumerable<GameVersion> GetGen1Versions(LegalInfo Info)
        {
            if (Info.EncounterMatch.Species == 133 && Info.Game == GameVersion.Stadium)
            {
                // Stadium Eevee; check for RB and yellow initial moves
                yield return GameVersion.RB;
                yield return GameVersion.YW;
                yield break;
            }
            if (Info.Game == GameVersion.YW)
            {
                yield return GameVersion.YW;
                yield break;
            }

            // Any encounter marked with version RBY is for pokemon with the same moves and catch rate in RB and Y,
            // it is sufficient to check just RB's case
            yield return GameVersion.RB;
        }

        private static bool GetCatchRateMatchesPreEvolution(PKM pkm, int catch_rate, IEnumerable<int> gen1)
        {
            // For species catch rate, discard any species that has no valid encounters and a different catch rate than their pre-evolutions
            var Lineage = gen1.Except(Species_NotAvailable_CatchRate);
            return IsCatchRateRBY(Lineage) || IsCatchRateTrade() || IsCatchRateStadium();

            // Dragonite's Catch Rate is different than Dragonair's in Yellow, but there is no Dragonite encounter.
            bool IsCatchRateRBY(IEnumerable<int> ds) => ds.Any(s => catch_rate == PersonalTable.RB[s].CatchRate || (s != 149 && catch_rate == PersonalTable.Y[s].CatchRate));
            // Krabby encounter trade special catch rate
            bool IsCatchRateTrade() => (pkm.Species == 098 || pkm.Species == 099) && catch_rate == 204;
            bool IsCatchRateStadium() => Stadium_GiftSpecies.Contains(pkm.Species) && Stadium_CatchRate.Contains(catch_rate);
        }

        /// <summary>
        /// Gets the Tradeback status depending on various values.
        /// </summary>
        /// <param name="pkm">Pokémon to guess the tradeback status from.</param>
        internal static TradebackType GetTradebackStatusInitial(PKM pkm)
        {
            if (pkm is PK1 pk1)
                return GetTradebackStatusRBY(pk1);

            if (pkm.Format == 2 || pkm.VC2) // Check for impossible tradeback scenarios
                return !pkm.CanInhabitGen1() ? TradebackType.Gen2_NotTradeback : TradebackType.Any;

            // VC2 is released, we can assume it will be TradebackType.Any.
            // Is impossible to differentiate a VC1 pokemon traded to Gen7 after VC2 is available.
            // Met Date cannot be used definitively as the player can change their system clock.
            return TradebackType.Any;
        }

        /// <summary>
        /// Gets the Tradeback status depending on the <see cref="PK1.Catch_Rate"/>
        /// </summary>
        /// <param name="pkm">Pokémon to guess the tradeback status from.</param>
        private static TradebackType GetTradebackStatusRBY(PK1 pkm)
        {
            if (!ParseSettings.AllowGen1Tradeback)
                return TradebackType.Gen1_NotTradeback;

            // Detect tradeback status by comparing the catch rate(Gen1)/held item(Gen2) to the species in the pkm's evolution chain.
            var catch_rate = pkm.Catch_Rate;
            if (catch_rate == 0)
                return TradebackType.WasTradeback;

            var table = EvolutionTree.GetEvolutionTree(1);
            var lineage = table.GetValidPreEvolutions(pkm, maxLevel: pkm.CurrentLevel);
            var gen1 = lineage.Select(evolution => evolution.Species);
            bool matchAny = GetCatchRateMatchesPreEvolution(pkm, catch_rate, gen1);

            if (!matchAny)
                return TradebackType.WasTradeback;

            if (HeldItems_GSC.Contains((ushort)catch_rate))
                return TradebackType.Any;

            return TradebackType.Gen1_NotTradeback;
        }

        internal static bool IsTradedKadabraG1(PKM pkm)
        {
            if (!(pkm is PK1 pk1) || pk1.Species != 64)
                return false;
            if (pk1.TradebackStatus == TradebackType.WasTradeback)
                return true;
            if (ParseSettings.ActiveTrainer.Game == (int)GameVersion.Any)
                return false;
            var IsYellow = ParseSettings.ActiveTrainer.Game == (int)GameVersion.YW;
            if (pk1.TradebackStatus == TradebackType.Gen1_NotTradeback)
            {
                // If catch rate is Abra catch rate it wont trigger as invalid trade without evolution, it could be traded as Abra
                // Yellow Kadabra catch rate in Red/Blue game, must be Alakazam
                var table = IsYellow ? PersonalTable.RB : PersonalTable.Y;
                if (pk1.Catch_Rate == table[64].CatchRate)
                    return true;
            }
            if (IsYellow)
                return false;
            // Yellow only moves in Red/Blue game, must be Alakazam
            var moves = pk1.Moves;
            if (moves.Contains(134)) // Kinesis, yellow only move
                return true;
            if (pk1.CurrentLevel < 20 && moves.Contains(50)) // Obtaining Disable below level 20 implies a yellow only move
                return true;

            return false;
        }
    }
}
