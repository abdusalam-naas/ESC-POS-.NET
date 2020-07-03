﻿using ESCPOS_NET.Emitters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ESCPOS_NET.DataValidation
{
    public abstract class BaseDataValidator<T> where T : Enum
    {
        protected Dictionary<T, DataConstraint> _constraints;

        public void Validate(T type, string data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            // Validate constraints on barcode.
            _constraints.TryGetValue(type, out var constraints);
            if (constraints is null)
                return;

            // Check lengths
            if (constraints.ValidLengths != null)
            {
                if (!constraints.ValidLengths.Contains(data.Length))
                {
                    throw new ArgumentException($"Code '{data}' is not a valid length from: [{string.Join(", ", constraints.ValidLengths)}].");
                }
            }
            else if (data.Length < constraints.MinLength)
            {
                throw new ArgumentException($"Code '{data}' is shorter than minimum length {constraints.MinLength}.");
            }
            else if (data.Length > constraints.MaxLength)
            {
                throw new ArgumentException($"Code '{data}' is longer than maximum length {constraints.MaxLength}.");
            }

            // Check if barcode contains invalid characters.
            if (constraints.ValidChars == "7BIT-ASCII")
            {
                if (!data.All(x => x <= 127 && x >= 0))
                {
                    throw new ArgumentException($"Code '{data}' contained invalid characters not in: {constraints.ValidChars}.");
                }
            }
            else if (!data.All(x => constraints.ValidChars.Contains(x)))
            {
                throw new ArgumentException($"Code '{data}' contained invalid characters not in: {constraints.ValidChars}.");
            }

            RunSpecificValidations(type, data);
        }

        protected abstract void RunSpecificValidations(T type, string data);
    }

    public static class DataValidator
    {
        private static BarcodeDataValidator singleton = null;
        public static void ValidateBarcode(BarcodeType type, string data)
        {
            if (singleton is null)
                singleton = new BarcodeDataValidator();

            singleton.Validate(type, data);
        }

        private class BarcodeDataValidator : BaseDataValidator<BarcodeType>
        {
            public BarcodeDataValidator()
            {
                _constraints = new Dictionary<BarcodeType, DataConstraint>()
                {
                    { BarcodeType.UPC_A, new DataConstraint() { MinLength = 11, MaxLength = 12, ValidChars = "0123456789" }    },
                    { BarcodeType.UPC_E, new DataConstraint() { ValidLengths = new List<int>() { 6, 7, 8, 11, 12 }, ValidChars = "0123456789" }},
                    { BarcodeType.JAN13_EAN13, new DataConstraint() { MinLength = 12, MaxLength = 13, ValidChars = "0123456789" } },
                    { BarcodeType.JAN8_EAN8, new DataConstraint() { MinLength = 7, MaxLength = 8, ValidChars = "0123456789" } },
                    { BarcodeType.CODE39, new DataConstraint() { MinLength = 1, MaxLength = 255, ValidChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./" } },
                    { BarcodeType.ITF, new DataConstraint() { MinLength = 2, MaxLength = 255, ValidChars = "0123456789" } },
                    { BarcodeType.CODABAR_NW_7, new DataConstraint() { MinLength = 2, MaxLength = 255, ValidChars = "0123456789ABCDabcd$+-./:" } },
                    { BarcodeType.CODE93, new DataConstraint() { MinLength = 1, MaxLength = 255, ValidChars = "7BIT-ASCII" } },
                    { BarcodeType.CODE128, new DataConstraint() { MinLength = 1, MaxLength = 253, ValidChars = "7BIT-ASCII" } },
                    { BarcodeType.GS1_128, new DataConstraint() { MinLength = 1, MaxLength = 253, ValidChars = "7BIT-ASCII" } },
                    { BarcodeType.GS1_DATABAR_OMNIDIRECTIONAL, new DataConstraint() { MinLength = 13, MaxLength = 13, ValidChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./" } },
                    { BarcodeType.GS1_DATABAR_TRUNCATED, new DataConstraint() { MinLength = 13, MaxLength = 13, ValidChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./" } },
                    { BarcodeType.GS1_DATABAR_LIMITED, new DataConstraint() { MinLength = 13, MaxLength = 13, ValidChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./" } },
                    { BarcodeType.GS1_DATABAR_EXPANDED, new DataConstraint() { MinLength = 2, MaxLength = 255, ValidChars = "0123456789ABCDabcd !\"%$'()*+,-./:;<=>?_{" } },
                };
            }

            protected override void RunSpecificValidations(BarcodeType type, string barcode)
            {
                switch (type)
                {
                    case BarcodeType.UPC_E:
                        if (barcode.Length != 6 && !barcode.StartsWith("0", StringComparison.InvariantCulture))
                            throw new ArgumentException($"UPC_E Barcode {barcode} with length of 7, 8, 11, or 12 must start with 0.");

                        break;

                    case BarcodeType.ITF:
                        if (barcode.Length % 2 != 0)
                            throw new ArgumentException($"ITF Barcode {barcode} has length {barcode.Length}, which is not an even number.");

                        break;

                    case BarcodeType.CODABAR_NW_7:
                        if (!"ABCD".Contains(barcode[0]) || !"ABCD".Contains(barcode[barcode.Length - 1]))
                            throw new ArgumentException($"CODABAR_NW_7 Barcode {barcode} must start and end with an ABCD character.");

                        if (barcode.Skip(1).Take(barcode.Length - 2).Any(x => "ABCD".Contains(x)))
                            throw new ArgumentException($"CODABAR_NW_7 Barcode {barcode} must not include ABCD characters in the body of the barcode.");

                        break;

                    case BarcodeType.CODE93:
                        if (!barcode.StartsWith("*", StringComparison.InvariantCulture) || !barcode.EndsWith("*", StringComparison.InvariantCulture))
                            throw new ArgumentException($"CODE39 Barcode {barcode} must start and end with * characters.");

                        break;
                }
            }
        }
    }

}
