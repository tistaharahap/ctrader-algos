using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class BillWilliamsBalanceLineBot : Robot
    {
        private BillWilliamsBalanceLine bwbl;
        private static readonly string name = "BBWL";

        [Parameter("Quantity (Lots)", Group = "Volume", DefaultValue = 1, MinValue = 0.01, Step = 0.01)]
        public double Quantity { get; set; }

        [Parameter("SL Pips", Group = "TPSL", DefaultValue = 2, MinValue = 0.1, Step = 0.1)]
        public double SlPips { get; set; }

        [Parameter("TP Pips", Group = "TPSL", DefaultValue = 20, MinValue = 0.1, Step = 0.1)]
        public double TpPips { get; set; }

        [Parameter("Period", Group = "Settings", DefaultValue = 528, MinValue = 1, Step = 1)]
        public int Period { get; set; }

        protected override void OnStart()
        {
            bwbl = Indicators.GetIndicator<BillWilliamsBalanceLine>(Period);
        }

        private void OnShortSignal()
        {
            Open(TradeType.Sell);
        }

        private void OnLongSignal()
        {
            Open(TradeType.Buy);
        }

        private void Open(TradeType tradeType)
        {
            var position = Positions.Find(name, SymbolName, tradeType);
            var volumeInUnits = Symbol.QuantityToVolumeInUnits(Quantity);

            //double tp = tradeType == TradeType.Sell ? -TpPips : TpPips;
            //double sl = tradeType == TradeType.Sell ? SlPips : -SlPips;

            if (position == null)
            {
                Print("Going to execute order to {0}", tradeType);
                ExecuteMarketOrder(tradeType, SymbolName, volumeInUnits, name, TpPips, SlPips);
            }
            else
            {
                Print("Has an ongoing position, not executing order");
            }
        }

        protected override void OnBar()
        {
            double longBL = bwbl.longBL.LastValue;
            double shortBL = bwbl.shortBL.LastValue;
            double longBLAvg = bwbl.longBLAvg.LastValue;
            double shortBLAvg = bwbl.shortBLAvg.LastValue;

            bool shortSignal = Bars.ClosePrices.IsFalling() && Bars.ClosePrices.HasCrossedBelow(shortBL, 5) && Bars.ClosePrices.HasCrossedBelow(shortBLAvg, 5);
            bool longSignal = Bars.ClosePrices.IsRising() && Bars.ClosePrices.HasCrossedAbove(longBL, 5) && Bars.ClosePrices.HasCrossedBelow(longBLAvg, 5);

            if (shortSignal && longSignal)
            {
                return;
            }
            else if (shortSignal)
            {
                OnShortSignal();
            }
            else if (longSignal)
            {
                OnLongSignal();
            }
        }
    }
}
