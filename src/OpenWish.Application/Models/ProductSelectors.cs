namespace OpenWish.Application.Models;

public class ProductSelectors
{
    public static readonly List<string> TitleSelectors = new()
    {
        "//meta[@property='og:title']",
        "//h1[@class='product-name']",
        "//h1[@itemprop='name']",
        "//h1[contains(@class, 'pdp-title')]",
        "//h1[contains(@class, 'product-title')]"
    };

    public static readonly List<string> DescriptionSelectors = new()
    {
        "//meta[@property='og:description']",
        "//div[@class='product-description']",
        "//div[@itemprop='description']",
        "//div[contains(@class, 'pdp-description')]"
    };

    public static readonly List<string> PriceSelectors = new()
    {
        "//meta[@property='product:price:amount']",
        "//span[@class='price-value']",
        "//span[@itemprop='price']",
        "//div[contains(@class, 'product-price')]//span[contains(@class, 'price')]"
    };

    public static readonly List<string> ImageSelectors = new()
    {
        "//meta[@property='og:image']",
        "//img[@id='main-image']",
        "//img[@itemprop='image']",
        "//img[contains(@class, 'product-image')]"
    };
}