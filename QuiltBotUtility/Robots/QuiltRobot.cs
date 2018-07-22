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
        public double HalfStopLossPip { get; set; }

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
            moneyManager = MoneyManagerFactory.Create(Account, this, (MoneyManagerType)MoneyManagerType, OneDayMaxLoss, Lots, Risk);
            label = Symbol.Code + "FiveMinutesRobot";
        }

        protected override void OnBar()
        {
            TradeResult result = null;
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


                var lastEma = ema.Result.Last(1);

                if (IsCurSeriesBreak(emaDatas) > 0 && Is5SeriesBreak(macdDatas) > 0 || IsCurSeriesBreak(macdDatas) > 0 && Is5SeriesBreak(emaDatas) > 0)
                {
                    var stopLoss = lastEma - Symbol.PipSize * OpenStopLossPip;

                    var stopLossPip = OpenStopLossPip;


                    var volume = moneyManager.GetOpenLongVolume(Symbol.Ask, stopLoss);
                    result = ExecuteMarketOrder(TradeType.Buy, Symbol, volume / 2, label, stopLossPip, null, null, InComment);
                    result = ExecuteMarketOrder(TradeType.Buy, Symbol, volume / 2, label, stopLossPip, null, null, HalfComment);
                }

                if (IsCurSeriesBreak(emaDatas) < 0 && Is5SeriesBreak(macdDatas) < 0 || IsCurSeriesBreak(macdDatas) < 0 && Is5SeriesBreak(emaDatas) < 0)
                {
                    var stopLoss = lastEma + Symbol.PipSize * OpenStopLossPip;

                    var stopLossPip = OpenStopLossPip;

                    var volume = moneyManager.GetOpenShortVolume(Symbol.Bid, stopLoss);
                    result = ExecuteMarketOrder(TradeType.Sell, Symbol, volume / 2, label, stopLossPip, null, null, InComment);
                    result = ExecuteMarketOrder(TradeType.Sell, Symbol, volume / 2, label, stopLossPip, null, null, HalfComment);

                    //   ExecuteMarketOrder(TradeType.Sell, Symbol, volume, label, stopLossPip, null, null, InComment);
                }

                if (result == null || result.IsSuccessful)
                    return;
                switch (result.Error)
                {
                    case ErrorCode.TechnicalError:
                        break;
                    case ErrorCode.BadVolume:
                        break;
                    case ErrorCode.NoMoney:
                        break;
                    case ErrorCode.MarketClosed:
                        break;
                    case ErrorCode.Disconnected:
                        break;
                    case ErrorCode.EntityNotFound:
                        break;
                    case ErrorCode.Timeout:
                        break;
                    case null:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        protected override void OnTick()
        {
            TradeResult result = null;
            var positions = Positions.FindAll(label, Symbol);
            if (positions == null)
            {
                return;
            }

            var position = positions.FirstOrDefault(c => c.Comment == InComment);
            if (position != null)
            {
                if (position.StopLoss == null)
                {
                    result = ClosePosition(position);
                    return;
                }


                if (position.TradeType == TradeType.Buy)
                {
                    double price = position.EntryPrice + position.EntryPrice - position.StopLoss.Value + position.Commissions;

                    if (Symbol.Bid >=price)
                    {
                    ClosePosition(position);

                    }
                }
                else if (position.TradeType == TradeType.Sell )
                {
                    double price = position.EntryPrice + position.EntryPrice - position.StopLoss.Value - position.Commissions;
                    if (Symbol.Ask<=price)
                    {
                    ClosePosition(position);

                    }
                }
            }
            else
            {
                position = positions.FirstOrDefault(c => c.Comment == HalfComment);
                if (position != null)
                {
                    if (position.StopLoss == null)
                    {
                        result = ClosePosition(position);
                        return;
                    }
                    var emaData = ema.Result.Last(1);
                    double stopLoss;
                    if (position.TradeType == TradeType.Buy)
                    {
                        var stopLoss1 = emaData - EmaStopLossPip * Symbol.PipSize;
                        var stopLoss2 = position.EntryPrice + HalfStopLossPip * Symbol.PipSize;

                        stopLoss = Math.Max(stopLoss1, stopLoss2);

                    }
                    else
                    {
                        var stopLoss1 = emaData + EmaStopLossPip * Symbol.PipSize;
                        var stopLoss2 = position.EntryPrice - HalfStopLossPip * Symbol.PipSize;

                        stopLoss = Math.Min(stopLoss1, stopLoss2);
                    }

                    result = ModifyPosition(position, stopLoss, null);
                }
            }


            if (result == null || result.IsSuccessful)
                return;


            switch (result.Error)
            {
                case ErrorCode.TechnicalError:
                    break;
                case ErrorCode.BadVolume:
                    break;
                case ErrorCode.NoMoney:
                    break;
                case ErrorCode.MarketClosed:
                    break;
                case ErrorCode.Disconnected:
                    break;
                case ErrorCode.EntityNotFound:
                    break;
                case ErrorCode.Timeout:
                    break;
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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
