using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace GiddyUpCore.Mechanoids
{
    public static class Extensions
    {
        public static bool IsHacked(this Pawn pawn, Pawn? by = null)
        {
            var overseer = pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Overseer);
            return pawn.RaceProps.IsMechanoid &&
                   (by != null ? by == overseer : overseer != null ||
                    WhatTheHackCompatibility.IsHacked(pawn));
        }

        public static bool IsActivated(this Pawn pawn)
        {
            return pawn.RaceProps.IsMechanoid &&  ((!pawn.needs.energy.IsLowEnergySelfShutdown && pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Overseer) != null) ||
                   WhatTheHackCompatibility.IsActivated(pawn));
        }


    }
}
