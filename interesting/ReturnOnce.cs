namespace streaming;

public class ReturnOnce
{
    Func<bool> condition;
    bool returnedTrue;

    public ReturnOnce(Func<bool> condition)
    {
        this.condition = condition;
    }

    public bool Value
    {
        get
        {
            // Only return true once
            if (returnedTrue)
                return false;

            return returnedTrue = condition();
        }
    }
}
