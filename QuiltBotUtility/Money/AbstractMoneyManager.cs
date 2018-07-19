using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;
using QuiltBotUtility.Exceptions;

namespace QuiltBotUtility.Money
{
    public abstract class AbstractMoneyManager
    {
        protected readonly IAccount account;
        protected readonly Algo algo;

        /// <summary>
        /// 一天最大损失
        /// </summary>
        protected readonly double OneDayMaxLoss;

        protected AbstractMoneyManager(IAccount account, Algo algo, double oneDayMaxLoss)
        {
            this.account = account;
            this.algo = algo;
            OneDayMaxLoss = oneDayMaxLoss;
        }

        public abstract double GetOpenLongVolume(double price, double stopLoss);
        public abstract double GetOpenShortVolume(double price, double stopLoss);

        protected bool ValidateVolume(double price, double stopLoss, double volume)
        {
            double todayLoss = 0;
            if (algo.Positions.Count > 0)
            {
                foreach (var position in algo.Positions)
                {
                    //if (position.StopLoss == null)
                    //    throw new PositionNoStopLessException(position);
                    if (position.EntryTime < DateTime.UtcNow.Date)
                        continue;

                    var symbol = algo.MarketData.GetSymbol(position.SymbolCode);
                    if ((position.TradeType == TradeType.Buy && position.EntryPrice > position.StopLoss)
                        || (position.TradeType == TradeType.Sell && position.EntryPrice < position.StopLoss))
                    {
                        var loss = Math.Abs((position.EntryPrice - position.StopLoss.Value) / symbol.PipSize *
                                            symbol.PipValue * position.VolumeInUnits);
                        if (position.EntryTime >= DateTime.UtcNow.Date)
                            todayLoss += loss;
                    }
                }
            }

            var thisLoss = Math.Abs((price - stopLoss) / algo.Symbol.PipSize * algo.Symbol.PipValue * volume);
            todayLoss += thisLoss;
            todayLoss += algo.History.Where(c => c.EntryTime >= DateTime.UtcNow.Date)
                             .Where(c => c.ClosingDealId > 0)
                             .Sum(c => c.NetProfit);

            return !(todayLoss >= OneDayMaxLoss);
        }
    }
}