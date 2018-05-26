﻿using Assignment2.Database;
using Assignment2.Database.SmallDbForQuestionHistory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Assignment2.Bussiness
{
    class AnswerToQuestion
    {
        public string answer;
        public string question;
    }

    public partial class QuestionManager
    {
        // Column names in weather table for searching.
        const string dayColumn = "day";
        const string weatherColumn = "weather";
        const string outfitColumn = "outfit";
        const string temperatureColumn = "temperature";

        public string TryGetAnswer(string question)
        {
            // Check simple rules first
            // Clean the spaces of the question.
            string lowerUserQuestion = question.ToLower().Trim();
            string cleanUserQuestion = Regex.Replace(lowerUserQuestion, @"[^a-zA-Z0-9]", string.Empty, RegexOptions.Compiled);


            using (Context context = new Context())
            {

                // Check data rules next. It is more complicated.
                // Clean the raw input first.
                lowerUserQuestion = Regex.Replace(question.ToLower().Trim(), @"\s+", " ", RegexOptions.Compiled);

                // Store a row of the weather table.
                WeatherInfo rowReturned;

                // Loop through the data-driven rule questions to check if the input question match any data-driven rule.
                foreach (var rule in context.DataDrivenRules)
                {
                    // Clean the question in the database.
                    string lowerDbQuestion = Regex.Replace(rule.Question.ToLower().Trim(), @"\s+", " ", RegexOptions.Compiled);

                    // If a match is found, get the correspoinding answer. 
                    if (IsThisQuestion(lowerUserQuestion, lowerDbQuestion, rule.QuestionColumn.ToLower(), out rowReturned))
                    {
                        return GetAnswer(rule.Answer, rule.AnswerColumn.ToLower(), rowReturned);
                    }
                }


                var answers = context.FixedRules
                    .Where(rule => rule.CurrentStatus == "Approved").ToList()
                    .Select(
                    rule => new AnswerToQuestion
                    {
                        question = Regex.Replace(rule.Question.ToLower().Trim(), @"[^a-zA-Z0-9]", string.Empty, RegexOptions.Compiled),
                        answer = rule.Answer
                    });

                var simpleAnswer = answers.Where(rule => rule.question == cleanUserQuestion).Select(rule => rule.answer);

                if (simpleAnswer.Count() > 0)
                {
                    return simpleAnswer.First();
                }
                else
                {
                    return null;
                }
            }

        }







        /// <summary>
        /// Check if the input question matches the data-driven question in database.
        /// </summary>
        /// <param name="askedQuestion"> The question given by user. </param>
        /// <param name="dbQuestion"> The question in the database. </param>
        /// <param name="questionColumn"> The column related to the question as the condition to search weather table. </param>
        /// <param name="theRow"> The row returned if there is a match. </param>
        /// <returns></returns>
        private bool IsThisQuestion(string askedQuestion, string dbQuestion, string questionColumn, out WeatherInfo theRow)
        {
            theRow = null;
            questionColumn = questionColumn.ToLower();

            // See if questions match. Split the database question into 2 parts. 
            string[] cutQuestion = dbQuestion.Split('*');

            // Return false if the input question cannot match the database question. 
            if (!askedQuestion.Contains(cutQuestion[0]) && !askedQuestion.Contains(cutQuestion[1])) return false;
            if (askedQuestion.IndexOf(cutQuestion[0]) < 0 || askedQuestion.IndexOf(cutQuestion[1]) < 0) return false;
            if (askedQuestion.IndexOf(cutQuestion[0]) + cutQuestion[0].Length + 1 > askedQuestion.IndexOf(cutQuestion[1])
                && cutQuestion[1].Length > 0) return false;

            // Extract colum value from the input question. 
            string columValue = askedQuestion.Substring(askedQuestion.IndexOf(cutQuestion[0]) + cutQuestion[0].Length,
                askedQuestion.IndexOf(cutQuestion[1]) - (askedQuestion.IndexOf(cutQuestion[0]) + cutQuestion[0].Length));
            if (columValue.Trim() == "") return false;

            // Return the answer value.
            int intValue;
            if (questionColumn == dayColumn) theRow = SearchByDay(columValue);
            else if (questionColumn == weatherColumn) theRow = SearchByWeather(columValue);
            else if (questionColumn == outfitColumn) theRow = SearchByOutfit(columValue);
            // Temperature must be a number. If the input is not a number, return false. 
            else if (questionColumn == temperatureColumn && Int32.TryParse(columValue, out intValue)) theRow = SearchByTemperature(intValue);
            else if (questionColumn == temperatureColumn && !Int32.TryParse(columValue, out intValue))
                return false;

            if (theRow == null) return false;
            else return true;
        }

        /// <summary>
        /// Get answer by assembling the row and column field value. 
        /// </summary>
        /// <param name="answerFormat"> The answer string with * symbol to be replaced. </param>
        /// <param name="answerColumn"> The column name to return corresponding value to replace the * symbol in the answer string. </param>
        /// <param name="theRow"> The row to extract the column value. </param>
        /// <returns> The final answer without * symbol. </returns>
        private string GetAnswer(string answerFormat, string answerColumn, WeatherInfo theRow)
        {
            string result = "";
            answerColumn = answerColumn.ToLower();
            if (answerColumn == dayColumn) result = theRow.Day;
            else if (answerColumn == weatherColumn) result = theRow.Weather;
            else if (answerColumn == outfitColumn) result = theRow.Outfit;
            else if (answerColumn == temperatureColumn) result = theRow.Temperature.ToString();

            return answerFormat.Replace("*", result);
        }


    }

}
