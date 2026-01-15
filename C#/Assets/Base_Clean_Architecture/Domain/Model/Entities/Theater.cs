using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Domain.Model.Entities
{
    public class Theater
    {
        private string _id;
        private List<Seat> _seats;
        private int _otherProperties;

        public string ID
        {
            get => default;
            set
            {
            }
        }

        public List<Seat> Seats
        {
            get => default;
            set
            {
            }
        }

        public int OtherProperties
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

        public Screening Screening
        {
            get => default;
            set
            {
            }
        }
    }
}