namespace SEE.Game.City
{
    /// <summary>
    /// This strategy shows edges connecting authors and their commits.
    /// </summary>
    public enum ShowAuthorEdgeStrategy : byte
    {
        ShowAllways,

        ShowOnHover,

        ShowOnHoverOrWithMultipleAuthors,
    }
}
