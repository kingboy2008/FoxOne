using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using FoxOne.Data.Mapping;
using FoxOne.Data.Sql;
using FoxOne.Data.Util;
using FoxOne.Core;
using System.Configuration;
using System.Text;

namespace FoxOne.Data
{
    public class Dao
    {
        private const string DefaultConnectionName = "DefaultDB";
        private string _connectionString;
        private IDaoProvider _provider;


        public static Dao Get()
        {
            return Get(DefaultConnectionName);
        }

        public static Dao Get(string name)
        {
            return DaoFactory.GetDao(name);
        }
        public Dao(string name)
        {
            var setting = ConfigurationManager.ConnectionStrings[name];
            this._connectionString = setting.ConnectionString;
            this._provider = DaoFactory.GetDaoProvider(setting.ProviderName);
            if (null == _provider)
            {
                throw new FoxOneException(string.Format("Dao Provider '{0}' Not Found", ConfigurationManager.ConnectionStrings[name].ProviderName));
            }
        }

        public IDaoProvider Provider
        {
            get { return _provider; }
        }

        public string ConnectionString
        {
            get { return _connectionString; }
        }

        public int ExecuteNonQuery(string sql, object parameters = null)
        {
            return Execute<int>(sql, parameters, ExecuteNonQuery);
        }

        public T QueryScalar<T>(string sql, object parameters = null)
        {
            return Execute<object>(sql, parameters, ExecuteScalar).ConvertTo<T>();
        }

        public IList<IDictionary<string, object>> QueryDictionaries(string sql, object parameters = null)
        {
            return ExecuteReader<IList<IDictionary<string, object>>>(sql, parameters,
                     (reader) => reader.ReadDictionaries());
        }

        public IList<T> QueryEntities<T>(string sql, object parameters = null) where T : class, new()
        {
            return ExecuteReader<IList<T>>(sql, parameters, (reader) =>
                    {
                        return TypeMapper.ReadList<T>(reader);
                    });
        }

        public IDictionary<string, object> QueryDictionary(string sql, object parameters = null)
        {
            return ExecuteReader<IDictionary<string, object>>(sql, parameters,
                    (reader) =>
                    {
                        var dict = reader.ReadDictionary();
                        if (reader.Read())
                        {
                            throw new FoxOneException("reader has multiple rows data");
                        }
                        return dict;
                    });
        }

        public DaoQueryable<T> Query<T>() where T : class, new()
        {
            return new DaoQueryable<T>(this);
        }

        public T QueryEntity<T>(string sql, object parameters = null)
        {
            return ExecuteReader<T>(sql, parameters, (reader) =>
            {
                T entity = TypeMapper.Read<T>(reader, typeof(T));

                if (reader.Read())
                {
                    throw new FoxOneException("reader has multiple rows data");
                }
                return entity;
            });
        }

        public IList<T> QueryScalarList<T>(string sql, object parameters = null)
        {
            return ExecuteReader<IList<T>>(sql, parameters, (reader) =>
            {
                IList<T> list = new List<T>();
                while (reader.Read())
                {
                    list.Add(reader[0].ConvertTo<T>());
                }
                return list;
            });
        }

        public void CreateTable<T>(bool existThenDrop = false)
        {
            CreateTable(typeof(T), existThenDrop);
        }

        public void CreateTable(Type type, bool existThenDrop = false)
        {
            var table = TableMapper.ReadTable(type);
            bool isTableExist = TableMapper.ExistTable(type, this);
            if (isTableExist && existThenDrop)
            {
                ExecuteNonQuery(Provider.GetDropTableCommand(table));
            }
            ExecuteNonQuery(Provider.CreateTableCommand(table));
        }
        public T Get<T>(object id)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, typeof(T));
            using (IDataReader reader = QueryReader(Provider.CreateSelectCommand(mapping, id)))
            {
                return TableMapper.Read<T>(reader, mapping);
            }
        }

        public object Get(Type type, object id)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, type);
            using (IDataReader reader = QueryReader(Provider.CreateSelectCommand(mapping, id)))
            {
                return TableMapper.Read(type, reader, mapping);
            }
        }

        public IDictionary<string, object> Get(string tableName, object id)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, tableName);
            using (IDataReader reader = QueryReader(Provider.CreateSelectCommand(mapping, id)))
            {
                return TableMapper.Read<IDictionary<string, object>>(reader, mapping);
            }
        }

        public IList<T> Select<T>(object parameter = null)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, typeof(T));
            using (IDataReader reader = QueryReader(Provider.CreateSelectAllCommand(mapping, parameter)))
            {
                return TableMapper.ReadAll<T>(reader, mapping);
            }
        }

        public IList<object> Select(Type type, object parameter = null)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, type);
            using (IDataReader reader = QueryReader(Provider.CreateSelectAllCommand(mapping, parameter)))
            {
                return TableMapper.ReadAll(type, reader, mapping);
            }
        }

        public IList<IDictionary<string, object>> Select(string tableName, object parameter = null)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, tableName);
            using (IDataReader reader = QueryReader(Provider.CreateSelectAllCommand(mapping, parameter)))
            {
                return TableMapper.ReadAll<IDictionary<string, object>>(reader, mapping);
            }
        }

        public int BatchUpdate(object entity, object parameter)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, entity.GetType());
            return ExecuteNonQuery(Provider.CreateBatchUpdateCommand(mapping, entity, parameter));
        }

        public int BatchUpdate<T>(object entity, object parameter)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, typeof(T));
            return ExecuteNonQuery(Provider.CreateBatchUpdateCommand(mapping, entity, parameter));
        }

        public int Delete(Type type, object parameter)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, type);
            return ExecuteNonQuery(Provider.CreateBatchDeleteCommand(mapping, parameter));
        }

        public int Insert(object entity)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, entity.GetType());
            return ExecuteNonQuery(Provider.CreateInsertCommand(mapping, entity));
        }

        public int Insert<T>(object entity)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, typeof(T));
            return ExecuteNonQuery(Provider.CreateInsertCommand(mapping, entity));
        }

        public int Insert(string tableName, object entity)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, tableName);
            return ExecuteNonQuery(Provider.CreateInsertCommand(mapping, entity));
        }

        public int InsertFields<T>(IDictionary<string, object> fields)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, typeof(T));
            return ExecuteNonQuery(Provider.CreateInsertCommand(mapping, fields));
        }

        public int Update(object entity)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, entity.GetType());
            return ExecuteNonQuery(Provider.CreateUpdateCommand(mapping, entity));
        }

        public int Update<T>(object entity)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, typeof(T));
            return ExecuteNonQuery(Provider.CreateUpdateCommand(mapping, entity));
        }

        public int Update(string tableName, object entity)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, tableName);
            return ExecuteNonQuery(Provider.CreateUpdateCommand(mapping, entity));
        }

        public int UpdateFields<T>(IDictionary<string, object> fields)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, typeof(T));
            return ExecuteNonQuery(Provider.CreateUpdateCommand(mapping, fields));
        }

        public int UpdateFields<T>(Object entity, params String[] inclusiveFields)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, typeof(T));
            return ExecuteNonQuery(Provider.CreateUpdateCommand(mapping, entity, inclusiveFields, true));
        }

        public int UpdateFields(object entity, params string[] inclusiveFields)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, entity.GetType());
            return ExecuteNonQuery(Provider.CreateUpdateCommand(mapping, entity, inclusiveFields, true));
        }

        public int UpdateFieldsExcluded(object entity, params string[] exclusiveFields)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, entity.GetType());
            return ExecuteNonQuery(Provider.CreateUpdateCommand(mapping, entity, exclusiveFields, false));
        }

        public int UpdateFieldsExcluded<T>(object entity, params string[] exclusiveFields)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, typeof(T));
            return ExecuteNonQuery(Provider.CreateUpdateCommand(mapping, entity, exclusiveFields, false));
        }

        public int UpdateFieldsNotNull(object entity)
        {
            IList<string> includedProperties = new List<string>();
            FastProperty[] properties = FastType.Get(entity.GetType()).Setters;
            foreach (FastProperty property in properties)
            {
                if (null != property.GetValue(entity))
                {
                    includedProperties.Add(property.Name);
                }
            }
            return this.UpdateFields(entity, includedProperties.ToArray());
        }

        public DaoDeleteable<T> Delete<T>() where T : class, new()
        {
            return new DaoDeleteable<T>(this);
        }

        public int Delete(object entity)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, entity.GetType());
            return ExecuteNonQuery(Provider.CreateDeleteCommand(mapping, entity));
        }

        public int Delete(string tableName, object id)
        {
            TableMapping mapping = TableMapper.GetTableMapping(this, tableName);
            return ExecuteNonQuery(Provider.CreateDeleteCommand(mapping, id));
        }

        public IList<IDictionary<string, object>> PageQueryDictionaries(string sql, int startRowIndex, int maximumRows, String sortExpression, out int totalRowCount, object parameters = null)
        {
            ISqlStatement statement;
            string text = FindText(sql, out statement);
            string newText = _provider.WrapCountSql(text);
            totalRowCount = QueryScalar<int>(newText, parameters);
            object outParams;
            string sqlText = GetPageQuery(text, startRowIndex, maximumRows, sortExpression, parameters, out outParams);
            return QueryDictionaries(sqlText, outParams);

        }

        public IList<T> PageQueryEntities<T>(string sql, int startRowIndex, int maximumRows, String sortExpression, out int totalRowCount, object parameters = null) where T : class, new()
        {
            ISqlStatement statement;
            string text = FindText(sql, out statement);
            string newText = _provider.WrapCountSql(text);
            totalRowCount = QueryScalar<int>(newText, parameters);
            object outParams;
            string sqlText = GetPageQuery(text, startRowIndex, maximumRows, sortExpression, parameters, out outParams);
            return ExecuteReader<IList<T>>(sqlText, outParams, TypeMapper.ReadList<T>);
        }

        protected virtual string GetPageQuery(string sql, int startRowIndex, int maximumRows, String sortExpression, object inParmas, out object outParams)
        {
            ParametersWrapper param = new ParametersWrapper(SqlParameters.GetParameters(inParmas));

            IDictionary<string, object> pageParam = null;
            string newText = _provider.WrapPageSql(sql, sortExpression, startRowIndex, maximumRows, out pageParam);

            foreach (var key in pageParam.Keys)
            {
                param.Add(key, pageParam[key]);
            }

            outParams = param;
            return newText;
        }

        public IList<IDictionary<string, object>> QueryDictionariesByPage(string sql, int page, int size, string sortExpression, out int totalRowCount, object parameters = null)
        {
            return PageQueryDictionaries(sql, (page - 1) * size + 1, size, sortExpression, out totalRowCount, parameters);
        }

        public IList<T> QueryEntitiesByPage<T>(string sql, int page, int size, string sortExpression, out int totalRowCount, object parameters = null) where T : class, new()
        {
            return PageQueryEntities<T>(sql, (page - 1) * size + 1, size, sortExpression, out totalRowCount, parameters);
        }

        #region 内部接口实现
        private int ExecuteNonQuery(ISqlCommand command)
        {
            return ExecuteNonQuery(CreateDbCommand(command));
        }

        private int ExecuteNonQuery(DbCommand command)
        {
            return Exec<int>(command, () =>
            {
                return command.ExecuteNonQuery();
            }, true);
        }

        private object ExecuteScalar(DbCommand command)
        {
            return Exec<object>(command, () =>
            {
                return command.ExecuteScalar();
            }, true);
        }

        private IDataReader QueryReader(ISqlCommand command)
        {
            return ExecuteReader(CreateDbCommand(command));
        }

        private IDataReader ExecuteReader(DbCommand command)
        {
            return Exec<IDataReader>(command, () =>
            {
                return command.ExecuteReader(false ? CommandBehavior.Default : CommandBehavior.CloseConnection);
            }, false);
        }

        private T ExecuteReader<T>(string sql, object parameters, Func<IDataReader, T> func)
        {
            using (IDataReader reader = Execute<IDataReader>(sql, parameters, ExecuteReader))
            {
                return func(reader);
            }
        }

        private T Exec<T>(DbCommand command, Func<T> action, bool closeConnection)
        {
            if (command.Connection.State != ConnectionState.Open)
            {
                command.Connection.Open();
            }
            try
            {
                var result = action();
                return result;
            }
            catch (Exception ex)
            {
                if (!closeConnection)
                {
                    //未使用事务且要求不关闭连接，说明是在ExecuteReader中使用，如果语句报错，Reader不执行Dispose()，连接
                    //也不会关闭，所以要在catch中显示关闭连接。
                    command.Connection.Close();
                }
                Logger.Error(ex.Message, ex);
                throw ex;
            }
            finally
            {
                if (closeConnection)
                {
                    command.Connection.Close();
                }
            }
        }

        private T Execute<T>(string sql, object parameters, Func<DbCommand, T> func)
        {
            ISqlStatement statement;

            //sql变量是否是sqlid，若是直接提取sqlstatement,若否则需要sqlparser提取转换
            bool isKey = FindStatement(sql, out statement);
            if (!isKey)
            {
                statement = SqlParser.Parse(sql);
                DaoFactory.GetSqlSource().Add(sql, statement);
            }

            var sw = Stopwatch.StartNew();
            var command = statement.CreateCommand(Provider, parameters);
            try
            {
                return func(CreateDbCommand(command));
            }
            finally
            {
                sw.Stop();
                Logger.Debug("Execute Command '{0}' Used {1} ms", command.ToString(), sw.ElapsedMilliseconds);
            }
        }


        /// <summary>
        /// 检查sqlId是否存在，如果存在则以ISqlStatement返回其sql
        /// </summary>
        private bool FindStatement(string sql, out ISqlStatement statement)
        {
            if (!DaoFactory.GetSqlSource().IsValidKey(sql))
            {
                statement = null;
                return false;
            }
            return null != (statement = DaoFactory.GetSqlSource().Find(sql, _provider.Name));
        }

        private string FindText(string sql, out ISqlStatement statement)
        {
            return FindStatement(sql, out statement) ? statement.Text : sql;
        }

        private DbCommand CreateDbCommand(ISqlCommand command)
        {
            DbCommand dbCommand = Provider.CreateDbCommand(command.CommandText);
            dbCommand.CommandType = CommandType.Text;
            dbCommand.Connection = Provider.CreateDbConnection(ConnectionString);
            foreach (var parameter in command.Parameters)
            {
                if (Provider.SupportsNamedParameter &&
                    dbCommand.Parameters.Contains(parameter.Key))
                {
                    if (!Provider.NamedParameterMustOneByOne)
                    {
                        continue;
                    }
                }
                var dbParameter = dbCommand.CreateParameter();
                dbParameter.ParameterName = parameter.Key;

                object value = parameter.Value;
                dbParameter.Value = value ?? DBNull.Value;
                dbCommand.Parameters.Add(dbParameter);
            }
            return dbCommand;
        }
        #endregion
    }
}