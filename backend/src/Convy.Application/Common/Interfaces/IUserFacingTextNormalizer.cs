namespace Convy.Application.Common.Interfaces;

public interface IUserFacingTextNormalizer
{
    string NormalizeTitle(string value);
    string NormalizeForComparison(string value);
}
