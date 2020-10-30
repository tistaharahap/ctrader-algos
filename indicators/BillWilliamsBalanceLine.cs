using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

/**
 * Bill Williams Balance Line
 * --------------------------
 * This indicator is based on Bill Williams' Balance Line from his book "New Trading Dimensions".
 * Along with the Balance Line, this indicator also calculates an EMA band of the lower and the upper lines.
 * --------------------------
 * How to use:
 * 1. Use an EMA period that suits your trading style, defaults to 528
 * 2. If price breaks and closes below lower line and its EMA, short it
 * 3. If price breaks and closes above upper line and its EMA, long it
 * --------------------------
 **/

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class BillWilliamsBalanceLine : Indicator
    {
        private AwesomeOscillator awesomeOscillator;
        private Alligator alligator;
        private AcceleratorOscillator acceleratorOscillator;

        private MovingAverage shortMovingAverage;
        private MovingAverage longMovingAverage;

        [Parameter(DefaultValue = 528, MinValue = 1)]
        public int Period { get; set; }

        [Output("ShortBL", LineColor = "#aaaaaa")]
        public IndicatorDataSeries shortBL { get; set; }

        [Output("LongBL", LineColor = "#aaaaaa")]
        public IndicatorDataSeries longBL { get; set; }

        [Output("ShortBLAvg", LineColor = "#aaaaaa")]
        public IndicatorDataSeries shortBLAvg { get; set; }

        [Output("LongBLAvg", LineColor = "#aaaaaa")]
        public IndicatorDataSeries longBLAvg { get; set; }

        protected override void Initialize()
        {
            awesomeOscillator = Indicators.AwesomeOscillator();
            alligator = Indicators.Alligator(13, 8, 8, 5, 5, 3);
            acceleratorOscillator = Indicators.AcceleratorOscillator();

            shortMovingAverage = Indicators.MovingAverage(shortBL, Period, MovingAverageType.Exponential);
            longMovingAverage = Indicators.MovingAverage(longBL, Period, MovingAverageType.Exponential);
        }

        private bool AoIsGreen(int index)
        {
            return awesomeOscillator.Result[index] > awesomeOscillator.Result[index - 1];
        }

        private bool AoIsRed(int index)
        {
            return awesomeOscillator.Result[index] < awesomeOscillator.Result[index - 1];
        }

        private bool AcIsBlue(int index)
        {
            return acceleratorOscillator.Result[index] > acceleratorOscillator.Result[index - 1];
        }

        private bool AcIsRed(int index)
        {
            return acceleratorOscillator.Result[index] < acceleratorOscillator.Result[index - 1];
        }

        private bool IsGreenZone(int index)
        {
            return AoIsGreen(index) && AcIsBlue(index);
        }

        private bool IsRedZone(int index)
        {
            return AoIsRed(index) && AcIsRed(index);
        }

        private bool IsGrayZone(int index)
        {
            return !IsGreenZone(index) && !IsRedZone(index);
        }

        private int CalculateShortBalanceLineSteps(int candleIndex, double jaw, bool greenZone, bool redZone, bool grayZone)
        {
            bool aboveJaw = Bars.LowPrices[candleIndex - 1] > jaw;
            bool belowJaw = Bars.LowPrices[candleIndex - 1] < jaw;

            int index = 0;

            if (aboveJaw)
            {
                if (greenZone)
                {
                    index = 4;
                }
                else if (redZone)
                {
                    index = 2;
                }
                else if (grayZone)
                {
                    index = 2;
                }
            }
            else if (belowJaw)
            {
                if (greenZone)
                {
                    index = 2;
                }
                else if (redZone)
                {
                    index = 1;
                }
                else if (grayZone)
                {
                    index = 1;
                }
            }

            return index;
        }

        private int CalculateLongBalanceLineSteps(int candleIndex, double jaw, bool greenZone, bool redZone, bool grayZone)
        {
            bool aboveJaw = Bars.HighPrices[candleIndex - 1] > jaw;
            bool belowJaw = Bars.HighPrices[candleIndex - 1] < jaw;

            int index = 0;

            if (aboveJaw)
            {
                if (greenZone)
                {
                    index = 1;
                }
                else if (redZone)
                {
                    index = 2;
                }
                else if (grayZone)
                {
                    index = 1;
                }
            }
            else if (belowJaw)
            {
                if (greenZone)
                {
                    index = 2;
                }
                else if (redZone)
                {
                    index = 4;
                }
                else if (grayZone)
                {
                    index = 2;
                }
            }

            return index;
        }

        private double FindLowestLow(int index, int steps)
        {
            int counter = 0;
            double lastLow = 0.0;

            for (int i = -1; i > -100; i--)
            {
                if (counter > steps)
                {
                    break;
                }
                else
                {
                    if (Bars.LowPrices[index + i] < Bars.LowPrices[index + i - 1])
                    {
                        counter += 1;
                        lastLow = Bars.LowPrices[index + i];
                    }
                }
            }

            return lastLow;
        }

        private double FindHighestHigh(int index, int steps)
        {
            int counter = 0;
            double lastHigh = 0.0;

            for (int i = -1; i > -100; i--)
            {
                if (counter > steps)
                {
                    break;
                }
                else
                {
                    if (Bars.HighPrices[index + i] > Bars.HighPrices[index + i - 1])
                    {
                        counter += 1;
                        lastHigh = Bars.HighPrices[index + i];
                    }
                }
            }

            return lastHigh;
        }

        public void CalculateShortBalanceLine(int index, bool greenZone, bool redZone, bool grayZone, double jaw)
        {
            int shortSteps = !(Bars.LowPrices[index - 2] > Bars.LowPrices[index - 1]) ? CalculateShortBalanceLineSteps(index, jaw, greenZone, redZone, grayZone) : 0;

            if (shortSteps > 0)
            {
                shortBL[index] = FindLowestLow(index, shortSteps);
            }
            else
            {
                shortBL[index] = shortBL[index - 1];
            }
        }

        public void CalculateLongBalanceLine(int index, bool greenZone, bool redZone, bool grayZone, double jaw)
        {
            int longSteps = !(Bars.HighPrices[index - 2] < Bars.HighPrices[index - 1]) ? CalculateLongBalanceLineSteps(index, jaw, greenZone, redZone, grayZone) : 0;

            if (longSteps > 0)
            {
                longBL[index] = FindHighestHigh(index, longSteps);
            }
            else
            {
                longBL[index] = longBL[index - 1];
            }
        }

        public override void Calculate(int index)
        {
            if (index < Period)
            {
                return;
            }

            bool greenZone = IsGreenZone(index - 1);
            bool redZone = IsRedZone(index - 1);
            bool grayZone = IsGrayZone(index - 1);
            double jaw = alligator.Jaws[index];

            CalculateShortBalanceLine(index, greenZone, redZone, grayZone, jaw);
            CalculateLongBalanceLine(index, greenZone, redZone, grayZone, jaw);

            shortBLAvg[index] = shortMovingAverage.Result[index];
            longBLAvg[index] = longMovingAverage.Result[index];
        }
    }
}
