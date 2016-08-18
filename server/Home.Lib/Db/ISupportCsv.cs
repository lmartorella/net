namespace Lucky.Home.Db
{
    interface ISupportCsv
    {
        string ToCsv();
        string CsvHeader { get; }
    }
}
