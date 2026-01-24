namespace Olympus.Windows.Training.Tabs;

using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Services.Training;

/// <summary>
/// Quizzes tab: skill quizzes to validate lesson understanding.
/// </summary>
public static class QuizzesTab
{
    // Colors
    private static readonly Vector4 GoodColor = new(0.3f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 WarningColor = new(0.9f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 ErrorColor = new(0.9f, 0.3f, 0.3f, 1.0f);
    private static readonly Vector4 NeutralColor = new(0.7f, 0.7f, 0.7f, 1.0f);
    private static readonly Vector4 InfoColor = new(0.4f, 0.7f, 1.0f, 1.0f);
    private static readonly Vector4 LockedColor = new(0.5f, 0.5f, 0.5f, 1.0f);

    // State
    private static string selectedJob = "whm";
    private static string? activeQuizId;
    private static int currentQuestionIndex;
    private static int[] selectedAnswers = Array.Empty<int>();
    private static bool showResults;
    private static bool reviewMode;

    public static void Draw(ITrainingService trainingService, TrainingConfig config)
    {
        // Job tabs
        if (ImGui.BeginTabBar("QuizJobTabs"))
        {
            if (ImGui.BeginTabItem("WHM"))
            {
                selectedJob = "whm";
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("SCH"))
            {
                selectedJob = "sch";
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("AST"))
            {
                selectedJob = "ast";
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("SGE"))
            {
                selectedJob = "sge";
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("PLD"))
            {
                selectedJob = "pld";
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("WAR"))
            {
                selectedJob = "war";
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("DRK"))
            {
                selectedJob = "drk";
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("GNB"))
            {
                selectedJob = "gnb";
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.Spacing();

        // Get quizzes for selected job
        var quizzes = QuizRegistry.GetQuizzesForJob(selectedJob);
        if (quizzes.Count == 0)
        {
            ImGui.TextColored(NeutralColor, "No quizzes available for this job.");
            return;
        }

        // Two-column layout
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var listWidth = Math.Min(180f, availableWidth * 0.35f);

        // Left panel: Quiz list
        if (ImGui.BeginChild("QuizList", new Vector2(listWidth, -1), true))
        {
            DrawQuizList(quizzes, trainingService, config);
        }

        ImGui.EndChild();

        ImGui.SameLine();

        // Right panel: Quiz content
        if (ImGui.BeginChild("QuizContent", new Vector2(-1, -1), true))
        {
            if (activeQuizId != null)
            {
                var quiz = QuizRegistry.GetQuiz(activeQuizId);
                if (quiz != null)
                {
                    if (showResults)
                    {
                        DrawQuizResults(quiz, trainingService, config);
                    }
                    else if (reviewMode)
                    {
                        DrawQuizReview(quiz);
                    }
                    else
                    {
                        DrawQuizQuestion(quiz);
                    }
                }
            }
            else
            {
                DrawQuizSelection(quizzes, trainingService, config);
            }
        }

        ImGui.EndChild();
    }

    private static void DrawQuizList(
        System.Collections.Generic.IReadOnlyList<QuizDefinition> quizzes,
        ITrainingService trainingService,
        TrainingConfig config)
    {
        ImGui.Text("Quizzes");
        ImGui.Separator();
        ImGui.Spacing();

        // Calculate progress
        var passedCount = quizzes.Count(q => trainingService.IsQuizPassed(q.QuizId));
        var progressFraction = quizzes.Count > 0 ? (float)passedCount / quizzes.Count : 0f;
        ImGui.ProgressBar(progressFraction, new Vector2(-1, 0), $"{passedCount}/{quizzes.Count}");
        ImGui.Spacing();

        foreach (var quiz in quizzes)
        {
            var isPassed = trainingService.IsQuizPassed(quiz.QuizId);
            var bestAttempt = trainingService.GetBestAttempt(quiz.QuizId);
            var lessonComplete = trainingService.IsLessonComplete(quiz.LessonId);
            var isSelected = activeQuizId == quiz.QuizId;

            // Status icon and color
            string statusIcon;
            Vector4 textColor;
            if (isPassed)
            {
                statusIcon = "[P]";
                textColor = GoodColor;
            }
            else if (bestAttempt != null)
            {
                statusIcon = "[X]";
                textColor = ErrorColor;
            }
            else
            {
                statusIcon = "[ ]";
                textColor = NeutralColor;
            }

            // Extract lesson number from quiz ID (e.g., "whm.lesson_1.quiz" -> "1")
            var lessonNum = quiz.LessonId.Split('_').LastOrDefault()?.Split('.').FirstOrDefault() ?? "?";
            var displayText = $"{statusIcon} Lesson {lessonNum}";

            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, InfoColor);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, textColor);
            }

            if (ImGui.Selectable(displayText, isSelected))
            {
                SelectQuiz(quiz);
            }

            ImGui.PopStyleColor();

            // Score tooltip
            if (bestAttempt != null && ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text($"Best: {bestAttempt.Score}/{quiz.Questions.Length}");
                ImGui.Text(isPassed ? "Passed" : "Not Passed");
                ImGui.EndTooltip();
            }
        }
    }

    private static void DrawQuizSelection(
        System.Collections.Generic.IReadOnlyList<QuizDefinition> quizzes,
        ITrainingService trainingService,
        TrainingConfig config)
    {
        ImGui.TextColored(InfoColor, "Select a Quiz");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextWrapped("Complete quizzes to test your understanding of each lesson. Answer 4 out of 5 questions correctly to pass.");
        ImGui.Spacing();

        // Show first unpassed quiz as recommendation
        var firstUnpassed = quizzes.FirstOrDefault(q => !trainingService.IsQuizPassed(q.QuizId));
        if (firstUnpassed != null)
        {
            ImGui.TextColored(WarningColor, "Recommended:");
            ImGui.SameLine();
            if (ImGui.Button($"Start {firstUnpassed.Title}"))
            {
                SelectQuiz(firstUnpassed);
            }
        }
        else
        {
            ImGui.TextColored(GoodColor, "All quizzes passed!");
        }
    }

    private static void DrawQuizQuestion(QuizDefinition quiz)
    {
        var question = quiz.Questions[currentQuestionIndex];

        // Header
        ImGui.TextColored(InfoColor, quiz.Title);
        ImGui.Text($"Question {currentQuestionIndex + 1} of {quiz.Questions.Length}");
        ImGui.Separator();
        ImGui.Spacing();

        // Scenario
        ImGui.TextColored(WarningColor, "SCENARIO:");
        ImGui.TextWrapped(question.Scenario);
        ImGui.Spacing();

        // Question
        ImGui.TextColored(InfoColor, "QUESTION:");
        ImGui.TextWrapped(question.Question);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Options
        var selected = selectedAnswers[currentQuestionIndex];
        for (var i = 0; i < question.Options.Length; i++)
        {
            var isSelected = selected == i;
            if (ImGui.RadioButton($"{(char)('A' + i)}. {question.Options[i]}##{i}", isSelected))
            {
                selectedAnswers[currentQuestionIndex] = i;
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Navigation
        if (currentQuestionIndex > 0)
        {
            if (ImGui.Button("Previous"))
            {
                currentQuestionIndex--;
            }

            ImGui.SameLine();
        }

        if (currentQuestionIndex < quiz.Questions.Length - 1)
        {
            if (ImGui.Button("Next"))
            {
                currentQuestionIndex++;
            }
        }
        else
        {
            // Check if all questions answered
            var allAnswered = selectedAnswers.All(a => a >= 0);

            if (!allAnswered)
            {
                ImGui.BeginDisabled();
            }

            if (ImGui.Button("Submit Quiz"))
            {
                SubmitQuiz(quiz);
            }

            if (!allAnswered)
            {
                ImGui.EndDisabled();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip("Answer all questions before submitting.");
                }
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            activeQuizId = null;
        }

        // Progress indicator
        ImGui.Spacing();
        var answeredCount = selectedAnswers.Count(a => a >= 0);
        ImGui.TextColored(NeutralColor, $"Answered: {answeredCount}/{quiz.Questions.Length}");
    }

    private static void DrawQuizResults(QuizDefinition quiz, ITrainingService trainingService, TrainingConfig config)
    {
        // Calculate score
        var score = 0;
        for (var i = 0; i < quiz.Questions.Length; i++)
        {
            if (selectedAnswers[i] == quiz.Questions[i].CorrectIndex)
            {
                score++;
            }
        }

        var passed = score >= quiz.PassingScore;

        // Header
        ImGui.TextColored(passed ? GoodColor : ErrorColor, passed ? "QUIZ PASSED!" : "QUIZ NOT PASSED");
        ImGui.Separator();
        ImGui.Spacing();

        // Score
        ImGui.Text($"Score: {score}/{quiz.Questions.Length}");
        ImGui.Text($"Required: {quiz.PassingScore}/{quiz.Questions.Length}");
        ImGui.Spacing();

        // Progress bar
        var fraction = (float)score / quiz.Questions.Length;
        ImGui.ProgressBar(fraction, new Vector2(-1, 0), $"{score}/{quiz.Questions.Length}");
        ImGui.Spacing();

        // Question breakdown
        ImGui.TextColored(InfoColor, "Results:");
        ImGui.Separator();
        for (var i = 0; i < quiz.Questions.Length; i++)
        {
            var isCorrect = selectedAnswers[i] == quiz.Questions[i].CorrectIndex;
            var icon = isCorrect ? "[OK]" : "[X]";
            var color = isCorrect ? GoodColor : ErrorColor;
            ImGui.TextColored(color, $"{icon} Q{i + 1}");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Actions
        if (ImGui.Button("Review Answers"))
        {
            showResults = false;
            reviewMode = true;
            currentQuestionIndex = 0;
        }

        ImGui.SameLine();

        if (!passed)
        {
            if (ImGui.Button("Retry Quiz"))
            {
                StartQuiz(quiz);
            }

            ImGui.SameLine();
        }

        if (ImGui.Button("Back to List"))
        {
            activeQuizId = null;
            showResults = false;
            reviewMode = false;
        }

        // Record attempt
        var attempt = new QuizAttempt
        {
            QuizId = quiz.QuizId,
            AttemptedAt = DateTime.Now,
            SelectedAnswers = selectedAnswers.ToArray(),
            Score = score,
            Passed = passed,
        };
        trainingService.RecordQuizAttempt(attempt);
    }

    private static void DrawQuizReview(QuizDefinition quiz)
    {
        var question = quiz.Questions[currentQuestionIndex];
        var userAnswer = selectedAnswers[currentQuestionIndex];
        var isCorrect = userAnswer == question.CorrectIndex;

        // Header
        ImGui.TextColored(InfoColor, "Review: " + quiz.Title);
        ImGui.Text($"Question {currentQuestionIndex + 1} of {quiz.Questions.Length}");
        ImGui.SameLine();
        ImGui.TextColored(isCorrect ? GoodColor : ErrorColor, isCorrect ? "(Correct)" : "(Incorrect)");
        ImGui.Separator();
        ImGui.Spacing();

        // Scenario
        ImGui.TextColored(WarningColor, "SCENARIO:");
        ImGui.TextWrapped(question.Scenario);
        ImGui.Spacing();

        // Question
        ImGui.TextColored(InfoColor, "QUESTION:");
        ImGui.TextWrapped(question.Question);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Options with correct/incorrect indicators
        for (var i = 0; i < question.Options.Length; i++)
        {
            var isUserAnswer = userAnswer == i;
            var isCorrectAnswer = question.CorrectIndex == i;

            Vector4 color;
            string prefix;
            if (isCorrectAnswer)
            {
                color = GoodColor;
                prefix = "[OK]";
            }
            else if (isUserAnswer)
            {
                color = ErrorColor;
                prefix = "[X]";
            }
            else
            {
                color = NeutralColor;
                prefix = "   ";
            }

            ImGui.TextColored(color, $"{prefix} {(char)('A' + i)}. {question.Options[i]}");
        }

        ImGui.Spacing();

        // Explanation
        ImGui.TextColored(InfoColor, "EXPLANATION:");
        ImGui.TextWrapped(question.Explanation);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Navigation
        if (currentQuestionIndex > 0)
        {
            if (ImGui.Button("Previous"))
            {
                currentQuestionIndex--;
            }

            ImGui.SameLine();
        }

        if (currentQuestionIndex < quiz.Questions.Length - 1)
        {
            if (ImGui.Button("Next"))
            {
                currentQuestionIndex++;
            }

            ImGui.SameLine();
        }

        if (ImGui.Button("Back to Results"))
        {
            reviewMode = false;
            showResults = true;
        }

        ImGui.SameLine();

        if (ImGui.Button("Exit Quiz"))
        {
            activeQuizId = null;
            showResults = false;
            reviewMode = false;
        }
    }

    private static void SelectQuiz(QuizDefinition quiz)
    {
        activeQuizId = quiz.QuizId;
        showResults = false;
        reviewMode = false;
        StartQuiz(quiz);
    }

    private static void StartQuiz(QuizDefinition quiz)
    {
        currentQuestionIndex = 0;
        selectedAnswers = new int[quiz.Questions.Length];
        for (var i = 0; i < selectedAnswers.Length; i++)
        {
            selectedAnswers[i] = -1; // -1 = not answered
        }

        showResults = false;
        reviewMode = false;
    }

    private static void SubmitQuiz(QuizDefinition quiz)
    {
        showResults = true;
        reviewMode = false;
    }
}
