namespace SofiValidator;
public class MenuItem(string name, Action action)
{
    public string Name { get; set; } = name;
    public Action Action { get; set; } = action;
}