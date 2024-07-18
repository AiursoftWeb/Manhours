using Aiursoft.UiStack.Layout;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.UiStack;

public static class Extensions
{
    public static ViewResult UiStackView(this Controller controller, UiStackLayoutViewModel model)
    {
        return controller.View(model: model);
    }
    
    public static ViewResult UiStackView(this Controller controller, UiStackLayoutViewModel model, string viewName)
    {
        return controller.View(viewName, model);
    }
    
    public static IMvcBuilder AddAiursoftUiStack(this IMvcBuilder builder)
    {
        builder.AddApplicationPart(typeof(UiStackLayoutViewModel).Assembly);
        return builder;
    }
}