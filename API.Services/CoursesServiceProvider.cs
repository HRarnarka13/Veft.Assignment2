﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using API.Models;
using API.Services.Repositories;
using API.Services.Exceptions;

namespace API.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class CoursesServiceProvider
    {

        private readonly AppDataContext _db;

        public CoursesServiceProvider()
        {
            _db = new AppDataContext();
        }

        #region Course only related methods
        public List<CourseDTO> GetCourses()
        {
            var courses = (from course in _db.Courses
                           join courseTemplate in _db.CourseTemplates on course.TemplateID equals courseTemplate.ID
                           select new CourseDTO
                           {
                               ID = course.ID,
                               TemplateID = courseTemplate.TemplateID,
                               Name = courseTemplate.Name,
                               StartDate = course.StartDate,
                               EndDate = course.EndDate
                           });
            return courses.Any() ? courses.ToList() : new List<CourseDTO>();
        }


        /// <summary>
        /// This method gets a single student with the provided id
        /// </summary>
        /// <param name="id">The id of the student</param>
        /// <returns>A course with the provided id</returns>
        public CourseDetailsDTO GetCourseByID(int id)
        {
            var course = (from c in _db.Courses
                          join ct in _db.CourseTemplates on c.TemplateID equals ct.ID
                          where c.ID == id
                          select new CourseDetailsDTO
                          {
                              ID = c.ID,
                              TemplateID = ct.TemplateID,
                              Name = ct.Name,
                              Description = ct.Description,
                              StartDate = c.StartDate,
                              EndDate = c.EndDate
                          }).SingleOrDefault();
            if(course == null)
            {
                throw new CourseNotFoundException();
            }
            return course;
        }
        /// <summary>
        /// This method adds a Course with the information from the CourseViewModel
        /// </summary>
        /// <param name="newCourse">Course containing all the information needed to add a new course</param>
        /// <returns></returns>
        public CourseDetailsDTO AddCourse(CourseViewModel newCourse)
        {
            // Check if the course exists
            var courseTemplate = _db.CourseTemplates.SingleOrDefault(x => x.TemplateID == newCourse.CourseID);
            if (courseTemplate == null)
            {
                throw new TemplateCourseNotFoundException();
            }
            Entities.Course course = new Entities.Course
            {
                ID = _db.Courses.Any() ? _db.Courses.Max(x => x.ID) + 1 : 1,
                TemplateID = courseTemplate.ID,
                Semester = newCourse.Semester,
                StartDate = newCourse.StartDate,
                EndDate = newCourse.EndDate
            };
            _db.Courses.Add(course);

            _db.SaveChanges();

            return new CourseDetailsDTO
            {
                ID = course.ID,
                TemplateID = courseTemplate.TemplateID,
                Name = courseTemplate.Name,
                Description = courseTemplate.Description,
                StartDate = newCourse.StartDate,
                EndDate = newCourse.EndDate
            };
        }
        /// <summary>
        /// Updates a course that already exists
        /// Note that this edits a Course, not a CourseTemplate.
        /// Only start and end date are editable
        /// </summary>
        /// <param name="courseID">The ID of the course to edit</param>
        /// <param name="course">a course with the information to edit</param>
        /// <returns></returns>
        public CourseDetailsDTO UpdateCourse(int courseID, UpdateCourseViewModel course)
        {
            Entities.Course c = _db.Courses.SingleOrDefault(x => x.ID == courseID);
            c.StartDate = course.StartDate;
            c.EndDate = course.EndDate;

            // Check if the course tamplate exists
            var courseTemplate = _db.CourseTemplates.SingleOrDefault(x => x.ID == c.TemplateID);
            if (courseTemplate == null)
            {
                throw new TemplateCourseNotFoundException();
            }

            // If all is successfull, we save our changes
            _db.SaveChanges();

            return new CourseDetailsDTO
            {
                ID = c.ID,
                TemplateID = courseTemplate.TemplateID,
                Name = courseTemplate.Name,
                Description = courseTemplate.Description,
                StartDate = course.StartDate,
                EndDate = course.EndDate,
                StudentCount = _db.StudentEnrollment.Count(x => x.CourseID == courseID)
            };
        }
        /// <summary>
        /// Deletes a course
        /// Note that this is a course not a course template.
        /// </summary>
        /// <param name="id">The ID of the course to delete</param>
        public void DeleteCourse(int id)
        {
            Entities.Course course = _db.Courses.SingleOrDefault(x => x.ID == id);
            if (course == null)
            {
                throw new CourseNotFoundException();
            }
            _db.Courses.Remove(course);
            _db.SaveChanges();
        }

        /// <summary>
        /// This method gets all the courses that are taught during the given semster.
        /// If no semester is given, then the default is the current semester.
        /// </summary>
        /// <param name="semester">The semester for the filter</param>
        /// <returns>A list of courses</returns>
        public List<CourseDTO> GetCoursesBySemester(string semester = null)
        {
            if (String.IsNullOrWhiteSpace(semester))
            {
                semester = "20153";
            }

            var courses = (from c in _db.Courses
                           join ct in _db.CourseTemplates on c.TemplateID equals ct.ID
                           where c.Semester == semester
                           select new CourseDTO
                           {
                               ID = c.ID,
                               TemplateID = ct.TemplateID,
                               Name = ct.Name,
                               StartDate = c.StartDate,
                               EndDate = c.EndDate
                           }).ToList();

            return courses;
        }
        #endregion
        #region Course and Student related functions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="courseID"></param>
        /// <returns></returns>
        public List<StudentDTO> GetStudentInCourse(int courseID)
        {
            // Check if the course exists
            if (_db.Courses.SingleOrDefault(x => x.ID == courseID) == null)
            {
                throw new CourseNotFoundException();
            }

            // Get the students in the course
            List<StudentDTO> students = (from se in _db.StudentEnrollment
                    join s in _db.Students on se.StudentID equals s.ID
                    where se.CourseID == courseID
                    select new StudentDTO
                    {
                        SSN = s.SSN,
                        Name = s.Name
                    }).ToList();
            return students;
        }

        public CourseDetailsDTO AddStudentToCourse(int courseID, StudentViewModel newStudent)
        {
            // Check if the course exists
            var course = _db.Courses.SingleOrDefault(x => x.ID == courseID);
            var courseDetails = _db.CourseTemplates.SingleOrDefault(x => x.ID == course.TemplateID);
            if (course == null)
            {
                // Todo: throw error
            }

            // Check if the student exists
            var student = _db.Students.SingleOrDefault(x => x.SSN == newStudent.SSN);
            if (student == null)
            {
                // todo : throw error
            }

            _db.StudentEnrollment.Add(new Entities.StudentEnrollment
            {
                StudentID = student.ID,
                CourseID = course.ID
            });

            _db.SaveChanges();

            return new CourseDetailsDTO
            {
                ID = course.ID,
                TemplateID = courseDetails.TemplateID,
                Name = courseDetails.Name,
                Description = courseDetails.Description,
                StartDate = course.StartDate,
                EndDate = course.EndDate
            };
        }
        #endregion

    }
}
