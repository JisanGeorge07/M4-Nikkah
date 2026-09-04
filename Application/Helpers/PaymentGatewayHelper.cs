using Application.Models.Transactions;
using Domain.Framework;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
namespace Application.Helpers
{
    public static class PaymentGatewayHelper
    {
        private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        public static string GenerateHash(PaymentGatewayRequest request, string _salt)
        {
            string text = $"{request.Key}|{request.TxnId}|{request.Amount}|{request.ProductInfo}|{request.FirstName}|{request.Email}|{request.Udf1}|{request.Udf2}|{request.Udf3}|{request.Udf4}|{request.Udf5}||||||{_salt}";
            string hashV1 = GetStringFromHash(text);
            return hashV1;
        }

        private static string GetStringFromHash(string text)
        {
            byte[] message = Encoding.UTF8.GetBytes(text);

            using (SHA512Managed hashString = new SHA512Managed())
            {
                byte[] hashValue = hashString.ComputeHash(message);
                StringBuilder hex = new StringBuilder(hashValue.Length * 2);
                foreach (byte x in hashValue)
                {
                    hex.AppendFormat("{0:x2}", x);
                }
                return hex.ToString();
            }
        }

        public static string GenerateTransactionId(int length)
        {
            if (length <= 0)
                throw new ArgumentException("Length must be greater than zero", nameof(length));

            StringBuilder result = new StringBuilder(length);
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] uintBuffer = new byte[sizeof(uint)];

                while (length-- > 0)
                {
                    rng.GetBytes(uintBuffer);
                    uint num = BitConverter.ToUInt32(uintBuffer, 0);
                    result.Append(chars[(int)(num % (uint)chars.Length)]);
                }
            }
            return result.ToString();
        }
    }
}
