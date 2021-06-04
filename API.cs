using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace RestApiCP
{
    /// <summary>
    /// the API class
    /// </summary>
    public static class API
    {
        /// <summary>
        /// this function init the DB
        /// </summary>
        public static SqlConnectionStringBuilder initBuilder()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = "checkpointtaskdb.database.windows.net";
            builder.UserID = "fooad444";
            builder.Password = "CheckpointTask123";
            builder.InitialCatalog = "TaskDB";
            return builder;
        }
        /// <summary>
        /// the function gets httpRequest, it expects "userName","subject" and "content" in the body or url params
        /// creates a post.
        /// </summary>
        [FunctionName("create")]
        public static async Task<IActionResult> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            string json = "";
            try
            {
                string name = req.Query["userName"];
                string subject = req.Query["subject"];
                string content = req.Query["content"];
                DateTime currentDate = DateTime.Now;

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                name = name ?? data?.userName;
                subject = subject ?? data?.subject;
                content = content ?? data?.content;
                if (name == null || subject == null || content == null)
                {
                    return new OkObjectResult("information missing, please send them  in body or params");
                }
                SqlConnectionStringBuilder builder = initBuilder();


                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();

                    String sql = $"INSERT INTO [dbo].[CP]\nOUTPUT INSERTED.id\nVALUES(N'{name}',N'{subject}',N'{content}',0,{currentDate.Ticks}) ";

                    SqlCommand command = new SqlCommand(sql, connection);
                    int insertedID = 0;
                    command.CommandType = CommandType.Text;
                    {
                        insertedID = Convert.ToInt32(command.ExecuteScalar());
                    }

                    List<DBdata> _data = getFromDB(insertedID, command, connection);




                    long ticks = _data[0].timestamp;
                    DateTime epochTime = new DateTime(ticks);

                    _data[0].createDate = epochTime.ToString("MM/dd/yyyy h:mm tt");
                    json = JsonConvert.SerializeObject(_data.ToArray(), Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                    connection.Close();

                }

            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
                return new OkObjectResult("failed!!!");
            }

            return new OkObjectResult(json);


        }

        /// <summary>
        /// this function gets rows from DB returned by the id, or if id is 0 then the function return all rows ( all posts).
        /// </summary>
        private static List<DBdata> getFromDB(int id, SqlCommand command, SqlConnection connection,string username=null)
        {
            string sql;
            if(username!=null)
            {
                sql = $"select *  FROM [dbo].[CP] WHERE userName='{username}';";
            }
            else if (id == 0)
            {
                sql = $"select * \n FROM [dbo].[CP];";
            }
            else { sql = $"select * \n FROM [dbo].[CP] WHERE id={id};"; }

            command = new SqlCommand(sql, connection);
            SqlDataReader reader = command.ExecuteReader();
            List<DBdata> _data = new List<DBdata>();



            while (reader.Read())
            {


                _data.Add(new DBdata()
                {
                    id = reader.GetInt32(0),
                    username = reader.GetString(1),
                    subject = reader.GetString(2),
                    content = reader.GetString(3),
                    likes = reader.GetInt32(4),
                    timestamp = reader.GetInt64(5)
                });

            }
            reader.Close();
            return _data;
        }

        /// <summary>
        /// the function gets httpRequest, it expects "userName" and "id" in the body or url params
        /// it then delete the post if the user who sent the request is the post owner
        /// </summary>
        [FunctionName("delete")]
        public static async Task<IActionResult> Run2(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {


                string name = req.Query["userName"];
                string id = req.Query["id"];


                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                name = name ?? data?.userName;
                id = id ?? data?.id;



                if (name == null || id == null)
                {
                    return new OkObjectResult("information missing, please send them  in body or params");
                }



                SqlConnectionStringBuilder builder = initBuilder();

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();

                    String sql = $"select * \n FROM [dbo].[CP] WHERE id={id};";
                    SqlCommand command = new SqlCommand(sql, connection);
                    SqlDataReader reader = command.ExecuteReader();
                    StringBuilder strbuild = new StringBuilder();

                    string user = "";
                    while (reader.Read())
                    {
                        user = reader.GetString(1);
                    }
                    reader.Close();
                    if (user.Equals(name))
                    {
                        sql = $"DELETE FROM [dbo].[CP] WHERE id={id};";

                        command = new SqlCommand(sql, connection);


                        command.ExecuteNonQuery();
                    }
                    else
                    {
                        return new OkObjectResult("username doesnt match!!!");
                    }
                    connection.Close();

                }

            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
                return new OkObjectResult("failed!!!");
            }

            return new OkObjectResult("succesed deleted 1 post");


        }

        /// <summary>
        /// the function gets httpRequest, it expects "userName","newSubject" and "newContent" and "id" in the body or url params
        /// the function updates the required post
        /// </summary>
        [FunctionName("update")]
        public static async Task<IActionResult> Run3(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {


                string name = req.Query["userName"];
                string id = req.Query["id"];
                string subject = req.Query["newSubject"];
                string content = req.Query["newContent"];


                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                name = name ?? data?.userName;
                id = id ?? data?.id;
                subject = subject ?? data?.newSubject;
                content = content ?? data?.newSubject;
                if (name == null || id == null || (subject == null && content == null))
                {
                    return new OkObjectResult("information missing, please send them  in body or params");
                }


                SqlConnectionStringBuilder builder = initBuilder();

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();

                    String sql = $"select * \n FROM [dbo].[CP] WHERE id={id};";
                    SqlCommand command = new SqlCommand(sql, connection);
                    SqlDataReader reader = command.ExecuteReader();
                    StringBuilder strbuild = new StringBuilder();

                    string user = "";
                    while (reader.Read())
                    {
                        user = reader.GetString(1);

                    }
                    reader.Close();
                    if (user.Equals(name))
                    {
                        string cont = $"content = '{content}'";
                        string subj = $"subject = '{subject}'";
                        string comma = "";
                        if (cont != null && subj != null)
                        {
                            comma = ",";
                        }
                        sql = $"UPDATE [dbo].[CP]\nSET {cont}{comma} {subj}\nWHERE id = {id}; ";

                        command = new SqlCommand(sql, connection);


                        command.ExecuteNonQuery();
                    }
                    else
                    {
                        return new OkObjectResult("username doesnt match!!!");
                    }
                    connection.Close();

                }

            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
                return new OkObjectResult("failed!!!");
            }

            return new OkObjectResult("succesed updated 1 post");


        }

        /// <summary>
        /// the function gets httpRequest, it expects "id" or "userName" in the body or url params
        /// it return the post with the provided id ro posts with the provided userName, if no id or userName passed as the param, it return all posts.
        /// priority for userName over id, sending body with both id and userName, the userName will work.
        /// </summary>
        [FunctionName("read")]
        public static async Task<IActionResult> Run4(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string json = "";
            try
            {


                string id = req.Query["id"];
                string name = req.Query["userName"];

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                id = id ?? data?.id;
                name = name ?? data?.userName;


                SqlConnectionStringBuilder builder = initBuilder();

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand();
                    List<DBdata> _data;
                    if(name!=null)
                    {
                        _data = getFromDB(0, command, connection,name);
                    }
                    else if (id == null)
                    {
                        _data = getFromDB(0, command, connection);
                    }
                    else
                    {
                        _data = getFromDB(Int32.Parse(id), command, connection);
                    }
                    for (int i = 0; i < _data.Count; i++)
                    {
                        long ticks = _data[i].timestamp;
                        DateTime epochTime = new DateTime(ticks);

                        _data[i].createDate = epochTime.ToString("MM/dd/yyyy h:mm tt");
                    }

                    json = JsonConvert.SerializeObject(_data.ToArray(), Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                    connection.Close();

                }

            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
                return new OkObjectResult("failed!!!");
            }

            return new OkObjectResult(json);


        }

        /// <summary>
        /// the function gets httpRequest, it expects "id" in the body or url params
        /// the function likes a post correlated to the id.
        /// </summary>
        [FunctionName("like")]
        public static async Task<IActionResult> Run5(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            
            try
            {


                string id = req.Query["id"];

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                id = id ?? data?.id;

                if (id == null)
                {
                    return new OkObjectResult("information missing, please send them  in body or params");
                }


                SqlConnectionStringBuilder builder = initBuilder();

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {

                    connection.Open();
                    var a = getFromDB(Int32.Parse(id), new SqlCommand(), connection);
                    if (a.Count == 0)
                    {
                        return new OkObjectResult("id doesnt exist!!!");
                    }
                    string sql = $"update[dbo].[CP] set numOfLikes = numOfLikes + 1 where id = {id}";

                    var command = new SqlCommand(sql, connection);


                    command.ExecuteNonQuery();
                    connection.Close();

                }

            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
                return new OkObjectResult("failed!!!");
            }

            return new OkObjectResult("liked the post");


        }
        /// <summary>
        /// the function expect nothing, it returns top trending, it return post with highest like count, and if more than 1 then it returns the nweset one.
        /// </summary>
        [FunctionName("getTrending")]
        public static async Task<IActionResult> Run6(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string json = "";
            try
            {

                SqlConnectionStringBuilder builder = initBuilder();

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {

                    connection.Open();

                    string sql = $"SELECT  *\nFROM[dbo].[CP]\nWHERE numOfLikes = (SELECT MAX(numOfLikes) FROM[dbo].[CP]); ";
                    var command = new SqlCommand(sql, connection);
                    SqlDataReader reader = command.ExecuteReader();
                    List<DBdata> _data = new List<DBdata>();



                    while (reader.Read())
                    {


                        _data.Add(new DBdata()
                        {
                            id = reader.GetInt32(0),
                            username = reader.GetString(1),
                            subject = reader.GetString(2),
                            content = reader.GetString(3),
                            likes = reader.GetInt32(4),
                            timestamp = reader.GetInt64(5)
                        });


                    }
                    DBdata champ;

                    champ = _data.OrderByDescending(item => item.timestamp).First();
                    long ticks = champ.timestamp;


                    DateTime epochTime = new DateTime(ticks);

                    champ.createDate = epochTime.ToString("MM/dd/yyyy h:mm tt");
                    json = JsonConvert.SerializeObject(champ, Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                    connection.Close();

                }

            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
                return new OkObjectResult("failed!!!");
            }

            return new OkObjectResult(json);


        }
    }
    /// <summary>
    /// json module
    /// 
    /// </summary>
    public class DBdata
    {
        [JsonProperty("id")]
        public int id { get; set; }
        [JsonProperty("author")]
        public string username;
        [JsonProperty("suject of the post")]
        public string subject { get; set; }
        [JsonProperty("content of the post")]
        public string content { get; set; }
        [JsonProperty("number of likes")]
        public int likes { get; set; }
        [JsonIgnore]
        public long timestamp { get; set; }
        [JsonProperty("creatation date")]
        public string createDate { get; set; }
    }
}
