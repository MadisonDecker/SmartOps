namespace Workforce.Bus.Models;

public class DataFeedResponse<T>
{
    public List<T> UpdateSequence { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string NextCursor { get; set; } = string.Empty;
}

public class BankUpdates : DataFeedResponse<Bank>
{
}

public class BankEventUpdates : DataFeedResponse<BankEvent>
{
}

public class CalculatedTimeUpdates : DataFeedResponse<CalculatedTime>
{
}

public class EmployeeUpdates : DataFeedResponse<Employee>
{
}

public class SwipeUpdates : DataFeedResponse<Swipe>
{
}

public class TimeOffUpdates : DataFeedResponse<TimeOff>
{
}

public class ShiftUpdates : DataFeedResponse<ShiftDay>
{
}

public class UserUpdates : DataFeedResponse<User>
{
}
