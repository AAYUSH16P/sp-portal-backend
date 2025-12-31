using Dapper;
using System.Data;
using System.Text.Json;

namespace Infrastructure.DataAccess.Dapper
{
    public class JsonListTypeHandler<T> : SqlMapper.TypeHandler<List<T>>
    {
        public override void SetValue(IDbDataParameter parameter, List<T> value)
        {
            parameter.Value = JsonSerializer.Serialize(value);
            parameter.DbType = DbType.String;
        }

        public override List<T> Parse(object value)
        {
            if (value == null || value == DBNull.Value)
                return new List<T>();

            return JsonSerializer.Deserialize<List<T>>(value.ToString())
                   ?? new List<T>();
        }
    }
}