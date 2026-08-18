using System.Text.Json;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KRSDealerManagement.Web.Helpers
{
    public static class ModelColorViewHelper
    {
        public static async Task SetModelColorMapAsync(Controller controller, IMediator mediator)
        {
            var map = await mediator.Send(new GetVehicleModelColorMapQuery());
            controller.ViewBag.ModelColorMap = map;
            controller.ViewBag.ModelColorMapJson = JsonSerializer.Serialize(
                map.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => kvp.Value.Select(c => new { id = c.ColorId, name = c.ColorName, hex = c.HexCode }).ToList()));
        }
    }
}
