namespace OpenWish.Application.Models;

public class ProductSelectors
{
    public static readonly List<string> TitleSelectors = new()
    {
        "//meta[@property='og:title']/@content",
        "//h1[@class='product-name']",
        "//h1[@itemprop='name']",
        "//h1[contains(@class, 'pdp-title')]",
        "//h1[contains(@class, 'product-title')]"
    };

    public static readonly List<string> DescriptionSelectors = new()
    {
        "//meta[@property='og:description']/@content",
        "//div[@class='product-description']",
        "//div[@itemprop='description']",
        "//div[contains(@class, 'pdp-description')]"
    };

    public static readonly List<string> PriceSelectors = new()
    {
        "//meta[@property='product:price:amount']/@content",
        "//span[@class='price-value']",
        "//span[@itemprop='price']",
        "//div[contains(@class, 'product-price')]//span[contains(@class, 'price')]"
    };

    public static readonly List<string> ImageSelectors = new()
    {
        "//meta[@property='og:image']/@content",
        "//img[@id='main-image']/@src",
        "//img[@itemprop='image']/@src",
        "//img[contains(@class, 'product-image')]/@src"
    };
}