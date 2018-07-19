using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API.Internals;

namespace QuiltBotUtility.Money
{
    public static class MoneyManagerFactory
    {
        public static AbstractMoneyManager Create(IAccount account, Algo algo, MoneyManagerType type, double oneDayMaxLoss, double lot = 0, double risk = 0)
        {
            switch (type)
            {
                case MoneyManagerType.FixLot:
                    return new FixedLotMoneyManager(account, algo, oneDayMaxLoss, lot);
                case MoneyManagerType.FixRisk:
                    return new FixedRiskMoneyManager(account, algo, oneDayMaxLoss, risk);
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
        }
    }
}