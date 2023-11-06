using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.OleDb;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using DAL;
using DTO;
using DAL.action;
using System.Net.Mail;
using System.Net;

namespace BLL
{
    public class functions : Ifunctions
    {
        IOwnerOfEventAction _ownerOfEventAction;
        IInvitedToEventAction _invitedToEventAction;
        EmailOrdersContext _db;

        public functions(IOwnerOfEventAction ownerOfEventAction, IInvitedToEventAction invitedToEventAction,EmailOrdersContext db)
        {
            _ownerOfEventAction = ownerOfEventAction;
            _invitedToEventAction = invitedToEventAction;
            _db = db;
        }

        string fileName = "";
        //שמירת הנתונים מקובץ אקסל
       static DataTable dt = new DataTable();
        static DataTable dt2 = new DataTable();
        //שמירת כתובות המייל של המוזמנים
        static List<string> listEmail = new List<string>();
        //שמירת קוד בעל השמחה
        static int idevent = 0;
        EmailOrdersContext myDb = new EmailOrdersContext();

        //שמירת שם ההזמנה
        public int putNameFileInvitationInData(string fileName)
        {
            OwnerOfEvent ownerOfEvent = new OwnerOfEvent();
            ownerOfEvent.NameFileInvitation = fileName;
            _ownerOfEventAction.AddOwnerOfEvent(ownerOfEvent);
            changeId(ref idevent, ownerOfEvent);

            return ownerOfEvent.IdEvent;
        }
        public void changeId(ref int id, OwnerOfEvent o)
        {
            idevent = o.IdEvent;
        }
        //פונקציה שמוסיפה בעל שמחה
        public int addOwnerOfEvent(OwnerOfEventNew o)
        {
            OwnerOfEvent theobj = myDb.OwnerOfEvents.FirstOrDefault(x => x.IdEvent == idevent);
            InsertToInvitedToEvent();
            if (theobj != null)
            {
                theobj.AdressOfEvent = o.AdressOfEvent;
                theobj.DateOfEvent = o.DateOfEvent;
                theobj.EmailOwnerOfEvent = o.EmailOwnerOfEvent;

                theobj.IdTypeEvent = o.IdTypeEvent;
                _ownerOfEventAction.updateOwnerOfEvent(idevent, theobj);
                
            }
            else
            {
                OwnerOfEvent obj = new OwnerOfEvent();
                obj.IdEvent = idevent;
                obj.AdressOfEvent = o.AdressOfEvent;
                obj.DateOfEvent = o.DateOfEvent;
                obj.EmailOwnerOfEvent = o.EmailOwnerOfEvent;
                obj.IdTypeEvent = o.IdTypeEvent;
            }
            return theobj.IdEvent;
        }

        //קריאת קובץ אקסל ושמירת הנתונים בדטה בייס
        public bool READExcel(string fileName)
        {
            string path = "G:\\לימודים רחלי\\פרויקט גמר\\26.08\\API-FINAL_PROJECT\\API-FINAL_PROJECT\\wwwroot\\" + fileName;
            Microsoft.Office.Interop.Excel.Application objXL = null;
            Microsoft.Office.Interop.Excel.Workbook objWB = null;
            objXL = new Microsoft.Office.Interop.Excel.Application();
            objWB = objXL.Workbooks.Open(path);
            Microsoft.Office.Interop.Excel.Worksheet objSHT = objWB.Worksheets[1];

            int rows = objSHT.UsedRange.Rows.Count;
            int cols = objSHT.UsedRange.Columns.Count;

            int noofrow = 1;

            for (int c = 1; c <= cols; c++)
            {
                string colname = objSHT.Cells[1, c].Text;
                dt.Columns.Add(colname);
                noofrow = 2;
            }

            for (int r = noofrow; r <= rows; r++)
            {
                DataRow dr = dt.NewRow();
                for (int c = 1; c <= cols; c++)
                {
                    dr[c - 1] = objSHT.Cells[r, c].Text;
                }

                dt.Rows.Add(dr);
            }

            objWB.Close();
            objXL.Quit();
            bool isSuccuss;
            
            
            //for (int l = 1; l <= dt.Rows.Count; l++)
            //{
            //    string ma = dt.Rows[l][0].ToString();
            //    Invited invited = _db.Inviteds.FirstOrDefault(m => m.EmailInvited == ma);
            //    if (invited == null)
            //        dt2.Rows.Add(dt.Rows[l]);
            //}
            try
            {
                using (SqlConnection conn = new SqlConnection("Server=DESKTOP-RHAN6AI\\SQLEXPRESS;Database=EmailOrders;Trusted_Connection=True;TrustServerCertificate=True;"))
                {
                    conn.Open();
                    SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.UseInternalTransaction, null);
                    bulkCopy.DestinationTableName = "Invited";
                    //  bulkCopy.SelectCo .CommandTimeout = 60;
                    bulkCopy.BulkCopyTimeout = 0;
                    bulkCopy.WriteToServer(dt);
                    isSuccuss = true;
                }
                DataTable columnNames = new DataTable();


                int i = 0;
                string columnNameInDB = "";
                foreach (DataColumn column in dt.Columns)
                {
                    if (columnNames.Rows.Count > 0)
                        columnNameInDB = columnNames.Rows[i++]["COLUMN_NAME"].ToString();
                    // Console.WriteLine("{0}\t{1}\t{2}", column.ColumnName, columnNameInDB, columnNameInDB == column.ColumnName);
                }
                for (int j = 0; j < dt.Rows.Count; j++)
                {
                    string ma = dt.Rows[j][0].ToString();
                    listEmail.Add(ma);
                }



            }
            catch (Exception ex)
            {
                isSuccuss = false;
            }
            

                return isSuccuss;
        }
        //פונקציה שמכניסה נתונים לטבלת 
        //InvitedToEvent
        public void InsertToInvitedToEvent()
        {

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string ma = dt.Rows[i][0].ToString();
                InvitedToEvent inv = _db.InvitedToEvents.FirstOrDefault(m => m.EmailInvited == ma && m.IdEvent==idevent);
                if(inv == null)
                { 
                InvitedToEvent newObj = new InvitedToEvent();
                newObj.EmailInvited = dt.Rows[i][0].ToString();
                newObj.IdEvent = idevent;
                newObj.NumBoys = 0;
                newObj.NumSonAdults = 0;
                newObj.NumDaughterAdults = 0;
                newObj.NumteenageBoys = 0;
                newObj.NumTeenageGirls = 0;
                newObj.NumGirls = 0;
                newObj.IdTypeInvite = 6001;
                _invitedToEventAction.AddInvitedToEvent(newObj);
                }
            }
        }
        //פונקציה שמחזירה את שם הקובץ של ההזמנה
        //מקבלת קוד ארוע
        public  string returnNameFile(int id)
        {
            string name = _ownerOfEventAction.GetAllOwnerOfEvent().FirstOrDefault(n => n.IdEvent == id).NameFileInvitation;
            return name;
        }
        public List<OwnerOfEventDto> returnListOfOwnerByEmail(string email)
        {
            List<OwnerOfEventDto> list = Converts.convertoDtoOwnerOfEventList(_ownerOfEventAction.GetAllOwnerOfEvent().FindAll(x => x.EmailOwnerOfEvent==email));
            return list;
        }
        public List<InvitedToEventDto> invitedToEventDtoList(int IdEvent)
        {
            List<InvitedToEventDto> list = Converts.convertoDtoInvitedToEventList(_invitedToEventAction.GetAllInvitedToEventd().Where(x => x.IdEvent == IdEvent).ToList());
       return list;
        }
        public bool SendEmail(int idevent ) { 
        // Set up email credentials and server details
        string smtpServer = "smtp.gmail.com";
        int port = 587;
        string senderEmail = "hazmanot222@gmail.com";
        
        string senderPassword = "***";
      
        string subject = "הנך מוזמן ";
        string body = " יש לאשר הגעה בלינק" +
        "http://localhost:3000/showMyEvents";
         List<OwnerOfEvent> listOwner = _ownerOfEventAction.GetAllOwnerOfEvent();

         string fileOrder = listOwner.FirstOrDefault(x => x.IdEvent == idevent).NameFileInvitation;

            foreach (var item in listEmail)
            {
                // Create a new MailMessage object
                MailMessage message = new MailMessage(senderEmail, item, subject, body);

                // Set up the SMTP client
                SmtpClient client = new SmtpClient(smtpServer, port);
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(senderEmail, senderPassword);
                Attachment attached = new Attachment("G:\\לימודים רחלי\\פרויקט גמר\\26.08\\API-FINAL_PROJECT\\API-FINAL_PROJECT\\wwwroot\\" + fileOrder);
                message.Attachments.Add(attached);
                // Send the email
                try
                {
                    client.Send(message);
                    Console.WriteLine("Email sent successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error sending email: " + ex.Message);
                }

            }
            
            return true;
        }
    }

}
