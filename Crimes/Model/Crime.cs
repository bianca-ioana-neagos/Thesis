namespace Model
{
    public class Crime
    {
        public string Address { get; set; }
        public string District { get; set; }
        public string Descr { get; set; }
        public string Code { get; set; }
        public int Severity { get; set; }


        public override string ToString()
        {
            return new
            {
                address = Address,
                district = District,
                descr = Descr,
                code = Code,
                severity = Severity

            }.ToString();
        }

        public Crime()
        {
        }
    }
}
