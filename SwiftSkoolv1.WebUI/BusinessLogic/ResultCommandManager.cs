﻿using SwiftSkoolv1.Domain;
using SwiftSkoolv1.WebUI.Models;
using SwiftSkoolv1.WebUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace SwiftSkoolv1.WebUI.BusinessLogic
{
    public class ResultCommandManager : IDisposable
    {

        private readonly SwiftSkoolDbContext _db;
        private readonly string _studentId;
        private readonly IQueryable<AssignedClass> _mYclassName;
        public readonly string _className;

        private readonly string _termName;
        private readonly string _sessionName;
        private readonly string _schoolId;
        public List<CaList> _caList;
        public List<CaList> _studentCa;

        public ResultCommandManager(List<CaList> studentCa)
        {
            _db = new SwiftSkoolDbContext();
        }
        public ResultCommandManager(string studentId, string termName, string sessionName, string schoolId)
        {

            _schoolId = schoolId;
            _termName = termName.ToUpper().Trim();
            _sessionName = sessionName.Trim();
            _studentId = studentId.Trim();
            _db = new SwiftSkoolDbContext();

            _mYclassName = GetClassName();

            _className = _mYclassName.Select(y => y.ClassName).FirstOrDefault();

            _caList = GetCaLists();
            _studentCa = _caList.Where(x => x.StudentId.ToUpper().Trim().Equals(_studentId)).ToList();

        }


        private List<CaList> GetCaLists()
        {
            return _db.CaLists.AsNoTracking().Where(x => x.SchoolId.ToUpper().Trim().Equals(_schoolId)
                                                //&& x.StudentId.ToUpper().Trim().Equals(_studentId)
                                                && x.ClassName.ToUpper().Trim().Equals(_className)
                                                && x.TermName.ToUpper().Trim().Equals(_termName)
                                                && x.SessionName.ToUpper().Trim().Equals(_sessionName))
                                                .ToList();
        }


        private IQueryable<AssignedClass> GetClassName()
        {
            return _db.AssignedClasses.AsNoTracking().Where(x => x.SchoolId.ToUpper().Trim().Equals(_schoolId)
                                            && x.StudentId.ToUpper().Trim().Equals(_studentId)
                                            && x.TermName.ToUpper().Trim().Equals(_termName)
                                            && x.SessionName.ToUpper().Trim().Equals(_sessionName));

        }


        public async Task<bool> CheckResultAvailability()
        {
            var resultStatus = _db.ResultAvailabilities.Where(x => x.Term.TermName.Equals(_termName)
                                                                   && x.Session.SessionName.Equals(_sessionName))
                                                                .Select(s => s.MakeResultAvailable).FirstOrDefault();

            if (resultStatus)
            {
                var registeredCourse = await SubjectOfferedByStudent();
                var subjectInResult = GetCaLists().Count;
                if (registeredCourse < subjectInResult)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        public async Task<List<SubjectNotAvailable>> GetSubjectNotAvailables()
        {
            var subjectNotAvailableList = new List<SubjectNotAvailable>();
            var subjectName = await NameOfSubjectOfferedByStudent();
            var caList = GetCaLists();
            foreach (var subject in subjectName)
            {
                var casubject = caList.FirstOrDefault(x => x.Subject.SubjectName.Equals(subject));
                if (casubject == null)
                {
                    var list = new SubjectNotAvailable(subject);
                    subjectNotAvailableList.Add(list);
                }
            }
            return subjectNotAvailableList;
        }
        public double TotalScorePerStudent()
        {
            var totalSum = _caList.Where(x => x.StudentId.ToUpper().Trim().Equals(_studentId)).Sum(y => y.Total);
            return Math.Round(totalSum, 2);
        }

        public async Task<int> SubjectOfferedByStudent()
        {
            var subjectAssigned = await _db.AssignSubjects.AsNoTracking().CountAsync(c => c.SchoolId.ToUpper().Trim().Equals(_schoolId)
                                            && c.ClassName.ToUpper().Trim().Equals(_className.ToUpper().Trim())
                                            && c.TermName.ToUpper().Trim().Equals(_termName.ToUpper().Trim()));

            var subjectregistration = await _db.SubjectRegistrations.AsNoTracking()
                                    .CountAsync(x => x.SchoolId.ToUpper().Trim().Equals(_schoolId) &&
                                    x.StudentId.ToUpper().Trim().Equals(_studentId));

            if (subjectregistration <= 8)
            {
                return subjectAssigned;
            }
            return subjectregistration;

        }


        public async Task<List<string>> NameOfSubjectOfferedByStudent()
        {
            var subjectAssigned = await _db.AssignSubjects.AsNoTracking().Where(c => c.SchoolId.ToUpper().Trim().Equals(_schoolId)
                                                        && c.ClassName.ToUpper().Trim().Equals(_className))
                                                        .Select(s => s.Subject.SubjectName).ToListAsync();
            var subjectregistration = await _db.SubjectRegistrations.AsNoTracking().Where(x => x.SchoolId.ToUpper().Trim().Equals(_schoolId)
                                                        && x.StudentId.ToUpper().Trim().Equals(_studentId.ToUpper().Trim()))
                                                        .Select(s => s.Subject.SubjectName).ToListAsync();
            if (subjectregistration.Count > 1)
            {
                return subjectregistration;
            }
            return subjectAssigned;
            //var noOfSubjectPerStudent = _db.AssignSubjects.Count(x => x.ClassName.Equals(className));

        }

        public double TotalScorePerSubject(int subject)
        {
            var sumPerSubject = _caList.Where(x => x.SubjectId.Equals(subject)).Sum(y => y.Total);
            return Math.Round(sumPerSubject, 2);
        }



        public async Task<int> NumberOfStudentPerClass()
        {
            return await _db.AssignedClasses.AsNoTracking().CountAsync(x => x.SchoolId.ToUpper().Trim().Equals(_schoolId)
                                            && x.ClassName.ToUpper().Trim().Equals(_className)
                                            && x.TermName.ToUpper().Trim().Equals(_termName)
                                            && x.SessionName.ToUpper().Trim().Equals(_sessionName));
            //return await _mYclassName.CountAsync();
        }

        public double SubjectHighest(int subjectId)
        {
            return _caList.Where(x => x.SubjectId.Equals(subjectId)).Max(i => i.Total);
        }

        public double SubjectLowest(int subjectId)
        {
            return _caList.Where(x => x.SubjectId.Equals(subjectId)).Min(i => i.Total);
        }

        public int FindSubjectPosition(int subjectId)
        {
            int subjectPosition = 0;
            var mySubjectPosition = _caList.Where(x => x.SubjectId.Equals(subjectId)).ToList();

            // .OrderByDescending(y => y.Total);

            var q = from s in mySubjectPosition
                    orderby s.Total descending
                    select new
                    {
                        Name = s.StudentId,
                        Rank = (from o in mySubjectPosition
                                where o.Total > s.Total
                                select o).Count() + 1
                    };

            foreach (var item in q.Where(s => s.Name.Equals(_studentId)))
            {
                subjectPosition = item.Rank;
            }

            return subjectPosition;
        }


        public int FindAggregatePosition(List<AggregateList> aggregateLists)
        {
            int subjectPosition = 0;

            //.OrderByDescending(y => y.AggretateScore);

            var q = from s in aggregateLists
                    orderby s.Score descending
                    select new
                    {
                        Name = s.StudentId,
                        Rank = (from o in aggregateLists
                                where o.Score > s.Score
                                select o).Count() + 1
                    };

            foreach (var item in q.Where(s => s.Name.Equals(_studentId)))
            {
                subjectPosition = item.Rank;
            }

            return subjectPosition;
        }

        public async Task<double> CalculateAverage()
        {
            double scorePerstudent = TotalScorePerStudent();
            int subjectOffered = await SubjectOfferedByStudent();
            return Math.Round((scorePerstudent / subjectOffered), 2);
        }

        public async Task<double> CalculateClassAverage(int subjectId)
        {
            var scorePerSubject = TotalScorePerSubject(subjectId);
            var studentInClass = await NumberOfStudentPerClass();
            return Math.Round((scorePerSubject / studentInClass), 2);
        }



        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}