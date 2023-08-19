namespace WpfApp2;

public delegate void Save(string data);
public interface IOnConfirmListener
{
    event Save saveEvent;
}