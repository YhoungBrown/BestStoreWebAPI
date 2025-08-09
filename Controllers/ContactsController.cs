using BestStoreApi.Models;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BestStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly EmailSender emailSender;

        public ContactsController(ApplicationDbContext context, EmailSender emailSender)
        {
            this.context = context;
            this.emailSender = emailSender;
        }


        [HttpGet("subjects")]
        public IActionResult GetSubjects()
        { 
            var subjectList = context.Subjects.ToList();
            return Ok(subjectList);  
        }


        [Authorize(Roles = "admin")]
        [HttpGet]
        public IActionResult GetContacts(int? page)
        {
            if (page == null || page < 1) 
            { 
                page = 1;
            }

            int pagesize = 5;
            int totalPages = 0;

            decimal totalContacts = context.Contacts.Count();
            totalPages = (int)Math.Ceiling(totalContacts / pagesize);

            var contacts = context.Contacts
                .Include(c => c.Subject)
                .OrderBy(c => c.Id)
                .Skip(((int) page - 1) * pagesize)
                .Take(pagesize)
                .ToList();

            var response = new
            {
                Contacts = contacts,
                TotalContacts = totalContacts,
                TotalPages = totalPages,
                CurrentPage = page
            };

            return Ok(response);
        }


        [Authorize(Roles = "asmin")]
        [HttpGet("{id}")]
        public IActionResult GetContact(int id) {
            var contact = context.Contacts.Include(c => c.Subject).FirstOrDefault(c => c.Id == id);
            if (contact == null)
            {
                return NotFound();
            }
            return Ok(contact);
        }

        [HttpPost]
        public IActionResult CreateContact(ContactDto contactDto)
        {
            //useful when we're creating a range of subjects that is acceptable from the users
            var subject = context.Subjects.Find(contactDto.SubjectId);
            if (subject == null)
            { 
                ModelState.AddModelError("Subject", "Invalid subject provided.");
                return BadRequest(ModelState);
            }
            //we're creating a type of contact because we want our object to contain the server added id and created time which are optional parameter in the contact Domain Model
            Contact contact = new Contact() 
            { 
                FirstName = contactDto.FirstName,
                LastName = contactDto.LastName,
                Email = contactDto.Email,
                Phone = contactDto.Phone ?? "",
                Subject = subject,
                Message = contactDto.Message,

            };

            context.Contacts.Add(contact);
            context.SaveChanges();

            //sending confirmation email to the user
            string EmailSubject = "Contact Information";
            string toEmail = contact.Email;
            string MailRecievingUser = contact.FirstName + " " + contact.LastName;
            string EmailMessage = $"Hello {MailRecievingUser},\n\nThank you for reaching out to us. We have received your message and will get back to you shortly.\n\nBest regards,\nYour Message: {contactDto.Message}";

            
            emailSender.SendEmail(EmailSubject, toEmail, MailRecievingUser, EmailMessage).Wait();

            return Ok(contact);
        }


        [HttpPut("{id}")]
        public IActionResult UpdateContact(int id, ContactDto contactDto)
        {
           var ContactToUpdate = context.Contacts.Find(id);

            if (ContactToUpdate == null)
            {
                return NotFound();
            }

            //useful when we're creating a range of subjects that is acceptable from the users
            var subject = context.Subjects.Find(contactDto.SubjectId);
            if (subject == null)
            {
                ModelState.AddModelError("Subject", "Invalid subject provided.");
                return BadRequest(ModelState);
            }


            ContactToUpdate.FirstName = contactDto.FirstName;
            ContactToUpdate.LastName = contactDto.LastName;
            ContactToUpdate.Email = contactDto.Email;
            ContactToUpdate.Phone = contactDto.Phone ?? "";
            ContactToUpdate.Subject = subject;
            ContactToUpdate.Message = contactDto.Message;
            
            
            context.SaveChanges();
            return Ok(ContactToUpdate);
        }


        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public IActionResult DeleteContact(int id)
        {
            //Method 1
            /*
            var contactToDelete = context.Contacts.Find(id);
            if (contactToDelete == null)
            {
                return NotFound();
            }

            context.Contacts.Remove(contactToDelete);
            context.SaveChanges();
            return Ok();*/

            //Method 2
            try
            {
                var contactToDelete = new Contact() { Id = id, Subject = new Subject()};
                context.Contacts.Remove(contactToDelete);
                context.SaveChanges();
            }
            catch (Exception)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
