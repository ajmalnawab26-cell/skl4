using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// نام کی ٹکراؤ سے بچنے کیلئے
using Color = System.Drawing.Color;
using SDImage = System.Drawing.Image;

namespace RishtaManagerPro
{
    public class MainForm : Form
    {
        // ---------- DB paths ----------
        string DbFile  => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "records.db");
        string ConnStr => $"Data Source={DbFile}";
        string photoPath = "";

        // ---------- top ----------
        TextBox? txtSearch;
        DataGridView? grid;
        PictureBox? pic;

        // ---------- personal ----------
        TextBox? txtName, txtFather, txtPhone, txtEdu, txtEduExtra, txtAge, txtHeight, txtWeight,
                 txtBody, txtComplexion, txtWork, txtAddress, txtMonthIncome;
        ComboBox? cmbGender, cmbMarital, cmbCity, cmbReligion, cmbMaslak, cmbCaste;

        // family
        TextBox? txtHouseType, txtHouseSize, txtOtherProp, txtParentsAlive, txtFatherJob, txtMotherJob,
                 txtSisters, txtMSisters, txtBrothers, txtMBrothers, txtSiblingNo, txtDisability,
                 txtFirstWife, txtChildren, txtRelToSeeker, txtExtra, txtSmoking;

        // desired
        TextBox? dMarital, dAge, dEdu, dReligion, dMaslak, dCaste, dHeight, dHouse, dCityProv, dMobile, dWA, dRef;

        // footer
        TextBox? txtReceivedBy, txtCheckedBy, txtCommission, txtMainOffice;

        // buttons
        Button? bAdd, bUpd, bDel, bNew, bPrint, bPdf, bCsv, bBackup, bRestore, bTheme, bOptions, bBrowse;

        public MainForm()
        {
            Text = "Rishta Manager Pro";
            Width = 1280; Height = 820;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);

            BuildUI();
            EnsureDb();
            LoadData();

            using var con = new SqliteConnection(ConnStr);
            con.Open();
            txtMainOffice!.Text = GetSetting(con, "MAIN_OFFICE");
        }

        // ================= UI =================
        void BuildUI()
        {
            // search
            Controls.Add(new Label{Text="Search / تلاش:", Left=20, Top=15, AutoSize=true});
            txtSearch = new TextBox{ Left=120, Top=12, Width=620 };
            txtSearch.TextChanged += (s,e)=>LoadData(txtSearch!.Text);
            Controls.Add(txtSearch);

            // grid
            grid = new DataGridView{
                Left=20, Top=45, Width=1220, Height=260,
                ReadOnly=true, AllowUserToAddRows=false,
                AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode=DataGridViewSelectionMode.FullRowSelect
            };
            grid.SelectionChanged += Grid_SelectionChanged;
            Controls.Add(grid);

            // photo
            pic = new PictureBox{ Left=1060, Top=320, Width=180, Height=220, BorderStyle=BorderStyle.FixedSingle, SizeMode=PictureBoxSizeMode.Zoom };
            bBrowse = Btn("تصویر / Browse",1060,545,180,34,(s,e)=>BrowsePhoto());
            Controls.Add(pic); Controls.Add(bBrowse);

            // tabs
            var tabs = new TabControl{ Left=20, Top=315, Width=1020, Height=320 };
            var tpP = new TabPage("ذاتی");
            var tpF = new TabPage("خاندان/گھر");
            var tpD = new TabPage("رشتہ مطلوب");
            var tpT = new TabPage("دستخط / Footer");
            tabs.TabPages.AddRange(new[]{tpP,tpF,tpD,tpT});
            Controls.Add(tabs);

            // settings for combos
            using var con = new SqliteConnection(ConnStr); con.Open();
            string[] castes   = GetSetting(con,"CASTE_OPTIONS").Split(';', StringSplitOptions.RemoveEmptyEntries);
            string[] cities   = GetSetting(con,"CITY_OPTIONS").Split(';', StringSplitOptions.RemoveEmptyEntries);
            string[] rels     = GetSetting(con,"RELIGION_OPTIONS").Split(';', StringSplitOptions.RemoveEmptyEntries);
            string[] maslaks  = GetSetting(con,"MASLAK_OPTIONS").Split(';', StringSplitOptions.RemoveEmptyEntries);

            // personal layout
            int x=10, y=15, w=320, gap=34;
            AddText("نام:", tpP, ref txtName, x, ref y, w, gap);
            AddText("والدیت:", tpP, ref txtFather, x, ref y, w, gap);
            AddCmb("جنس:", tpP, ref cmbGender, new[]{"Male","Female"}, x, ref y, w, gap);
            AddCmb("شادی شدہ:", tpP, ref cmbMarital, new[]{"Single","Married","Divorced","Widowed"}, x, ref y, w, gap);
            AddText("عمر:", tpP, ref txtAge, x, ref y, w, gap);
            AddText("قد(سمی):", tpP, ref txtHeight, x, ref y, w, gap);
            AddText("وزن(کلو):", tpP, ref txtWeight, x, ref y, w, gap);
            AddText("فون:", tpP, ref txtPhone, x, ref y, w, gap);

            AddCmb("شہر:", tpP, ref cmbCity, cities, x, ref y, w, gap);
            AddCmb("مذہب:", tpP, ref cmbReligion, rels, x, ref y, w, gap);
            AddCmb("مسلک:", tpP, ref cmbMaslak, maslaks, x, ref y, w, gap);
            AddCmb("ذات:", tpP, ref cmbCaste, castes, x, ref y, w, gap);

            int x2=360; y=15;
            AddText("تعلیم:", tpP, ref txtEdu, x2, ref y, w, gap);
            AddText("اضافی تعلیم:", tpP, ref txtEduExtra, x2, ref y, w, gap);
            AddText("جسامت:", tpP, ref txtBody, x2, ref y, w, gap);
            AddText("رنگت:", tpP, ref txtComplexion, x2, ref y, w, gap);
            AddMulti("کام/کاروبار:", tpP, ref txtWork, x2, ref y, w, 60, gap);
            AddText("والد کا پیشہ:", tpP, ref txtFatherJob, x2, ref y, w, gap);
            AddText("والدہ کا پیشہ:", tpP, ref txtMotherJob, x2, ref y, w, gap);
            AddText("آمدنی:", tpP, ref txtMonthIncome, x2, ref y, w, gap);
            AddMulti("ایڈریس:", tpP, ref txtAddress, x2, ref y, w, 60, gap);

            // family
            x=10; y=15;
            AddText("گھر(ذاتی/کرایہ):", tpF, ref txtHouseType, x, ref y, w, gap);
            AddText("گھر کا سائز:", tpF, ref txtHouseSize, x, ref y, w, gap);
            AddText("دیگر جائیداد:", tpF, ref txtOtherProp, x, ref y, w, gap);
            AddText("والدین حیات:", tpF, ref txtParentsAlive, x, ref y, w, gap);
            AddText("بہنیں:", tpF, ref txtSisters, x, ref y, w, gap);
            AddText("شادی شدہ بہنیں:", tpF, ref txtMSisters, x, ref y, w, gap);
            AddText("بھائی:", tpF, ref txtBrothers, x, ref y, w, gap);
            AddText("شادی شدہ بھائی:", tpF, ref txtMBrothers, x, ref y, w, gap);
            AddText("نمبر (بہن بھائیوں میں):", tpF, ref txtSiblingNo, x, ref y, w, gap);
            AddText("بیماری/معذوری:", tpF, ref txtDisability, x, ref y, w, gap);
            AddText("پہلی بیوی:", tpF, ref txtFirstWife, x, ref y, w, gap);
            AddText("بچے:", tpF, ref txtChildren, x, ref y, w, gap);
            AddText("رشتہ والے سے تعلق:", tpF, ref txtRelToSeeker, x, ref y, w, gap);
            AddMulti("اضافی معلومات:", tpF, ref txtExtra, x, ref y, w, 60, gap);
            AddText("سگریٹ/نشہ:", tpF, ref txtSmoking, x, ref y, w, gap);

            // desired
            x=10; y=15;
            AddText("چاہیے شادی حیثیت:", tpD, ref dMarital, x, ref y, w, gap);
            AddText("عمر:", tpD, ref dAge, x, ref y, w, gap);
            AddText("تعلیم:", tpD, ref dEdu, x, ref y, w, gap);
            AddText("مذہب:", tpD, ref dReligion, x, ref y, w, gap);
            AddText("مسلک:", tpD, ref dMaslak, x, ref y, w, gap);
            AddText("ذات:", tpD, ref dCaste, x, ref y, w, gap);
            AddText("قد:", tpD, ref dHeight, x, ref y, w, gap);
            AddText("گھر:", tpD, ref dHouse, x, ref y, w, gap);
            AddText("شہر/صوبہ قید:", tpD, ref dCityProv, x, ref y, w, gap);
            AddText("موبائل:", tpD, ref dMobile, x, ref y, w, gap);
            AddText("واٹس ایپ:", tpD, ref dWA, x, ref y, w, gap);
            AddMulti("ریفرینس:", tpD, ref dRef, x, ref y, w, 60, gap);

            // footer
            x=10; y=15;
            AddText("وصول کنندہ:", tpT, ref txtReceivedBy, x, ref y, 380, gap);
            AddText("چیک کرنے والا:", tpT, ref txtCheckedBy, x, ref y, 380, gap);
            AddMulti("کمیشن/نوٹ:", tpT, ref txtCommission, x, ref y, 380, 60, gap);
            AddMulti("Main Office لائن:", tpT, ref txtMainOffice, x, ref y, 950, 60, gap);

            // buttons
            int bx=20, by=650, bw=120, bh=36, s=10;
            bAdd    = Btn("شامل کریں", bx,by,bw,bh, (s1,e)=>AddRecord());
            bUpd    = Btn("تبدیلی", bx+(bw+s),by,bw,bh,(s1,e)=>UpdateRecord());
            bDel    = Btn("حذف", bx+2*(bw+s),by,bw,bh,(s1,e)=>DeleteRecord());
            bNew    = Btn("نیا", bx+3*(bw+s),by,bw,bh,(s1,e)=>ClearForm());
            bPrint  = Btn("پرنٹ", bx+4*(bw+s),by,bw,bh,(s1,e)=>PrintRecord());
            bPdf    = Btn("PDF", bx+5*(bw+s),by,bw,bh,(s1,e)=>ExportPdf());
            bCsv    = Btn("CSV", bx+6*(bw+s),by,bw,bh,(s1,e)=>ExportCsv());
            bBackup = Btn("Backup", bx+7*(bw+s),by,bw,bh,(s1,e)=>BackupDb());
            bRestore= Btn("Restore", bx+8*(bw+s),by,bw,bh,(s1,e)=>RestoreDb());
            bTheme  = Btn("رنگ", bx+9*(bw+s),by,bw,bh,(s1,e)=>PickTheme());
            bOptions= Btn("⚙ Options", bx+10*(bw+s),by,110,bh,(s1,e)=>OpenOptions());
            Controls.AddRange(new Control[]{bAdd,bUpd,bDel,bNew,bPrint,bPdf,bCsv,bBackup,bRestore,bTheme,bOptions});
        }

        Label L(string t)=>new Label{Text=t,AutoSize=true};
        Button Btn(string t,int l,int tp,int w,int h,EventHandler onClick){ var b=new Button{Text=t,Left=l,Top=tp,Width=w,Height=h}; b.Click+=onClick; return b; }
        void AddText(string cap, Control p, ref TextBox? box, int x, ref int y, int w, int gap)
        { p.Controls.Add(L(cap){Left=x,Top=y}); box=new TextBox{Left=x+220,Top=y-4,Width=w}; p.Controls.Add(box); y+=gap; }
        void AddMulti(string cap, Control p, ref TextBox? box, int x, ref int y, int w, int h, int gap)
        { p.Controls.Add(L(cap){Left=x,Top=y}); box=new TextBox{Left=x+220,Top=y-4,Width=w,Height=h,Multiline=true,ScrollBars=ScrollBars.Vertical}; p.Controls.Add(box); y+=h+(gap-10); }
        void AddCmb(string cap, Control p, ref ComboBox? cmb, string[] items, int x, ref int y, int w, int gap)
        {
            p.Controls.Add(L(cap){Left=x,Top=y});
            cmb=new ComboBox{Left=x+220,Top=y-6,Width=w,DropDownStyle=ComboBoxStyle.DropDown,AutoCompleteMode=AutoCompleteMode.SuggestAppend,AutoCompleteSource=AutoCompleteSource.ListItems};
            cmb.Items.AddRange(items); p.Controls.Add(cmb); y+=gap;
        }

        // ================= DB =================
        void EnsureDb()
        {
            if (!File.Exists(DbFile)) File.Create(DbFile).Dispose();
            using var con = new SqliteConnection(ConnStr);
            con.Open();

            new SqliteCommand(@"
CREATE TABLE IF NOT EXISTS Users(
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 Name TEXT,FatherName TEXT,Phone TEXT,City TEXT,Religion TEXT,Maslak TEXT,Caste TEXT,
 Age INTEGER,Height REAL,Weight REAL,Gender TEXT,MaritalStatus TEXT,
 Education TEXT,EducationExtra TEXT,BodyType TEXT,Complexion TEXT,WorkDetails TEXT,
 FatherJob TEXT,MotherOccupation TEXT,MonthlyIncome TEXT,Address TEXT,
 HouseType TEXT,HouseSize TEXT,OtherProperty TEXT,ParentsAlive TEXT,
 Sisters INTEGER,MarriedSisters INTEGER,Brothers INTEGER,MarriedBrothers INTEGER,SiblingNumber INTEGER,Disability TEXT,
 FirstWife TEXT,Children TEXT,RelationWithSeeker TEXT,ExtraInfo TEXT,SmokingDrugs TEXT,
 D_MaritalStatus TEXT,D_Age TEXT,D_Education TEXT,D_Religion TEXT,D_Maslak TEXT,D_Caste TEXT,D_Height TEXT,D_House TEXT,D_CityProvince TEXT,D_Mobile TEXT,D_WhatsApp TEXT,D_Reference TEXT,
 ReceivedBy TEXT,CheckedBy TEXT,CommissionNote TEXT,MainOffice TEXT,
 ImagePath TEXT,CreatedAt TEXT
);", con).ExecuteNonQuery();

            // Settings
            new SqliteCommand("CREATE TABLE IF NOT EXISTS Settings(Key TEXT PRIMARY KEY, Val TEXT);", con).ExecuteNonQuery();
            Seed("MAIN_OFFICE","Main Office: Gulzar Colony, Sialkot Road, Gujranwala");
            Seed("CITY_OPTIONS","Gujranwala;Sialkot;Lahore;Faisalabad;Multan;Karachi;Islamabad");
            Seed("RELIGION_OPTIONS","Islam;Christian;Hindu");
            Seed("MASLAK_OPTIONS","Barelvi;Deobandi;Ahl-e-Hadith;Shia");
            Seed("CASTE_OPTIONS","Rajput;Jutt;Arain;Mughal;Syed;Sheikh;Pathan");

            void Seed(string k,string v){ var c=new SqliteCommand("INSERT OR IGNORE INTO Settings(Key,Val) VALUES(@k,@v);",con); c.Parameters.AddWithValue("@k",k); c.Parameters.AddWithValue("@v",v); c.ExecuteNonQuery(); }
        }

        string GetSetting(SqliteConnection con,string key)
        { var c=new SqliteCommand("SELECT Val FROM Settings WHERE Key=@k;",con); c.Parameters.AddWithValue("@k",key); return c.ExecuteScalar()?.ToString() ?? ""; }

        void SetSetting(SqliteConnection con,string key,string val)
        { var c=new SqliteCommand("INSERT INTO Settings(Key,Val) VALUES(@k,@v) ON CONFLICT(Key) DO UPDATE SET Val=excluded.Val;",con); c.Parameters.AddWithValue("@k",key); c.Parameters.AddWithValue("@v",val); c.ExecuteNonQuery(); }

        // ================= Load/Search =================
        void LoadData(string q="")
        {
            using var con=new SqliteConnection(ConnStr); con.Open();
            var cmd=con.CreateCommand();
            cmd.CommandText=@"
SELECT Id,Name,FatherName,Phone,City,Religion,Maslak,Caste,Age,Height,Weight,Gender,MaritalStatus,Education,
Address,ImagePath,ReceivedBy,CheckedBy,MainOffice
FROM Users
WHERE @q='' OR (Name LIKE @p OR Phone LIKE @p OR City LIKE @p OR Caste LIKE @p OR Religion LIKE @p)
ORDER BY Id DESC;";
            cmd.Parameters.AddWithValue("@q", q); cmd.Parameters.AddWithValue("@p",$"%{q}%");
            var dt=new DataTable(); dt.Load(cmd.ExecuteReader());
            grid!.DataSource=dt;
            if (grid.Columns.Contains("ImagePath")) grid.Columns["ImagePath"].Visible=false;
        }

        // ================= CRUD =================
        void AddRecord()
        {
            if (!ValidateForm()) return;
            using var con=new SqliteConnection(ConnStr); con.Open();
            var cmd=con.CreateCommand();
            cmd.CommandText=@"
INSERT INTO Users
(Name,FatherName,Phone,City,Religion,Maslak,Caste,Age,Height,Weight,Gender,MaritalStatus,
 Education,EducationExtra,BodyType,Complexion,WorkDetails,FatherJob,MotherOccupation,MonthlyIncome,Address,
 HouseType,HouseSize,OtherProperty,ParentsAlive,Sisters,MarriedSisters,Brothers,MarriedBrothers,SiblingNumber,Disability,
 FirstWife,Children,RelationWithSeeker,ExtraInfo,SmokingDrugs,
 D_MaritalStatus,D_Age,D_Education,D_Religion,D_Maslak,D_Caste,D_Height,D_House,D_CityProvince,D_Mobile,D_WhatsApp,D_Reference,
 ReceivedBy,CheckedBy,CommissionNote,MainOffice,ImagePath,CreatedAt)
VALUES(@n,@f,@ph,@city,@rel,@mas,@caste,@age,@h,@w,@gen,@mar,
 @edu,@eduX,@body,@comp,@work,@fJob,@mJob,@income,@addr,
 @hType,@hSize,@oProp,@pAlive,@sis,@mSis,@bro,@mBro,@sibNo,@dis,
 @first,@child,@relSeek,@extra,@smoke,
 @dMar,@dAge,@dEdu,@dRel,@dMas,@dCast,@dHgt,@dHouse,@dCity,@dMob,@dWa,@dRef,
 @rec,@chk,@comm,@off,@img,@t);";
            PutParams(cmd);
            cmd.Parameters.AddWithValue("@t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
            LoadData(txtSearch!.Text); ClearForm(); MessageBox.Show("ریکارڈ شامل ہو گیا");
        }

        void UpdateRecord()
        {
            if (grid!.CurrentRow==null) return;
            if (!ValidateForm()) return;
            int id=Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);

            using var con=new SqliteConnection(ConnStr); con.Open();
            var cmd=con.CreateCommand();
            cmd.CommandText=@"
UPDATE Users SET
 Name=@n,FatherName=@f,Phone=@ph,City=@city,Religion=@rel,Maslak=@mas,Caste=@caste,Age=@age,Height=@h,Weight=@w,Gender=@gen,MaritalStatus=@mar,
 Education=@edu,EducationExtra=@eduX,BodyType=@body,Complexion=@comp,WorkDetails=@work,FatherJob=@fJob,MotherOccupation=@mJob,MonthlyIncome=@income,Address=@addr,
 HouseType=@hType,HouseSize=@hSize,OtherProperty=@oProp,ParentsAlive=@pAlive,Sisters=@sis,MarriedSisters=@mSis,Brothers=@bro,MarriedBrothers=@mBro,SiblingNumber=@sibNo,Disability=@dis,
 FirstWife=@first,Children=@child,RelationWithSeeker=@relSeek,ExtraInfo=@extra,SmokingDrugs=@smoke,
 D_MaritalStatus=@dMar,D_Age=@dAge,D_Education=@dEdu,D_Religion=@dRel,D_Maslak=@dMas,D_Caste=@dCast,D_Height=@dHgt,D_House=@dHouse,D_CityProvince=@dCity,D_Mobile=@dMob,D_WhatsApp=@dWa,D_Reference=@dRef,
 ReceivedBy=@rec,CheckedBy=@chk,CommissionNote=@comm,MainOffice=@off,ImagePath=@img
WHERE Id=@id;";
            PutParams(cmd); cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery(); LoadData(txtSearch!.Text); MessageBox.Show("ریکارڈ اپڈیٹ ہو گیا");
        }

        void DeleteRecord()
        {
            if (grid!.CurrentRow==null) return;
            int id=Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);
            if (MessageBox.Show("حذف کریں؟","Confirm",MessageBoxButtons.YesNo)==DialogResult.No) return;
            using var con=new SqliteConnection(ConnStr); con.Open();
            var c=new SqliteCommand("DELETE FROM Users WHERE Id=@id;",con); c.Parameters.AddWithValue("@id",id); c.ExecuteNonQuery();
            LoadData(txtSearch!.Text); ClearForm(); MessageBox.Show("ریکارڈ حذف ہو گیا");
        }

        void PutParams(SqliteCommand c)
        {
            c.Parameters.AddWithValue("@n", txtName!.Text);
            c.Parameters.AddWithValue("@f", txtFather!.Text);
            c.Parameters.AddWithValue("@ph", txtPhone!.Text);
            c.Parameters.AddWithValue("@city", cmbCity!.Text);
            c.Parameters.AddWithValue("@rel", cmbReligion!.Text);
            c.Parameters.AddWithValue("@mas", cmbMaslak!.Text);
            c.Parameters.AddWithValue("@caste", cmbCaste!.Text);
            c.Parameters.AddWithValue("@age", int.TryParse(txtAge!.Text,out var age)?age:0);
            c.Parameters.AddWithValue("@h", double.TryParse(txtHeight!.Text,out var h)?h:0);
            c.Parameters.AddWithValue("@w", double.TryParse(txtWeight!.Text,out var w)?w:0);
            c.Parameters.AddWithValue("@gen", cmbGender!.Text);
            c.Parameters.AddWithValue("@mar", cmbMarital!.Text);
            c.Parameters.AddWithValue("@edu", txtEdu!.Text);
            c.Parameters.AddWithValue("@eduX", txtEduExtra!.Text ?? "");
            c.Parameters.AddWithValue("@body", txtBody!.Text ?? "");
            c.Parameters.AddWithValue("@comp", txtComplexion!.Text ?? "");
            c.Parameters.AddWithValue("@work", txtWork!.Text ?? "");
            c.Parameters.AddWithValue("@fJob", txtFatherJob!.Text ?? "");
            c.Parameters.AddWithValue("@mJob", txtMotherJob!.Text ?? "");
            c.Parameters.AddWithValue("@income", txtMonthIncome!.Text ?? "");
            c.Parameters.AddWithValue("@addr", txtAddress!.Text ?? "");
            c.Parameters.AddWithValue("@hType", txtHouseType!.Text ?? "");
            c.Parameters.AddWithValue("@hSize", txtHouseSize!.Text ?? "");
            c.Parameters.AddWithValue("@oProp", txtOtherProp!.Text ?? "");
            c.Parameters.AddWithValue("@pAlive", txtParentsAlive!.Text ?? "");
            c.Parameters.AddWithValue("@sis", int.TryParse(txtSisters!.Text,out var a)?a:0);
            c.Parameters.AddWithValue("@mSis", int.TryParse(txtMSisters!.Text,out var b)?b:0);
            c.Parameters.AddWithValue("@bro", int.TryParse(txtBrothers!.Text,out var c1)?c1:0);
            c.Parameters.AddWithValue("@mBro", int.TryParse(txtMBrothers!.Text,out var d)?d:0);
            c.Parameters.AddWithValue("@sibNo", int.TryParse(txtSiblingNo!.Text,out var e)?e:0);
            c.Parameters.AddWithValue("@dis", txtDisability!.Text ?? "");
            c.Parameters.AddWithValue("@first", txtFirstWife!.Text ?? "");
            c.Parameters.AddWithValue("@child", txtChildren!.Text ?? "");
            c.Parameters.AddWithValue("@relSeek", txtRelToSeeker!.Text ?? "");
            c.Parameters.AddWithValue("@extra", txtExtra!.Text ?? "");
            c.Parameters.AddWithValue("@smoke", txtSmoking!.Text ?? "");
            c.Parameters.AddWithValue("@dMar", dMarital!.Text ?? "");
            c.Parameters.AddWithValue("@dAge", dAge!.Text ?? "");
            c.Parameters.AddWithValue("@dEdu", dEdu!.Text ?? "");
            c.Parameters.AddWithValue("@dRel", dReligion!.Text ?? "");
            c.Parameters.AddWithValue("@dMas", dMaslak!.Text ?? "");
            c.Parameters.AddWithValue("@dCast", dCaste!.Text ?? "");
            c.Parameters.AddWithValue("@dHgt", dHeight!.Text ?? "");
            c.Parameters.AddWithValue("@dHouse", dHouse!.Text ?? "");
            c.Parameters.AddWithValue("@dCity", dCityProv!.Text ?? "");
            c.Parameters.AddWithValue("@dMob", dMobile!.Text ?? "");
            c.Parameters.AddWithValue("@dWa", dWA!.Text ?? "");
            c.Parameters.AddWithValue("@dRef", dRef!.Text ?? "");
            c.Parameters.AddWithValue("@rec", txtReceivedBy!.Text);
            c.Parameters.AddWithValue("@chk", txtCheckedBy!.Text);
            c.Parameters.AddWithValue("@comm", txtCommission!.Text ?? "");
            c.Parameters.AddWithValue("@off", txtMainOffice!.Text);
            c.Parameters.AddWithValue("@img", SavePhotoIfAny());
        }

        // ================= Events/Utils =================
        void Grid_SelectionChanged(object? s, EventArgs e)
        {
            if (grid!.CurrentRow==null) return;
            string V(string col)=> grid.CurrentRow.Cells[col]?.Value?.ToString() ?? "";
            txtName!.Text=V("Name"); txtFather!.Text=V("FatherName"); txtPhone!.Text=V("Phone");
            cmbCity!.Text=V("City"); cmbReligion!.Text=V("Religion"); cmbMaslak!.Text=V("Maslak"); cmbCaste!.Text=V("Caste");
            txtAge!.Text=V("Age"); txtHeight!.Text=V("Height"); txtWeight!.Text=V("Weight");
            cmbGender!.Text=V("Gender"); cmbMarital!.Text=V("MaritalStatus");
            txtEdu!.Text=V("Education"); txtAddress!.Text=V("Address");
            txtReceivedBy!.Text=V("ReceivedBy"); txtCheckedBy!.Text=V("CheckedBy"); txtMainOffice!.Text=V("MainOffice");
            var img=V("ImagePath"); if (!string.IsNullOrWhiteSpace(img) && File.Exists(img)){ pic!.Image=SDImage.FromFile(img); photoPath=img; } else { pic!.Image=null; photoPath=""; }
        }

        bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtName!.Text)) { MessageBox.Show("نام لازمی ہے"); return false; }
            if (string.IsNullOrWhiteSpace(txtPhone!.Text) || txtPhone.Text.Length<10 || txtPhone.Text.Length>15) { MessageBox.Show("درست فون نمبر لکھیں"); return false; }
            if (string.IsNullOrWhiteSpace(txtReceivedBy!.Text)) { MessageBox.Show("وصول کنندہ لازمی"); return false; }
            if (string.IsNullOrWhiteSpace(txtCheckedBy!.Text))  { MessageBox.Show("چیک لازمی"); return false; }
            if (string.IsNullOrWhiteSpace(txtMainOffice!.Text)) { MessageBox.Show("Main Office لازمی"); return false; }
            return true;
        }

        void ClearForm(){ foreach(Control c in Controls) Clear(c); photoPath=""; pic!.Image=null; }
        void Clear(Control c){ if (c is TextBox t && t!=txtSearch) t.Text=""; if (c is ComboBox cb) cb.Text=""; foreach(Control k in c.Controls) Clear(k); }

        string SavePhotoIfAny()
        {
            try{
                if (string.IsNullOrEmpty(photoPath) || !File.Exists(photoPath)) return "";
                var dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"photos"); Directory.CreateDirectory(dir);
                var dest=Path.Combine(dir, $"{Guid.NewGuid()}{Path.GetExtension(photoPath)}"); File.Copy(photoPath,dest,true); return dest;
            }catch{ return ""; }
        }
        void BrowsePhoto(){ using var ofd=new OpenFileDialog{Filter="Images|*.jpg;*.jpeg;*.png"}; if (ofd.ShowDialog()==DialogResult.OK){ photoPath=ofd.FileName; pic!.Image=SDImage.FromFile(photoPath);} }

        // CSV/Backup/Restore
        void ExportCsv()
        {
            var sfd=new SaveFileDialog{Filter="CSV|*.csv",FileName="records.csv"}; if (sfd.ShowDialog()!=DialogResult.OK) return;
            using var con=new SqliteConnection(ConnStr); con.Open();
            var r=new SqliteCommand("SELECT * FROM Users ORDER BY Id DESC",con).ExecuteReader();
            using var sw=new StreamWriter(sfd.FileName,false,System.Text.Encoding.UTF8);
            for(int i=0;i<r.FieldCount;i++){ if(i>0) sw.Write(","); sw.Write(r.GetName(i)); } sw.WriteLine();
            while(r.Read()){ for(int i=0;i<r.FieldCount;i++){ if(i>0) sw.Write(","); var v=r.IsDBNull(i)?"":r.GetValue(i)!.ToString()!.Replace("\"","\"\""); sw.Write($"\"{v}\""); } sw.WriteLine(); }
            MessageBox.Show("CSV تیار");
        }
        void BackupDb(){ var sfd=new SaveFileDialog{Filter="SQLite DB|*.db",FileName=$"backup_{DateTime.Now:yyyyMMdd_HHmm}.db"}; if(sfd.ShowDialog()!=DialogResult.OK) return; File.Copy(DbFile,sfd.FileName,true); MessageBox.Show("Backup مکمل"); }
        void RestoreDb(){ var ofd=new OpenFileDialog{Filter="SQLite DB|*.db"}; if(ofd.ShowDialog()!=DialogResult.OK) return; var bak=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,$"before_restore_{DateTime.Now:yyyyMMdd_HHmm}.db"); File.Copy(DbFile,bak,true); File.Copy(ofd.FileName,DbFile,true); EnsureDb(); LoadData(); MessageBox.Show("Restore ہو گیا"); }

        // Print/PDF
        void PrintRecord()
        {
            if (grid!.CurrentRow==null){ MessageBox.Show("کوئی ریکارڈ منتخب کریں"); return; }
            var doc=new PrintDocument();
            doc.PrintPage+=(s,e)=>{
                float x=50,y=60,lh=26; var title=new Font("Segoe UI",16,FontStyle.Bold); var f=new Font("Segoe UI",10);
                e.Graphics.DrawString("Matrimonial Record / ریکارڈ",title,Brushes.Black,x,y); y+=40;
                void Line(string k,string v){ e.Graphics.DrawString($"{k}: {v}",f,Brushes.Black,x,y); y+=lh; }
                Line("Name / نام",txtName!.Text); Line("Father",txtFather!.Text); Line("Phone",txtPhone!.Text);
                Line("City",cmbCity!.Text); Line("Caste",cmbCaste!.Text); Line("Religion",cmbReligion!.Text);
                if(!string.IsNullOrEmpty(photoPath) && File.Exists(photoPath)) e.Graphics.DrawImage(SDImage.FromFile(photoPath),new Rectangle(620,80,180,220));
                y+=20; e.Graphics.FillRectangle(Brushes.Black,new RectangleF(40,y,e.PageBounds.Width-80,24));
                e.Graphics.DrawString(txtMainOffice!.Text,new Font("Segoe UI",10,FontStyle.Bold),Brushes.White,50,y+4);
            };
            using var p=new PrintPreviewDialog{Document=doc,Width=1000,Height=700}; p.ShowDialog();
        }

        void ExportPdf()
        {
            if (grid!.CurrentRow==null){ MessageBox.Show("کوئی ریکارڈ منتخب کریں"); return; }
            var sfd=new SaveFileDialog{Filter="PDF|*.pdf",FileName="Record.pdf"}; if(sfd.ShowDialog()!=DialogResult.OK) return;
            QuestPDF.Settings.License=LicenseType.Community;
            Document.Create(c=>{
                c.Page(p=>{
                    p.Margin(30);
                    p.Header().Text("Matrimonial Record / ریکارڈ").SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
                    p.Content().Column(col=>{
                        col.Item().Table(t=>{
                            t.ColumnsDefinition(k=>{ k.ConstantColumn(170); k.RelativeColumn(); });
                            void Row(string k,string v){ t.Cell().Element(x=>x.Background(Colors.Grey.Lighten3).Padding(6)).Text(k);
                                                         t.Cell().Element(x=>x.BorderBottom(1).Padding(6)).Text(v); }
                            Row("Name / نام",txtName!.Text);
                            Row("Father",txtFather!.Text);
                            Row("Phone",txtPhone!.Text);
                            Row("City",cmbCity!.Text);
                            Row("Caste",cmbCaste!.Text);
                            Row("Religion",cmbReligion!.Text);
                            Row("Maslak",cmbMaslak!.Text);
                            Row("Education",txtEdu!.Text);
                            Row("Address",txtAddress!.Text);
                        });
                        if(!string.IsNullOrEmpty(photoPath) && File.Exists(photoPath))
                            col.Item().PaddingTop(8).AlignRight().Width(180).Height(220).Image(photoPath).FitArea();
                        col.Item().PaddingTop(8).Background(Colors.Black).Padding(6).Text(txtMainOffice!.Text).FontColor(Colors.White).SemiBold().AlignCenter();
                    });
                });
            }).GeneratePdf(sfd.FileName);
            MessageBox.Show("PDF تیار!");
        }

        // Theme + Options
        void PickTheme(){ using var cd=new ColorDialog(); if(cd.ShowDialog()==DialogResult.OK){ BackColor=cd.Color; foreach(Control c in Controls) if(c is Button b) b.BackColor=ControlPaint.Light(cd.Color); } }
        void OpenOptions()
        {
            using var con=new SqliteConnection(ConnStr); con.Open();
            string caste=GetSetting(con,"CASTE_OPTIONS"), city=GetSetting(con,"CITY_OPTIONS"),
                   rel=GetSetting(con,"RELIGION_OPTIONS"), mas=GetSetting(con,"MASLAK_OPTIONS"),
                   office=GetSetting(con,"MAIN_OFFICE");

            var f=new Form{Text="Options",Width=760,Height=560,StartPosition=FormStartPosition.CenterParent};
            TextBox tC=new(){Left=20,Top=42,Width=700,Text=caste};
            TextBox tCity=new(){Left=20,Top=102,Width=700,Text=city};
            TextBox tR=new(){Left=20,Top=162,Width=700,Text=rel};
            TextBox tM=new(){Left=20,Top=222,Width=700,Text=mas};
            TextBox tOff=new(){Left=20,Top=282,Width=700,Text=office};
            f.Controls.AddRange(new Control[]{ L("CASTE_OPTIONS (;)"){Left=20,Top=20}, tC,
                                               L("CITY_OPTIONS (;)"){Left=20,Top=80}, tCity,
                                               L("RELIGION_OPTIONS (;)"){Left=20,Top=140}, tR,
                                               L("MASLAK_OPTIONS (;)"){Left=20,Top=200}, tM,
                                               L("MAIN_OFFICE"){Left=20,Top=260}, tOff,
                                               new Button{Text="Save",Left=20,Top=330,Width=120,DialogResult=DialogResult.OK}});
            if(f.ShowDialog()==DialogResult.OK){
                SetSetting(con,"CASTE_OPTIONS",tC.Text); SetSetting(con,"CITY_OPTIONS",tCity.Text);
                SetSetting(con,"RELIGION_OPTIONS",tR.Text); SetSetting(con,"MASLAK_OPTIONS",tM.Text);
                SetSetting(con,"MAIN_OFFICE",tOff.Text);
                cmbCaste!.Items.Clear(); cmbCaste.Items.AddRange(tC.Text.Split(';',StringSplitOptions.RemoveEmptyEntries));
                cmbCity!.Items.Clear(); cmbCity.Items.AddRange(tCity.Text.Split(';',StringSplitOptions.RemoveEmptyEntries));
                cmbReligion!.Items.Clear(); cmbReligion.Items.AddRange(tR.Text.Split(';',StringSplitOptions.RemoveEmptyEntries));
                cmbMaslak!.Items.Clear(); cmbMaslak.Items.AddRange(tM.Text.Split(';',StringSplitOptions.RemoveEmptyEntries));
                txtMainOffice!.Text=tOff.Text; MessageBox.Show("سیٹنگز اپڈیٹ ہو گئیں");
            }
        }
    }
}
