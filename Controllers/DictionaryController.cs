using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using bili.Models;

namespace bili.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DictionaryController : ControllerBase
    {
        private readonly string _jsonFilePath = "Data/dictionary.json";

        private List<Dictionary> LoadDictionary()
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                Console.WriteLine("Файл не найден!");
                return new List<Dictionary>();
            }

            try
            {
                // Читаем JSON в кодировке UTF-8
                var jsonData = System.IO.File.ReadAllText(_jsonFilePath, System.Text.Encoding.UTF8);
                Console.WriteLine("JSON-файл загружен:\n" + jsonData); // Логируем JSON

                var originalList = JsonSerializer.Deserialize<List<DictionaryOldFormat>>(jsonData);

                if (originalList == null || !originalList.Any())
                {
                    Console.WriteLine("Ошибка: JSON не распарсился!");
                    return new List<Dictionary>();
                }

                Console.WriteLine("JSON успешно распарсен, количество записей: " + originalList.Count);

                var convertedList = originalList.Select(d => new Dictionary
                {
                    Term = d.En ?? "ERROR",
                    Translate = d.Ru ?? "ERROR",
                    Transcription = d.Tr ?? "ERROR",
                    Synonyms = new List<string>()
                }).ToList();

                return convertedList;
            }
            catch (JsonException ex)
            {
                Console.WriteLine("Ошибка JSON: " + ex.Message);
                return new List<Dictionary>();
            }
        }

        private void SaveDictionary(List<Dictionary> dictionary)
        {
            var jsonData = JsonSerializer.Serialize(dictionary, new JsonSerializerOptions { WriteIndented = true });

            // Записываем JSON в UTF-8
            System.IO.File.WriteAllText(_jsonFilePath, jsonData, System.Text.Encoding.UTF8);
        }


        [HttpGet]
        public IActionResult Get()
        {
            var dictionary = LoadDictionary();
            return Ok(dictionary);
        }

        [HttpGet("search/{query}")]
        public IActionResult Search(string query)
        {
            var dictionary = LoadDictionary();

            var entry = dictionary.FirstOrDefault(d =>
                string.Equals(d.Term, query, System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.Translate, query, System.StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                return NotFound(new { message = "Слово не найдено" });
            }

            return Ok(entry);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Dictionary newEntry)
        {
            if (newEntry == null || string.IsNullOrEmpty(newEntry.Term) || string.IsNullOrEmpty(newEntry.Translate))
            {
                return BadRequest("Неправильный формат данных");
            }

            var dictionary = LoadDictionary();
            dictionary.Add(newEntry);
            SaveDictionary(dictionary);

            return CreatedAtAction(nameof(Search), new { query = newEntry.Term }, newEntry);
        }

        [HttpPatch("{term}")]
        public IActionResult Update(string term, [FromBody] Dictionary updatedEntry)
        {
            var dictionary = LoadDictionary();
            var entry = dictionary.FirstOrDefault(d => d.Term == term || d.Translate == term);

            if (entry == null)
            {
                return NotFound();
            }

            entry.Translate = updatedEntry.Translate;
            entry.Transcription = updatedEntry.Transcription;
            entry.Synonyms = updatedEntry.Synonyms;

            SaveDictionary(dictionary);

            return Ok(entry);
        }

        [HttpDelete("{term}")]
        public IActionResult Delete(string term)
        {
            var dictionary = LoadDictionary();
            var entry = dictionary.FirstOrDefault(d => d.Term == term || d.Translate == term);

            if (entry == null)
            {
                return NotFound();
            }

            dictionary.Remove(entry);
            SaveDictionary(dictionary);

            return Ok(new { message = "Запись удалена" });
        }
    }
}