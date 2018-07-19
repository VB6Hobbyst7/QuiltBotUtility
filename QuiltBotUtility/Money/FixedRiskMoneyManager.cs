using System;
using System.IO;
using cAlgo.API.Internals;

namespace QuiltBotUtility.Money
{
    public class FixedRiskMoneyManager : AbstractMoneyManager
    {
        /// <summary>
        /// 最多可以损失多少钱
        /// </summary>
        private readonly double risk;

        public FixedRiskMoneyManager(IAccount account, Algo algo, double oneDayMaxLoss, double risk) : base(account, algo, oneDayMaxLoss)
        {
            if (risk < 0)
                throw new Exception(string.Format("risk is negtive, risk={0}", risk));

            this.risk = risk;
        }

        public override double GetOpenLongVolume(double price, double stopLoss)
        {
            throw new NotImplementedException();
        }

        public override double GetOpenShortVolume(double price, double stopLoss)
        {
            throw new NotImplementedException();
        }
    }
}