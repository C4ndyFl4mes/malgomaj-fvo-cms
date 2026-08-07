using Microsoft.AspNetCore.Components;
// using Server.API.Routes.ImageFile.POST;
using Server.UI.Layout;
using Server.API.Enums;
using Microsoft.AspNetCore.Components.Forms;
using FluentValidation;
using FluentValidation.Results;
using Server.API.Exceptions;
// using Server.API.Routes.ImageFile.GET;
// using Server.UI.Components.ImageTabs;
// using Server.UI.Components;

namespace Server.UI.Pages.Files.ImagesPage;

public class ImagesBase : ComponentBase
{
    // [Inject]
    // protected NavigationState NavigationState { get; set; } = default!;
    // [Inject]
    // protected IValidator<PostImageRequest> PostImageValidator { get; set; } = default!;
    // [Inject]
    // protected ImagePostData ImagePostData { get; set; } = default!;

    // protected ImageInspectionModel? ImageInspection { get; set; } = null;
    // protected List<ImageType> AllowedImageTabs { get; set; } = [ImageType.Normal, ImageType.Banner, ImageType.Square, ImageType.Icon];

    // protected Dictionary<string, string[]> ValidationErrors { get; set; } = [];
    // protected Dictionary<string, string[]> ValidationErrorsOfImageRetrieval
    // {
    //     get;
    //     set
    //     {
    //         ValidationErrorsOfImageRetrievalDisplay = value;
    //     }
    // } = [];
    // protected Dictionary<string, string[]> ValidationErrorsOfImageRetrievalDisplay { get; set; } = [];

    // protected (bool IsSuccess, string Message) ResultMessage { get; set; } = (false, string.Empty);

    // protected bool IsNewImageOverlayOpen { get; set; } = false;
    // protected bool IsImageInspectionOverlayOpen => ImageInspection is not null;


    // protected IBrowserFile? NewImageFile { get; set; } = null;
    // protected ImageType NewImageType { get; set; } = ImageType.Normal;
    // protected List<(string, ImageType)> ImageTypes { get; set; } = new()
    // {
    //     ("Normal (16:9)", ImageType.Normal),
    //     ("Banderoll (3:1)", ImageType.Banner),
    //     ("Fyrkant (1:1)", ImageType.Square),
    //     ("Ikon (1:1)", ImageType.Icon)
    // };
    // protected (ImageType, IEnumerable<ImageDTO>) ImagesOfSelectedTab { get; set; } = (ImageType.Normal, []);

    // protected ImageTabs? ImageTabsRef { get; set; }

    // protected Dictionary<string, string> NewImageTranslations { get; set; } = new()
    // {
    //     ["sv"] = string.Empty
    // };
    // protected string NewImageAltText { get; set; } = string.Empty;


    // // CSS-klasser för att bibehålla rätt bildförhållande i de olika tabbarna.
    // protected Dictionary<ImageType, string> CSSClassesForImageType = new()
    // {
    //     [ImageType.Normal] = "aspect-video",
    //     [ImageType.Banner] = "aspect-[3/1]",
    //     [ImageType.Square] = "aspect-square",
    //     [ImageType.Icon] = "aspect-square"
    // };

    // // Dimensioner för de olika bildtyperna, används i attributen.
    // protected Dictionary<ImageType, (int, int)> DimensionsForImageType = new()
    // {
    //     [ImageType.Normal] = (800, 450),
    //     [ImageType.Banner] = (1200, 400),
    //     [ImageType.Square] = (380, 380),
    //     [ImageType.Icon] = (64, 64)
    // };

    // protected override void OnInitialized()
    // {
    //     NavigationState.SetBreadcrumbs([
    //         new BreadcrumbModel
    //         {
    //             Title = "Panel",
    //             Href = "/admin"
    //         },
    //         new BreadcrumbModel
    //         {
    //             Title = "Filer",
    //             Href = "/admin/files"
    //         },
    //         new BreadcrumbModel
    //         {
    //             Title = "Bildhantering",
    //             Href = "/admin/files/images"
    //         }
    //     ]);
    // }

    // // Hanterar uppladdning av en ny bild: validerar, skickar till API:t och uppdaterar UI.
    // protected async Task HandleAddNewImageAsync()
    // {
    //     if (NewImageFile is not null)
    //     {
    //         ValidationErrors.Clear();

    //         PostImageRequest request = new()
    //         {
    //             ImageFile = NewImageFile,
    //             Type = NewImageType.ToString(),
    //             AltText = NewImageAltText
    //         };

    //         ValidationResult validationResult = await PostImageValidator.ValidateAsync(request);

    //         if (!validationResult.IsValid)
    //         {
    //             ErrorsToDictionary(validationResult);
    //             await InvokeAsync(StateHasChanged);
    //             return;
    //         }

    //         try
    //         {
    //             await ImagePostData.PostImageAsync(request, CancellationToken.None);
    //             ResultMessage = (true, "Bilden har laddats upp.");

    //             // Ladda om bilderna i den aktiva tabben så att den nya bilden visas direkt.
    //             if (ImageTabsRef is not null)
    //             {
    //                 await ImageTabsRef.ReloadActiveTab();
    //             }
    //         }
    //         catch (BadRequestException ex)
    //         {
    //             ResultMessage = (false, ex.Message);
    //         }
    //         catch (ArgumentOutOfRangeException ex)
    //         {
    //             ResultMessage = (false, ex.Message);
    //         }
    //         catch (Exception)
    //         {
    //             ResultMessage = (false, "Ett oväntat fel inträffade. Vänligen försök igen senare.");
    //         }
    //         finally
    //         {
    //             await InvokeAsync(StateHasChanged);
    //         }
    //     }
    // }

    // protected void HandleImageClick(ImageDTO image, int width, int height)
    // {
    //     ImageInspection = new ImageInspectionModel
    //     {
    //         Image = image,
    //         Width = width,
    //         Height = height
    //     };
    // }

    // protected async Task UpdateUIAfterImageDeletion()
    // {
    //     if (ImageInspection is not null && ImageTabsRef is not null)
    //     {
    //         ImageInspection = null;
    //         await ImageTabsRef.ReloadActiveTab();
    //     }
    // }

    // protected void ResetNewImageForm()
    // {
    //     NewImageFile = null;
    //     NewImageType = ImageType.Normal;
    //     NewImageTranslations = new Dictionary<string, string> { ["sv"] = string.Empty };
    //     NewImageAltText = string.Empty;
    //     ValidationErrors.Clear();
    //     ResultMessage = (false, string.Empty);
    // }

    // protected void SetImageField(InputFileChangeEventArgs e)
    // {
    //     IsNewImageOverlayOpen = true;
    //     NewImageFile = e.File;
    //     StateHasChanged();
    // }


    // private void ErrorsToDictionary(ValidationResult validationResult)
    // {
    //     ValidationErrors = validationResult.Errors
    //         .GroupBy(e => e.PropertyName)
    //         .ToDictionary(
    //             g => g.Key,
    //             g => g.Select(e => e.ErrorMessage).ToArray()
    //         );
    // }
}