using AutoMapper;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ControlInventario.Infrastructure.Mappings
{
    public class PostProfile : Profile
    {
        public PostProfile()
        {
            CreateMap<Post, PostDto>()
                .ForMember(dest => dest.Imagen, 
                opt => opt.MapFrom(src => src.Image))
                .ForMember(dest => dest.Date,
                    opt => opt.ConvertUsing<DateTimeToStringConverter, DateTime>());

            CreateMap<PostDto, Post>()
                .ForMember(dest => dest.Image, 
                opt => opt.MapFrom(src => src.Imagen))
                .ForMember(dest => dest.Date,
                    opt => opt.ConvertUsing<StringToDateTimeConverter, string>());
        }
    }

    public class DateTimeToStringConverter : IValueConverter<DateTime, string>
    {
        public string Convert(DateTime source, ResolutionContext context)
        {
            // Por defecto, convertir a formato completo sin AM/PM
            return source.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        }
    }

    public class StringToDateTimeConverter : IValueConverter<string, DateTime>
    {
        public DateTime Convert(string source, ResolutionContext context)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("La fecha no puede estar vacía");

            // Limpiar y normalizar el string
            source = source.Trim();

            // Reemplazar variaciones de AM/PM
            source = source.Replace("a. m.", "AM")
                          .Replace("p. m.", "PM")
                          .Replace("a.m.", "AM")
                          .Replace("p.m.", "PM")
                          .Replace("am", "AM")
                          .Replace("pm", "PM");

            // Definir todos los formatos posibles
            string[] formats = new[]
            {
        "dd-MM-yyyy",                    // 18-11-2025
        "dd-MM-yyyy HH:mm:ss",            // 18-11-2025 20:01:11
        "dd-MM-yyyy hh:mm:ss tt",         // 18-11-2025 08:01:11 PM
        "dd-MM-yyyy H:mm:ss",              // 18-11-2025 20:01:11
        "dd-MM-yyyy h:mm:ss tt",           // 18-11-2025 8:01:11 PM
        "dd/MM/yyyy",                      // 18/11/2025
        "dd/MM/yyyy HH:mm:ss",             // 18/11/2025 20:01:11
        "dd/MM/yyyy hh:mm:ss tt",          // 18/11/2025 08:01:11 PM
        "yyyy-MM-dd",                       // 2025-11-18
        "yyyy-MM-dd HH:mm:ss",              // 2025-11-18 20:01:11
        "yyyy-MM-dd hh:mm:ss tt"            // 2025-11-18 08:01:11 PM
    };

            // Intentar parsear con los formatos definidos
            if (DateTime.TryParseExact(source, formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime result))
            {
                return result;
            }

            // Si no funciona con formatos exactos, intentar parseo genérico
            if (DateTime.TryParse(source, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return result;
            }

            throw new FormatException($"No se pudo convertir la fecha '{source}' a DateTime. Formatos soportados: fecha, fecha y hora, fecha con AM/PM");
        }
    }

}
