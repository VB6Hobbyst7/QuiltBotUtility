using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace QuiltBotUtility.RobotUtil
{
    public class FiveMinutesOpenStrategy
    {
        private readonly Algo algo;

        private MacdHistogram macd;
        private ExponentialMovingAverage ema;

        /// <summary>
        /// 开仓时，价格与ema距离
        /// </summary>
        private readonly double openPip;

        private readonly double stopLossPip;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="algo"></param>
        /// <param name="openPip"></param>
        public FiveMinutesOpenStrategy(Algo algo, double openPip, double stopLossPip)
        {
            this.algo = algo;
            this.openPip = openPip;
            this.stopLossPip = stopLossPip;
        }

        public int GetOpenConfidence(out double price, out double stopLoss, out double takeProfit)
        {
            price = 0;
            stopLoss = 0;
            takeProfit = 0;
            macd = algo.Indicators.MacdHistogram(algo.MarketSeries.Close, 26, 12, 9);
            ema = algo.Indicators.ExponentialMovingAverage(algo.MarketSeries.Close, 20);

            var emaDatas = new double[6];
            var macdDatas = new double[6];

            for (int i = 1; i < 7; i++)
            {
                double upper = algo.MarketSeries.High.Last(i);
                double lower = algo.MarketSeries.Low.Last(i);

                if (upper < lower)
                    return 0;

                var emaData = ema.Result.Last(i);
                var macdData = macd.Histogram.Last(i);

                if (lower > emaData)
                    emaDatas[6 - i] = lower - emaData;
                else if (lower <= emaData && emaData <= upper)
                    emaDatas[6 - i] = 0;
                else
                    emaDatas[6 - i] = upper - emaData;

                macdDatas[6 - i] = macdData;
            }

          

            if ((IsCurSeriesBreak(emaDatas) > 0 && Is5SeriesBreak(macdDatas) > 0)
                || (IsCurSeriesBreak(macdDatas) > 0 && Is5SeriesBreak(emaDatas) > 0))
            {
                algo.Print(string.Join(",", emaDatas));
                algo.Print(string.Join(",", macdDatas));
                price = ema.Result.Last(1) + algo.Symbol.PipSize * openPip;
                stopLoss  = ema.Result.Last(1) - algo.Symbol.PipSize * stopLossPip;
                takeProfit = price + (price - stopLoss);
                return 100;
            }

            if ((IsCurSeriesBreak(emaDatas) < 0 && Is5SeriesBreak(macdDatas) < 0)
                || (IsCurSeriesBreak(macdDatas) < 0 && Is5SeriesBreak(emaDatas) < 0))
            {
                algo.Print(string.Join(",", emaDatas));
                algo.Print(string.Join(",", macdDatas));
                price = ema.Result.Last(1) - algo.Symbol.PipSize * openPip;
                stopLoss = ema.Result.Last(1) + algo.Symbol.PipSize * stopLossPip;
                takeProfit = price - (stopLoss - price);
                return -100;
            }

            return 0;
        }

        /// <summary>
        /// 5个时间周期内，是向上突破还是向下突破
        /// 1-上穿
        /// 0-无变化
        /// -1-下穿
        /// </summary>
        /// <param name="series"></param>
        /// <returns></returns>
        private int Is5SeriesBreak(double[] series)
        {
            if (series == null || series.Length < 6)
                return 0;
            if (series[0] == 0)
                return 0;

            if (series[0] > 0)
            {
                if (series[1] <= 0)
                    return 1;
                if (series[2] <= 0)
                    return 1;
                if (series[3] <= 0)
                    return 1;
                if (series[4] <= 0)
                    return 1;
                if (series[5] <= 0)
                    return 1;
                return 0;
            }

            if (series[0] < 0)
            {
                if (series[1] >= 0)
                    return -1;
                if (series[2] >= 0)
                    return -1;
                if (series[3] >= 0)
                    return -1;
                if (series[4] >= 0)
                    return -1;
                if (series[5] >= 0)
                    return -1;
                return 0;
            }

            return 0;
        }

        private int IsCurSeriesBreak(double[] series)
        {
            if (series == null || series.Length < 2)
                return 0;

            if (series[0] > 0 && series[1] <= 0)
                return 1;

            if (series[0] < 0 && series[1] >= 0)
                return -1;
            return 0;
        }
    }
}