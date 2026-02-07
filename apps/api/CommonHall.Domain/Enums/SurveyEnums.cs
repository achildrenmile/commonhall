namespace CommonHall.Domain.Enums;

public enum SurveyType
{
    OneTime = 0,
    Recurring = 1
}

public enum SurveyStatus
{
    Draft = 0,
    Active = 1,
    Closed = 2,
    Archived = 3
}

public enum SurveyQuestionType
{
    SingleChoice = 0,
    MultiChoice = 1,
    FreeText = 2,
    Rating = 3,
    NPS = 4,
    YesNo = 5
}

public enum FormFieldType
{
    Text = 0,
    Textarea = 1,
    Email = 2,
    Phone = 3,
    Number = 4,
    Date = 5,
    Dropdown = 6,
    Radio = 7,
    Checkbox = 8,
    File = 9
}
