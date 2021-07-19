// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using McMaster.Extensions.CommandLineUtils;

namespace osu.Server.Queues.ScorePump
{
    [Command("clear-qeuue", Description = "Completely empties the processing queue")]
    public class ClearQueue : ScorePump
    {
        public int OnExecute(CancellationToken cancellationToken)
        {
            Queue.ClearQueue();
            return 0;
        }
    }
}
