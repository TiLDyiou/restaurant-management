using PayOS;
using PayOS.Models.Webhooks;
using PayOS.Models.V2.PaymentRequests;
using System.Reflection;
using System;

Console.WriteLine("PaymentLinkItem:");
foreach(var p in typeof(PaymentLinkItem).GetProperties()) Console.WriteLine(p.Name);

Console.WriteLine("CreatePaymentLinkRequest:");
foreach(var p in typeof(CreatePaymentLinkRequest).GetProperties()) Console.WriteLine(p.Name);
