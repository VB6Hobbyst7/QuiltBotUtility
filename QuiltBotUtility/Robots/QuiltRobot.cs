using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using QuiltBotUtility.Money;

namespace cRobots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class FiveMinutesRobot : Robot
    {
        [Parameter(DefaultValue = 0)]
        public int MoneyManagerType { get; set; }

        [Parameter(DefaultValue = 100)]
        public double OneDayMaxLoss { get; set; }

        [Parameter(DefaultValue = 0.02)]
        public double Lots { get; set; }

        [Parameter(DefaultValue = 0)]
        public double Risk { get; set; }

        [Parameter(DefaultValue = 20)]
        public double OpenStopLossPip { get; set; }

        [Parameter(DefaultValue = 0)]
        public double EmaStopLossPip { get; set; }

        private string label;

        private MacdHistogram macd;
        private ExponentialMovingAverage ema;

        private AbstractMoneyManager moneyManager;

        private const string InComment = "开仓";
        private const string HalfComment = "减仓";
        private const string OutComment = "平仓";

        protected override void OnStart()
        {
            macd = Indicators.MacdHistogram(MarketSeries.Close, 26, 12, 9);
            ema = Indicators.ExponentialMovingAverage(MarketSeries.Close, 20);
            moneyManager = MoneyManagerFactory.Create(Account, this, (MoneyManagerType) MoneyManagerType, OneDayMaxLoss, Lots, Risk);
            label = Symbol.Code + "FiveMinutesRobot";
        }

        protected override void OnBar()
        {
            var position = Positions.Find(label, Symbol);
            if (position == null)
            {
                var emaDatas = new double[6];
                var macdDatas = new double[6];

                for (int i = 1; i < 7; i++)
                {
                    double upper = MarketSeries.High.Last(i);
                    double lower = MarketSeries.Low.Last(i);

                    var emaData = ema.Result.Last(i);
                    var macdData = macd.Histogram.Last(i);

                    if (lower > emaData)
                        emaDatas[i - 1] = lower - emaData;
                    else if (lower <= emaData && emaData <= upper)
                        emaDatas[i - 1] = 0;
                    else
                        emaDatas[i - 1] = upper - emaData;

                    macdDatas[i - 1] = macdData;
                }

                if (IsCurSeriesBreak(emaDatas) > 0 && Is5SeriesBreak(macdDatas) > 0 || IsCurSeriesBreak(macdDatas) > 0 && Is5SeriesBreak(emaDatas) > 0)
                {
                    var volume = moneyManager.GetOpenLongVolume(Symbol.Ask, Symbol.Ask - OpenStopLossPip * Symbol.PipSize);
                    ExecuteMarketOrderAsync(TradeType.Buy, Symbol, volume / 2, label, OpenStopLossPip, null, null, InComment);
                    ExecuteMarketOrderAsync(TradeType.Buy, Symbol, volume / 2, label, OpenStopLossPip, null, null, HalfComment);
                }

                if (IsCurSeriesBreak(emaDatas) < 0 && Is5SeriesBreak(macdDatas) < 0 || IsCurSeriesBreak(macdDatas) < 0 && Is5SeriesBreak(emaDatas) < 0)
                {
                    var volume = moneyManager.GetOpenShortVolume(Symbol.Bid, Symbol.Bid + OpenStopLossPip * Symbol.PipSize);
                    ExecuteMarketOrderAsync(TradeType.Sell, Symbol, volume / 2, label, OpenStopLossPip, null, null, InComment);
                    ExecuteMarketOrderAsync(TradeType.Sell, Symbol, volume / 2, label, OpenStopLossPip, null, null, HalfComment);
                }
            }
        }

        protected override void OnTick()
        {
            var positions = Positions.FindAll(label, Symbol);
            if (positions == null)
            {
                return;
            }

            var emaData = ema.Result.Last(1);

            var position = positions.FirstOrDefault(c => c.Comment == InComment);
            if (position != null)
            {
                if (position.StopLoss == null)
                {
                    ClosePosition(position);
                    return;
                }

                switch (position.TradeType)
                {
                    case TradeType.Buy:
                    {
                        double price = position.EntryPrice + position.EntryPrice - position.StopLoss.Value - position.Commissions * 2 / position.VolumeInUnits;

                        if (Symbol.Bid >= price)
                        {
                            ClosePositionAsync(position);
                            var pos = Positions.FindAll(position.Label, Symbol).FirstOrDefault(c => c.Comment == HalfComment);
                            if (pos != null)
                            {
                                var stopLoss = Math.Max(emaData - EmaStopLossPip * Symbol.PipSize, pos.EntryPrice - pos.Commissions * 2 / pos.VolumeInUnits);
                                ModifyPositionAsync(pos, stopLoss, null);
                            }
                        }

                        break;
                    }
                    case TradeType.Sell:
                    {
                        double price = position.EntryPrice + position.EntryPrice - position.StopLoss.Value + position.Commissions * 2 / position.VolumeInUnits;
                        if (Symbol.Ask <= price)
                        {
                            ClosePositionAsync(position);
                            var pos = Positions.FindAll(position.Label, Symbol).FirstOrDefault(c => c.Comment == HalfComment);
                            if (pos != null)
                            {
                                var stopLoss = Math.Min(emaData + EmaStopLossPip * Symbol.PipSize, pos.EntryPrice + pos.Commissions * 2 / pos.VolumeInUnits);
                                ModifyPositionAsync(pos, stopLoss, null);
                            }
                        }

                        break;
                    }
                    default:
                        break;
                }
            }
            else
            {
                position = positions.FirstOrDefault(c => c.Comment == HalfComment);
                if (position != null)
                {
                    if (position.StopLoss == null)
                    {
                        ClosePositionAsync(position);
                        return;
                    }

                    double stopLoss;
                    switch (position.TradeType)
                    {
                        case TradeType.Buy:
                            stopLoss = Math.Max(emaData - EmaStopLossPip * Symbol.PipSize,
                                position.EntryPrice - position.Commissions * 2 / position.VolumeInUnits);
                            ModifyPositionAsync(position, stopLoss, null);
                            break;
                        case TradeType.Sell:
                            stopLoss = Math.Min(emaData + EmaStopLossPip * Symbol.PipSize,
                                position.EntryPrice + position.Commissions * 2 / position.VolumeInUnits);
                            ModifyPositionAsync(position, stopLoss, null);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
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