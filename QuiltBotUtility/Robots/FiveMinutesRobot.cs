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

        [Parameter(DefaultValue = 10)]
        public double OpenPip { get; set; }

        [Parameter(DefaultValue = 20)]
        public double StopLossPip { get; set; }

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
                        emaDatas[6 - i] = lower - emaData;
                    else if (lower <= emaData && emaData <= upper)
                        emaDatas[6 - i] = 0;
                    else
                        emaDatas[6 - i] = upper - emaData;

                    macdDatas[6 - i] = macdData;
                }

                var lastEma = ema.Result.Last(1);

                if (IsCurSeriesBreak(emaDatas) > 0 && Is5SeriesBreak(macdDatas) > 0
                    || IsCurSeriesBreak(macdDatas) > 0 && Is5SeriesBreak(emaDatas) > 0)
                {
                    var price = lastEma + Symbol.PipSize * OpenPip;
                    var stopLoss = lastEma - Symbol.PipSize * StopLossPip;

                    var stopLossPip = OpenPip + StopLossPip;

                    var volume = moneyManager.GetOpenLongVolume(price, stopLoss);
                    PlaceLimitOrder(TradeType.Buy, Symbol, volume, price, label, stopLossPip, null, null, InComment);
                }

                if (IsCurSeriesBreak(emaDatas) < 0 && Is5SeriesBreak(macdDatas) < 0
                    || IsCurSeriesBreak(macdDatas) < 0 && Is5SeriesBreak(emaDatas) < 0)
                {
                    var price = lastEma - Symbol.PipSize * OpenPip;
                    var stopLoss = lastEma + Symbol.PipSize * StopLossPip;

                    var stopLossPip = OpenPip + StopLossPip;

                    var volume = moneyManager.GetOpenShortVolume(price, stopLoss);
                    var result = PlaceLimitOrder(TradeType.Sell, Symbol, volume, price, label, stopLossPip, null, null, InComment);

                    //   ExecuteMarketOrder(TradeType.Sell, Symbol, volume, label, stopLossPip, null, null, InComment);
                }
            }
            else
            {
                if (position.Comment == InComment)
                {
                    if (position.StopLoss == null)
                    {
                        ClosePosition(position);
                        return;
                    }

                    var tradeType = position.TradeType == TradeType.Buy ? TradeType.Sell : TradeType.Buy;
                    var volume = position.VolumeInUnits / 2;
                    double price = position.EntryPrice + position.EntryPrice - position.StopLoss.Value;

                    PlaceStopOrder(tradeType, Symbol, volume, price, label, null, null, null, HalfComment);
                }
                else if (position.Comment == HalfComment)
                {
                    var emaData = ema.Result.Last(1);
                    double stopLoss;
                    if (position.TradeType == TradeType.Buy)
                    {
                        stopLoss = emaData - StopLossPip * Symbol.PipSize;
                    }
                    else
                    {
                        stopLoss = emaData + StopLossPip * Symbol.PipSize;
                    }

                    ModifyPosition(position, stopLoss, null);
                }
                else
                {
                    ClosePosition(position);
                }
            }
        }

        protected override void OnTick()
        {
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