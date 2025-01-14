﻿using AuthSystem.Data;
using AuthSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace AuthSystem.Controllers
{
    [Authorize]
    public class MCQController : Controller

    {
        private readonly AuthDbContext _test;

        public MCQController(AuthDbContext test)
        {
            _test = test;
        }

        [Authorize]
        [HttpGet]
        public ActionResult Index()
        {
            IEnumerable<MCQ> getQuestions = _test.MCQs.Include(q => q.Subject);
            return View(getQuestions);
        }

        public ActionResult MCQs()
        {
            IEnumerable<MCQ> getQuestions = _test.MCQs.Include(q => q.Subject);
            return View(getQuestions);
        }

        [Authorize]
        public IActionResult Create()
        {
            int? selectedsubjectId = HttpContext.Session.GetInt32("SelectedSubjectId");
            if (selectedsubjectId == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MCQ obj)
        {
            int? selectedsubjectId = HttpContext.Session.GetInt32("SelectedSubjectId");

            obj.SubjectId = selectedsubjectId.Value;

            _test.MCQs.Add(obj);
            _test.SaveChanges();
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [Authorize]
        public IActionResult Edit()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Edit(int? Id)
        {
            if (Id == null)
            {
                return NotFound();
            }
            var EditMCQData = _test.MCQs.Find(Id);
            if (EditMCQData == null)
            {
                return NotFound();
            }
            return View(EditMCQData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(MCQ obj, int Id)
        {
            _test.MCQs.Update(obj);
            _test.SaveChanges();
            return RedirectToAction("ViewQuestions", "Subject", new { subjectId = obj.SubjectId });
        }

        public IActionResult Delete(int? Id, int subjectId)
        {
            var mcqData = _test.MCQs.Find(Id);
            if (mcqData == null)
            {
                return NotFound();
            }
            _test.MCQs.Remove(mcqData);
            _test.SaveChanges();
            return RedirectToAction("ViewQuestions", "Subject", new { subjectId = subjectId });
        }

        public IActionResult View(int? Id)
        {
            if (Id == null)
            {
                return NotFound();
            }
            var questionData = _test.MCQs.Find(Id);

            return View(questionData);
        }

        public IActionResult UploadFile()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UploadFile(IFormFile file, int subjectId)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Please select a file to upload.");
                return View();
            }

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("file", "Invalid file format. Please upload an Excel file with .xlsx extension.");
                return View();
            }

            var existingQuestions = _test.MCQs.ToList();
            var newQuestions = new List<MCQ>();

            using (var stream = new MemoryStream())
            {
                file.CopyTo(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        ModelState.AddModelError("file", "Excel file is empty or has no worksheets.");
                        return Content("Please provide a valid excel file!");
                    }

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var content = worksheet.Cells[row, 1].Value?.ToString();
                        var difficulty = worksheet.Cells[row, 7].Value?.ToString();
                        var answer = worksheet.Cells[row, 2].Value?.ToString();
                        var option1 = worksheet.Cells[row, 2].Value?.ToString();
                        var option2 = worksheet.Cells[row, 3].Value?.ToString();
                        var option3 = worksheet.Cells[row, 4].Value?.ToString();
                        var option4 = worksheet.Cells[row, 5].Value?.ToString();
                        if (difficulty == null)
                        {
                            difficulty = "Medium";
                        }
                        if (string.IsNullOrEmpty(content))
                        {
                            // Ignore empty rows
                            continue;
                        }
                        var question = new MCQ
                        {
                            Content = content,
                            Answer = answer,
                            Option1 = option1,
                            Option2 = option2,
                            Option3 = option3,
                            Option4 = option4,
                            Difficulty = difficulty,
                            SubjectId = subjectId
                        };

                        if (existingQuestions.Any(q =>
                            string.Equals(q.Content.Trim(), content.Trim(), StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(q.Answer.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(q.Option1.Trim(), option1.Trim(), StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(q.Option2.Trim(), option2.Trim(), StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(q.Option3.Trim(), option3.Trim(), StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(q.Option4.Trim(), option4.Trim(), StringComparison.OrdinalIgnoreCase) &&
                            q.Difficulty == difficulty && q.SubjectId == subjectId))
                        {
                            continue;
                        }

                        if (string.IsNullOrEmpty(question.Answer))
                        {
                            ModelState.AddModelError("Answer", $"The answer for row {row} is required.");
                            return View();
                        }

                        newQuestions.Add(question);
                    }
                }
            }

            // Save the new questions to the database
            if (newQuestions.Count > 0)
            {
                _test.MCQs.AddRange(newQuestions);
                _test.SaveChanges();
            }

            return RedirectToAction("ViewQuestions", "Subject", new { subjectId = subjectId });
        }

        public IActionResult CreateQuestion(int subjectId, string statement, string answer, string option1, string option2, string option3, string option4, string diffLevel)
        {
            try
            {
                MCQ mcq = new()
                {
                    Content = statement,
                    Answer = answer,
                    Option1 = option1,
                    Option2 = option2,
                    Option3 = option3,
                    Option4 = option4,
                    SubjectId = subjectId,
                    Difficulty = diffLevel
                };

                _test.MCQs.Add(mcq);
                _test.SaveChanges();
                return RedirectToAction("ViewQuestions", "Subject", new { subjectId = subjectId });
            }
            catch (Exception e)
            {
                return Json(new { Error = e.Message });
            }
        }

        public IActionResult GetQuestions(int subjectId)
        {
            try
            {
                var questions = _test.MCQs.Where(q => q.SubjectId == subjectId).ToArray();

                return Json(questions);
            }
            catch (Exception e)
            {
                return Json(new { Error = e.Message });
            }
        }
    }
}