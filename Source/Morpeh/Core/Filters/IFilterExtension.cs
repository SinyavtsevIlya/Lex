namespace Nanory.Lex {
    public interface IFilterExtension {
        FilterBuilder Extend(FilterBuilder rootFilter);
    }
}