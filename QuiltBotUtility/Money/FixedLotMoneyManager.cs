using System;
using cAlgo.API;
using cAlgo.API.Internals;

namespace QuiltBotUtility.Money
{
    public class FixedLotMoneyManager : AbstractMoneyManager
    {
        private readonly double lots;

        public FixedLotMoneyManager(IAccount account, Algo algo, double oneDayMaxLoss, double lot) : base(account, algo, oneDayMaxLoss)
        {
            if (lot < algo.Symbol.VolumeInUnitsToQuantity(algo.Symbol.VolumeInUnitsMin))
            {
                throw new Exception(
                    string.Format("lots value is error! It's {0}, but it should not be smaller than {1}", lot,
                        algo.Symbol.VolumeInUnitsToQuantity(algo.Symbol.VolumeInUnitsMin)));
            }

            if (lot > algo.Symbol.VolumeInUnitsToQuantity(algo.Symbol.VolumeInUnitsMax))
            {

                throw new Exception(
                    string.Format("lots value is error! It's {0}, but it should not be bigger than {1}", lot,
                        algo.Symbol.VolumeInUnitsToQuantity(algo.Symbol.VolumeInUnitsMax)));
            }

            this.lots = lot;
        }

        public override double GetOpenLongVolume(double price, double stopLoss)
        {
            var result = algo.Symbol.QuantityToVolumeInUnits(lots);

            result = algo.Symbol.NormalizeVolumeInUnits(result, RoundingMode.Down);
            return ValidateVolume(price, stopLoss, result) ? result : 0;
        }

        public override double GetOpenShortVolume(double price, double stopLoss)
        {
            var result = algo.Symbol.QuantityToVolumeInUnits(lots);
            result = algo.Symbol.NormalizeVolumeInUnits(result, RoundingMode.Down);
            return ValidateVolume(price, stopLoss, result) ? result : 0;
        }
    }
}