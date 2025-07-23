using System.Security.Cryptography;

var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
Console.WriteLine(key);