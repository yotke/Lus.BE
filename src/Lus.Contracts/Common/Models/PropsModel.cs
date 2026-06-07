namespace Lus.Contracts.Common.Models
{
    public class PropsModel
    {
        public PropsModel() { }
        public PropsModel(string k, object v)
        {
            this.Key = k;
            this.Value = v;
            this.typeCode = v != null ? Type.GetTypeCode(v.GetType()) : TypeCode.Empty;
        }
        public PropsModel(string k, object v, TypeCode t)
        {
            this.Key = k;
            this.Value = v;
            this.typeCode = t;
        }
        public string Key { get; set; }
        public object Value { get; set; }
        public TypeCode typeCode { get; set; }
    }
}
