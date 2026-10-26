namespace PriorityQueue
{
    public class Activity:IComparable<Activity>
    {
        private string _id;
        private int _priority, _timeToElaborate;

        public string ID
        {
            get { return _id; }
        }

        public int Priority
        {
            get { return _priority; }
        }

        public int TimeToElaborate
        {
            get { return _timeToElaborate; }
        }

        public Activity(string id, int prority, int timeToElaborate)
        {
            _id = id;
            _priority = prority;
            _timeToElaborate = timeToElaborate;
        }

        public int CompareTo(Activity? other)
        {
            if (other == null) return 1;
            return this.Priority.CompareTo(other.Priority);
        }
    }
}
