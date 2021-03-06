﻿using System.Collections.Generic;
using SwiftSkoolv1.Domain;

namespace SwiftSkoolv1.WebUI.ViewModels
{
    public class SummaryViewModel
    {
        public int NoOfStudentPerClass { get; set; }
        public Student Student { get; set; }
        public List<ContinuousAssessment> ContinuousAssessments { get; set; }
        public List<Result> Results { get; set; }
    }
}