using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SelfServiceAPI.Classes
{
    public class PayTabsClasses
    {

    }

    public class PaymentObject
    {
        public Int32 PaymentTransactionId { get; set; }
        public String Amount { get; set; }
        public String TransactionDescription { get; set; }
        public String OrderId { get; set; }
        public String Status { get; set; }
        public String PeopleId { get; set; }
        public String Email { get; set; }
        public String MerchantID { get; set; }
    }
    public class PayTabsRequest
    {
        public int profile_id { get; set; }
        public string tran_type { get; set; }
        public string tran_class { get; set; }
        public string cart_description { get; set; }
        public string cart_id { get; set; }
        public double cart_amount { get; set; }
        public string cart_currency { get; set; }
        public string callback { get; set; }
        public string @return { get; set; }
        public Boolean hide_shipping { get; set; }
    }

    public class PaymentTransaction
    {
        public Int32 PaymentTransactionId { get; set; }
        public Decimal Amount { get; set; }
        public String Description { get; set; }
        public Int32 Area { get; set; }
        public Int32 PersonId { get; set; }
        public Int32 BeneficiaryId { get; set; }
        public Int32 TermPeriodId { get; set; }
        public Int32 SessionPeriodId { get; set; }
        public DateTime TransactionDate { get; set; }
        public Int32 CashReceiptOfficeId { get; set; }
        public Int32 CashReceiptCodeId { get; set; }
        public Boolean IsDownPayment { get; set; }
        public Boolean IsAppFormFees { get; set; }
    }

    public class CustomerDetails
    {
        public string name { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string street1 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string ip { get; set; }
    }

    public class PaymentInfo
    {
        public string card_type { get; set; }
        public string card_scheme { get; set; }
        public string payment_description { get; set; }
    }

    public class PaymentResult
    {
        public string response_status { get; set; }
        public string response_code { get; set; }
        public string response_message { get; set; }
        public string acquirer_message { get; set; }
        public string acquirer_rrn { get; set; }
        public DateTime transaction_time { get; set; }
    }

    public class Root
    {
        public string tran_ref { get; set; }
        public string cart_id { get; set; }
        public string cart_description { get; set; }
        public string cart_currency { get; set; }
        public string cart_amount { get; set; }
        public CustomerDetails customer_details { get; set; }
        public PaymentResult payment_result { get; set; }
        public PaymentInfo payment_info { get; set; }
        public string redirect_url { get; set; }
        public string tran_type { get; set; }

    }
    public class AdditionalResponse1
    {
        public int StatusCode { get; set; }
        public Root Data { get; set; }
        public string Status { get; set; }

    }
}