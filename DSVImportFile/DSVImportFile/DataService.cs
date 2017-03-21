using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSVImportFile
{
   class DataService
   {
      public static int SaveErrorCount(string Filename)
      {
         int SaveCount = 0;
         try
         {

            string ConnectionString = ConfigurationManager.ConnectionStrings["DSVdb"].ConnectionString;
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
               SqlCommand cmd = new SqlCommand("USP_IU_ErrorStatus", con);
               cmd.CommandType = System.Data.CommandType.StoredProcedure;
               cmd.Parameters.AddWithValue("@Filename", Filename);
               SqlParameter outPutParameter = new SqlParameter();
               outPutParameter.ParameterName = "@RetErrorCount";
               outPutParameter.SqlDbType = System.Data.SqlDbType.Int;
               outPutParameter.Direction = System.Data.ParameterDirection.Output;
               cmd.Parameters.Add(outPutParameter);

               con.Open();
               cmd.ExecuteNonQuery();
               SaveCount = Convert.ToInt32(outPutParameter.Value == DBNull.Value ? "0" : outPutParameter.Value);
            }

         }
         catch (Exception ex)
         {
            throw ex;
         }


         return SaveCount;
      }



      public static int GetErrorCount(string filename)
      {
         int SaveCount = 0;
         try
         {

            string ConnectionString = ConfigurationManager.ConnectionStrings["DSVdb"].ConnectionString;
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
               SqlCommand cmd = new SqlCommand("USP_GetErrorOccurrenceCount", con);
               cmd.CommandType = System.Data.CommandType.StoredProcedure;
               cmd.Parameters.AddWithValue("@Filename", filename);
               SqlParameter outPutParameter = new SqlParameter();
               outPutParameter.ParameterName = "@RetErrorCount";
               outPutParameter.SqlDbType = System.Data.SqlDbType.Int;
               outPutParameter.Direction = System.Data.ParameterDirection.Output;
               cmd.Parameters.Add(outPutParameter);

               con.Open();
               cmd.ExecuteNonQuery();
               SaveCount =Convert.ToInt32(outPutParameter.Value==DBNull.Value?"0":outPutParameter.Value);
            }

         }
         catch (Exception ex)
         {
            throw ex;
         }


         return SaveCount;
      }
   }
}
