using System.Diagnostics;
using Xunit;

namespace osu.Server.Queues.ScoreStatisticsProcessor.Tests
{
    public class PlayCountProcessorTests : DatabaseTest
    {
        [Fact]
        public void TestPlaycountIncreaseMania()
        {
            WaitForDatabaseState("SELECT playcount FROM osu_user_stats_mania WHERE user_id = 2", (int?)null, CancellationToken);

            Processor.PushToQueue(CreateTestScore(3));
            WaitForDatabaseState("SELECT playcount FROM osu_user_stats_mania WHERE user_id = 2", 1, CancellationToken);

            Processor.PushToQueue(CreateTestScore(3));
            WaitForDatabaseState("SELECT playcount FROM osu_user_stats_mania WHERE user_id = 2", 2, CancellationToken);
        }

        [Fact]
        public void TestPlaycountIncrease()
        {
            WaitForDatabaseState("SELECT playcount FROM osu_user_stats WHERE user_id = 2", (int?)null, CancellationToken);

            Processor.PushToQueue(CreateTestScore());
            WaitForDatabaseState("SELECT playcount FROM osu_user_stats WHERE user_id = 2", 1, CancellationToken);

            Processor.PushToQueue(CreateTestScore());
            WaitForDatabaseState("SELECT playcount FROM osu_user_stats WHERE user_id = 2", 2, CancellationToken);
        }

        [Fact]
        public void TestProcessingSameScoreTwiceRaceCondition()
        {
            WaitForDatabaseState("SELECT playcount FROM osu_user_stats WHERE user_id = 2", (int?)null, CancellationToken);

            var score = CreateTestScore();

            Processor.PushToQueue(score);
            Processor.PushToQueue(score);
            Processor.PushToQueue(score);
            Processor.PushToQueue(score);
            Processor.PushToQueue(score);

            WaitForDatabaseState("SELECT playcount FROM osu_user_stats WHERE user_id = 2", 1, CancellationToken);

            WaitForTotalProcessed(5, CancellationToken);

            // check only one score was counted, even though many were pushed.
            WaitForDatabaseState("SELECT playcount FROM osu_user_stats WHERE user_id = 2", 1, CancellationToken);
        }

        [Fact]
        public void TestPlaycountReprocessDoesntIncrease()
        {
            var score = CreateTestScore();

            WaitForDatabaseState("SELECT playcount FROM osu_user_stats WHERE user_id = 2", (int?)null, CancellationToken);

            Processor.PushToQueue(score);
            WaitForDatabaseState("SELECT playcount FROM osu_user_stats WHERE user_id = 2", 1, CancellationToken);

            // the score will be marked as processed (in the database) at this point, so should not increase the playcount if processed a second time.
            score.MarkProcessed();

            Processor.PushToQueue(score);
            WaitForDatabaseState("SELECT playcount FROM osu_user_stats WHERE user_id = 2", 1, CancellationToken);
        }

        [Fact]
        public void TestUserBeatmapPlaycountIncrease()
        {
            WaitForDatabaseState($"SELECT playcount FROM osu_user_beatmap_playcount WHERE user_id = 2 and beatmap_id = {TEST_BEATMAP_ID}", (int?)null, CancellationToken);

            Processor.PushToQueue(CreateTestScore());
            WaitForDatabaseState($"SELECT playcount FROM osu_user_beatmap_playcount WHERE user_id = 2 and beatmap_id = {TEST_BEATMAP_ID}", 1, CancellationToken);

            Processor.PushToQueue(CreateTestScore());
            WaitForDatabaseState($"SELECT playcount FROM osu_user_beatmap_playcount WHERE user_id = 2 and beatmap_id = {TEST_BEATMAP_ID}", 2, CancellationToken);
        }

        [Fact]
        public void TestUserBeatmapPlaycountReprocessDoesntIncrease()
        {
            var score = CreateTestScore();

            WaitForDatabaseState($"SELECT playcount FROM osu_user_beatmap_playcount WHERE user_id = 2 and beatmap_id = {TEST_BEATMAP_ID}", (int?)null, CancellationToken);

            Processor.PushToQueue(score);
            WaitForDatabaseState($"SELECT playcount FROM osu_user_beatmap_playcount WHERE user_id = 2 and beatmap_id = {TEST_BEATMAP_ID}", 1, CancellationToken);

            // the score will be marked as processed (in the database) at this point, so should not increase the playcount if processed a second time.
            score.MarkProcessed();

            Processor.PushToQueue(score);
            WaitForDatabaseState($"SELECT playcount FROM osu_user_beatmap_playcount WHERE user_id = 2 and beatmap_id = {TEST_BEATMAP_ID}", 1, CancellationToken);
        }

        [Fact]
        public void TestMonthlyPlaycountIncrease()
        {
            WaitForDatabaseState("SELECT playcount FROM osu_user_month_playcount WHERE user_id = 2", (int?)null, CancellationToken);

            Processor.PushToQueue(CreateTestScore());
            WaitForDatabaseState("SELECT playcount FROM osu_user_month_playcount WHERE user_id = 2 AND `year_month` = '2002'", 1, CancellationToken);

            Processor.PushToQueue(CreateTestScore());
            WaitForDatabaseState("SELECT playcount FROM osu_user_month_playcount WHERE user_id = 2 AND `year_month` = '2002'", 2, CancellationToken);
            WaitForDatabaseState("SELECT COUNT(*) FROM osu_user_month_playcount WHERE user_id = 2", 1, CancellationToken);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void TestMonthlyPlaycountReprocessOldVersionIncrease(int version)
        {
            var score = CreateTestScore();

            WaitForDatabaseState("SELECT playcount FROM osu_user_month_playcount WHERE user_id = 2", (int?)null, CancellationToken);
            Processor.PushToQueue(score);

            score.MarkProcessed();

            // check reprocessing results in increase.
            Debug.Assert(score.ProcessHistory != null);
            score.ProcessHistory.processed_version = (byte)version;

            Processor.PushToQueue(score);
            WaitForDatabaseState("SELECT playcount FROM osu_user_month_playcount WHERE user_id = 2 AND `year_month` = '2002'", 2, CancellationToken);
        }

        [Fact]
        public void TestMonthlyPlaycountReprocessDoesntIncrease()
        {
            var score = CreateTestScore();

            WaitForDatabaseState("SELECT playcount FROM osu_user_month_playcount WHERE user_id = 2", (int?)null, CancellationToken);

            Processor.PushToQueue(score);
            WaitForDatabaseState("SELECT playcount FROM osu_user_month_playcount WHERE user_id = 2 AND `year_month` = '2002'", 1, CancellationToken);

            // the score will be marked as processed (in the database) at this point, so should not increase the playcount if processed a second time.
            score.MarkProcessed();

            Processor.PushToQueue(score);
            WaitForDatabaseState("SELECT playcount FROM osu_user_month_playcount WHERE user_id = 2 AND `year_month` = '2002'", 1, CancellationToken);
            WaitForDatabaseState("SELECT COUNT(*) FROM osu_user_month_playcount WHERE user_id = 2", 1, CancellationToken);
        }
    }
}
