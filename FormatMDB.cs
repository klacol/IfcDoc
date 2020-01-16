// Name:        FormatMdb.cs
// Description: Microsoft Access database (.mdb) storage
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2010 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using System.Data.OleDb;

using IfcDoc.Schema;

namespace IfcDoc.Format.MDB
{
	public class FormatMDB : IDisposable
	{
		string m_file;
		OleDbConnection m_connection;
		Dictionary<string, Type> m_typemap;
		Dictionary<long, SEntity> m_instances;
		long m_lastOID; // highest inclusive OID

		/// <summary>
		/// Encapsulates a Microsoft Access database file
		/// </summary>
		/// <param name="file">Required file path</param>
		/// <param name="types">Required map of string identifiers and types</param>
		/// <param name="instances">Optional map of instance identifiers and objects (specified if saving)</param>
		public FormatMDB(
			string file,
			Dictionary<string, Type> types,
			Dictionary<long, SEntity> instances)
		{
			this.m_file = file;
			this.m_typemap = types;
			this.m_instances = instances;

			if (m_instances == null)
			{
				this.m_instances = new Dictionary<long, SEntity>();
			}

			string connectionstring =
				"Provider=Microsoft.Jet.OLEDB.4.0;" +
				"Data Source=" + this.m_file + ";" +
				"Jet OLEDB:Engine Type=5;";

			// create database if it doesn't exist
			bool bNew = false;
			if (!System.IO.File.Exists(file))
			{
				ADOX.CatalogClass cat = new ADOX.CatalogClass();
				cat.Create(connectionstring);
				bNew = true;
			}

			this.m_connection = new OleDbConnection(connectionstring);
			this.m_connection.Open();

			// build schema
			if (bNew)
			{
				foreach (Type t in this.m_typemap.Values)
				{
					InitType(t);
				}
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (this.m_connection != null)
			{
				this.m_connection.Close();
				this.m_connection = null;
			}
		}

		#endregion

		public Dictionary<string, Type> Types
		{
			get
			{
				return this.m_typemap;
			}
		}

		public Dictionary<long, SEntity> Instances
		{
			get
			{
				return this.m_instances;
			}
		}

		private void Execute(string sql)
		{
			using (OleDbCommand cmd = this.m_connection.CreateCommand())
			{
				cmd.CommandText = sql;
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Creates database table(s) for type
		/// </summary>
		/// <param name="t"></param>
		public void InitType(Type t)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("CREATE TABLE ");
			sb.Append(t.Name);
			sb.Append(" (oid INTEGER");
			IList<FieldInfo> fields = SEntity.GetFieldsOrdered(t);
			foreach (FieldInfo f in fields)
			{
				if (!f.FieldType.IsGenericType) // don't deal with lists
				{
					sb.Append(", ");

					sb.Append(f.Name);
					sb.Append(" ");

					if (typeof(SEntity).IsAssignableFrom(f.FieldType))
					{
						sb.Append(" INTEGER"); // oid
					}
					else if (f.FieldType.IsEnum)
					{
						sb.Append(" VARCHAR");
					}
					else if (typeof(string) == f.FieldType)
					{
						//if (f.Name.Equals("_Documentation") || f.Name.Equals("_Description") || f.Name.Equals("_Expression"))
						if (f.Name.Equals("_Name"))
						{
							// for name, indexable
							sb.Append(" VARCHAR");
						}
						else
						{
							// all others unlimited text
							sb.Append(" TEXT");
						}
					}
					else if (typeof(bool) == f.FieldType)
					{
						sb.Append(" BIT");
					}
					else if (typeof(int) == f.FieldType)
					{
						sb.Append(" INTEGER");
					}
					else if (typeof(double) == f.FieldType)
					{
						sb.Append(" FLOAT");
					}
					else if (typeof(byte[]) == f.FieldType)
					{
						sb.Append(" TEXT");
					}
					else
					{
						System.Diagnostics.Debug.WriteLine("FormatMDB::InitType() - incompatible field type");
					}
				}
			}
			sb.Append(");");
			Execute(sb.ToString());

			// populate relationships for LISTs
			foreach (FieldInfo f in fields)
			{
				if (f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(List<>))
				{
					sb = new StringBuilder();
					sb.Append("CREATE TABLE ");
					sb.Append(t.Name);
					sb.Append("_");
					sb.Append(f.Name);
					sb.Append(" (source INTEGER, sequence INTEGER, target INTEGER);");
					Execute(sb.ToString());
				}
			}
		}

		public void Register(SEntity entity)
		{
			this.m_lastOID++;
			entity.OID = this.m_lastOID;
			this.Instances.Add(this.m_lastOID, entity);
		}

		/// <summary>
		/// Loads all instances from the database
		/// </summary>
		public void Load()
		{
			// first pass: load all entities and non-list attributes
			foreach (Type t in this.m_typemap.Values)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("SELECT * FROM ");
				sb.Append(t.Name);

				IList<FieldInfo> fields = SEntity.GetFieldsOrdered(t);

				// load scalar attributes
				using (OleDbCommand cmd = this.m_connection.CreateCommand())
				{
					cmd.CommandText = sb.ToString();
					using (OleDbDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							int oid = reader.GetInt32(0);

							SEntity entity = null;
							if (!this.Instances.TryGetValue(oid, out entity))
							{
								entity = (SEntity)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t);
								entity.Existing = true;
								entity.OID = oid;
								this.Instances.Add(entity.OID, entity);

								if (entity.OID > m_lastOID)
								{
									this.m_lastOID = entity.OID;
								}
							}

							int index = 0;
							foreach (FieldInfo f in fields)
							{
								if (!f.FieldType.IsGenericType)
								{
									// non-list attribute
									index++;
									object value = reader.GetValue(index);
									if (value != null && !(value is DBNull))
									{
										if (f.FieldType.IsEnum)
										{
											object eval = Enum.Parse(f.FieldType, value.ToString());
											f.SetValue(entity, eval);
										}
										else if (typeof(SEntity).IsAssignableFrom(f.FieldType))
										{
											if (value is Int32)
											{
												int ival = (int)value;
												SEntity eval = null;
												if (this.Instances.TryGetValue(ival, out eval))
												{
													f.SetValue(entity, eval);
												}
											}
										}
										else if (typeof(byte[]) == f.FieldType)
										{
											string strval = (string)value;
											int len = strval.Length / 2;

											Byte[] valuevector = new byte[len];
											for (int i = 0; i < len; i++)
											{
												char hi = strval[i * 2 + 0];
												char lo = strval[i * 2 + 1];

												byte val = (byte)(
													((hi >= 'A' ? +(int)(hi - 'A' + 10) : (int)(hi - '0')) << 4) +
													((lo >= 'A' ? +(int)(lo - 'A' + 10) : (int)(lo - '0'))));

												valuevector[i] = val;
											}

											f.SetValue(entity, valuevector);
										}
										else
										{
											f.SetValue(entity, value);
										}
									}
								}
							}
						}
					}
				}
			}

			// second pass: load lists (now that all entities are loaded)
			foreach (Type t in this.m_typemap.Values)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("SELECT * FROM ");
				sb.Append(t.Name);
				sb.Append(";");

				IList<FieldInfo> fields = SEntity.GetFieldsOrdered(t);

				// load vector attributes
				using (OleDbCommand cmd = this.m_connection.CreateCommand())
				{
					cmd.CommandText = sb.ToString();
					using (OleDbDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							long oid = (long)reader.GetInt32(0);
							SEntity entity = this.Instances[oid];

							foreach (FieldInfo f in fields)
							{
								if (f.FieldType.IsGenericType)
								{
									// list
									System.Collections.IList list = (System.Collections.IList)f.GetValue(entity);
									if (list == null)
									{
										list = (System.Collections.IList)Activator.CreateInstance(f.FieldType);
										f.SetValue(entity, list);
									}

									using (OleDbCommand cmdList = this.m_connection.CreateCommand())
									{
										StringBuilder sbl = new StringBuilder();
										sbl.Append("SELECT * FROM ");
										sbl.Append(t.Name);
										sbl.Append("_");
										sbl.Append(f.Name);
										sbl.Append(" WHERE source=");
										sbl.Append(entity.OID.ToString());
										sbl.Append(" ORDER BY sequence;");
										cmdList.CommandText = sbl.ToString();
										using (OleDbDataReader readerList = cmdList.ExecuteReader())
										{
											while (readerList.Read())
											{
												long refid = (long)readerList.GetInt32(2);
												SEntity target = this.Instances[refid];
												list.Add(target);
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Saves all instances to the database
		/// </summary>
		public void Save()
		{
			foreach (SEntity entity in this.Instances.Values)
			{
				Type t = entity.GetType();
				IList<FieldInfo> fields = SEntity.GetFieldsOrdered(t);

				if (entity.Existing)
				{
					//...update attributes...
				}
				else
				{

					// create new
					StringBuilder sb = new StringBuilder();
					sb.Append("INSERT INTO ");
					sb.Append(t.Name);
					sb.Append("(oid");
					foreach (FieldInfo f in fields)
					{
						if (!f.FieldType.IsGenericType) // don't deal with lists
						{
							sb.Append(", ");
							sb.Append(f.Name);
						}
					}
					sb.Append(") VALUES (");
					sb.Append(entity.OID.ToString());
					foreach (FieldInfo f in fields)
					{
						if (!f.FieldType.IsGenericType) // don't deal with lists
						{
							sb.Append(", ");
							object val = f.GetValue(entity);
							if (val is SEntity)
							{
								SEntity entref = (SEntity)val;
								sb.Append(entref.OID);
							}
							else if (val is byte[])
							{
								byte[] bval = (byte[])val;
								sb.Append("'");
								sb.Append(BitConverter.ToString(bval));
								sb.Append("'");
							}
							else if (val != null)
							{
								string encoded = val.ToString();
								if (encoded != null)
								{
									if (val is string || val is Enum)
									{
										sb.Append("'");
									}
									sb.Append(encoded.Replace("'", "''")); // escape single quotes - note: does not handle other unicode quotes
									if (val is string || val is Enum)
									{
										sb.Append("'");
									}
								}
								else
								{
									sb.Append("NULL");
								}
							}
							else
							{
								sb.Append("NULL");
							}
						}
					}
					sb.Append(");");

					Execute(sb.ToString());
					entity.Existing = true;
				}

				// lists
				foreach (FieldInfo f in fields)
				{
					if (f.FieldType.IsGenericType)
					{
						string tablename = t.Name + "_" + f.Name;

						// clear existing
						Execute("DELETE FROM " + tablename + " WHERE source=" + entity.OID.ToString() + ";");

						System.Collections.IList list = (System.Collections.IList)f.GetValue(entity);
						if (list != null)
						{
							for (int i = 0; i < list.Count; i++)
							{
								SEntity target = (SEntity)list[i];

								Execute("INSERT INTO " + tablename +
									" (source, sequence, target) VALUES (" +
									entity.OID.ToString() + ", " +
									i.ToString() + ", " +
									target.OID.ToString() + ");");
							}
						}
					}
				}

			}
		}
	}
}
