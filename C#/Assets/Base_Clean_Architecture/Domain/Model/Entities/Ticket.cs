using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain.Model.ValueObjects;

namespace Domain.Model.Entities
{
    public class Ticket
    {
        private string _ticketID;
        private Screening _screening;
        private Theater _theater;
        private DateTime _screeningDate;
        private TicketType _ticketType;
        private double _price;
        private int _supplementPercentage;
        private DateTime _purchaseTime;
        private int _salePercentage;

        public Screening Screening
        {
            get => default;
            set
            {
            }
        }

        public DateTime ScreeningDate
        {
            get => default;
            set
            {
            }
        }

        public int TicketType
        {
            get => default;
            set
            {
            }
        }

        public double Price
        {
            get => default;
            set
            {
            }
        }

        public int SupplementPercentage
        {
            get => default;
            set
            {
            }
        }

        public DateTime PurchaseTime
        {
            get => default;
            set
            {
            }
        }

        public int SalePercentage
        {
            get => default;
            set
            {
            }
        }

        public string TicketID
        {
            get => default;
            set
            {
            }
        }

        public Theater Theater
        {
            get => default;
            set
            {
            }
        }

        public Seat Seat
        {
            get => default;
            set
            {
            }
        }
    }
}